## Why

当前玩家初始化阶段加入了统一动画预加载链路：`Player.Awake()` 会收集 `PlayerSO` 下所有移动状态动画 transition，并通过 `Animancer.States.GetOrCreate()` 逐个预热。该改动引入了大量临时接口、数据类遍历方法和运行时诊断日志，但它本身不是修复 `MoveStart` 卡顿的稳定解法，也会扩大玩家初始化复杂度与首帧行为风险。

现在需要把这组预加载动画改动回退，让玩家动画加载重新回到按状态播放时自然创建 Animancer state 的路径，同时保留后续已确认必要的移动状态修复和 `Game.unity` 接地配置修复。

## What Changes

- 移除 `Player.Awake()` 中统一预热所有动画 transition 的调用和 `PreloadAllAnimations()` 实现。
- 移除为预加载服务新增的 `IAnimationProvider` 接口与各 `Player*Data.CollectTransitions()` 实现。
- 移除预加载链路附带的 `[Claude][Preload]`、`[Claude][Heartbeat]`、`dt spike` 等临时运行时诊断日志。
- 恢复玩家动画数据类只承载状态所需 serialized animation references 的职责，不承担全量遍历/预热职责。
- 不回退 `PlayerMoveStartState` 的提前衔接、非接地下落兜底、`Player.prefab` 的 `whatIsGround` 配置修复，以及 Animancer `OnEnd` 延迟切换相关修复。

## Capabilities

### New Capabilities
- `player-animation-loading`: 约束玩家动画加载策略，确保玩家初始化不执行全量动画预加载，动画 state 仍按实际状态播放路径按需创建。

### Modified Capabilities
- 无。

## Impact

- 影响代码区域：
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/Player/Player.cs`
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/Player/Data/IAnimationProvider.cs`
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/Player/Data/PlayerStateDataSO/PlayerMovementData.cs`
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/Player/Data/PlayerStateDataSO/PlayerLockMovementData.cs`
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/Player/Data/PlayerStateDataSO/PlayerMovementData/*.cs`
- 预期行为：
  - 玩家初始化不再输出预加载相关日志，不再在 `Awake()` 中遍历和创建所有 Animancer states。
  - 玩家进入各移动/跳跃/攀爬状态时，仍按对应状态原有 `animancer.Play(...)` 路径播放动画。
  - `MoveStart` 到 `MoveLoop` 的提前衔接和落地检测修复不受影响。
- 验证方式：
  - 代码搜索确认 `PreloadAllAnimations`、`IAnimationProvider`、`CollectTransitions` 不再残留。
  - Unity 运行 `Game.unity`，确认初始化阶段没有 `[Preload]` 日志，玩家仍能进入 Idle、MoveStart、MoveLoop、Fall/Land 等状态。
