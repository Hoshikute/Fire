## 为什么

当前 FrameSync 玩家可以正常进入 Game 场景，Player prefab 也已经加载成功，但动画配置没有加载到。运行日志显示 `TPBattleContext` 在检查 `CheckLocationValid("PlayerAnimConfig")` 时失败，随后 `PlayerAnimViewSystem` 因为 `PlayerViewComponent.animConfig == null` 直接跳过动画播放，导致角色虽然逻辑状态在切换，画面上仍停在默认 T-Pose。

这个问题需要现在修复，因为最近的动画路径重构已经把动画配置注入方式从 `PlayerFrameSyncEntry` 的序列化字段改成了 `TPBattleContext` 通过 TEngine `GameModule.Resource` 加载 `PlayerAnimConfig`。代码契约已经切到 TEngine ResourceModule/YooAsset 资源定位链路，但对应的资源资产、DefaultPackage 收集规则验证和加载失败诊断还没有补齐。

## TEngine 规范依据

已对照阅读项目知识库与 TEngine RepoWiki 中的资源规范：`.knowledge/framework/tengine-repowiki.md`、`资源管理/资源管理.md`、`资源管理/YooAsset集成.md`、`资源管理/资源加载机制.md`、`编辑器工具/资源收集器.md`、`开发者指南/最佳实践指南/资源管理最佳实践.md`。本变更按这些规范收敛到现有资源系统：

- 资源必须通过 `ResourceModule`/`GameModule.Resource` 统一加载，不新增绕过 YooAsset 的 `Resources.Load`、AssetDatabase 运行时依赖或手写路径加载。
- 运行时定位地址以 TEngine/YooAsset 的 location 为准；当前 `DefaultPackage` 下 `Assets/AssetRaw/Configs` 已由 collector 使用 `AddressByFileName`、`PackDirectory`、`CollectAll` 收集，因此 `PlayerAnimConfig.asset` 的预期地址就是 `PlayerAnimConfig`。
- 资源可用性验证必须覆盖 `CheckLocationValid("PlayerAnimConfig")` 和 `LoadAssetAsync<PlayerAnimConfig>("PlayerAnimConfig")`，并在地址无效或加载返回空时给出明确日志。
- 动画配置资源属于表现层输入，生命周期由 TEngine 资源模块和对象池/句柄机制管理，不写入 FrameSync 可回滚逻辑组件。

## 变更内容

- 新增一份资源化的 `PlayerAnimConfig` 配置契约，通过默认资源包以地址 `PlayerAnimConfig` 加载。
- 复用现有 `Assets/AssetRaw/Configs` collector，不新增并行资源加载体系；如 collector 规则漂移，优先修复资源配置而不是改运行时代码绕过框架。
- 确保配置至少包含进入游戏和地面移动所需的基础动画映射：Idle、MoveStart 八方向、MoveLoop、MoveEnd、MoveToWall 兜底、LockIdle 兜底。
- 保持逻辑层和表现层分离：动画资源只驱动 Animancer 表现，不反向写入 FrameSync 逻辑组件。
- 补充校验和诊断，让缺失或不完整的动画配置能在 TEngine 资源边界被明确发现，而不是静默表现为 T-Pose。
- 不恢复旧 `ThirdPersonController` 运行链路，也不恢复已删除的 `PlayerFrameSyncEntry` 序列化注入链路。

## 能力

### 新增能力
- `framesync-player-animation-resources`: FrameSync 玩家资源化动画配置能力，覆盖 TEngine/YooAsset 资源地址有效性、基础状态动画映射、资源加载失败诊断，以及动画资源缺失时的运行时诊断。

### 修改能力
- 无。

## 影响范围

- 运行时代码：`Assets/GameScripts/HotFix/GameLogic/Context/TPBattleContext.cs`、`Assets/GameScripts/HotFix/GameLogic/Module/FrameSync/ClientLogic/System/PlayerAnimViewSystem.cs`、`Assets/GameScripts/HotFix/GameLogic/Module/FrameSync/ClientLogic/Config/PlayerAnimConfig.cs`。
- 资源资产：`Assets/AssetRaw/Configs/PlayerAnimConfig.asset` 及其 `.meta`，以及现有 Animancer 过渡资源 `Assets/AssetRaw/ThirdPersonController/AnimancerController/Resources/Config/PlayerAnimacer/`。
- 资源配置：`DefaultPackage` 必须能解析地址 `PlayerAnimConfig`；当前预期链路是 `Assets/AssetRaw/Configs` + `AddressByFileName` + `PackDirectory` + `CollectAll`。
- 验证影响：Game 场景运行验证必须确认不再出现 `PlayerAnimConfig missing` 或 `Could not found location [PlayerAnimConfig]` 错误，并且角色能播放 Idle 和移动动画、脱离 T-Pose。