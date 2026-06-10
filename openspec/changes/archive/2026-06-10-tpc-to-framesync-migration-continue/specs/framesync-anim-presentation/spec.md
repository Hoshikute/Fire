# framesync-anim-presentation

表现层动画驱动——`PlayerAnimViewSystem` 只读逻辑层 `PlayerStateComponent` 状态枚举，驱动 Animancer 播放对应动画。动画层完全降为纯表现，不回写逻辑。

## ADDED Requirements

### Requirement: PlayerAnimViewSystem 动画驱动
系统 MUST 创建 `PlayerAnimViewSystem`（继承 `ViewSystemBase`），过滤 `PlayerStateComponent` 和 `PlayerViewComponent`。在渲染帧 `Update` 中，读 `PlayerStateComponent.state` 枚举，查询 `PlayerViewComponent.animConfig` 获取对应 `ClipTransition`，调用 `PlayerViewComponent.animancer.Play(clip)` 播放。系统 MUST 已在 `PlayerWorld.GetSystemTypes()` 中注册。

#### Scenario: 状态枚举驱动动画切换
- **WHEN** `PlayerStateComponent.state` 从 `Idle` 变为 `MoveLoop`
- **THEN** `PlayerAnimViewSystem` 调用 `animancer.Play(moveLoopClip)`，角色动画从 Idle 切换到 MoveLoop

#### Scenario: 同一状态不重复播放
- **WHEN** `PlayerStateComponent.state` 连续两帧均为 `MoveLoop`
- **THEN** `PlayerAnimViewSystem` 仅在首次进入时调用 `animancer.Play()`，后续帧不重复触发

### Requirement: PlayerAnimConfig 动画配置 SO
系统 MUST 定义 `PlayerAnimConfig` ScriptableObject，包含所有 `PlayerLogicState` 对应的 `TransitionAsset` 引用。`PlayerFrameSyncEntry` SHALL 持有 `_animConfig` 序列化字段并注入到 `PlayerViewComponent.animConfig`。

#### Scenario: 动画配置注入
- **WHEN** Unity 场景中 `PlayerFrameSyncEntry` 的 `_animConfig` 字段已赋 SO 资源
- **THEN** `SpawnPlayer` 将 `_animConfig` 写入 `PlayerViewComponent.animConfig`，`PlayerAnimViewSystem` 可正常查询

### Requirement: 动画层不回写逻辑
`PlayerAnimViewSystem` SHALL NOT 修改任何逻辑层组件（`PlayerMoveComponent`、`PlayerStateComponent`、`PlayerInputComponent` 等）。Animancer 的 `AnimancerState.Events` 回调（OnEnd 等）SHALL NOT 中包含任何逻辑状态变更逻辑。动画仅用于视觉表现。

#### Scenario: 动画回调不触发状态切换
- **WHEN** `PlayerAnimViewSystem` 播放的动画触发 `OnEnd` 回调
- **THEN** 回调中无任何对逻辑层组件的修改或状态切换调用
