## 1. 协议与传输边界

- [x] 1.1 对齐客户端与服务端 `CommandComponent` / `PlayerCommandBase` 字段，确认 `moveDir`、`skillDir`、`jump`、`toggleLock`、`platformJump`、`speedGear`、`element1`、`element2`、`isFire` 都可序列化且 `EqualsCmd` 覆盖完整。
- [x] 1.2 新增或整理客户端命令传输适配层，把单条 `CommandComponent` 和 `CommandMsg` 统一转换为 `CommandComponent` 列表。
- [x] 1.3 为上行预测命令补齐 `id`、`frame`、`time` 字段，并通过 `GameModule.Network` 发送到服务端。
- [x] 1.4 确认服务端生成协议包含 `StartSyncMsg`、`CommandComponent` 或 `CommandMsg`、`PursueMsg`、`AffirmMsg`，缺失时更新协议定义与生成物。

## 2. 客户端权威命令接线

- [x] 2.1 新增 `FrameAuthoritySyncSystem`（或等价命名），订阅网络消息并分发 `StartSyncMsg`、权威命令、`PursueMsg`、`AffirmMsg`。
- [x] 2.2 实现 `StartSyncMsg` 处理：设置 `BattleWorld.FrameCount`、`EntityIndex`、`SyncRule`、`GameModule.FrameSync.IntervalTime`、`ConnectStatusComponent.aheadFrame` 并启动世界。
- [x] 2.3 实现 `AffirmMsg` 处理：清理 `ConnectStatusComponent.unConfirmFrame` 并更新 RTT。
- [x] 2.4 实现权威命令记录：按实体写入 `PlayerCommandRecordComponent`，并用深拷贝避免网络对象污染历史缓存。
- [x] 2.5 实现权威命令冲突检测：历史帧命令与预测命令不一致时标记冲突并触发回滚重算。
- [x] 2.6 实现 `PursueMsg` 处理：确认消息、从 `recalcFrame` 重算，并提前预测到 `frame + advanceCount`。
- [x] 2.7 将权威接线系统注册进 `BattleWorld.GetSystemTypes()`，确保它在输入命令固化和逻辑系统推进前完成可用命令缓存更新。

## 3. 客户端输入语义调整

- [x] 3.1 调整 `PlayerInputCommandSystem`：同帧已有权威/缓存命令时优先使用缓存命令，本地输入只在缺失权威命令时生成预测命令。
- [x] 3.2 本地预测命令生成后加入待确认队列并上行服务端；`m_world.m_isRecalc` 为 true 时不得发送网络消息。
- [x] 3.3 保持非本地实体只读取权威缓存或预测命令，不读取 `PlayerInputComponent`。
- [x] 3.4 确保预测命令延续持续输入但清空 `jump`、`toggleLock`、`platformJump` 等 one-shot 字段。
- [x] 3.5 调整联网模式下 `BattleContext` 起跑逻辑：玩家表现和实体可先准备，确定性逻辑等待 `StartSyncMsg`；离线调试模式保留本地直接启动。

## 4. 服务端权威帧聚合

- [x] 4.1 对齐 `Server/LockStepDemo/Service/Game/CommandMessageService.cs`，接收 Fire 客户端命令后立即回 `AffirmMsg`。
- [x] 4.2 服务端收到未来帧输入时缓存命令，并在该帧广播给所有客户端。
- [x] 4.3 服务端收到迟到输入时发送 `PursueMsg`，包含 `recalcFrame`、当前服务端帧、提前帧数和服务端时间。
- [x] 4.4 对齐 `PlayerInputSystem`：每个服务端逻辑帧为每个玩家选择真实命令或预测/默认命令，写入服务端世界。
- [x] 4.5 服务端缺输入时生成权威预测命令并广播给所有客户端，预测命令必须清空 one-shot 字段。
- [x] 4.6 `ServiceSyncSystem` 下发 `StartSyncMsg` 时明确设置 `SyncRule.Frame`、逻辑帧间隔、实体索引和提前帧数。

## 5. 回滚与一致性验证

- [ ] 5.1 为 `PlayerCommandRecordComponent` 增加或更新单元测试：记录/读取均为深拷贝，同帧覆盖不会污染历史对象。
- [ ] 5.2 增加 one-shot 预测测试：连续缺权威命令时 `jump`、`toggleLock`、`platformJump` 不重复触发。
- [ ] 5.3 增加客户端回滚测试：预测命令与权威命令冲突时从冲突帧重算，重算结果与权威重放一致。
- [ ] 5.4 增加服务端缺帧测试：玩家未上报输入时服务端生成默认/预测权威命令并广播。
- [ ] 5.5 增加双客户端模拟测试：两个客户端接收同一 `StartSyncMsg` 和同一权威命令序列后，可回滚组件状态一致。
- [ ] 5.6 运行客户端可用的 `dotnet build GameLogic.csproj` 或等价编译验证，并运行服务端测试项目 `Server/LockStepDemo.Tests`。

## 6. 文档与验收

- [ ] 6.1 更新 `.knowledge/architecture/网络同步.md`，说明服务端权威命令、确认、追帧和回滚接线已经闭环。
- [ ] 6.2 更新 `.knowledge/modules/玩家帧同步ECS.md`，把“本地记录不是联网完成”改为新的联网权威流程说明。
- [ ] 6.3 更新 `.knowledge/pitfalls/回滚踩坑.md`，追加“客户端预测命令不能当确定命令”的排查与预防条目。
- [x] 6.4 运行 `openspec validate replace-client-authority-with-server-frame-authority --strict`。
- [ ] 6.5 刷新 `Temp/openspec-html/replace-client-authority-with-server-frame-authority/index.html` 供审阅。
