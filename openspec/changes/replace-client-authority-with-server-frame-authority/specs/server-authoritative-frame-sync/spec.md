## ADDED Requirements

### Requirement: 联网帧同步以服务端帧命令为权威

系统 SHALL 在联网模式下把服务端下发的同帧命令作为确定性逻辑的最终输入来源；客户端本地采集的输入 SHALL 仅作为预测命令和上行数据使用，不得在收到权威命令前被标记为确定帧结果。

#### Scenario: 本地输入先预测但不成为确定帧
- **WHEN** 本地玩家在逻辑帧 F 产生移动或动作输入
- **THEN** 客户端记录一份帧号为 F 的预测命令并上行服务端
- **AND** 客户端不得仅因为本地预测命令存在就把 F 标记为服务端确认帧

#### Scenario: 权威命令覆盖本地预测
- **WHEN** 客户端收到服务端下发的玩家 P 在逻辑帧 F 的权威命令
- **THEN** 客户端 MUST 将该命令写入 P 的 `PlayerCommandRecordComponent`
- **AND** 后续逻辑或重算 SHALL 读取该权威命令而不是旧预测命令

### Requirement: 客户端处理同步开始消息

系统 SHALL 在联网模式下通过 `StartSyncMsg` 启动或校准 `BattleWorld`，并使用消息中的帧号、提前帧数、逻辑帧间隔、实体索引和同步规则作为世界起跑配置。

#### Scenario: 收到 StartSyncMsg 后启动世界
- **WHEN** 客户端收到 `StartSyncMsg(frame=S, advanceCount=A, intervalTime=I, createEntityIndex=E, SyncRule=Frame)`
- **THEN** `BattleWorld.FrameCount` SHALL 设置为 S
- **AND** `GameModule.FrameSync.IntervalTime` SHALL 设置为 I
- **AND** `BattleWorld.EntityIndex` SHALL 设置为 E
- **AND** `ConnectStatusComponent.aheadFrame` SHALL 设置为 A
- **AND** `BattleWorld.SyncRule` SHALL 设置为 `Frame`
- **AND** `BattleWorld.IsStart` SHALL 为 true

#### Scenario: 联网模式等待服务端起跑
- **WHEN** `BattleContext` 已加载玩家表现对象且当前为联网模式
- **THEN** 客户端 SHALL 等待 `StartSyncMsg` 后再推进确定性逻辑帧

### Requirement: 客户端上行预测命令并跟踪确认

系统 SHALL 在本地玩家每个预测逻辑帧生成命令后发送给服务端，并用确认队列跟踪尚未被服务端确认的帧。

#### Scenario: 发送本地预测命令
- **WHEN** 本地玩家在逻辑帧 F 生成预测 `CommandComponent`
- **THEN** 客户端 SHALL 设置命令的 `id`、`frame` 和 `time`
- **AND** 客户端 SHALL 通过网络模块发送该命令或等价 `CommandMsg`
- **AND** `ConnectStatusComponent.unConfirmFrame` SHALL 包含 F

#### Scenario: 收到确认后清理待确认帧
- **WHEN** 客户端收到 `AffirmMsg(frame=F, time=T, id=P)`
- **THEN** 客户端 SHALL 从 `ConnectStatusComponent.unConfirmFrame` 移除 F
- **AND** 客户端 SHALL 根据当前客户端时间和 T 更新 RTT

### Requirement: 客户端检测权威命令冲突并回滚重算

系统 SHALL 在收到历史帧权威命令时比较该命令与本地预测命令；若任一影响逻辑的字段不一致，客户端 MUST 回滚到冲突帧前一帧，并用权威命令重放到当前帧。

#### Scenario: 历史权威命令与预测一致
- **WHEN** 客户端当前 `BattleWorld.FrameCount` 为 C 且 C >= F
- **AND** 收到逻辑帧 F 的权威命令与本地记录命令 `EqualsCmd` 一致
- **THEN** 客户端 SHALL 保留当前世界状态
- **AND** 客户端 MAY 将 F 标记为已确认帧

#### Scenario: 历史权威命令与预测不一致
- **WHEN** 客户端当前 `BattleWorld.FrameCount` 为 C 且 C >= F
- **AND** 收到逻辑帧 F 的权威命令与本地记录命令 `EqualsCmd` 不一致
- **THEN** 客户端 MUST 调用回滚流程恢复到 F-1 帧快照
- **AND** 客户端 MUST 使用 F 到 C 的已缓存权威命令或预测命令重新计算
- **AND** 客户端 MUST 在重算完成后清理被覆盖的旧快照

#### Scenario: 冲突帧早于清理窗口
- **WHEN** 客户端收到冲突帧 F 且 F <= `ConnectStatusComponent.ClearFrame`
- **THEN** 客户端 MUST 不执行越界回滚
- **AND** 客户端 SHALL 记录错误并触发重新同步或断线保护策略

