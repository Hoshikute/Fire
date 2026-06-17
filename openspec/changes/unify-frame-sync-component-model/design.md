## Context

Fire 当前客户端帧同步已经有 `BattleWorld`、`WorldBase` 回滚、`PlayerMoveComponent`、`PlayerStateComponent`、`PlayerCommandRecordComponent` 和 `FrameAuthoritySyncSystem`。联网模式下 `BattleContext` 已经会在创建世界后等待 `StartSyncMsg`，但它仍提前用本地 ID 创建权威玩家实体。

服务端 `ServiceSyncSystem` 已经有玩家加入时同步已有实体的雏形：`OnPlayerJoin` 会把新连接加入所有实体的同步队列，`EndFrame` 先 `PushAllData()` 再 `PushStartSyncMsg()`。问题在于 server/client 的确定性组件模型不一致，客户端也没有完整应用 `SyncEntityMsg`、单例组件和 `SelfComponent`/`TheirComponent` 的流程。结果是服务端可以发消息，但客户端无法可靠生成同一份实体状态，后进玩家也无法进入已有世界。

本设计是 `replace-client-authority-with-server-frame-authority` 的配套补齐：前者解决“命令谁说了算”，本变更解决“实体状态和组件模型谁说了算”。本项目不引入匹配流程，玩家登录后直接加入服务端唯一 `BattleWorld`。

## Goals / Non-Goals

**Goals:**

- 采用方案 A：server/client 使用同一套确定性帧同步组件模型，不在网络层做字段适配转换。
- 服务端维护唯一 `BattleWorld`，玩家登录后直接加入该世界，后续玩家也进入同一个世界。
- 后进玩家在启动逻辑帧前收到已有实体、已有单例、当前帧号、实体索引、逻辑帧间隔、提前帧和自身实体 ID。
- 客户端以服务端实体 ID 创建或更新 ECS 实体，并用 `SelfComponent` 绑定本地输入、相机和表现对象。
- UDP 通信下必须处理快照和 `StartSyncMsg` 的乱序、丢包和重复到达。
- 离线/本地调试模式继续允许 `BattleContext` 本地创建实体并立即启动。

**Non-Goals:**

- 不引入匹配、房间、选角、队伍或多 BattleWorld 调度。
- 不同步 Unity `Transform`、`Vector3`、`Animancer`、相机、Prefab 或任何表现层引用。
- 不重写现有回滚、状态机、移动碰撞和动画表现系统。
- 不做兴趣管理、区域裁剪或大规模实体分片优化；第一阶段以单 BattleWorld 正确性为目标。
- 不把 server-only 的连接队列、Session、Socket 状态暴露给客户端。

## Decisions

### Decision: 以客户端 FrameSync 确定性组件作为共享模型基准

共享模型以客户端 `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/FrameSync/GameLogic/Component/` 下已经服务于回滚的组件为基准，服务端 `Server/LockStepDemo/SyncGameLogic/Component/` 向它对齐。

跨端可同步组件只允许包含确定性字段：

- `CommandComponent`：帧命令字段，包括 `moveDir`、`skillDir`、`jump`、`toggleLock`、`platformJump`、`speedGear` 等。
- `PlayerMoveComponent`：位置、朝向、移动意图、速度、接地、胶囊尺寸等定点状态。
- `PlayerStateComponent`：逻辑状态枚举、状态帧计数、锁定/交互等确定性状态。
- `PlayerComponent`：玩家身份字段。`isLocal` 这类客户端相对字段不得作为服务端权威内容传播，客户端应由 `SelfComponent`/`TheirComponent` 或 owner 信息派生。
- `PlayerCommandRecordComponent`：server/client 都可以持有命令记录能力，但入场快照只初始化当前实体所需的默认命令/必要缓存，不把历史命令队列当作可长期同步状态。

server-only 组件包括 `ConnectionComponent`、`SyncComponent`、`ServiceComponent` 及所有 Session/发送队列。client-only 组件包括 `PlayerViewComponent`、`PlayerInputComponent`、Animancer、相机、Prefab 实例和资源加载状态。

