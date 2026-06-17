## ADDED Requirements

### Requirement: 跨端帧同步组件模型必须统一

系统 SHALL 为 server/client 定义同一套确定性帧同步组件模型。任何进入实体快照、权威命令、回滚或服务端逻辑帧计算的 shared 组件，SHALL 在两端使用相同组件名、相同字段语义和兼容的序列化格式。

#### Scenario: 服务端实体快照可被客户端按同名组件应用
- **WHEN** 服务端在入场快照中发送包含 `PlayerMoveComponent`、`PlayerStateComponent` 和 `PlayerComponent` 的实体
- **THEN** 客户端 SHALL 按相同组件名反序列化并创建对应组件
- **AND** 客户端实体中的确定性字段 SHALL 与服务端快照字段一致

#### Scenario: 旧服务端移动组件不得作为新快照权威状态
- **WHEN** 服务端准备向 Fire 客户端发送玩家实体快照
- **THEN** 快照 MUST 使用统一模型中的 `PlayerMoveComponent`
- **AND** 快照 MUST NOT 使用旧的 `MoveComponent` 或 `TransfromComponent` 作为客户端权威移动状态

### Requirement: 组件同步边界必须区分 shared、server-only 和 client-only

系统 SHALL 明确定义组件同步边界。shared 组件只能包含确定性数据；server-only 组件不得下发给客户端；client-only 组件不得进入服务端权威状态或入场快照。

#### Scenario: 服务端连接组件不进入客户端实体快照
- **WHEN** 服务端构建某个玩家实体的快照
- **THEN** 快照 MUST NOT 包含 `ConnectionComponent`、`SyncComponent`、Session 或发送队列字段
- **AND** 客户端 SHALL 只通过 `SelfComponent`、`TheirComponent` 或 owner 信息判断实体归属

#### Scenario: 客户端表现组件不进入服务端权威逻辑
- **WHEN** 客户端给本地玩家绑定 `PlayerViewComponent`、Animancer、相机或 Prefab 实例
- **THEN** 这些 client-only 状态 MUST NOT 被序列化到 `SyncEntityMsg`、入场快照或服务端回滚组件中

### Requirement: PlayerComponent 的本地性必须由归属标记派生

`PlayerComponent` 中的玩家身份字段 SHALL 可跨端同步；客户端相对字段，例如 `isLocal`，MUST NOT 作为服务端权威值传播。客户端 SHALL 根据 `SelfComponent`、`TheirComponent` 或等价 owner 标记派生本地性。

#### Scenario: 同一服务端玩家在不同客户端具有不同本地性
- **WHEN** 服务端向玩家 A 和玩家 B 分别发送同一个玩家实体 E 的快照
- **THEN** 只有拥有 E 的客户端 SHALL 将 E 识别为本地玩家
- **AND** 其他客户端 SHALL 将 E 识别为远端玩家
- **AND** 该判断 MUST NOT 依赖服务端快照中固定的 `PlayerComponent.isLocal` 值

### Requirement: 登录玩家必须直接加入服务端唯一 BattleWorld

系统 SHALL 在登录成功后把玩家直接加入服务端唯一 `BattleWorld`，不依赖匹配人数、房间凑齐或多世界选择。若 singleton `BattleWorld` 尚不存在，服务端 SHALL 创建它；若已经存在，服务端 SHALL 将新玩家加入已有世界。

#### Scenario: 第一名玩家登录创建服务端世界
- **WHEN** 服务端收到第一名玩家登录成功事件
- **THEN** 服务端 SHALL 创建 singleton `BattleWorld`
- **AND** 服务端 SHALL 为该玩家创建带有统一组件模型的玩家实体
- **AND** 服务端 SHALL 准备该玩家的入场快照

#### Scenario: 后进玩家加入已有世界
- **WHEN** 服务端 `BattleWorld` 已经存在且已有玩家实体
- **AND** 新玩家登录成功
- **THEN** 服务端 SHALL 将新玩家加入同一个 `BattleWorld`
- **AND** 服务端 SHALL 为新玩家的入场快照包含已有玩家实体状态

### Requirement: 入场快照必须包含起跑所需的完整状态

服务端 SHALL 在玩家加入时发送入场快照。入场快照 MUST 包含 `snapshotId`、`snapshotFrame`、`selfEntityId`、`createEntityIndex`、`intervalTime`、`advanceCount`、已有可同步实体、必要单例组件和快照完成标记。

#### Scenario: 后进玩家收到已有实体状态
- **WHEN** 玩家 B 在玩家 A 已经位于 `BattleWorld` 后登录
- **THEN** 服务端发送给玩家 B 的入场快照 SHALL 包含玩家 A 的实体 ID
- **AND** 快照 SHALL 包含玩家 A 的 shared 组件状态
- **AND** 玩家 B 客户端在启动逻辑帧前 SHALL 创建或更新玩家 A 对应的本地 ECS 实体

#### Scenario: 快照提供客户端起跑配置
- **WHEN** 客户端收到入场快照
- **THEN** 客户端 SHALL 从快照读取 `snapshotFrame`、`selfEntityId`、`createEntityIndex`、`intervalTime` 和 `advanceCount`
- **AND** 客户端 SHALL 在应用 `StartSyncMsg` 前保存这些配置用于世界初始化校验