### Requirement: 客户端处理追帧消息

系统 SHALL 在收到 `PursueMsg` 时确认消息、从 `recalcFrame` 开始回滚重算，并按照服务端指定的目标帧继续提前预测。

#### Scenario: 收到 PursueMsg 后追帧
- **WHEN** 客户端收到 `PursueMsg(recalcFrame=R, frame=S, advanceCount=A, serverTime=T, id=P)`
- **THEN** 客户端 SHALL 立即发送 `AffirmMsg(frame=S, time=T, id=P)`
- **AND** 当 R 大于 `ConnectStatusComponent.ClearFrame` 时，客户端 MUST 从 R 开始重算
- **AND** 客户端 SHALL 推进预测到 S + A
- **AND** `ConnectStatusComponent.aheadFrame` SHALL 更新为 A

### Requirement: 服务端按逻辑帧聚合并广播权威命令

服务端 SHALL 在每个服务端逻辑帧为每个参战玩家选择一条权威命令；若某玩家真实输入未到达，服务端 MUST 使用该玩家上一条持续输入或默认输入生成预测命令，并向所有客户端广播服务端确定的命令。

#### Scenario: 服务端收到未来帧输入
- **WHEN** 服务端收到玩家 P 的输入命令且命令帧 F 大于服务端当前帧
- **THEN** 服务端 SHALL 缓存该命令
- **AND** 服务端 SHALL 向发送方返回 `AffirmMsg`

#### Scenario: 服务端逻辑帧输入到齐
- **WHEN** 服务端推进到逻辑帧 F 且所有参战玩家都有 F 帧输入
- **THEN** 服务端 SHALL 将这些输入作为 F 帧权威命令写入服务端世界
- **AND** 服务端 SHALL 向所有客户端广播同一组 F 帧权威命令

#### Scenario: 服务端逻辑帧缺少玩家输入
- **WHEN** 服务端推进到逻辑帧 F 且玩家 P 缺少 F 帧真实输入
- **THEN** 服务端 MUST 为 P 生成 F 帧默认或预测命令
- **AND** 服务端 SHALL 向所有客户端广播该预测命令作为 F 帧权威命令

#### Scenario: 服务端收到迟到输入
- **WHEN** 服务端收到玩家 P 的输入命令且命令帧 F 小于或等于服务端当前帧
- **THEN** 服务端 SHALL 发送 `PursueMsg` 给 P
- **AND** `PursueMsg.recalcFrame` SHALL 指向需要客户端重算的帧

### Requirement: 权威命令预测不得重复 one-shot 输入

系统 SHALL 在客户端和服务端生成预测命令时延续持续性输入，但 MUST 清空 `jump`、`toggleLock`、`platformJump` 等 one-shot 字段，除非该帧存在真实输入或服务端权威命令。

#### Scenario: 客户端预测连续移动
- **WHEN** 玩家上一帧命令包含移动方向且下一帧缺少权威命令
- **THEN** 客户端预测命令 SHALL 延续移动方向和速度档
- **AND** 客户端预测命令 MUST 清空 one-shot 字段

#### Scenario: 服务端预测缺帧输入
- **WHEN** 服务端为缺输入玩家生成预测命令
- **THEN** 服务端预测命令 SHALL 延续持续性输入
- **AND** 服务端预测命令 MUST 清空 one-shot 字段

### Requirement: 回滚重算期间禁止不可回退副作用

系统 SHALL 在 `WorldBase.m_isRecalc` 为 true 期间只修改确定性逻辑状态，不得发送新的输入命令、播放表现层特效、触发音效或写入表现层对象作为确定性结果。

#### Scenario: 重算期间不发送输入
- **WHEN** 客户端正在执行回滚重算
- **THEN** 本地输入系统 MUST 不向服务端发送新的历史帧命令

#### Scenario: 重算期间表现层只读
- **WHEN** 客户端正在执行回滚重算
- **THEN** 表现层系统 MUST 不把 Transform、动画或特效状态写回确定性组件

### Requirement: 同步一致性必须可验证

系统 SHALL 提供自动化验证，覆盖权威命令接收、冲突回滚、缺帧预测、确认队列和 one-shot 清理；验证结果 MUST 能证明两个客户端在同一权威命令序列下得到一致的逻辑状态。

#### Scenario: 双客户端同权威命令一致
- **WHEN** 两个客户端从同一 `StartSyncMsg` 启动并接收相同权威命令序列
- **THEN** 两个客户端在相同逻辑帧的可回滚组件状态 SHALL 一致

#### Scenario: 迟到命令触发回滚后恢复一致
- **WHEN** 客户端先用预测命令推进，随后收到同帧不同的服务端权威命令
- **THEN** 客户端 MUST 回滚重算
- **AND** 重算后的可回滚组件状态 SHALL 与从权威命令直接重放的结果一致
