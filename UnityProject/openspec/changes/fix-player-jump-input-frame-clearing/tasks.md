## 1. 现状确认

- [x] 1.1 复查 `InputSystem_Actions.inputactions` 中 `Jump` 与 `<Keyboard>/space` 的绑定，确认不需要重新生成输入资源
- [x] 1.2 复查 `InputModule.Update()`、`HandleButtonState()`、`GetButtonDown()`、`GetButtonUp()` 的按钮边沿生命周期
- [x] 1.3 复查 `ModuleSystem` 的优先级排序规则，确认 `InputModule` 当前会早于 `FsmModule` 执行清理逻辑
- [x] 1.4 搜索所有 `GameModule.Input.GetButtonDown(...)` 调用点，确认修复影响的动作按钮范围

## 2. 输入边沿生命周期修复

- [x] 2.1 调整 `InputModule` 的按钮边沿清理时机，使 `_buttonDownThisFrame` 和 `_buttonUpThisFrame` 在玩家 FSM 消费后再清除
- [x] 2.2 保持 `Move`、`Look`、`Scroll` 等值输入由现有 Input System 回调更新，不引入新的输入接口
- [x] 2.3 确认 `GetButtonDown(InputButtonType.Jump)` 一次按下只在一个可消费窗口内有效，按住空格不会重复触发
- [x] 2.4 确认松开后再次按下空格会产生新的 Jump 按下边沿
- [x] 2.5 验证 `Crouch`、`Lock` 等同类 `GetButtonDown` 动作按钮不因清理时机变化而退化
- [x] 2.6 修正 `started` 先于 `performed` 更新 `_buttonStates` 导致 `GetButtonDown` 边沿被吞掉的问题

## 3. 玩家跳跃链路验证

- [ ] 3.1 在 Unity 中按空格复现，确认 `InputModule.OnJump` 后玩家 FSM 能执行 `OnJumpStart()`
- [ ] 3.2 在 `PlayerIdleState` 中按空格，确认进入现有跳跃决策流程并能到达 `PlayerJumpState` 或现有攀爬/翻越分支
- [ ] 3.3 在 `PlayerMoveStartState`、`PlayerMoveLoopState`、`PlayerMoveEndState` 中按空格，确认移动中跳跃输入可被读取
- [ ] 3.4 在 `PlayerLandState` 落地动画期间按空格，确认落地缓冲跳跃路径仍可触发
- [ ] 3.5 持续按住 WASD 移动，确认 `GameModule.Input.Move` 的连续输入行为不受影响

## 4. 验证与收尾

- [x] 4.1 运行可用的 C# 编译或 Unity 静态检查；如果本机目标框架或 Unity 运行条件阻塞，记录具体原因
- [x] 4.2 运行 `openspec validate fix-player-jump-input-frame-clearing --strict` 并修复所有规范问题
- [x] 4.3 刷新 `Temp/openspec-html/fix-player-jump-input-frame-clearing/index.html`
- [x] 4.4 复查最终 diff，确认没有改动跳跃动画资源、`whatIsGround` 配置或无关玩家状态机结构
