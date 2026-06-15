## Context

当前 FrameSync 玩家动画链路已经从旧 `ThirdPersonController` 迁移到 `TPBattleContext -> PlayerAnimConfig -> PlayerAnimViewSystem`。最近的运行结果确认 `PlayerAnimConfig` 已能加载，Idle 站立姿态和地面移动动画也已正常；新的可见问题集中在跳跃：按下 Jump 后，输入回调、`PlayerMoveSystem` 垂直速度、`PlayerStateSystem` 的 `Idle -> JumpInPlace -> Fall -> Land` 状态链都正常，但 `PlayerAnimViewSystem` 进入这些状态时找不到对应过渡。

当前 `UnityProject/Assets/AssetRaw/Configs/PlayerAnimConfig.asset` 中 `jumpForward`、`jumpInPlace`、`fallStart`、`fallLoop`、`land` 以及平台跳字段均为 `{fileID: 0}`。因此 Animancer 没有新的空中动画可播放，画面会保留上一段行走或待机姿势。

旧 `Player SO.asset` 仍保留可迁移来源：

- `placeJumpStart` -> `female_unarmed_jump_inplace_up_01.anim`
- `forwardJumpStart` -> `female_unarmed_jump_forward_up_01.anim`
- `fallStart` -> `female_unarmed_jump_inplace_up_02.anim`
- `fall` -> `female_unarmed_jump_down_loop_01.anim`
- `placeJumpLand` / `forwardJumpLand` -> `FemaleMovementAnimsetPro_2.fbx` 中的落地 clip
- `platformerUpStart` / `platformerUpLoop` / `platFormerDownLoop` -> `FemaleMovementAnimsetPro_2.fbx` 中的平台跳 clip

## Goals / Non-Goals

**Goals:**

- 让 `Jump`、`JumpInPlace`、`Fall`、`Land` 进入时播放对应空中或落地动画，不再沿用行走姿势。
- 复用旧配置中已经存在的动画 clip 来源，按当前 `PlayerAnimConfig` 的 `TransitionAsset` 聚合方式迁移。
- 维持现有 FrameSync 确定性边界：动画只影响表现层，不写入可回滚逻辑组件。
- 保留运行时 warning 的诊断价值，但对已经纳入本变更的 Jump/Fall/Land 字段进行更早的配置校验。
- 在 Unity Game 场景中验证原地跳、移动中前跳、下落、落地、回到 Idle 的完整可见链路。

**Non-Goals:**

- 不修改 `PlayerMoveSystem` 的跳跃速度、重力、地面检测或定点运算规则。
- 不修改 `PlayerStateSystem` 的状态判定语义，除非实现时发现表现层必须区分落地动画且现有状态数据完全无法表达。
- 不恢复旧 `ThirdPersonController` 运行链路，不重新挂回旧 `Player` 控制脚本。
- 不在本变更中完整迁移 Vault、Climb、LedgeClimb 等交互动画，除非已有明确无歧义的专用资源。
- 不引入 `Resources.Load`、运行时 `AssetDatabase` 或绝对路径加载。

## Decisions

### D1：继续以 `PlayerAnimConfig` 为唯一动画聚合入口

空中动画仍接到 `PlayerAnimConfig.asset`，由 `TPBattleContext` 通过 TEngine/YooAsset 加载后注入 `PlayerViewComponent`。这样和地面动画恢复保持同一条资源链路，避免重新引入旧 `Player SO.asset` 作为第二套权威数据。

备选方案：让 `PlayerAnimViewSystem` 直接读取旧 `Player SO.asset`。这能快速拿到 clip，但会制造并行配置来源，并破坏当前 FrameSync 表现层的聚合配置契约。

### D2：为旧的内嵌 `ClipTransition` 创建或复用独立 `TransitionAsset`

