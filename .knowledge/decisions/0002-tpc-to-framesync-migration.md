# ADR 0002: 删除 ThirdPersonController，迁移到 TEngine + FrameSync

状态：**进行中**（核查完成，决策 1 已定为 B）
作者：HuHu
日期：2026-06

## 背景与目标

- **工具类** → 用 TEngine 规范重新开发
- **角色移动** → 用 FrameSync（帧同步 / 确定性定点）规范开发
- **最终目标** → 完全删除 `ThirdPersonController` 模块

老 TPC 的移动是 **Root Motion（动画根运动）驱动**，与帧同步「确定性定点位移、可回滚」根本对立：

| 老 TPC 做法 | 帧同步要求 |
|------------|-----------|
| 位移来自 `Animator.deltaPosition` → `CharacterController.Move` | 确定性定点位移（逻辑帧算位移，动画只表现） |
| 接地 `Physics.CheckSphere`、斜坡 `Physics.Raycast`、重力 `Time.deltaTime` | 确定性物理检测算法（定点数，可回滚） |
| 攀爬/翻越位移精确绑定动画曲线（`PlayerReusableLogic` 371 行操作 Animancer） | 逻辑与表现彻底分离 |

真迁移 = **放弃 Root Motion、动画降为纯表现、物理检测改确定性算法**。

## 依赖核查结论（已验证）

### 模块规模
`Assets/GameScripts/HotFix/GameLogic/Player/Controller/` 共 **44 个 .cs 文件**，全部 `namespace ThirdPersonController`。

### 对外引用（爆炸半径）
真正跨模块的硬依赖链：
```
TPBattleContext.cs ──①──> GameModule.Character ──②──> Player.prefab ──③──> Player.cs(TPC)
  (Battle上下文)        加载/销毁API层(TPC专属)    预制体挂载脚本      模块本体(44文件)
```

| 依赖点 | 位置 | 性质 |
|--------|------|------|
| ① 业务入口 | `Context/TPBattleContext.cs:64-70`（`Character.SetThirdPersonPlayerPrefab` + `LoadThirdPersonPlayerAsync`）、`:88`（`GetComponent<Player>()` 诊断） | 唯一业务调用 |
| ② 中间层 | `Module/Character/`（`ICharacterModule` + `CharacterModule`），所有成员/方法名都是 `ThirdPersonPlayer*` | **仅被 TPBattleContext 使用**（除 GameModule.cs:97 门面注册外无其他调用方） |
| ③ 资源 | `Assets/AssetRaw/Actor/Player.prefab` 挂载了 `Player.cs`（GUID `45522ce2a543d7042a8689219d852219`） | 删脚本→Missing Script |
| ④ 模块本体 | `Player/Controller/` 44 文件 | 模块内部自引用 |
| ⑤ 过时注释 | `Module/FrameSync/ClientLogic/PlayerFrameSyncEntry.cs:18`（"老 TPC 仍可独立存在"） | 顺手清理 |

> 用户原述「对外只有 1 处引用」指 `GetComponent<Player>()` 类型引用——准确。但真正的耦合在 `Character` 模块（TPC 专属加载器）。

### FrameSync 角色现状
`PlayerFrameSyncEntry`（85 行）**完全自给自足**：`GameModule.FrameSync.CreateWorld<PlayerWorld>()` + 代码 `new PlayerMoveComponent` 生成 ECS 实体，表现 Transform 靠 Inspector 手拖 `_viewRoot`。**没走 Character 模块，目前是"挂空物体手拖"的最小 demo，缺动态加载 prefab 能力。**

## P0 工具类盘点（4 个，已读全文）

| 文件 | 内容 | 调用点 | TEngine 对应 / 迁移方向 |
|------|------|--------|------------------------|
| `Tool/Singleton/MonoSingleton.cs` | MonoBehaviour 泛型单例 + DontDestroyOnLoad | （需查具体引用） | TEngine 推 Module 注册；如确需，挪到通用 Utility 并去 TPC namespace |
| `Tool/Singleton/NoMonoSingleton.cs` | 纯 C# 泛型单例 | （需查具体引用） | 同上 |
| `Tool/BindableProperty/BindableProperty.cs` | 值变更回调 `Action<T>`（27 行） | `CharacterBase`、`PlayerReusableData`、`PlayerLedgeClimbState` 等 | 可保留为通用工具，或改用 TEngine `GameEvent`；注意帧同步逻辑层**不能**用（含闭包/事件，非确定性） |
| `Tool/ToolFunction/ToolFunction.cs` | UI 颜色设置 + `GetDeltaAngle`/`GetJumpInitVelocity`（116 行） | `PlayerMovementFsmState`、多个 State | 角度/跳跃速度计算是纯数学，迁移到确定性定点版本供 FrameSync 用；UI 颜色函数挪到通用 UI Utility |

TEngine 现有可复用设施：`GameEvent`（事件）、Module 体系（替代运行时单例）、`EditorScriptableSingleton`（编辑器单例）、`Utility.*`（Json/Material/Tween 等扩展）。

## 决策（已定）

### 决策 1：`GameModule.Character` 模块 → **B（已选定）** ✅
保留 Character 模块，去 TPC 化为通用 `LoadCharacterAsync(location)`，FrameSync 复用它加载角色 prefab。
理由：符合"复用现有接口、遵循 TEngine 模块化"准则；Character 的加载/缓存/销毁框架本身通用，仅 API 命名被 `ThirdPersonPlayer` 污染。

### 决策 2：阶段范围
| 阶段 | 内容 | 风险 | 状态 |
|------|------|------|------|
| P0 | 工具类迁移（4 个 → TEngine 规范 / 通用 Utility，去 TPC namespace） | 低 | 待开始 |
| P1 | Character 模块去 TPC 化（B）：API 改通用 `LoadCharacterAsync` | 中 | 待开始 |
| P2 | FrameSync 角色移动补全（确定性定点位移、接地/斜坡/重力确定性算法、攀爬翻越） | **高，核心难点** | 待开始 |
| P3 | 动画降为纯表现（Animancer 只读逻辑帧状态播动画，不驱动位移） | 高 | 待开始 |
| P4 | `TPBattleContext` 切到 FrameSync 角色 | 中 | 待开始 |
| P5 | 删除 `Player/Controller/` 44 文件 + Player.prefab 解绑 + 清理 ⑤ 注释 | 中 | 待开始 |

顺序：P0 → P1 →（P2+P3 核心）→ P4 → P5。

## 风险与原则
- 帧同步逻辑层禁止任何非确定性：浮点 `Time.deltaTime`、`Physics.*`、随机数、事件闭包顺序依赖 —— 全部改定点数 + 确定性算法。
- 动画（Animancer）只允许**读**逻辑帧状态来播放，绝不反向驱动逻辑位移。
- diff 收窄：每阶段独立可编译验证；不碰工作区已有的无关脏改动。
