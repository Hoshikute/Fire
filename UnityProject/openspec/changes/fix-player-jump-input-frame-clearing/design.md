## Context

当前输入链路使用 Unity Input System 生成的 `InputSystem_Actions`。`<Keyboard>/space` 已绑定到 `Jump`，`InputModule.OnJump()` 会调用 `HandleButtonState(InputButtonType.Jump, context)`。旧实现只在 `Performed && !wasPressed` 时设置 `_buttonDownThisFrame[Jump] = true`，但 Button action 可能先触发 `started`，`started` 已经把 `_buttonStates[Jump]` 置为 pressed，随后 `performed` 到来时 `!wasPressed` 不再成立，导致 Jump 按下边沿没有被记录。

玩家状态机通过 `FsmModule` 驱动。`PlayerIdleState`、`PlayerMoveStartState`、`PlayerMoveLoopState`、`PlayerMoveEndState`、`PlayerLandState` 等状态在 `OnUpdate` 中轮询 `GameModule.Input.GetButtonDown(InputButtonType.Jump)`，再调用 `OnJumpStart()`。这条路径没有使用 `InputModule.RegisterButtonCallback()` 或 `IInputActionListener` 旁路。

风险点在模块更新顺序：`ModuleSystem` 按较高 `Priority` 先轮询模块，`InputModule.Priority == 5`，`FsmModule.Priority == 1`。而 `InputModule.Update()` 当前负责清理 `_buttonDownThisFrame`。如果 Input System 回调先把 JumpDown 置为 true，随后 `InputModule.Update()` 又在 `FsmModule.Update()` 前执行清理，玩家 FSM 就会读不到本帧跳跃输入。

## Goals / Non-Goals

**Goals:**

- 确保 `InputButtonType.Jump` 的按下边沿在玩家 FSM 更新时可见。
- 保持 `GetButtonDown` 的边沿语义：一次按下只触发一次，不因按住空格在后续帧重复触发。
- 保持 `GetButton`、`GetButtonUp`、`Move`、`Look` 等现有访问模型稳定。
- 复用现有 `InputModule`、`ModuleSystem`、`FsmModule` 和玩家状态轮询路径，不创造新的输入服务接口。
- 让验证能区分“输入没进 FSM”和“进入 JumpState 后被接地/动画路径影响”。

**Non-Goals:**

- 不重做玩家移动状态机。
- 不替换 Unity Input System 或重新生成 `InputSystem_Actions`。
- 不调整跳跃、下落、落地动画资源。
- 不改变攀爬/翻越判定规则。
- 不改变 `whatIsGround` 的地面检测配置语义。

## Decisions

### 决策一：把动作边沿清理放到玩家 FSM 消费之后

`InputModule.Update()` 不应在 `FsmModule.Update()` 之前清理 `_buttonDownThisFrame` 和 `_buttonUpThisFrame`。实现时应让动作边沿清理阶段晚于玩家 FSM 消费阶段，例如通过调整 `InputModule` 的模块更新优先级，使其清理逻辑在 `FsmModule` 之后执行。

这个方案保留现有输入回调和玩家状态轮询模型，只修正“采集输入”和“清理输入边沿”的时序契约。由于 `InputModule.Update()` 当前主要做边沿清理，而不是采集 `Move`/`Look` 原始值，把它放到 FSM 后面不会阻断持续输入值更新。

替代方案：

- 让玩家状态改用 `RegisterButtonCallback()` 或 `IInputActionListener`。这能绕过轮询时序，但会把跳跃触发拆到事件路径，改动范围更大，也容易和现有状态优先级、`base.OnUpdate()`、状态切换时机产生新耦合。
- 在每个玩家状态里读取 `InputSystem_Actions.Player.Jump` 原始 action。这样绕过 `InputModule`，会破坏统一输入模块的边界。
- 用帧号延迟清理 `_buttonDownThisFrame`。这个方案能保留边沿一段时间，但如果处理不精确，可能导致同一次按键被多个帧重复消费。

### 决策二：修复对象是通用动作按钮边沿，不只特判 Jump

虽然用户可见症状是“按空格不跳”，但底层问题发生在 `GetButtonDown` 的生命周期。`Crouch`、`Lock`、`Attack`、`Previous`、`Next` 等按钮也使用同一套 `_buttonDownThisFrame` 机制。因此修复应保持通用按钮边沿逻辑一致，不能只在 `Jump` 上做状态特判。

按钮边沿判断应基于实际 pressed 状态跃迁：当 `context.action.IsPressed()` 从 false 变为 true 时记录 `_buttonDownThisFrame`，从 true 变为 false 时记录 `_buttonUpThisFrame`。这样不依赖 `started`、`performed` 的具体到达顺序，也能保持一次按下只产生一次 down 边沿。

替代方案：

- 只在玩家状态中对 Jump 使用 `GetButton(InputButtonType.Jump)`。这会把“按住”当作“按下边沿”，可能导致重复跳跃或落地缓冲行为异常。
- 只在 `PlayerIdleState` 中补跳跃检测。移动中、落地中、锁定状态和 MoveStart 状态仍会漏掉同类输入。

### 决策三：验证要先证明 FSM 收到 Jump，再验证跳跃表现

本变更需要先确认 `InputModule.OnJump()` 之后玩家状态机能执行 `OnJumpStart()`，再验证是否进入 `PlayerJumpState`、`PlayerClimbState` 或其他现有跳跃分支。这样可以把输入时序问题和后续接地/动画/攀爬判定问题分开。

替代方案：

- 只观察角色是否离地。这个验证太粗，若角色进入 JumpState 后被落地判定或动画事件打断，仍无法判断输入时序是否已修复。
- 长期保留大量逐帧日志。这个方案会污染运行日志；应优先使用短期、搜索友好的诊断点或可自动化测试。

## Risks / Trade-offs

- [Risk] 调整 `InputModule` 清理时机可能影响其他使用 `GetButtonDown` 的系统。→ Mitigation：先搜索所有 `GetButtonDown` 调用点，确认主要消费者；验证 `Crouch`、`Lock` 等动作按钮不退化。
- [Risk] 如果某些模块依赖 `InputModule.Update()` 在高优先级执行，降低优先级可能产生间接影响。→ Mitigation：确认 `InputModule.Update()` 当前只做边沿清理；持续输入值由 Input System 回调写入，不依赖该更新顺序。
- [Risk] 修复输入边沿后，跳跃仍可能因为接地、落地或攀爬判定问题表现异常。→ Mitigation：验证分两层记录：先确认 `OnJumpStart()`，再确认状态切换和角色表现；若第二层失败，另开变更处理。
- [Risk] 过度延长 `GetButtonDown` 可见窗口会导致一次按键被多次消费。→ Mitigation：保持一次按下边沿只在一个玩家 FSM 消费窗口内有效，按住不重复触发。
