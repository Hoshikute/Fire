## Context

FrameSync 玩家当前通过 `TPBattleContext -> PlayerAnimConfig -> PlayerAnimViewSystem` 播放 Animancer 动画，通过 `PlayerMoveSystem` 在 200ms 逻辑帧中更新 `PlayerMoveComponent.faceDir` 和位置，再由 `PlayerViewSystem` 在渲染帧只读逻辑状态、用 `Quaternion.LookRotation(face, Vector3.up)` 平滑表现朝向。

最新 Console 证据显示，奔跑左右转时角色出现可见侧倾，但地面状态仍正常切换，且 Transform 表现层只做 yaw 旋转。异常更集中在 locomotion mixer 参数：`PlayerAnimViewSystem.CalculateRotationValue` 当前把 `input.moveDir` 与 `move.faceDir` 的差角直接作为 `RotationValue` 写入 Animancer；日志中 `MoveLoop` 的 `RotationValue` 可到约 `2.65`。旧资源中 `MoveRunLoop.asset` 的 `RotationValue` 阈值是 `-2 / 0 / 2`，`MoveWalkLoop.asset` 是 `-2.5 / 0 / 2.5`，这会让持续奔跑转向进入左右跑或斜跑分支。

旧 `ThirdPersonController` 路径有两个隐含前提：一是 `rotationValueParameter` 通过 `SmoothedFloatParameter` 平滑，二是 `player.transform` 会持续 Slerp 到输入方向。FrameSync 迁移后，位移改为确定性代码驱动，`faceDir` 受逻辑帧和转向速率限制；如果动画参数仍直接读“输入方向与当前逻辑朝向差”，就会把正常转向误解释成持续横移。

## Goals / Non-Goals

**Goals:**

- 让奔跑左右转时的 `MoveLoop` 动画保持符合前进跑/自然转向的姿态，不因 `RotationValue` 过大而长期混入明显侧倾的左右跑/斜跑分支。
- 明确 `RotationValue` 在 FrameSync 表现层中的来源、限幅和平滑策略，并与旧 Animancer 阈值对齐。
- 保留 `MoveStart` 起步方向选择，不把起步瞬间的 8 向选择和持续奔跑循环混为一套逻辑。
- 保持动画参数为表现层数据，不写入回滚组件，不改变确定性移动、碰撞、重力或逻辑帧步长。
- 提供可重复验证方式：临时 `RotationValue=0` 对照、Console 参数范围、Unity Editor 视觉检查。

**Non-Goals:**

- 不重新启用 root motion 驱动位移。
- 不恢复旧 `ThirdPersonController` 作为运行时控制器。
- 不重做地面 locomotion 资源层级、跑步/下蹲输入契约；这些属于 `fix-player-locomotion-mixer-params` 已覆盖或后续范围。
- 不修改 `PlayerMoveSystem.RotateTowards` 的确定性算法，除非后续运行证据证明逻辑朝向本身错误。
- 不处理锁定模式八向移动、Vault/Climb/LedgeClimb 等交互动画的完整方向混合。

## Decisions

### D1：先用 `RotationValue=0` 对照验证，再做正式参数修正

实现时应先提供一个极窄的本地验证路径：在 `MoveLoop` 状态临时把 `RotationValue` 固定为 `0`，进入 Unity EditorSimulateMode 测试奔跑左右转。如果侧倾消失，说明问题是 locomotion mixer 分支选择；如果仍侧倾，再检查具体跑步 clip 或模型骨骼姿态。

备选方案：直接调整动画资源或替换 clip。这个方案会把根因掩盖在资源变更里，无法区分是参数错误、阈值错误还是 clip 本身带侧倾。

### D2：`MoveLoop` 的 `RotationValue` 不应直接等于输入与逻辑朝向的原始差角

`PlayerMoveSystem` 的 `faceDir` 在固定逻辑帧中逐步转向，表现层每帧用原始差角计算 `RotationValue` 会在转向期间持续得到大值。正式实现应将 `MoveLoop` 的 `RotationValue` 映射到旧资源可接受的方向域，并在非锁定奔跑转向时优先表达“前进跑的自然转向”，而不是“横向跑”。

