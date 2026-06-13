## Why

最新 Unity Console 运行日志显示，FrameSync 玩家在奔跑左右转时会出现可见侧倾；地面状态仍正常进入 `MoveStart`、`MoveLoop`、`MoveEnd`、`Idle`，且 `PlayerViewSystem` 只用 `Quaternion.LookRotation(face, Vector3.up)` 做水平朝向，因此问题更像是 Animancer locomotion mixer 的 `RotationValue` 驱动语义不匹配，而不是确定性位移或 Transform 直接产生了 roll。

`fix-player-locomotion-mixer-params` 已恢复地面 locomotion mixer 的基本参数和资源层级，但运行日志进一步暴露出 `MoveLoop` 中 `RotationValue` 会在 `-1.81` 到 `2.65` 间变化，足以把跑步循环推到旧资源的左右跑/斜跑分支。需要把奔跑转向时的方向参数从“直接角度差”修正为符合 FrameSync 代码驱动位移的表现层语义。

## What Changes

- 调整 FrameSync 玩家 `MoveLoop`/地面 locomotion 的 `RotationValue` 来源、范围和平滑策略，避免奔跑左右转时长期选中带明显侧倾的左右跑/斜跑动画分支。
- 保留 `MoveStart` 的起步方向选择能力，但明确 `MoveLoop` 持续移动阶段不应因为逻辑朝向滞后而被误判为横向跑。
- 增加一次性或变更触发的诊断，能够对比“运行时 `RotationValue`”“资源阈值”“当前状态/速度档位”，避免每帧刷屏。
- 保持 FrameSync 确定性边界：动画参数平滑、Animancer mixer 权重和视觉验证均停留在表现层，不写入 `PlayerMoveComponent`、`PlayerStateComponent` 等回滚快照。
- 不重新启用 root motion 驱动位移，不回退到旧 `ThirdPersonController` 链路。

## Capabilities

### New Capabilities

- `framesync-player-turn-locomotion-mixer`: 定义 FrameSync 玩家奔跑转向时 locomotion mixer 的 `RotationValue` 语义、范围控制、表现层平滑和验证要求。

### Modified Capabilities

## Impact

- 表现层代码：`UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/FrameSync/ClientLogic/System/PlayerAnimViewSystem.cs`。
- 参考表现层代码：`UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/FrameSync/ClientLogic/System/PlayerViewSystem.cs`。
- 参考资源资产：`UnityProject/Assets/AssetRaw/ThirdPersonController/AnimancerController/Resources/Config/PlayerAnimacer/NoneLock/ChildMixer/MoveMoveLoop.asset`、`MoveRunLoop.asset`、`MoveWalkLoop.asset`。
- 参考旧 TPC 代码：`PlayerMovementFsmState.UpdateRotation`、`PlayerReusableData.rotationValueParameter` 的平滑参数语义。
- 验证路径：Unity EditorSimulateMode 的 Game 场景中验证走/跑直行、奔跑左转、奔跑右转、停止回 Idle，并确认 Console 没有 locomotion 参数或配置缺失 warning。
