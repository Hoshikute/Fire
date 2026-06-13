## Context

当前 `Game` 场景进入后，`TPBattleContext.InitializeGameScene()` 负责相机初始化和 Player prefab 加载；Player 加载完成后再运行时补挂 `PlayerFrameSyncEntry`。`PlayerFrameSyncEntry.Start()` 随后创建 `PlayerWorld`、生成本地玩家实体、刷新实体操作、启动 world，并绑定相机。

这条链路能跑通，但职责边界不清晰：场景初始化在 `TPBattleContext`，FrameSync world 启动在 MonoBehaviour `Start()`，角色表现依赖的 `AnimancerComponent`、`PlayerAnimConfig` 和相机 LookAt 又跨两个入口传递。当前资产中也没有发现 `PlayerFrameSyncEntry` 作为脚本组件被 `Game.unity` 或 Player prefab 直接序列化引用，它更像一个运行时过渡桥接层。

`PlayerWorld` 本身仍然是 ECS 世界定义：它声明系统顺序、需要快照记录的组件类型，以及进入 world 时的初始化日志。它不应该承担场景加载、prefab 查找、相机绑定或生命周期清理职责。

## Goals / Non-Goals

**Goals:**

- 让 `TPBattleContext` 统一负责编排 Player prefab 加载、Player FrameSync world 创建、本地玩家实体生成、相机绑定和 world 清理。
- 移除 `TPBattleContext` 对 `AddComponent<PlayerFrameSyncEntry>()` 和 MonoBehaviour `Start()` 时序的依赖。
- 保持 `PlayerWorld` 的职责为 ECS world 定义和系统注册，不把场景管理代码移动进 `PlayerWorld`。
- 明确 `PlayerViewComponent` 所需的 `Transform`、`AnimancerComponent`、`PlayerAnimConfig` 注入路径，避免 animation config 为 null 时静默跳过动画。
- 保持 `FrameSyncModule` 固定 200ms 逻辑帧驱动和现有 `CharacterModule.SetCharacterPrefab("Player")` / `LoadCharacterAsync()` 动态加载路径不变。

**Non-Goals:**

- 不重做帧同步 ECS 架构、预测回滚、输入采样或网络同步协议。
- 不修改 `FrameSyncModule.Update()` / `WorldBase.Loop()` 的时间推进语义。
- 不重写 Player 状态机、移动系统或 Animancer transition 资产。
- 不把 `TPBattleContext` 改造成通用框架模块；本变更只收敛 `Game` 场景 Player startup 的职责边界。

## Decisions

### 决策一：`TPBattleContext` 拥有 Player FrameSync world 生命周期

`TPBattleContext` 已经是 `Game` 场景初始化入口，因此它应在 Player prefab 加载完成后直接创建 `PlayerWorld`、生成本地玩家实体并启动 world。对应的 world 引用和本地玩家 entity id 应保存在 `TPBattleContext` 内部，供 shutdown 或场景退出时清理。

替代方案：

- 保留 `PlayerFrameSyncEntry` 作为运行时补挂组件。这个方案改动最少，但仍然让场景入口和 world 启动分裂在两个生命周期系统里。
- 把 `PlayerFrameSyncEntry` 序列化挂到 Player prefab。这个方案减少 `AddComponent`，但仍然依赖 MonoBehaviour `Start()` 顺序，并让 prefab 承担逻辑 world 启动职责。
- 由 `TPBattleContext` 直接拥有 startup。这个方案最贴近现有 `TPBattleContext` 的 GameManager 职责，也让初始化、错误处理和清理路径都可见，因此作为首选。

### 决策二：`PlayerWorld` 保持 simulation-only

`PlayerWorld` 继续只暴露 ECS 系统列表和回滚记录组件列表。它不加载 Player prefab，不查找 camera，不绑定 Cinemachine，不持有 `Transform` 或 `AnimancerComponent`。Unity 表现对象与 ECS view component 的桥接由 `TPBattleContext` 在创建本地玩家实体时完成。

替代方案：

