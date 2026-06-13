## Context

当前玩家移动状态机从 `PlayerIdleState` 收到移动输入后进入 `PlayerMoveStartState`。`PlayerMoveStartState` 根据 `targetAngle` 播放对应的 `moveStart_*` Animancer transition，并把 `state.Events(player).OnEnd` 绑定到 `OnMoveStartEnd`，再通过 `DeferredSwitch<PlayerMoveLoopState>()` 在下一次 `OnUpdate` 中进入 `PlayerMoveLoopState`。

运行日志显示，持续按住前进时，`PlayerMoveStartState` 从 `rt=34.869s` 到 `rt=37.298s`，停留约 2.4 秒。期间 Heartbeat 正常，说明不是主循环冻结。`MoveStart_F.asset` 是 `LinearMixerTransition`，其结束事件配置在 normalized time `1.85`，因此状态停留更像是被动画结束事件锁住。

同一段日志还显示 `isGround=False` 且 `VS=-20.00` 持续存在。现有 `PlayerIdleState`、`PlayerMoveStartState`、`PlayerMoveLoopState` 的下落兜底依赖 `IsOnGround.ValueChanged`，如果进入状态前已经是 `false`，后续没有变化事件，就可能继续停留在地面移动状态。

## Goals / Non-Goals

**Goals:**

- 持续移动输入时，`PlayerMoveStartState` 应在起步动画进入可衔接窗口后及时切到 `PlayerMoveLoopState`，避免秒级等待。
- 松开移动输入时，仍保留进入 `PlayerMoveEndState` 的行为。
- 非接地状态下，移动状态应能稳定进入 `PlayerFallLoopState`，不依赖必须发生一次 `ValueChanged`。
- 保留 Animancer `OnEnd` 回调中使用延迟切换的安全做法，避免在 PlayableGraph 评估期间直接改状态。
- 使用现有输入、状态机、Animancer、Timer、ReusableData 能力，不创造新的系统级接口。

**Non-Goals:**

- 不重做玩家移动状态机架构。
- 不替换 Animancer 或动画资源管理方式。
- 不调整相机、输入模块、CharacterController 的基础接口。
- 不把所有 `MoveStart_*` 资源都改成统一新资源；资源改动只作为必要时的补充手段。

## Decisions

### 决策一：用“可衔接窗口”驱动 MoveStart 到 MoveLoop

`PlayerMoveStartState` 不应只依赖 `OnEnd` 进入 `PlayerMoveLoopState`。在 `OnUpdate` 中，当移动输入仍然存在、当前播放状态已经达到安全的 normalized time 阈值，并且没有更高优先级动作要处理时，应通过现有 `SwitchState`/`DeferredSwitch` 机制进入 `PlayerMoveLoopState`。

替代方案：

- 只把 `MoveStart_F.asset` 的 EndEvent 从 `1.85` 调小。这个方案对前进方向有效，但容易漏掉其他 `MoveStart_*`，也把行为契约藏在资源里。
- 直接在进入 `PlayerMoveStartState` 时跳过起步动画。这个方案响应最快，但会损失起步动作表现，也可能破坏根运动衔接。
- 在代码中引入可衔接窗口。这个方案能保留起步动作的前段表现，也让状态切换规则可读、可测，作为首选。

### 决策二：下落兜底要同时覆盖“已非接地”和“接地变化”

移动状态现在只监听 `IsOnGround.ValueChanged`。需要补充进入状态或更新状态时对当前 `IsOnGround.Value` 的判断：如果当前已经是 `false`，应启动与现有 0.05 秒 timer 一致的下落确认流程，避免因为没有变化事件而漏切 `PlayerFallLoopState`。

替代方案：

- 修改 `CharacterBase.CheckOnGround()` 的判定。它可能是另一个问题，但直接改地面检测风险更大，会影响跳跃、落地、斜坡和根运动。
- 只在 `PlayerMoveStartState` 特判。可以解决当前日志中的现象，但 `Idle`、`MoveLoop`、`MoveEnd` 仍有同类风险。
- 在移动状态公共逻辑中复用下落确认。这样更贴合现有状态机边界，也减少重复。

### 决策三：日志用于验证，不作为最终业务依赖

当前已有 `[Claude]` 诊断日志能观察状态进入、离开、接地变化和 Heartbeat。实现时可以临时补充更具体的状态停留时间或切换原因日志，但最终业务逻辑不应依赖日志内容。

替代方案：

- 不增加任何日志，仅靠观察体感。这个方案无法稳定证明秒级停留已经消失。
- 长期保留大量逐帧日志。这个方案会污染运行日志和性能观测。
- 用短期、搜索友好的日志验证关键切换点，确认后再按项目习惯决定是否保留。这个方案更稳妥。

### 决策四：Game.unity 的 Player prefab 必须配置有效 Ground LayerMask

非接地下落兜底生效后，`PlayerFallLoopState` 会依赖 `IsOnGround.ValueChanged` 切到 `PlayerLandState`。`Game.unity` 的环境碰撞体主要位于 layer 6，如果 `Assets/AssetRaw/Actor/Player.prefab` 上的 `whatIsGround` 为空，`CharacterBase.CheckOnGround()` 永远无法命中地面，角色会直接进入 `PlayerFallLoopState` 并无法落地。因此需要把 Player prefab 的 `whatIsGround` 配置为覆盖 `Game.unity` 的环境碰撞层。

替代方案：

- 在 `PlayerFallLoopState` 中绕过 `IsOnGround` 强制回到 Idle。这个方案会掩盖真实接地配置错误，并破坏跳跃/平台/下落语义。
- 在代码中为 `whatIsGround == 0` 自动回退到所有层或 Default 层。这个方案会让墙体、触发器或非地面对象参与接地检测，风险更大。
- 修正 Player prefab 的 LayerMask。这个方案范围最小，也让接地、墙体检测、攀爬检测继续使用同一套环境碰撞配置。

## Risks / Trade-offs

- [Risk] 起步动画过早切入 loop，可能出现脚步滑动或动作截断。→ Mitigation：阈值应选在动画已完成起步发力后的窗口，并用前进、斜向、急转和松手场景验证。
- [Risk] 非接地下落兜底过于激进，可能让短暂台阶/斜坡检测抖动直接进入 FallLoop。→ Mitigation：复用现有 0.05 秒延迟确认，避免单帧误判。
- [Risk] 改公共移动状态可能影响 `Idle`、`MoveLoop`、`MoveEnd`。→ Mitigation：任务中要求分别验证持续输入、松手、跳跃、下落、锁定状态。
- [Risk] 资源 EndEvent 与代码阈值重复表达同一件事，后续维护者不清楚优先级。→ Mitigation：代码应把 `OnEnd` 作为兜底或自然结束路径，把提前衔接作为持续输入路径。
- [Risk] 当前日志显示接地检测异常，真正根因可能在场景地面层、角色生成高度或 `CharacterBase.CheckOnGround()`。→ Mitigation：本变更先修移动状态不要秒级等待，同时把接地检测作为验证项；若接地仍异常，再单独提出地面检测变更。
