## Context

Fire 项目目前存在两套玩家角色控制系统并存：

1. **老 TPC**（`ThirdPersonController` 命名空间，44 文件）：基于 TEngine FSM + Animancer Root Motion + Unity Physics 的第三人称角色控制器。位移由 `Animator.deltaPosition` → `CharacterController.Move` 驱动，接地/斜坡/碰撞全部依赖 `Physics.Raycast`/`Physics.CheckSphere`/`Time.deltaTime`。**不具备确定性，无法参与帧同步**。

2. **新 FrameSync ECS**（`GameLogic` 命名空间）：基于确定性定点积分（`SyncVector3` + 200ms 逻辑帧）+ ECS（`PlayerMoveComponent`/`PlayerMoveSystem`/`PlayerWorld`）的角色控制器。已完成 P2 第 1 轮（`IDeterministicGround` 接地抽象）和第 2 轮（`PlayerStateComponent`+`PlayerStateSystem` 逻辑状态机），支持 WASD 行走/奔跑/跳跃/空中惯性。

ADR 0002 已决策（方案 B）：保留 `GameModule.Character` 模块但去 TPC 化，FrameSync 复用它加载角色 prefab。ADR 0003 已定下 P2 全量复刻的 7 轮迭代计划。

## Goals / Non-Goals

**Goals:**
- P0：四个工具类（MonoSingleton/NoMonoSingleton/BindableProperty/ToolFunction）脱离 `ThirdPersonController` 命名空间，按 TEngine 规范组织
- P1：`GameModule.Character` API 通用化（`LoadCharacterAsync(location)`），供 FrameSync 和任何未来角色类型复用
- P2-3：确定性斜坡，角色不穿墙（AABB/胶囊碰撞 vs 定点几何）
- P2-5：程序化定点攀爬/翻越轨迹，替代动画曲线位移
- P2-6：`PlayerAnimViewSystem` 只读 `PlayerStateComponent` 枚举驱动 Animancer 播放，动画层完全降为纯表现
- P2-7：定点惯性、转向插值、加减速曲线，复刻老 TPC 手感
- P4：`TPBattleContext` 从 `Character.SetThirdPersonPlayerPrefab` 切到 `PlayerFrameSyncEntry`
- P5：删除 `Player/Controller/` 44 文件，`Player.prefab` 解绑 `Player.cs`，清理过时注释

**Non-Goals:**
- 不涉及网络多人同步（当前所有工作基于单机本地验证路径，`PlayerInputComponent` 为单例，World SyncRule 为 Frame）
- 不改造 TEngine FSM 框架本身——只删除对它的业务用法
- 不动 Animancer 库本身，只在表现层使用它做纯动画播放
- 不碰 `Server/LockStepDemo/` 服务端代码

## Decisions

### D1: 按 P0→P1→P2(3-7)→P3→P4→P5 顺序推进

**选择**：严格按依赖链串行推进，每阶段独立可 `dotnet build` 验证 0 错误后再进下一轮。

**理由**：ADR 0002/0003 已定顺序。工具类（P0）可能被 Character 模块和 FrameSync ECS 共用，Character 去 TPC 化（P1）是后续所有角色加载的基础，P2 各轮之间低级特性依赖高级特性（斜坡需要接地、碰撞需要斜坡、攀爬需要碰撞）。

**替代方案**：P0 与 P2 并行——但 P2 可能复用 P0 迁移后的工具（如 `ToolFunction.GetDeltaAngle` 的定点版本），并行会引入不确定的依赖顺序。

### D2: P0 工具类迁移——挪位置去 TPC namespace，不改逻辑

**选择**：四个工具类从 `ThirdPersonController` 命名空间迁移到 TEngine 通用 Utility 目录，文件名/类名保持，去掉 `using ThirdPersonController` 引用。`BindableProperty` 保留为通用工具，但在帧同步逻辑层（`GameLogic`）禁止使用（含闭包/事件，非确定性）。

