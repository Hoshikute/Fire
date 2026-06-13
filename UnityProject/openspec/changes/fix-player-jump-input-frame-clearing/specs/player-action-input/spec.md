## ADDED Requirements

### Requirement: Action button down edge reaches player FSM
玩家动作输入系统 SHALL 保证通过 `InputModule` 收到的按钮按下边沿，在玩家 FSM 的同一次或下一次可消费更新中可见，且不会在玩家 FSM 更新前被 `InputModule.Update()` 清除。

#### Scenario: Jump performed before module update
- **WHEN** Unity Input System 在一次模块更新前触发 `Jump` performed 回调
- **THEN** 玩家 FSM 在该次更新中 SHALL 能通过 `GameModule.Input.GetButtonDown(InputButtonType.Jump)` 读取到本次按下边沿

#### Scenario: Jump performed after player update
- **WHEN** Unity Input System 在玩家 FSM 已完成当帧更新后才触发 `Jump` performed 回调
- **THEN** 玩家 FSM 在下一次可消费更新中 SHALL 能读取到本次按下边沿

### Requirement: Jump input triggers existing jump entry
玩家地面移动相关状态 SHALL 在读取到 `InputButtonType.Jump` 的按下边沿后调用现有 `OnJumpStart()` 路径，并继续由 `PlayerReusableLogic.OnJump()` 决定进入普通跳跃、攀爬或翻越分支。

#### Scenario: Jump from idle
- **WHEN** 玩家处于 `PlayerIdleState` 且按下空格
- **THEN** 状态机 SHALL 执行 `OnJumpStart()` 并进入现有跳跃决策流程

#### Scenario: Jump while moving
- **WHEN** 玩家处于 `PlayerMoveStartState`、`PlayerMoveLoopState` 或 `PlayerMoveEndState` 且按下空格
- **THEN** 状态机 SHALL 执行 `OnJumpStart()` 并进入现有跳跃决策流程

#### Scenario: Buffered jump during landing
- **WHEN** 玩家处于 `PlayerLandState` 的落地动画期间且按下空格
- **THEN** 状态机 SHALL 执行 `OnJumpStart()` 并进入现有跳跃决策流程

### Requirement: Button down edge is single-consumption
动作按钮的 `GetButtonDown` 语义 SHALL 保持按下边沿语义；同一次按住不应在后续多个玩家 FSM 更新中重复表现为新的按下。

#### Scenario: Holding jump does not repeat edge
- **WHEN** 玩家按住空格不松开
- **THEN** `GameModule.Input.GetButtonDown(InputButtonType.Jump)` SHALL 只对本次按下边沿返回一次有效结果

#### Scenario: Re-press jump creates a new edge
- **WHEN** 玩家松开空格后再次按下空格
- **THEN** `GameModule.Input.GetButtonDown(InputButtonType.Jump)` SHALL 对第二次按下产生新的有效边沿

### Requirement: Continuous and other action inputs remain stable
修复动作按钮边沿清理时机后，持续输入和值输入 SHALL 保持现有行为，其他使用相同边沿机制的动作按钮不应退化。

#### Scenario: Movement vector remains continuous
- **WHEN** 玩家持续按住 WASD 移动
- **THEN** `GameModule.Input.Move` SHALL 持续反映当前移动方向，不受按钮边沿清理时机调整影响

#### Scenario: Crouch and lock still receive button down
- **WHEN** 玩家按下 `Crouch` 或 `Lock` 对应按键
- **THEN** 对应玩家状态逻辑 SHALL 能通过 `GetButtonDown` 读取到按下边沿
