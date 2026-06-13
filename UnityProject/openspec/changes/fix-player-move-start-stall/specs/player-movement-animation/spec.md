## ADDED Requirements

### Requirement: MoveStart responsive transition
玩家移动动画系统 SHALL 在持续移动输入存在时，让 `PlayerMoveStartState` 在起步动画达到可衔接窗口后进入 `PlayerMoveLoopState`，而不是必须等待起步动画完整结束事件。

#### Scenario: 持续前进输入进入移动循环
- **WHEN** 玩家从 `PlayerIdleState` 持续按住前进输入并进入 `PlayerMoveStartState`
- **THEN** 系统 SHALL 在起步动画达到可衔接窗口后切换到 `PlayerMoveLoopState`

#### Scenario: 起步动画结束作为兜底路径
- **WHEN** `PlayerMoveStartState` 未提前进入 `PlayerMoveLoopState` 且当前起步动画触发 Animancer OnEnd
- **THEN** 系统 SHALL 通过延迟状态切换进入 `PlayerMoveLoopState`

### Requirement: MoveStart input release behavior
玩家移动动画系统 SHALL 在 `PlayerMoveStartState` 中保留移动输入释放后的停止动作路径。

#### Scenario: 起步期间释放移动输入
- **WHEN** 玩家处于 `PlayerMoveStartState` 且 `GameModule.Input.Move` 变为 zero
- **THEN** 系统 SHALL 切换到 `PlayerMoveEndState`

### Requirement: Airborne movement fallback
玩家移动动画系统 SHALL 在地面移动状态中处理已处于非接地状态的情况，不能只依赖 `IsOnGround.ValueChanged` 事件触发下落。

#### Scenario: 进入移动状态时已经非接地
- **WHEN** 玩家进入 `PlayerIdleState`、`PlayerMoveStartState`、`PlayerMoveLoopState` 或 `PlayerMoveEndState` 时 `IsOnGround` 已经为 false
- **THEN** 系统 SHALL 启动下落确认流程，并在确认仍非接地后切换到 `PlayerFallLoopState`

#### Scenario: 移动状态中接地状态变为 false
- **WHEN** 玩家处于地面移动状态且 `IsOnGround` 从 true 变为 false
- **THEN** 系统 SHALL 启动下落确认流程，并在确认仍非接地后切换到 `PlayerFallLoopState`

### Requirement: Movement transition diagnostics
玩家移动动画系统 SHALL 提供足够的运行时可观测信息，用于验证 `PlayerMoveStartState` 不再出现秒级停留。

#### Scenario: 验证 MoveStart 停留时间
- **WHEN** 开发者在 Unity 中复现持续移动输入
- **THEN** 日志或调试信息 SHALL 能确认 `PlayerMoveStartState` 的进入时间、离开时间和离开原因
