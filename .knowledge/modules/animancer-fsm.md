# Animancer 动画 + TEngine FSM 状态机

## 一句话
玩家状态基于 TEngine 的 `FsmState<Player>`，每个状态在进入时用 Animancer 播放对应动画，并订阅输入/属性事件驱动状态切换；动画回调里的状态切换走「延迟切换」避免破坏 PlayableGraph。

## 关键文件
- `Player/Controller/Core/StateMachine/State/PlayerFsmState.cs` — 玩家状态基类（核心，继承 `FsmState<Player>`）
- `Player/Controller/Core/StateMachine/State/PlayerMovementFsmState.cs` — 移动类状态中间基类
- `Player/Controller/Core/StateMachine/State/StateBase.cs` / `StateHandler.cs` — 旧状态基础设施（被适配到 FSM）
- `Player/Controller/Core/Player/State/PlayerMovemenState/*.cs` — 各具体状态
- `Player/Controller/Core/Animation/AnimationID.cs` — 动画 ID 定义

## 状态基类约定（PlayerFsmState）
继承 TEngine `FsmState<Player>`，统一持有 `player` / `animancer` / `reusableData` / `cam` / `reusableLogic`，生命周期：
- `OnInit(fsm)` — 从 `fsm.Owner` 注入依赖（player、ReusableData、CamTransform、Animancer）。
- `OnEnter(fsm)` — 保存 `currentFsm`，调用 `AddEventListening()`。
- `OnLeave(fsm, isShutdown)` — `RemoveEventListening()`。
- `OnUpdate(fsm, ...)` — 逻辑帧，**开头应先调 `TryExecuteDeferredSwitch()`**。
- `OnAnimationUpdate()` / `OnAnimationEnd()` — 由 Player 转发的动画帧/结束回调。
- 子类必须实现 `AddEventListening()` / `RemoveEventListening()`（成对订阅/退订，防止泄漏）。

## 状态切换两种方式
1. **直接切换**：`SwitchState<TState>()`（用保存的 `currentFsm`）或 `SwitchState<TState>(fsm)` → 内部 `ChangeState<TState>(fsm)`。
2. **延迟切换 `DeferredSwitch<TState>()`**（关键设计）：
   - 用途：在 **Animancer `OnEnd` 等回调**中不能直接改 PlayableGraph，否则会出错。
   - 机制：只置标志位 `_pendingDeferredSwitch` + 目标类型，**真正切换在下一帧 `OnUpdate` 开头的 `TryExecuteDeferredSwitch()` 执行**。
   - 调用方在 `TryExecuteDeferredSwitch()` 返回 true 后应立即 `return`，避免在已切换的旧状态上下文里继续跑业务逻辑。

## 典型状态写法（以 PlayerIdleState 为例）
- `OnInit`：`base.OnInit` 后从 `playerSO` 取本状态配置（如 `idleData`）。
- `OnEnter`：重置复用数据 → `reusableLogic.PlayNextState()` 播放动画 → 检查是否该转 Fall。
- `AddEventListening`：订阅 `player.IsOnGround.ValueChanged`、锁定参数变化等，变化时 `SwitchState<>()`。
- `OnUpdate`：先 `TryExecuteDeferredSwitch()`，再读输入（`GameModule.Input.GetButtonDown(...)`）决定跳跃/下蹲等切换。

## 注意事项 / 坑
- **事件订阅必须成对**：`AddEventListening`/`RemoveEventListening` 一一对应，漏退订会导致状态切换后回调仍触发、内存泄漏。
- **动画回调里别直接切状态**：一律用 `DeferredSwitch`，否则可能在 Animancer 更新 PlayableGraph 期间改图导致异常。
- 动画播放统一走 Animancer（`animancer` 引用），不要混用 Unity Animator 的 SetTrigger 那套。

## 相关代码位置
`UnityProject/Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/StateMachine/`
`UnityProject/Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/Player/State/`

## 关联文档
- 控制器入口：`modules/player-controller.md`