可选实现路径：

- 第一版保守修复：非锁定 `MoveLoop` 中当玩家有移动输入且不是显式横移/锁定移动时，将 `RotationValue` 向 `0` 平滑，避免跑步转向混入侧跑分支。
- 进阶修复：引入表现层私有的平滑值，按旧 `SmoothedFloatParameter` 的时间常数将目标值限幅后再写入 Animancer。

备选方案：继续使用 `Mathf.Atan2(cross, dot)` 原始弧度值。这个方案与当前日志中的侧倾现象吻合，不能作为最终行为。

### D3：平滑状态必须停留在 `PlayerViewComponent` 或 `PlayerAnimViewSystem` 私有表现层状态

如果需要恢复旧 TPC 的参数平滑，平滑缓存应放在 `PlayerViewComponent` 或 `PlayerAnimViewSystem` 的私有字典中，只用于 Animancer 参数写入。不要把 `RotationValue` 写入 `PlayerMoveComponent`、`PlayerStateComponent` 或任何参与 `RecordSystem<T>` 快照的组件。

备选方案：把平滑后的方向参数作为逻辑组件字段保存。这样会把视觉 mixer 细节变成回滚事实，破坏逻辑/表现边界。

### D4：日志按参数桶变化输出，不能每帧刷屏

现有 `[CODEX_LOG] Player locomotion mixer params` 已按快照变化输出。修复后应继续保留少量诊断，至少能看出 `state`、`SpeedValue`、`RotationValue`、`speedGear`，并在 `MoveLoop` 期间确认 `RotationValue` 不再频繁越过跑步左右阈值。

备选方案：删除日志会降低复测可见性；每帧打印会淹没 Console，影响用户判断。

## Risks / Trade-offs

- [Risk] 固定或过度压低 `MoveLoop.RotationValue` 会让真正的横移/锁定移动缺少侧向姿态。Mitigation：本变更只约束非锁定奔跑左右转；锁定模式和显式横移作为后续单独验证范围。
- [Risk] 如果跑步正向 clip 本身带有明显侧倾，参数修正只能减轻分支误选，不能消除资源姿态问题。Mitigation：先做 `RotationValue=0` 对照，必要时再追 clip 资源。
- [Risk] 平滑实现若使用 `Time.deltaTime` 写进逻辑组件会污染确定性。Mitigation：只在表现层 `Update` 使用渲染帧时间，且不回写回滚组件。
- [Risk] 当前 `MoveStart` 8 向 clip 和 `MoveLoop` 参数之间可能存在过渡不连续。Mitigation：保留起步方向选择，重点验证进入 `MoveLoop` 后姿态是否稳定。

## Migration Plan

1. 复核 `MoveMoveLoop.asset`、`MoveRunLoop.asset`、`MoveWalkLoop.asset` 的 `SpeedValue`/`RotationValue` 参数名和阈值，确认 `RotationValue` 的左右分支边界。
2. 在本地验证或代码分支中临时强制非锁定 `MoveLoop` 的 `RotationValue=0`，用 Unity Editor 观察奔跑左右转侧倾是否消失。
3. 若验证成立，在 `PlayerAnimViewSystem` 中实现表现层 `RotationValue` 策略：非锁定 `MoveLoop` 对原始差角进行限幅、归零或平滑，保留其它状态的必要方向参数。
4. 保留参数变更日志，确认 `MoveLoop` 跑步转向期间 `RotationValue` 不再频繁越过跑步左右阈值。
5. 运行 GameLogic C# 构建检查和 `openspec validate fix-player-run-turn-side-tilt --strict`。
6. 在 Unity EditorSimulateMode 复测直行跑、左转跑、右转跑、停止回 Idle，并明确记录是否完成真实运行验证。

## Open Questions

- 非锁定 `MoveLoop` 是否需要保留任何轻微左右摆动值，还是第一版应完全归零以稳定前进跑姿态？
- 锁定模式下的左右移动是否也存在侧倾问题，是否应作为本变更之外的新范围处理？
- 如果 `RotationValue=0` 后仍侧倾，是否需要进一步检查跑步正向 clip 的 root motion/骨骼姿态导入设置？