**理由**：这四个类是纯工具，不依赖 TPC 业务逻辑。迁移后老 TPC 和 FrameSync ECS 都能引用，避免重复造轮子。`BindableProperty` 在表现层（`PlayerViewSystem`、`PlayerFrameSyncEntry`）仍有使用价值（如响应状态变化更新 UI）。

**替代方案**：删除这四个类并用 TEngine 内置替代——但 `GameEvent`（TEngine 事件）与 `BindableProperty`（值变化回调）语义不同，`ToolFunction.GetDeltaAngle` 等是纯数学无 TEngine 等价物，`MonoSingleton`/`NoMonoSingleton` 在 TEngine Module 体系外仍有使用场景。保留更安全。

### D3: Character 模块去 TPC 化——B 方案：保留模块，API 重命名

**选择**：保留 `ICharacterModule`/`CharacterModule` 框架，将 API 从 `SetThirdPersonPlayerPrefab`/`LoadThirdPersonPlayerAsync` 改为 `SetCharacterPrefab`/`LoadCharacterAsync`。参数从 TPC 专用类型改为通用 `string location`。

**理由**：ADR 0002 决策 1 已定。Character 模块的加载/缓存/销毁框架本身通用，仅命名被 `ThirdPersonPlayer` 污染。重命名后 FrameSync 的 `PlayerFrameSyncEntry` 可直接调用 `GameModule.Character.LoadCharacterAsync("hero_01")` 加载角色 prefab。

**替代方案**：删除 Character 模块，在 FrameSync 内部自行处理 prefab 加载——破坏了模块化原则，且 Character 模块的缓存/生命周期管理值得复用。

### D4: 斜坡——确定性地面法线 + 坡度约束

**选择**：扩展 `IDeterministicGround` 接口，增加 `GetNormal(x, z): SyncVector3` 方法。`PlayerMoveSystem.Step` 在接地时查询法线，若坡度角超过阈值（例如 45°），角色沿斜面投影位移（水平分量沿斜面切向推进，竖直分量受法线约束）。位移公式：`horizontalDelta = inputDir × speed × dt`，投影到斜面切平面得到 `slopeDelta`，`pos += slopeDelta`。

**理由**：与已完成第 1 轮 `IDeterministicGround` 接口兼容，只需加一个方法 + 在 `FlatGround` 返回 `(0,1,0)`（即无坡度）。后续可扩展 `MeshGround`/`TerrainGround` 查询真实法线。不引入 `Physics.Raycast`，保持确定性。

### D5: 碰撞——AABB/胶囊 vs 定点静态几何

**选择**：引入 `ICollisionWorld` 接口（确定性碰撞查询），返回碰撞信息（碰撞法线、穿透深度）。角色用竖直胶囊（capsule，半径+高度）做碰撞体，场景几何用 AABB 列表或 BVH。碰撞响应：分离+滑动（沿碰撞法线切向投影速度）。

**风险点**：确定性场景几何数据来源未定——需要确认项目是否有导出的确定性几何（格子/碰撞网格/导航网格），或在当前阶段用 AABB 包围盒近似。碰撞轮可能因数据未就绪而降级为"AABB 阻挡框架 + 接口预留"。

### D6: 攀爬/翻越——程序化定点轨迹

**选择**：不复制老 TPC 的 Animancer 动画曲线驱动（`PlayerReusableLogic` 371 行动画混合操作）。改为逻辑层按帧推进的固定轨迹：`ClimbTrajectory` 表（逻辑帧索引 → `SyncVector3 delta`），从老动画曲线的关键位移量提取转定点。攀爬/翻越期间 `PlayerStateComponent.state` 设置为交互状态（`Vault/Climb/LedgeClimb/PlatformerUp/MoveToWall`），`PlayerMoveSystem.IsInteractState()` 屏蔽主动水平移动。

**风险点**：提取老动画曲线关键位移量需要工具或人工采样。如果老曲线复杂（多层混合、IK），定点轨迹表可能只能近似还原手感。

### D7: 动画表现降级——PlayerAnimViewSystem