备选方案是保留服务端旧组件并在网络层做适配。放弃该方案，因为它会让快照反序列化、回滚字段和服务端命令计算长期分裂，后续每新增一个玩家状态都要维护双份映射。

### Decision: 登录直接加入服务端唯一 BattleWorld

服务端启动后维护一个单例 `BattleWorld`。第一名玩家登录时创建世界并开始服务端逻辑帧；后续玩家登录时直接加入同一个世界。加入流程只负责创建连接实体、玩家实体和必要组件，不等待匹配人数。

推荐流程：

1. `LoginService` 或等价入口接收登录成功。
2. `WorldManager` 获取或创建 singleton `BattleWorld`。
3. 服务端用统一组件模型创建玩家实体，并绑定 `ConnectionComponent`、`SyncComponent`、`CommandComponent` 默认输入。
4. `session.m_connect` 指向该连接实体。
5. 派发 `ServiceEventDefine.c_playerJoin`，由同步系统准备入场快照。

备选方案是保留 MatchService 的两人匹配后启动。放弃该方案，因为当前 Fire 验证目标是直接进入 Game/BattleWorld，匹配流程会让单客户端调试永远等不到 `StartSyncMsg`。

### Decision: 入场快照必须先于逻辑启动完成

入场快照是一个逻辑阶段，不强制绑定到单个协议类。第一阶段可以复用 `SyncEntityMsg` 和 `ChangeSingletonComponentMsg` 的实体/组件载荷，但必须补充快照元信息：`snapshotId`、`snapshotFrame`、`selfEntityId`、`createEntityIndex`、`intervalTime`、`advanceCount` 和完成标记。若单包过大，可按 `snapshotId + sequence/total` 分片。

客户端只有在满足以下条件后才能让联网 `BattleWorld.IsStart = true`：

- 已应用同一个 `snapshotId` 的全部实体和单例状态。
- 已知道 `selfEntityId` 并完成本地表现对象绑定。
- 已收到或缓存了与该快照兼容的 `StartSyncMsg`。

如果 `StartSyncMsg` 先到，客户端缓存它，不启动世界。如果快照重复到达，客户端按 `snapshotId` 幂等处理。

备选方案是依赖当前服务端 `PushAllData()` 先于 `PushStartSyncMsg()` 的发送顺序。放弃该方案，因为 UDP 不保证顺序和可靠到达，发送顺序不能成为启动正确性的依据。

### Decision: 客户端以服务端实体 ID 创建和绑定实体

联网模式下，客户端不得把 `LOCAL_PLAYER_ENTITY_KEY.ToHash()` 创建的本地实体当作权威实体。入场快照应用系统应按服务端 `EntityInfo.id` 创建或更新实体。收到 `SelfComponent` 的实体才是本地玩家实体，并在该实体上补充 client-only 的 `PlayerViewComponent`、相机跟随和输入绑定。

`TheirComponent` 标记的实体只创建逻辑实体和远端表现绑定，不读取本地 `PlayerInputComponent`。后续服务端下发的 `CommandComponent.id` 必须能在客户端用相同实体 ID 找到 `PlayerCommandRecordComponent`，否则应记录错误并阻止世界启动或触发重同步。

备选方案是先本地创建实体，再在快照到达时重映射 ID。放弃该方案作为第一阶段路径，因为它容易污染快照、命令缓存和相机绑定，回滚历史也更难维护。

### Decision: 快照可靠性使用 ACK/重发而不是假设一次成功

服务端为每个加入中的连接维护快照等待状态。发送快照后，在收到 `JoinSnapshotAck(snapshotId, selfEntityId)` 前，不应认为该客户端已经可安全起跑。若超时未确认，服务端重发快照或重新生成同一帧附近的快照。

