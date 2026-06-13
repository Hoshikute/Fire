## 1. 证据与资源来源确认

- [x] 1.1 复核最新 Unity Console：确认 Jump 输入触发，`PlayerStateSystem` 进入 `JumpInPlace`、`Fall`、`Land`，且 `PlayerAnimViewSystem` 缺少 `jumpInPlace`、`fallLoop`、`land`。
- [x] 1.2 复核 `PlayerAnimConfig.asset` 当前空中字段：`jumpForward`、`jumpInPlace`、`fallStart`、`fallLoop`、`land`、平台跳三阶段是否仍为空。
- [x] 1.3 从旧 `Player SO.asset` 提取 Jump/Fall/Land/Platformer clip 引用、淡入时间、速度和 normalized start time，形成迁移映射表。
- [x] 1.4 确认目标 animation clip 资源和 `.meta` GUID 存在，并位于当前资源收集范围内或能被 `PlayerAnimConfig` 引用打包。

## 2. 资源迁移

- [x] 2.1 为 `jumpInPlace` 创建或复用独立 `TransitionAsset`，引用旧 `placeJumpStart` 的原地起跳 clip 和过渡参数。
- [x] 2.2 为 `jumpForward` 创建或复用独立 `TransitionAsset`，引用旧 `forwardJumpStart` 的前跳起跳 clip 和过渡参数。
- [x] 2.3 为 `fallStart` 和 `fallLoop` 创建或复用独立 `TransitionAsset`，分别引用旧 `fallStart` 和 `fall` clip。
- [x] 2.4 为 `land` 选择安全兜底 clip 并创建或复用独立 `TransitionAsset`；若验证显示单一落地不够，再记录是否扩展原地/前跳落地字段。
- [x] 2.5 为 `platformerUpStart`、`platformerUpLoop`、`platformerDownLoop` 接入旧平台跳三阶段资源，若当前链路无法触发则至少完成静态接线和说明。

## 3. 配置与表现层接线

- [x] 3.1 更新 `PlayerAnimConfig.asset`，填入 Jump/Fall/Land/Platformer 相关 `TransitionAsset` 引用。
- [x] 3.2 更新 `PlayerAnimConfig` 校验 helper，使 `jumpForward`、`jumpInPlace`、`fallLoop`、`land` 缺失时能在配置校验阶段被识别。
- [x] 3.3 检查 `PlayerAnimViewSystem.PlayFall` 的 `fallStart -> fallLoop` 逻辑，确保 `fallStart` 缺失时仍能直接播放 `fallLoop`。
- [x] 3.4 如 `land` 单一字段无法满足原地跳/前跳视觉要求，最小化扩展 `PlayerAnimConfig` 和 `PlayerAnimViewSystem`，并保持逻辑组件只读。
- [x] 3.5 确认本变更不写入 `PlayerMoveComponent`、`PlayerStateComponent`、`PlayerInputComponent` 或任何回滚快照组件。

## 4. 静态验证

- [x] 4.1 针对 `jumpForward`、`jumpInPlace`、`fallStart`、`fallLoop`、`land`、平台跳字段做符号和 GUID 引用扫描。
- [x] 4.2 运行当前工作区可用的 GameLogic C# 构建检查；如果无法运行，需要记录原因。
- [x] 4.3 运行 `openspec validate fix-player-air-animation-config --strict`。
- [x] 4.4 刷新 OpenSpec HTML 审阅页，确认 proposal、design、specs、tasks 都能被正常查看。

## 5. Unity 运行时验证

- [ ] 5.1 在 EditorSimulateMode 中进入 Game 场景，确认 `PlayerAnimConfig` 加载成功且没有 Jump/Fall/Land 配置缺失 warning。
- [ ] 5.2 从站立 Idle 原地按 Jump，确认可见动画经过原地起跳、空中/下落、落地，并回到站立 Idle。
- [ ] 5.3 移动中按 Jump，确认动画离开移动循环，播放前跳或空中动画，落地后回到正确的地面动画。
- [ ] 5.4 确认 Console 中不再出现 `Missing PlayerAnimConfig transition` 的 `jumpInPlace`、`fallLoop`、`land` 警告，也没有每帧刷屏的新 warning。
