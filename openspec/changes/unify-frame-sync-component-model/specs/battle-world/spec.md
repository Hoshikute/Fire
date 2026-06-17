## ADDED Requirements

### Requirement: 联网模式 BattleWorld 必须等待服务端入场状态

联网模式下，`BattleContext` SHALL 只负责加载场景表现对象、创建空的本地 `BattleWorld` 容器和准备相机/资源绑定；确定性 ECS 玩家实体 MUST 来自服务端入场快照。离线或本地调试模式 SHALL 保留本地创建实体并立即启动的能力。

#### Scenario: 联网模式不立即启动逻辑帧
- **WHEN** `BattleContext.InitializeGameScene()` 在网络已连接状态下完成 Player 表现对象加载
- **THEN** 客户端 SHALL 创建或准备 `BattleWorld`
- **AND** 客户端 MUST NOT 因本地表现对象加载完成而设置 `BattleWorld.IsStart = true`
- **AND** 客户端 SHALL 等待服务端入场快照和 `StartSyncMsg`

#### Scenario: 本地调试模式继续直接启动
- **WHEN** `BattleContext.InitializeGameScene()` 在网络未连接的本地调试模式下完成 Player 表现对象加载
- **THEN** 客户端 MAY 按本地调试流程创建本地玩家 ECS 实体
- **AND** 客户端 SHALL 允许 `BattleWorld.IsStart = true`

### Requirement: 联网模式不得预创建权威本地玩家实体

联网模式下，`BattleContext` MUST NOT 使用本地固定 ID 创建权威玩家 ECS 实体。客户端 SHALL 等待快照中的 `SelfComponent` 实体，再把已加载的 Player 表现对象和相机绑定到该服务端实体。

#### Scenario: Self 快照实体成为本地玩家
- **WHEN** 客户端收到入场快照中的实体 E
- **AND** E 包含 `SelfComponent`
- **THEN** 客户端 SHALL 将 E 设置为本地玩家实体
- **AND** 客户端 SHALL 为 E 补充 client-only 的表现绑定
- **AND** 客户端后续上行命令 SHALL 使用 E 的实体 ID

#### Scenario: 未收到 Self 实体不得启动
- **WHEN** 客户端收到的入场快照不包含任何 `SelfComponent` 实体
- **THEN** 客户端 MUST NOT 启动联网 `BattleWorld`
- **AND** 客户端 SHALL 请求重同步或报告可定位错误

### Requirement: BattleWorld 启动配置必须由快照和 StartSyncMsg 共同确认

联网模式下，`BattleWorld` 起跑帧、实体索引、同步规则、逻辑帧间隔和提前帧 SHALL 来自服务端入场快照与 `StartSyncMsg` 的一致结果。若两者不兼容，客户端 MUST 拒绝启动并触发重同步保护。

#### Scenario: 快照帧和 StartSyncMsg 兼容
- **WHEN** 客户端已应用 `snapshotFrame = F` 的完整入场快照
- **AND** 客户端收到 `StartSyncMsg(frame = S)` 且 S 大于或等于 F
- **THEN** 客户端 SHALL 用服务端配置初始化 `BattleWorld`
- **AND** 客户端 SHALL 启动联网逻辑帧

#### Scenario: 快照帧和 StartSyncMsg 不兼容
- **WHEN** 客户端已应用的入场快照与收到的 `StartSyncMsg` 不属于同一次加入流程或帧号不兼容
- **THEN** 客户端 MUST NOT 启动联网 `BattleWorld`
- **AND** 客户端 SHALL 丢弃该启动组合并请求重同步