客户端应用快照成功后发送 ACK；如果发现缺少组件、未知组件、反序列化失败、缺少 `SelfComponent` 或 `StartSyncMsg` 与快照帧不兼容，应发送失败/重同步请求或断开保护，不能启动世界。

备选方案是只依赖后续 `SyncEntityMsg` 自然补齐。放弃该方案，因为后进玩家第一帧就需要完整已有状态，否则会出现 T-Pose、无实体命令记录、相机绑定空对象或实体 ID 不一致。

### Decision: 协议生成物必须从同一契约同步更新

服务端和客户端协议定义必须同时包含统一组件字段、快照元信息、单例同步、`SyncEntityMsg`、`ChangeSingletonComponentMsg`、`StartSyncMsg` 和 ACK/重同步消息。协议生成后需要提交 server/client 两侧生成物，避免一个端新增字段另一个端仍按旧结构解析。

备选方案是手写临时解析兼容。放弃该方案，因为已有报错已经说明协议漂移会直接阻断 SceneLauncher 启动流程。

## Risks / Trade-offs

- [Risk] 旧服务端组件和新客户端组件字段不能一一对应，迁移时可能丢状态。Mitigation: 先列出组件字段对照表，缺失字段必须补齐或明确不进同步。
- [Risk] `PlayerComponent.isLocal` 是客户端相对字段，若从服务端原样同步会让不同客户端状态冲突。Mitigation: 通过 `SelfComponent`/`TheirComponent` 派生本地性，服务端权威快照不传播客户端相对值。
- [Risk] 入场快照过大导致 UDP 分片或丢包。Mitigation: 快照支持 `snapshotId`、序号、完成标记和 ACK/重发；必要时限制第一阶段同步实体数量。
- [Risk] `StartSyncMsg` 和快照帧不一致会导致客户端从错误帧起跑。Mitigation: 客户端校验 `snapshotFrame <= start.frame`，不兼容则请求重同步。
- [Risk] 快照应用期间表现对象已经加载，但逻辑实体还不存在，会短暂 T-Pose。Mitigation: 表现对象加载后保持待绑定状态，只有 Self 实体和 `PlayerViewComponent` 绑定完成后才允许逻辑和动画系统推进。
- [Risk] 与服务端帧命令权威变更互相依赖。Mitigation: 本变更先保证实体 ID、组件和快照一致，再让命令权威系统按相同实体 ID 写入 `PlayerCommandRecordComponent`。

## Migration Plan

1. 梳理 client/server 当前帧同步组件，输出共享组件字段清单，标记 shared、server-only、client-only。
2. 将服务端 `SyncGameLogic` 组件迁移到共享模型，确保序列化名称和字段与客户端一致。
3. 调整服务端登录入口，绕过匹配流程，登录成功后直接加入 singleton `BattleWorld`。
4. 扩展同步协议和生成物，补齐快照元信息、ACK/重发和必要组件字段。
5. 实现客户端快照应用系统，支持 `SyncEntityMsg`、单例同步、Self/Their 绑定和 StartSync 缓存。
6. 调整 `BattleContext`：联网模式只加载表现对象和创建空世界，不预创建权威 ECS 玩家实体。
7. 补齐自动化测试和手动验证：单人直连、后进玩家、StartSync 乱序、快照重发、服务端命令按实体 ID 应用。
8. 回退策略：保留离线/本地调试启动路径；若联网快照失败，客户端保持未启动状态并输出可搜索错误，不回退为本地权威。

## Open Questions

- 快照第一阶段采用单条 `JoinWorldSnapshotMsg` 还是在现有 `SyncEntityMsg` / `ChangeSingletonComponentMsg` 上增加 `snapshotId` 和完成标记，需要结合协议生成器支持度决定。
- `PlayerCommandRecordComponent` 的历史缓存是否需要在断线重连时同步，不影响本次普通后进玩家入场，但会影响未来重连。
- 服务端 singleton `BattleWorld` 的生命周期何时销毁：所有玩家离开后立即销毁，还是保留空世界等待下一名玩家。
