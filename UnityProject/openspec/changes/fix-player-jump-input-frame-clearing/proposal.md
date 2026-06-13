## Why

玩家按下空格后角色无法稳定触发跳跃。代码调查显示 `Jump` 输入绑定存在，但 `InputModule` 的单帧 `GetButtonDown` 标志可能在 `FsmModule` 更新玩家状态机前被清除，导致 `PlayerIdleState`、`PlayerMoveStartState`、`PlayerMoveLoopState` 等状态读不到跳跃请求。

这个问题现在需要单独收束，因为它会让跳跃、蹲伏、锁定等依赖 `GetButtonDown` 的瞬时动作输入和模块更新顺序耦合，属于输入系统与玩家状态机之间的契约问题，而不只是某个动画状态的表现问题。

## What Changes

- 调整玩家动作按钮的单帧输入生命周期：`InputButtonType.Jump` 等 `GetButtonDown` 标志 SHALL 在玩家 FSM 有机会读取后再清除。
- 保留现有 `InputSystem_Actions` 绑定和 `InputModule` 对 `Move`、`Look` 等持续输入的读取方式，不引入新的输入系统。
- 保留玩家各状态通过 `GameModule.Input.GetButtonDown(InputButtonType.Jump)` 触发 `OnJumpStart()` 的现有调用模型，优先修复输入可见性契约。
- 增加可验证的运行时观察点或测试覆盖，能确认按下空格后玩家状态机能接收到跳跃输入，并能进入 `PlayerJumpState` 或对应攀爬/翻越状态。
- 不在本变更中重做玩家移动状态机、不调整跳跃动画资源、不改变 `whatIsGround` 的接地配置语义。

## Capabilities

### New Capabilities

- `player-action-input`: 约束玩家动作按钮输入在 `InputModule`、`ModuleSystem` 与玩家 FSM 之间的单帧可见性，覆盖 Jump 等依赖 `GetButtonDown` 的动作触发。

### Modified Capabilities

- 无。

## Impact

- 影响代码区域：
  - `Assets/TEngine/Runtime/Module/InputModule/InputModule.cs`
  - `Assets/TEngine/Runtime/Core/ModuleSystem.cs`
  - `Assets/TEngine/Runtime/Module/FsmModule/FsmModule.cs`
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/StateMachine/State/PlayerMovementFsmState.cs`
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/Player/State/PlayerMovemenState/PlayerIdleState.cs`
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/Player/State/PlayerMovemenState/PlayerMoveStartState.cs`
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/Player/State/PlayerMovemenState/PlayerMoveLoopState.cs`
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/Player/State/PlayerMovemenState/PlayerMoveEndState.cs`
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/Player/State/PlayerMovemenState/PlayerLandState.cs`
- 影响行为：
  - 按下空格后，玩家地面移动相关状态 SHALL 能稳定读取 Jump 的按下边沿并触发跳跃逻辑。
  - `Move` 这类持续输入不应被改变。
  - `Crouch`、`Lock` 等同样依赖 `GetButtonDown` 的动作输入不应因修复 Jump 而退化。
- 验证方式：
  - 通过 Unity 运行时日志或调试信息确认 `InputModule.OnJump` 后玩家状态机能执行 `OnJumpStart()`。
  - 通过按空格、移动中按空格、落地缓冲按空格等场景确认 `PlayerJumpState` 或攀爬/翻越分支仍按现有规则触发。
  - 通过 OpenSpec 验证确认工件完整。
