## 1. 现状确认

- [x] 1.1 复查 `PlayerMoveStartState`、`PlayerIdleState`、`PlayerMoveLoopState`、`PlayerMoveEndState` 的状态切换入口和下落监听路径
- [x] 1.2 复查 `MoveStart_F.asset` 以及其他 `MoveStart_*` transition 的 EndEvent、normalized time、Speed 和 mixer 参数
- [x] 1.3 用现有日志确认 `PlayerMoveStartState` 的秒级停留、`MoveStart_F` 的 OnEnd 触发时机，以及 `isGround=False`/`VS=-20.00` 是否同时出现

## 2. 起步到循环衔接

- [x] 2.1 在 `PlayerMoveStartState` 中加入持续移动输入下的可衔接窗口判定，避免只等待 Animancer OnEnd
- [x] 2.2 保留 Animancer OnEnd 到 `PlayerMoveLoopState` 的延迟切换兜底路径
- [x] 2.3 保留移动输入释放时进入 `PlayerMoveEndState` 的路径，并确认不会被提前衔接逻辑覆盖
- [ ] 2.4 验证前进、左右斜向、急转方向和锁定状态下的 `MoveStart_*` 到 `PlayerMoveLoopState` 衔接表现

## 3. 非接地下落兜底

- [x] 3.1 提取或复用地面移动状态的下落确认逻辑，覆盖进入状态时已经 `IsOnGround == false` 的情况
- [x] 3.2 将下落确认应用到 `PlayerIdleState`、`PlayerMoveStartState`、`PlayerMoveLoopState` 和 `PlayerMoveEndState`，避免重复 timer 或重复切换
- [x] 3.3 保持现有 `IsOnGround.ValueChanged` 触发下落的行为，并确认 0.05 秒延迟确认仍能过滤单帧误判
- [ ] 3.4 验证跳跃、落地、台阶或斜坡边缘场景不会被错误切入 `PlayerFallLoopState`

## 4. 诊断与验证

- [x] 4.1 补充或复用搜索友好的运行时日志，能观察 `PlayerMoveStartState` 的进入时间、离开时间和离开原因
- [ ] 4.2 在 Unity 中持续按住前进复现，确认 `PlayerMoveStartState` 不再停留秒级时间后才进入 `PlayerMoveLoopState`
- [ ] 4.3 在 Unity 中松开移动输入复现，确认仍进入 `PlayerMoveEndState`
- [ ] 4.4 在 Unity 中非接地场景复现，确认地面移动状态能进入 `PlayerFallLoopState`
- [x] 4.5 运行 `openspec validate fix-player-move-start-stall --strict` 并修复所有规范问题
