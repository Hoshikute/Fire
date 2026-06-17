## Why

当前 Fire 客户端和 Server LockStepDemo 的帧同步组件模型不一致：客户端已经使用 `PlayerMoveComponent`、`PlayerStateComponent`、`PlayerCommandRecordComponent` 等新模型，而服务端仍保留 `MoveComponent`、`TransfromComponent` 等旧模型。这样会导致服务端即使能发送 `StartSyncMsg` 和实体同步消息，客户端也无法稳定反序列化并应用已有实体状态，后进玩家无法可靠进入同一个 `BattleWorld`。

本变更采用方案 A：统一组件模型。服务端和客户端共享同一套确定性 ECS 组件契约，再通过入场快照让新玩家在 `StartSyncMsg` 生效前拿到已有实体和单例状态。

## What Changes

- **BREAKING**：服务端帧同步世界的确定性组件改为与客户端 `FrameSync/GameLogic/Component` 对齐，旧的服务端 `MoveComponent` / `TransfromComponent` 等仅保留兼容迁移或移除出同步协议。
- 新增“统一帧同步组件模型”能力，定义哪些组件可跨端同步、哪些组件只能留在 server-only/client-only 层。
- 新增“入场快照”规则：玩家登录后直接加入服务端唯一 `BattleWorld`，服务端先下发已有实体/单例状态，再允许客户端应用 `StartSyncMsg` 启动逻辑帧。
- 客户端联网模式不再把本地预创建实体当作权威实体；必须以服务端实体 ID 创建或绑定 `SelfComponent` 实体。
- 后进玩家加入时，必须收到已有玩家、已有可同步实体、当前帧号、实体索引、逻辑帧间隔、提前帧等状态，且乱序收到 `StartSyncMsg` 时要缓存等待快照完成。
- 对齐协议生成物，使 `SyncEntityMsg`、`ChangeSingletonComponentMsg`、`StartSyncMsg` 和确定性组件字段在 server/client 两端保持一致。

## Capabilities

### New Capabilities

- `frame-sync-component-model`: 定义 server/client 共用的确定性帧同步组件模型、入场快照、实体 ID 绑定和 UDP 乱序保护规则。

### Modified Capabilities

- `battle-world`: 联网模式下 `BattleWorld` 的启动入口改为等待服务端入场快照和 `StartSyncMsg`，不再直接创建本地权威 ECS 玩家实体。

## Impact

- `Server/LockStepDemo/SyncGameLogic/Component/`：服务端确定性组件字段和命名需要与客户端对齐。
- `Server/LockStepDemo/Service/ServiceLogic/System/ServiceSyncSystem.cs`：玩家加入时需要形成可重放的入场快照，并处理快照发送/确认或重发。
- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/FrameSync/`：客户端需要处理 `SyncEntityMsg`、单例同步、快照完成门禁、`SelfComponent` 绑定和 `StartSyncMsg` 缓存。
- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Context/BattleContext.cs`：联网模式的实体创建和世界启动时机需要调整为服务端权威。
- `Server/LockStepDemo/Network/ProtocolInfo.txt`、`UnityProject/Assets/Resources/Protocol/ProtocolInfo.txt`、`UnityProject/Assets/Resources/Protocol/MethodInfo.txt`：协议定义和生成物需要同步更新。
- 与 `replace-client-authority-with-server-frame-authority` 协同：该变更负责“服务端帧命令权威”，本变更补齐“服务端实体状态权威和后进玩家快照”。
