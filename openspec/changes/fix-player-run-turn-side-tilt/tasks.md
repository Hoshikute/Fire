## 1. 证据复核与边界确认

- [ ] 1.1 复核最新 Unity Console 日志，确认侧倾发生时地面状态仍正常进入 `MoveStart`、`MoveLoop`、`MoveEnd`、`Idle`，并记录 `MoveLoop` 的 `RotationValue` 范围。
- [ ] 1.2 复核 `PlayerViewSystem` 的 Transform 朝向逻辑，确认它只做 yaw 旋转且不会主动写入 roll。
- [ ] 1.3 复核 `MoveMoveLoop.asset`、`MoveRunLoop.asset`、`MoveWalkLoop.asset` 的 `SpeedValue`/`RotationValue` 参数名与阈值。
- [ ] 1.4 对照旧 `ThirdPersonController` 的 `PlayerMovementFsmState.UpdateRotation` 和 `PlayerReusableData.rotationValueParameter`，记录旧路径的平滑与转向补偿语义。

## 2. RotationValue 对照验证

- [ ] 2.1 在本地临时验证路径中将非锁定 `MoveLoop` 的 `RotationValue` 固定为 `0` 或等价归零策略。
- [ ] 2.2 在 Unity EditorSimulateMode 中验证奔跑左转和奔跑右转，记录侧倾是否消失。
- [ ] 2.3 若归零后侧倾仍存在，暂停正式实现并转为检查跑步正向 clip 或模型骨骼姿态，不继续扩大代码改动。
- [ ] 2.4 若归零后侧倾消失，确认正式实现应集中在 `PlayerAnimViewSystem` 的表现层 `RotationValue` 策略。

## 3. 表现层参数修正

- [ ] 3.1 在 `PlayerAnimViewSystem` 中区分 `MoveStart` 起步方向选择和 `MoveLoop` 持续奔跑方向参数，避免二者共用不合适的原始差角语义。
- [ ] 3.2 为非锁定 `MoveLoop` 实现 `RotationValue` 限幅、归零或表现层平滑策略，使奔跑转向不长期越过跑步左右阈值。
- [ ] 3.3 如需缓存平滑值，将缓存放在 `PlayerViewComponent` 或 `PlayerAnimViewSystem` 私有表现层状态中，不写入回滚组件。
- [ ] 3.4 保留或调整 `Player locomotion mixer params` 诊断，使参数桶变化时可看到 `state`、`SpeedValue`、`RotationValue`、`speedGear`，且不每帧刷屏。
- [ ] 3.5 确认本变更不改 `PlayerMoveSystem.RotateTowards`、固定逻辑帧步长、碰撞、重力或 root motion 位移边界。

## 4. 静态验证

- [ ] 4.1 运行当前工作区可用的 GameLogic C# 构建检查；如果无法运行，记录具体原因。
- [ ] 4.2 运行 `openspec validate fix-player-run-turn-side-tilt --strict`。
- [ ] 4.3 检查 `PlayerMoveComponent.DeepCopy()` 与 `PlayerStateComponent.DeepCopy()`，确认没有新增 Animancer 参数、TransitionAsset 或表现层平滑缓存。

## 5. Unity 运行验证

- [ ] 5.1 在 EditorSimulateMode 中进入 Game 场景，确认 `PlayerAnimConfig` 加载成功且 Console 没有地面 locomotion 配置缺失 warning。
- [ ] 5.2 正常走路直行、跑步直行时确认动画仍按 `SpeedValue` 正确切换走/跑分支。
- [ ] 5.3 按住 Shift 跑步并持续左转，确认角色不再出现明显侧倾。
- [ ] 5.4 按住 Shift 跑步并持续右转，确认角色不再出现明显侧倾。
- [ ] 5.5 停止移动后确认角色经 `MoveEnd` 回到 Idle，且没有因 `RotationValue` 修正造成收步异常。
- [ ] 5.6 确认 Console 中 `MoveLoop` 的 `RotationValue` 不再频繁越过跑步左右阈值，并记录真实运行验证结果。
