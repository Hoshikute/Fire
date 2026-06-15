## 1. 证据与映射复核

- [x] 1.1 复核最新 Unity Console，确认地面状态仍进入 `MoveStart`、`MoveLoop`、`MoveEnd`、`Idle`，且问题集中在可见动画分支而非状态机不切换。
- [x] 1.2 从 `PlayerAnimConfig.asset` 记录当前 `idle`、`moveStart_*`、`moveLoop`、`moveEnd_L/R`、`lockIdle` 的 GUID 和资源路径。
- [x] 1.3 从旧 `Player SO.asset` 记录 `PlayerIdleData`、`PlayerMoveStartData`、`PlayerMoveLoopData`、`PlayerMoveEndData` 的 GUID 和资源路径。
- [x] 1.4 对照 `PlayerIdleLoop.asset`、`PlayerMoveLoop.asset`、`PlayerMoveEnd_L/R.asset` 与当前 `NoneLock/*` 内层资源，标出哪些字段丢失了 `LockValue`、`StandValue`、`SpeedValue`、`RotationValue` 层级。
- [x] 1.5 扫描 `PlayerAnimacer/Parameter` 下 `StandValue`、`SpeedValue`、`RotationValue`、`LockValue` 的 `.meta` 和引用关系，确认参数名称和阈值语义。
- [x] 1.6 查找 `InputButtonType`、输入配置和旧 TPC 代码，确认是否已有下蹲输入和旧下蹲 gameplay 语义；若找不到，记录为阻塞点而不是硬编码按键。

## 2. 资源配置修正

- [x] 2.1 根据映射表更新 `PlayerAnimConfig.asset` 的地面 locomotion 字段，使需要外层 mixer 的字段恢复到旧 `Player*` TransitionAsset 或等价资源。
- [x] 2.2 明确 `moveStart_*` 是否保留当前 8 向字段；若保留，确认每个字段仍可由 `StandValue` 选择站/蹲分支。
- [x] 2.3 明确 `moveLoop` 是否恢复为 `PlayerMoveLoop.asset`；恢复后确认走/跑分支仍可由 `SpeedValue` 到达。
- [x] 2.4 明确 `moveEnd_L/R` 是否恢复为 `PlayerMoveEnd_L/R.asset`；恢复后确认停止动画仍可通过 `LockValue` 或站/蹲分支到达正确子资源。
- [x] 2.5 更新 `PlayerAnimConfig` 校验或诊断，使地面 locomotion 关键字段缺失时能暴露具体字段名。

## 3. 表现层 mixer 参数驱动

- [x] 3.1 在 `PlayerAnimViewSystem` 中集中定义 locomotion 参数名和值映射：`StandValue`、`SpeedValue`、`RotationValue`、`LockValue`。
- [x] 3.2 增加统一的 locomotion 参数刷新方法，读取 `PlayerInputComponent`、`PlayerMoveComponent`、`PlayerStateComponent` 后设置 Animancer 参数。
- [x] 3.3 确保 `Idle`、`MoveStart`、`MoveLoop`、`MoveEnd`、`LockIdle` 进入播放前后都会刷新参数，而不是只在 Idle 首次播放时写 `StandValue=1`。
- [x] 3.4 在 `MoveLoop` 同状态持续期间刷新 `SpeedValue`，使 Shift 按下/松开时可在不切逻辑状态的情况下切换走/跑动画。
- [x] 3.5 为 `RotationValue` 建立第一版方向参数来源；若实现时确认当前 8 向 `MoveStart` 已覆盖起步方向，至少保证 `MoveLoop` 不因缺方向参数而选不到正向走/跑。
- [x] 3.6 保留一次性或变更时诊断日志，输出当前 locomotion 参数值和关键资源字段，避免每帧刷屏。

## 4. 下蹲契约处理

- [x] 4.1 如果已有项目下蹲输入，接入 `PlayerInputCollectSystem` 和 `PlayerInputComponent`，并决定该输入是持续态还是边沿态。
- [x] 4.2 如果下蹲只影响动画姿态，在表现层用确认过的输入/状态驱动 `StandValue=0`，不新增回滚状态。
- [x] 4.3 如果下蹲影响 capsule、速度或碰撞通过性，最小化扩展确定性逻辑组件并确保 `DeepCopy()` 覆盖新增字段。
- [x] 4.4 如果无法确认下蹲输入或旧 gameplay 语义，暂停实现并更新 OpenSpec artifacts，不能把下蹲任务标记完成。

## 5. 边界与静态验证

- [x] 5.1 确认本变更不把 Animancer state、TransitionAsset 或视觉-only mixer 参数写入 `PlayerMoveComponent`、`PlayerStateComponent` 或回滚快照。
- [x] 5.2 针对 `PlayerAnimConfig.asset` 和相关 `.asset/.meta` 做 GUID 引用扫描，确认引用资源存在且未跨到错误目录。
- [x] 5.3 运行当前工作区可用的 GameLogic C# 构建检查；如果无法运行，需要记录原因。
- [x] 5.4 运行 `openspec validate fix-player-locomotion-mixer-params --strict`。
- [x] 5.5 刷新 OpenSpec HTML 审阅页，确认 proposal、design、specs、tasks 都能正常查看且中文无乱码。

## 6. Unity 运行验证

- [ ] 6.1 在 EditorSimulateMode 中进入 Game 场景，确认 `PlayerAnimConfig` 加载成功，Console 没有地面 locomotion 配置缺失 warning。
- [ ] 6.2 站立不输入时确认角色保持站立 Idle，不回到之前的蹲伏待机问题。
- [ ] 6.3 正常移动时确认角色进入走路动画，停止后经过收步并回到站立 Idle。
- [ ] 6.4 按住 Shift 移动时确认角色进入跑步动画，松开 Shift 后回到走路动画，逻辑移动速度和可见动画一致。
- [ ] 6.5 触发下蹲输入时确认角色进入下蹲待机；下蹲移动时确认进入下蹲移动动画。
- [ ] 6.6 确认 Console 不出现每帧刷屏的 locomotion mixer 参数 warning 或 `Missing PlayerAnimConfig transition` 地面状态 warning。
