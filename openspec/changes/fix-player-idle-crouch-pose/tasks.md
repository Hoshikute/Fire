## 1. 证据与范围确认

- [x] 1.1 重新确认运行证据：`PlayerAnimConfig` 已成功加载，`PlayerAnimViewSystem` 播放了 `Idle`，同时 `PlayerStateSystem` 仍保持 `Idle`。
- [x] 1.2 重新确认序列化引用：`PlayerAnimConfig.idle`、`PlayerAnimConfig.lockIdle`、`NoneLock/IdleLoop.asset`、`IdleCrouch`、`IldeStand`。
- [x] 1.3 搜索当前运行时代码对 `NoneLock/IdleLoop.asset` 的引用，并判断代码侧 mixer 参数修复和资产默认参数修复哪一个更安全。

## 2. 实现

- [x] 2.1 在表现层实现站立 Idle 选择，优先修改 `PlayerAnimViewSystem` 或 `PlayerAnimConfig` helper，而不是 FrameSync 逻辑组件。
- [x] 2.2 确保修复不会把动画资源、mixer 参数或表现层专用状态写入 `PlayerMoveComponent`、`PlayerStateComponent` 或回滚快照。
- [x] 2.3 保留 `IdleCrouch` 和备用待机资源，供未来明确的蹲伏或锁定状态使用。
- [x] 2.4 为 Idle 映射路径增加简洁诊断或校验，不允许每个渲染帧刷日志。

## 3. 静态验证

- [x] 3.1 针对 `StandValue`、`IdleLoop`、`PlayerAnimConfig.idle`、`PlayerAnimViewSystem` 做符号/引用扫描，确认接线符合预期。
- [x] 3.2 运行当前工作区可用的 hotfix C# 编译或构建检查；如果无法运行，需要记录原因。
- [x] 3.3 运行 `openspec validate fix-player-idle-crouch-pose --strict`。

## 4. 运行时验证

- [ ] 4.1 在 Unity EditorSimulateMode 中进入 Game 场景，确认首次可见的 `Idle` 是站立姿态，不是蹲伏或偏低姿态。
- [ ] 4.2 短按移动，确认动画能经过移动状态并回到同样的站立 `Idle` 姿态。
- [ ] 4.3 确认 Console 中没有 `PlayerAnimConfig` 缺失或 location 错误，也没有重复刷屏的 Idle 选择警告。
