## Why

`PlayerFrameSyncEntry` 当前作为运行时补挂的 MonoBehaviour 桥接层，负责创建 `PlayerWorld`、生成玩家实体并绑定相机；但 `TPBattleContext` 已经承担 `Game` 场景初始化与 Player 加载的 GameManager 职责。继续把启动逻辑拆在 `PlayerFrameSyncEntry.Start()` 中，会让 Player prefab 配置、动画配置注入、世界销毁和场景生命周期边界变得不清晰。

## What Changes

- 将 Player FrameSync 启动职责集中到 `TPBattleContext`：加载 Player prefab 后直接创建 `PlayerWorld`、生成本地玩家实体、启动 world，并绑定相机。
- 保持 `PlayerWorld` 只作为 ECS 世界定义，继续负责系统注册、快照组件声明和 FrameSync 逻辑循环，不承担场景管理职责。
- 移除或废弃 `PlayerFrameSyncEntry` 作为运行时必需桥接层，避免再依赖 `AddComponent<PlayerFrameSyncEntry>()` 和 MonoBehaviour `Start()` 时序启动 FrameSync。
- 让 `TPBattleContext.Shutdown()` 或明确退出流程清理 PlayerWorld 和角色实例，避免 `FrameSyncModule` 中残留 world。
- 保持现有 `CharacterModule.SetCharacterPrefab("Player")` / `LoadCharacterAsync()` 动态加载路径，以及 `FrameSyncModule` 驱动 world 的机制不变。

## Capabilities

### New Capabilities
- `player-framesync-startup`: 约束 `Game` 场景中 Player FrameSync world、玩家实体、相机绑定和清理流程由 `TPBattleContext` 统一编排。

### Modified Capabilities
- None.

## Impact

- 主要影响 `TPBattleContext`、`PlayerFrameSyncEntry`、`PlayerWorld` 初始化调用边界，以及 `PlayerViewComponent` 所需的 `Transform`、`AnimancerComponent`、`PlayerAnimConfig` 注入路径。
- 需要同步更新 `.knowledge/modules/player-framesync-ecs.md`、`.knowledge/modules/player-controller.md` 等仍描述 `PlayerFrameSyncEntry` 为当前入口的文档，避免知识库继续误导。
- 不改变 `FrameSyncModule` 的固定 200ms 逻辑帧驱动、不改变 `PlayerWorld.GetSystemTypes()` / `GetRecordTypes()` 的运行语义，也不引入新的网络同步协议。
