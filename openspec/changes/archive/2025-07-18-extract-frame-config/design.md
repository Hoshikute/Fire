## Context

当前 `FrameSyncModule` 在字段初始化时硬编码 `m_intervalTime = 200`（毫秒），表示每个逻辑帧间隔 200ms。`PlayerStateSystem` 中的帧计数阈值（`MoveStartFrames=1`、`MoveEndFrames=1`、`LandBufferFrames=1`）和 `PlayerViewSystem` 中的插值时长（`LogicFrameDuration=0.2f`）都隐式依赖这个 200ms 假设。

需要一个集中的配置入口，让帧间隔可配置，同时让依赖它的阈值自动跟随调整。

## Goals / Non-Goals

**Goals:**
- 提供 `FrameConfig` 静态配置类，集中定义逻辑帧间隔
- `FrameSyncModule` 从 `FrameConfig` 读取间隔而非硬编码
- 帧计数阈值（MoveStartFrames 等）改为基于 `FrameConfig` 计算，自动适配帧率变化
- `PlayerViewSystem` 的插值时长从 `FrameConfig` 读取

**Non-Goals:**
- 不提供运行时热切换帧率（需要重启 World）
- 不涉及网络同步频率的配置
- 不改变帧同步核心架构（累积-追赶模型不变）

## Decisions

### 1. 使用静态类而非 ScriptableObject

**选择**: `public static class FrameConfig` 带 `const` 默认值

**理由**:
- 逻辑帧间隔是底层确定性常量，不随场景/角色切换
- `const` 可被 JIT 内联，零开销
- 避免 SO 加载时机问题（`FrameSyncModule.OnInit()` 可能在资源系统就绪前调用）
- 如需持久化配置，后续可改为从 JSON 文件读取

**未选**: ScriptableObject — 加载时序不确定，且热更新工程中 SO 资源路径管理复杂

### 2. 帧计数阈值改为计算公式

**选择**: `PlayerStateSystem` 中帧阈值改为 `ms / FrameConfig.LogicFrameIntervalMs` 形式

```
// 之前
private const int MoveStartFrames = 1; // 假设 200ms

// 之后
private const int MoveStartDurationMs = 200; // 意图：起步持续 200ms
// 实际帧数 = MoveStartDurationMs / FrameConfig.LogicFrameIntervalMs（在 Step 中计算）
```

**理由**: 阈值本意是"持续多少毫秒"，帧数只是表示方式。分离意图和换算避免修改帧率时遗漏。

### 3. FrameConfig 放在 `GameLogic.Module` 命名空间

**选择**: `D:\UGitD\Fire\UnityProject\Assets\GameScripts\HotFix\GameLogic\Module\FrameSync\FrameConfig.cs`

**理由**: 与 `FrameSyncModule` 同目录，方便查找；所有帧同步相关代码都引用它。

### 4. FrameId 全局帧计数器

**选择**: `FrameConfig` 提供 `public static long FrameId`，由 `FrameSyncModule.FixedUpdateWorlds` 每次调用时 `++`

**理由**:
- 提供无需 World 引用的全局帧号，方便日志/诊断
- `WorldBase.FrameCount` 是 per-world 的，`FrameConfig.FrameId` 是全局的，互补不冲突
- `FrameSyncModule` 本身就是固定帧更新的唯一入口，在此递增最自然
- 使用 `long` 避免溢出

## Risks / Trade-offs

- [Risk] 改帧率后动画状态持续时长偏移 → 已在 PlayerStateSystem 用毫秒意图常量解耦
- [Risk] 非 200ms 整倍数可能导致帧阈值取整偏差 → 使用整数除法取整，偏差 < 1 帧，可接受

## Open Questions

- 是否需要支持 JSON 配置文件覆盖默认值？当前先保持 `const`，后续按需扩展