### Requirement: 客户端必须先应用快照再启动联网 BattleWorld

联网模式下客户端 `BattleWorld` MUST 在入场快照应用完成且 `StartSyncMsg` 可用后才设置 `IsStart = true`。若 `StartSyncMsg` 先到，客户端 SHALL 缓存该消息并等待快照完成。

#### Scenario: StartSyncMsg 先于快照到达
- **WHEN** 客户端先收到 `StartSyncMsg`
- **AND** 对应入场快照尚未应用完成
- **THEN** 客户端 MUST NOT 启动 `BattleWorld`
- **AND** 客户端 SHALL 缓存 `StartSyncMsg`

#### Scenario: 快照和 StartSyncMsg 都准备完成后启动
- **WHEN** 客户端已经应用完整入场快照
- **AND** 客户端已经收到兼容的 `StartSyncMsg`
- **THEN** 客户端 SHALL 设置 `BattleWorld.FrameCount`、`EntityIndex`、`SyncRule`、`IntervalTime` 和 `aheadFrame`
- **AND** 客户端 SHALL 设置 `BattleWorld.IsStart = true`

### Requirement: 客户端实体 ID 必须以服务端为准

客户端 SHALL 使用服务端快照中的 `EntityInfo.id` 创建或更新 ECS 实体。联网模式下，本地输入、命令记录、相机和表现绑定 MUST 绑定到服务端标记为 Self 的实体 ID。

#### Scenario: Self 实体绑定本地输入和表现
- **WHEN** 客户端在入场快照中收到带 `SelfComponent` 的实体 E
- **THEN** 客户端 SHALL 把本地玩家输入绑定到 E 的 `PlayerCommandRecordComponent`
- **AND** 客户端 SHALL 将已加载的本地玩家表现对象绑定到 E
- **AND** 后续上行 `CommandComponent.id` MUST 等于 E 的实体 ID

#### Scenario: 权威命令必须能找到服务端实体 ID
- **WHEN** 客户端收到服务端下发的 `CommandComponent(id=E)`
- **THEN** 客户端 SHALL 使用 E 查找本地 ECS 实体
- **AND** 若 E 不存在，客户端 MUST 记录错误并触发重同步保护，不得静默丢弃后继续推进世界

### Requirement: 入场快照在 UDP 下必须有可靠性保护

系统 SHALL 为入场快照提供 `snapshotId` 级别的 ACK、重发或等价可靠性保护。客户端重复收到相同 `snapshotId` 的快照片段时 SHALL 幂等处理；服务端在收到 ACK 前 SHALL 保留重发能力。

#### Scenario: 快照丢包后服务端重发
- **WHEN** 服务端向新玩家发送入场快照
- **AND** 服务端在超时时间内未收到该 `snapshotId` 的 ACK
- **THEN** 服务端 SHALL 重发快照或重新生成兼容快照
- **AND** 服务端 MUST NOT 假定该客户端已经可以安全起跑

#### Scenario: 客户端重复收到同一快照
- **WHEN** 客户端已经应用 `snapshotId = S` 的完整快照
- **AND** 客户端再次收到同一 `snapshotId` 的快照数据
- **THEN** 客户端 SHALL 幂等忽略或覆盖为相同状态
- **AND** 客户端 MUST NOT 创建重复实体

### Requirement: 协议定义和生成物必须跨端一致

系统 SHALL 同步更新 server/client 两端协议定义和生成物，使 `SyncEntityMsg`、`ChangeSingletonComponentMsg`、`StartSyncMsg`、快照 ACK/重同步消息以及 shared 组件字段保持一致。

#### Scenario: 协议字段不一致时阻止联网启动
- **WHEN** 服务端协议定义和客户端协议定义不一致
- **THEN** SceneLauncher 或联网初始化流程 SHALL 报告协议不一致
- **AND** 客户端 MUST NOT 自动进入 Play Mode 或启动联网 `BattleWorld`

#### Scenario: 生成物包含快照所需字段
- **WHEN** 协议生成完成
- **THEN** server/client 生成物 SHALL 同时包含入场快照所需的 `snapshotId`、`snapshotFrame`、`selfEntityId`、实体列表和单例组件字段

### Requirement: 统一组件模型必须可验证

系统 SHALL 提供自动化或可重复的验证覆盖组件字段一致性、快照应用、后进玩家、StartSync 乱序、ACK/重发和实体 ID 绑定。

#### Scenario: 后进玩家快照验证
- **WHEN** 测试中玩家 A 已经进入服务端 `BattleWorld`
- **AND** 玩家 B 随后登录
- **THEN** 玩家 B 客户端 SHALL 在启动逻辑帧前拥有玩家 A 的实体和 shared 组件状态

#### Scenario: 乱序启动验证
- **WHEN** 测试中客户端先收到 `StartSyncMsg` 后收到完整入场快照
- **THEN** 客户端 SHALL 在快照完成前保持 `BattleWorld.IsStart = false`
- **AND** 快照完成后 SHALL 使用缓存的 `StartSyncMsg` 启动世界