`PlayerAnimConfig` 字段类型是 `TransitionAsset`。旧 `Player SO.asset` 里的 Jump/Fall 多数是内嵌 `ClipTransition`，不能直接拖到当前字段中。实现时应优先创建独立 TransitionAsset，复制旧配置中的 `_FadeDuration`、`_Speed`、`_NormalizedStartTime` 和 `_Clip` 引用，再把这些 asset 接到 `PlayerAnimConfig`。

备选方案：把 `PlayerAnimConfig` 字段改成可序列化内嵌 `ClipTransition`。这会扩大代码和资产结构变更，而且会和现有地面动画 `TransitionAsset` 模式不一致。

### D3：先补齐主空中链路，落地使用安全兜底

现有 `PlayerAnimConfig` 只有一个 `land` 字段，而旧配置有 `placeJumpLand` 和 `forwardJumpLand` 两组落地 clip。第一步应选择一个稳定的 `land` 兜底，优先保证落地时不继续显示走路姿势。若运行时视觉明显需要区分原地/前跳落地，再在本变更内扩展配置字段和 `PlayerAnimViewSystem` 选择逻辑。

备选方案：立即新增 `landInPlace`、`landForward` 等字段。这样表达更完整，但会扩大迁移、校验和资源接线范围；是否必要应由首轮视觉验证决定。

### D4：把 Jump/Fall/Land 从“高级可选缺口”提升为当前必验链路

之前 `fix-player-tpose-missing-anim-config` 允许高级状态缺映射时只 warning，不阻塞地面动画恢复。现在用户已经验证地面动画正常，并明确触发了跳跃问题，因此 Jump/Fall/Land 不再适合只作为可选 warning；至少 `jumpForward`、`jumpInPlace`、`fallLoop`、`land` 应进入配置完整性检查。`fallStart` 可作为 Fall 入场增强，但若缺失也必须有 `fallLoop` 兜底。

备选方案：继续只靠进入状态时 warning。运行时现象已经证明这会让用户看到错误姿势，诊断太晚。

## Risks / Trade-offs

- [Risk] 手写 Unity YAML 创建 `TransitionAsset` 容易写错 SerializeReference 或 GUID -> Mitigation：优先参考现有 TransitionAsset YAML 形状，并通过编译、OpenSpec 校验和 Unity 运行验证确认。
- [Risk] 落地只有一个 `land` 字段可能无法同时适配原地跳和前跳 -> Mitigation：先用安全兜底恢复可见链路，若验证中明显不匹配，再扩展字段并记录到 tasks。
- [Risk] 旧跳跃动画来自 FemaleMovement 资源，与当前 Rusk Avatar 可能有重定向瑕疵 -> Mitigation：保留运行时视觉验证任务；若 clip 播放但姿势不合适，再换资源而不是改逻辑状态。
- [Risk] 把所有高级字段都列为必填会扩大范围 -> Mitigation：本变更只提升 Jump/Fall/Land 和可直接验证的平台跳字段，Vault/Climb/LedgeClimb 继续作为后续专项。

## Migration Plan

1. 从旧 `Player SO.asset` 提取 Jump/Fall/Land/Platformer 的 clip、速度、淡入和 normalized start time。
2. 为缺少独立资源的 clip 创建 `TransitionAsset`，放在现有 `PlayerAnimacer` 配置树下，命名与 `PlayerAnimConfig` 字段一致。
3. 更新 `PlayerAnimConfig.asset`，接入 `jumpForward`、`jumpInPlace`、`fallStart`、`fallLoop`、`land` 以及平台跳三阶段。
4. 如落地需要区分原地/前跳，最小化扩展 `PlayerAnimConfig` 和 `PlayerAnimViewSystem`，并保持逻辑组件只读。
5. 运行 GameLogic 构建、OpenSpec strict 校验，并在 Unity Game 场景验证跳跃可见链路。

## Open Questions

- `land` 第一版应优先使用 `placeJumpLand` 的第一个 clip，还是 `forwardJumpLand` 的第一个 clip 作为统一兜底？
- 平台跳三阶段在当前 Game 场景是否有可直接触发入口，还是只做资源接线和静态校验？
