## Why

玩家在持续按住前进输入时，会在 `PlayerMoveStartState` 中停留约 2.4 秒后才进入 `PlayerMoveLoopState`，表现为起步动画期间人物像是卡住。现有日志显示主循环仍在正常更新，问题更像状态切换被 `MoveStart_F` 的 Animancer 结束事件锁住，而不是帧率或输入系统卡死。

## What Changes

- 调整玩家移动起步状态的切换契约：持续移动输入下，`PlayerMoveStartState` 不应只等待起步动画完整结束才进入 `PlayerMoveLoopState`。
- 为起步动画到移动循环的衔接建立清晰判定：可基于动画归一化时间、移动输入状态和当前接地/下落状态决定是否提前切换。
- 补强非接地状态下的移动状态兜底：当玩家已经处于 `IsOnGround == false` 时，移动状态不应因为缺少 `ValueChanged` 事件而长时间停留在地面移动状态。
- 保留 Animancer 回调中延迟切换的安全边界，避免在 PlayableGraph 评估期间直接修改状态拓扑。
- 不引入新的输入接口或动画系统接口，优先复用现有 `PlayerMovementFsmState`、`PlayerMoveStartState`、`PlayerFallLoopState`、`ReusableData` 与 Animancer transition 数据。

## Capabilities

### New Capabilities
- `player-movement-animation`: 约束玩家移动状态与 Animancer 起步、循环、下落动画之间的切换行为，覆盖持续移动输入、起步动画提前衔接、非接地兜底和验证日志。

### Modified Capabilities
- 无。

## Impact

- 影响代码区域：
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/Player/State/PlayerMovemenState/PlayerMoveStartState.cs`
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/Player/State/PlayerMovemenState/PlayerIdleState.cs`
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/Player/State/PlayerMovemenState/PlayerMoveLoopState.cs`
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/StateMachine/State/PlayerMovementFsmState.cs`
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/CharacterBase/CharacterBase.cs`
- 影响资源与配置：
  - `Assets/AssetRaw/ThirdPersonController/AnimancerController/Resources/Config/PlayerAnimacer/NoneLock/MoveStart_F.asset`
  - 其他 `MoveStart_*` transition 资源需要作为对照检查，但不一定需要修改。
  - `Assets/AssetRaw/Actor/Player.prefab` 的 `whatIsGround` 需要覆盖 `Game.unity` 使用的环境碰撞层，否则下落状态无法收到落地事件。
- 影响行为：
  - 持续移动输入下，起步到循环的体感延迟应显著缩短。
  - 松开移动输入仍应进入 `PlayerMoveEndState`。
  - 跳跃、蹲伏、锁定移动和下落切换不应被破坏。
- 验证方式：
  - 通过 Unity 运行时日志确认 `PlayerMoveStartState` 停留时间不再出现秒级延迟。
  - 通过前进、斜向、转向、松手、离地等场景验证状态切换顺序。