**选择**：新建 `PlayerAnimViewSystem`（继承 `ViewSystemBase`，渲染帧执行），过滤 `PlayerStateComponent`+`PlayerViewComponent`，读 `PlayerStateComponent.state` 枚举（Idle/MoveStart/MoveLoop/MoveEnd/Jump/Fall/Land/Vault/Climb/...），调用 `PlayerViewComponent.animancer.Play(config.GetClip(state))` 播放对应动画。系统已注册在 `PlayerWorld.GetSystemTypes()` 的末尾（排在 PlayerViewSystem 之后）。

**理由**：老 TPC 的动画播放与状态逻辑耦合（每个 FsmState 里直接调 `animancer.Play()`），新架构里动画完全由表现层 System 根据逻辑层 `PlayerStateComponent` 枚举驱动，逻辑不感知动画，动画不驱动逻辑。这就是"逻辑与表现分离"的最终态。

### D8: TPBattleContext 切换

**选择**：将 `TPBattleContext.cs:64-70` 的 `Character.SetThirdPersonPlayerPrefab`+`LoadThirdPersonPlayerAsync` 替换为：使用 P1 通用化后的 `Character.LoadCharacterAsync` 加载 prefab，然后挂载 `PlayerFrameSyncEntry` 组件并注入动画配置 SO。

**理由**：`TPBattleContext` 是唯一业务调用方，切换后老 Character API 无残留引用，可安全删除。`PlayerFrameSyncEntry` 已在 `PlayerFrameSyncEntry.cs` 中自包含相机绑定+动画注入+World 创建逻辑。

## Risks / Trade-offs

- **[风险] 碰撞几何数据来源未定**：确定性碰撞需要场景几何的定点表示，不能调 `Physics.Raycast`。→ **缓解**：第 4 轮先以 AABB 包围盒近似 + 接口预留；若项目已导出碰撞网格则对接，否则回退到简单 AABB。
- **[风险] 攀爬/翻越轨迹还原度不足**：定点轨迹表可能无法完全复刻老 TPC 的动画曲线混合手感。→ **缓解**：提取关键偏移量（如爬升总高度、翻越前移距离、各阶段耗时帧数），用线性/二次近似；手感打磨在第 7 轮微调。
- **[风险] 动画过渡自然度**：`PlayerAnimViewSystem` 逐个状态枚举切换，可能不如老 TPC 的 Animancer `CrossFade`/`ManualMixerState` 权重混合平滑。→ **缓解**：Animancer 的 `Play` 本身就支持过渡配置（`FadeDuration`），在 `PlayerAnimConfig` 中预配置过渡时间；必要的话在第 7 轮加入状态间混合逻辑。
- **[权衡] 两套系统并存期**：P0-P4 期间老 TPC 和新 FrameSync ECS 并存，需要确保两套不互相干扰。→ **缓解**：每轮独立可编译验证；`PlayerFrameSyncEntry` 只在挂载它的 GameObject 上生效，不影响挂 `Player.cs` 的老角色。
- **[权衡] 当前是单机本地验证路径**：`PlayerInputComponent` 为单例、World SyncRule=Frame 但无网络输入源。→ **缓解**：设计上预留扩展点——输入组件后续可替换为按玩家 ID 的 `CommandComponent`/`PlayerCommandBase`，接 `PursueMsg` 驱动回滚，不改移动/状态系统的核心逻辑。

## Open Questions

1. **碰撞几何数据来源**：项目是否有导出的确定性场景几何（网格/导航网格/格子）？若无，第 4 轮碰撞降级为"AABB 包围盒近似"是否可接受？
2. **攀爬动画曲线提取**：老 TPC 攀爬/Vault 动画的曲线数据在哪里（Animancer `ClipTransition` 里？单独的曲线 SO？），如何批量提取关键位移量？
3. **手感打磨的验收标准**：第 7 轮"手感与老 TPC 一致"如何量化评判？是靠主观体验还是有具体的参数指标（如转向角速度、加速曲线）？
