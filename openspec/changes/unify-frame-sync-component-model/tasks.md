## 1. 组件模型梳理

- [x] 1.1 梳理 client/server 当前帧同步组件字段，列出 shared、server-only、client-only 分类。
- [x] 1.2 明确 shared 组件清单：`CommandComponent`、`PlayerComponent`、`PlayerMoveComponent`、`PlayerStateComponent`、必要的命令记录初始化字段。
- [x] 1.3 明确 `PlayerComponent.isLocal` 的迁移规则，改为由 `SelfComponent` / `TheirComponent` 或 owner 标记在客户端派生。

## 2. 服务端统一组件模型

- [x] 2.1 将 `Server/LockStepDemo/SyncGameLogic/Component/` 下的玩家移动、状态、命令组件字段对齐客户端 FrameSync 模型。
- [x] 2.2 从服务端同步快照中移除或隔离旧 `MoveComponent`、`TransfromComponent` 等不兼容权威状态。
- [x] 2.3 确保服务端 shared 组件序列化名称、字段类型和默认值与客户端一致。
- [x] 2.4 补齐服务端创建玩家实体时的默认 `CommandComponent` / 命令记录初始化，保证缺输入时可生成预测命令。

## 3. 服务端单例 BattleWorld 与入场快照

- [x] 3.1 调整登录成功流程，绕过匹配人数限制，直接获取或创建服务端 singleton `BattleWorld`。
- [x] 3.2 为登录玩家在 singleton `BattleWorld` 中创建服务端权威玩家实体，并绑定连接、同步和玩家 shared 组件。
- [x] 3.3 扩展 `ServiceSyncSystem` 的玩家加入流程，为新连接生成带 `snapshotId`、`snapshotFrame`、`selfEntityId`、`createEntityIndex`、`intervalTime`、`advanceCount` 的入场快照。
- [x] 3.4 入场快照必须包含已有可同步实体、必要单例组件，以及针对接收方的 `SelfComponent` / `TheirComponent` 标记。
- [x] 3.5 增加快照 ACK、超时重发或等价可靠性状态，服务端在确认前不得假定客户端已安全起跑。

## 4. 客户端快照应用与启动门禁

- [x] 4.1 扩展客户端协议读取层，支持 `SyncEntityMsg`、`ChangeSingletonComponentMsg`、快照元信息、ACK 和重同步失败路径。
- [x] 4.2 新增或扩展客户端快照应用系统，按服务端 `EntityInfo.id` 创建或更新 ECS 实体并反序列化 shared 组件。
- [x] 4.3 处理 `SelfComponent` / `TheirComponent`，将本地输入、`PlayerCommandRecordComponent`、相机和已加载 Player 表现对象绑定到 Self 实体。
- [x] 4.4 缓存先到达的 `StartSyncMsg`，只有入场快照完整应用且启动配置兼容时才设置 `BattleWorld.IsStart = true`。
- [x] 4.5 当缺少 Self 实体、未知 shared 组件、反序列化失败或实体 ID 找不到时，阻止启动并输出可搜索错误。

## 5. BattleContext 联网启动调整

- [x] 5.1 将 `BattleContext` 的离线本地调试路径和联网路径拆开，离线继续本地创建实体并立即启动。
- [x] 5.2 联网模式下 `BattleContext` 只加载表现对象和创建空世界容器，不再用本地固定 ID 创建权威 ECS 玩家实体。
- [x] 5.3 在 Self 实体绑定完成后再补充 `PlayerViewComponent`、相机跟随和动画表现启动所需引用。

## 6. 协议定义与生成物同步

- [x] 6.1 更新 server/client 两端 `ProtocolInfo.txt`、`MethodInfo.txt` 或等价源定义，补齐快照、ACK、单例同步和 shared 组件字段。
- [x] 6.2 重新生成并提交 server/client 协议生成物，确保两端字段、消息 ID 和协议一致性检查全部通过。
- [x] 6.3 确认 SceneLauncher 的 server 环境检查能识别新的 UDP 协议配置，并在协议漂移时继续阻止自动 Play Mode。

## 7. 验证

- [x] 7.1 增加组件字段一致性和快照反序列化测试，覆盖 `PlayerMoveComponent`、`PlayerStateComponent`、`PlayerComponent` 和 `CommandComponent`。
- [x] 7.2 增加入场快照测试：玩家 A 已在世界中时，玩家 B 后进后必须在启动前拥有玩家 A 的实体状态。
- [ ] 7.3 增加乱序测试：`StartSyncMsg` 先于快照到达时客户端不得启动，快照完成后再使用缓存消息启动。
- [ ] 7.4 增加快照 ACK/重发或等价可靠性验证，覆盖重复快照不创建重复实体。
- [ ] 7.5 运行 `openspec validate unify-frame-sync-component-model --strict`，并执行本变更涉及的 server/client 自动化测试或手动验证记录。
