## Context

当前 `Player.Awake()` 在创建 FSM 后调用 `PreloadAllAnimations()`，该方法从 `playerSO.playerMovementData` 收集全部 `ITransition`，再通过 `Animancer.States.GetOrCreate(t)` 逐个创建 Animancer state。为了支撑这条链路，多个 `Player*Data` 类实现了 `IAnimationProvider` 和 `CollectTransitions()`，并新增 `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/Player/Data/IAnimationProvider.cs`。

这套预加载逻辑原本用于规避首次播放动画时的卡顿，但它把所有玩家状态动画都拉进初始化阶段，扩大了 `Awake()` 的职责，也引入了大量和业务状态无关的遍历代码与 `[Claude][Preload]`、`[Claude][Heartbeat]` 等临时诊断日志。当前 `MoveStart` 卡顿的主要修复方向已经转向状态衔接和接地检测配置，因此预加载链路需要单独回退。

## Goals / Non-Goals

**Goals:**

- 移除玩家初始化阶段的全量动画预加载行为。
- 删除仅为预加载服务的 `IAnimationProvider` / `CollectTransitions()` 结构。
- 清理预加载链路附带的临时日志和每 30 帧 heartbeat 诊断。
- 保持原有状态动画播放路径：各状态继续在需要时调用 `animancer.Play(...)`。
- 保留 `fix-player-move-start-stall` 中和状态切换、下落兜底、`whatIsGround` 配置相关的修复。

**Non-Goals:**

- 不重新设计 Animancer 资源管理系统。
- 不改 `PlayerSO` 的 serialized 字段结构和动画资源引用。
- 不修改 `MoveStart` 到 `MoveLoop` 的提前衔接阈值。
- 不回退 `Player.prefab` 的接地 LayerMask 修复。
- 不新增另一套异步预加载、缓存或 Addressables 管线。

## Decisions

### 决策一：直接删除 eager preload，而不是替换成新的预热机制

本变更的目标是“回退预加载动画的更改”，因此实现应恢复为状态驱动的按需播放路径。`Player.Awake()` 创建 FSM 后直接启动 `PlayerIdleState`，不再主动遍历所有 transition。

替代方案：

- 改成延迟到首帧后分批预热。这个方案仍然保留预加载系统，超出“回退”的范围。
- 只预热 `MoveStart_*`。这会继续把卡顿修复绑定到预加载假设上，而当前卡顿已由状态衔接和接地检测方向处理。
- 完整删除 eager preload。该方案范围最小，最符合回退诉求。

### 决策二：删除 `IAnimationProvider` 和各数据类遍历方法

`IAnimationProvider` 的唯一职责是支撑 `PreloadAllAnimations()` 收集 transitions。回退预加载后，这个接口和各 `CollectTransitions()` 实现不再有业务价值，应一起删除，避免数据类承担“全局枚举动画资源”的额外职责。

替代方案：

- 保留接口但不调用。这样会留下无用 API 和误导性的维护入口。
- 保留接口用于未来功能。未来如果需要正式预加载，应重新提出设计，而不是保留当前临时链路。

### 决策三：清理预加载诊断日志，保留无关诊断边界

`[Claude][Preload]`、`[Claude][Heartbeat]`、`[Claude][Player][Update] dt spike` 是为定位预加载/卡顿加入的临时运行时日志。回退预加载时应删除这些日志和相关字段，避免正常运行时持续刷日志。

本变更不主动清理其他与相机绑定、状态进入离开、接地检测相关的诊断日志；它们属于其他问题域或当前正在验证的移动状态修复。

## Risks / Trade-offs

- [Risk] 删除预加载后，首次播放某些动画仍可能出现资源加载抖动。→ Mitigation：本变更先回退临时预加载链路；若仍存在可复现首次播放卡顿，应另开变更设计正式、可验证的加载策略。
- [Risk] 删除 `CollectTransitions()` 时误删 serialized 动画字段。→ Mitigation：只移除接口、using 和方法，不改现有 `[SerializeField]` 字段。
- [Risk] 回退范围误伤移动状态修复。→ Mitigation：任务中明确保留 `PlayerMoveStartState`、`PlayerMovementFsmState`、`Player.prefab whatIsGround` 的修复，不做宽泛回滚。
- [Risk] 运行时验证仍需 Unity 场景确认。→ Mitigation：OpenSpec 完成实现后仍要求运行 `Game.unity`，确认无 `[Preload]` 日志且基础动画状态可播放。