- 把 `SpawnPlayer()` 迁入 `PlayerWorld`。这会让 world 依赖 Unity 表现对象，扩大 `GameLogic` 和表现层耦合。
- 新建单独的 `PlayerFrameSyncBootstrap` 服务。当前只有 `Game` 场景本地玩家启动一条链路，新增服务会提前抽象。
- 在 `TPBattleContext` 内使用私有方法拆分 `CreatePlayerWorld`、`SpawnLocalPlayerEntity`、`BindPlayerCamera`、`CleanupPlayerWorld`。这个方案范围小，边界清楚。

### 决策三：显式处理 `PlayerViewComponent` 依赖

创建本地玩家实体前，`TPBattleContext` 应从已加载的 Player prefab 上解析 `Transform` 和 `AnimancerComponent`，并通过明确路径加载或注入 `PlayerAnimConfig`。如果关键依赖缺失，启动流程应给出搜索友好的错误或警告，不能只让 `PlayerAnimViewSystem` 因 `animConfig == null` 静默跳过。

替代方案：

- 继续依赖 `PlayerFrameSyncEntry.SetAnimConfig()`。当前调用链没有实际调用它，问题会继续隐藏。
- 允许 `animConfig` 长期为 null。world 能启动，但动画状态表现不可验证，调试成本高。
- 在 startup 中把 view 依赖集中解析并注入。这样缺失配置会在场景启动阶段暴露，最容易定位。

### 决策四：shutdown 必须幂等且先清理 world 引用

`TPBattleContext.Shutdown()` 或后续明确的 Game 场景退出流程，应在销毁角色实例前后清理 Player FrameSync world，并将本地引用置空。重复 shutdown、未成功启动 world、角色已提前销毁等情况都不应抛异常或留下 `FrameSyncModule` 残留 world。

替代方案：

- 继续依赖 `PlayerFrameSyncEntry.OnDestroy()`。如果入口组件被移除，world 清理会丢失。
- 在 `CharacterModule.DestroyCharacter()` 内顺手清理 world。CharacterModule 不应知道 FrameSync world 生命周期。
- 在 `TPBattleContext` 维护清理顺序。它已经知道 scene/player/world/camera 的组合关系，职责最匹配。

### 决策五：`PlayerFrameSyncEntry` 作为过渡文件处理

实现时应移除 `TPBattleContext` 对 `PlayerFrameSyncEntry` 的运行时依赖。若确认没有 prefab/scene 仍引用该脚本，可删除 `PlayerFrameSyncEntry.cs` 并同步 `GameLogic.csproj`；若需要短期保留，则应标记为 obsolete 或改成不启动 world 的兼容壳，避免重复创建 `PlayerWorld`。

替代方案：

- 直接删除且不检查资产引用。风险是 Unity 场景或 prefab 留下 Missing Script。
- 永久保留完整 Entry。会让后续维护者误以为它仍是入口。
- 先用 GUID 搜索和 Unity prefab 检查确认引用，再删除或降级为兼容壳。这个方案更稳妥。

## Risks / Trade-offs

- [Risk] `TPBattleContext.InitializeGameScene()` 被重复调用时可能创建多个 `PlayerWorld`。→ Mitigation：startup 前检查已有 world，必要时先执行幂等 cleanup。
- [Risk] shutdown 顺序不当导致 world 系统仍访问已销毁的 Player transform。→ Mitigation：退出时先停止/销毁 world 或让 view component 清空引用，再销毁角色实例。
- [Risk] `PlayerAnimConfig` 当前没有可用资产实例，集中注入后会暴露配置缺口。→ Mitigation：实现任务中要求确认或创建 canonical config asset，并在缺失时输出明确日志。
- [Risk] Player prefab 可能存在其他缺失脚本或历史字段。→ Mitigation：只清理经 GUID 和 Unity 检查确认的 stale script，不顺手重写 prefab。
- [Risk] 移除 Entry 后遗漏 `.knowledge` 文档更新，后续仍按旧入口排查。→ Mitigation：tasks 中把 `.knowledge/modules/` 更新作为独立验收项。
