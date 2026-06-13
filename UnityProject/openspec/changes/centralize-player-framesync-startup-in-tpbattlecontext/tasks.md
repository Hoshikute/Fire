## 1. 现状复核

- [x] 1.1 复查 `PlayerFrameSyncEntry` 的 `.meta` GUID，确认 `Game.unity`、Player prefab 和相关 prefab/asset 没有仍序列化引用该脚本
- [x] 1.2 复查 Player prefab 上的现有脚本组件，确认是否存在缺失脚本或历史遗留字段，并只记录与本变更相关的清理项
- [x] 1.3 查明 `PlayerAnimConfig` 的资产来源；若当前没有实例，确定 canonical asset 路径或实现明确的加载/注入方案

## 2. TPBattleContext startup 收敛

- [x] 2.1 在 `TPBattleContext` 中增加 Player FrameSync world、本地玩家 entity id、已加载 Player 实例等必要状态字段
- [x] 2.2 将 `PlayerFrameSyncEntry.Start()` 中创建 `PlayerWorld`、设置 `SyncRule.Frame`、生成本地玩家实体、`FlushEntityOperations()`、启动 world 的逻辑迁移到 `TPBattleContext` 私有方法
- [x] 2.3 在 `TPBattleContext.LoadPlayerAsync()` 成功加载 Player prefab 后，解析 `Transform`、`AnimancerComponent`、`PlayerAnimConfig` 并创建 `PlayerViewComponent`
- [x] 2.4 保持 `CharacterModule.SetCharacterPrefab("Player")` / `LoadCharacterAsync()` 的现有加载路径不变
- [x] 2.5 保留或迁移现有相机绑定逻辑，让 `TPBattleContext` 在 Player 实体创建后绑定 follow/lookAt target
- [x] 2.6 为重复调用 `InitializeGameScene()` 或重复启动 Player FrameSync 增加保护，避免创建多个 `PlayerWorld`

## 3. 清理路径

- [x] 3.1 在 `TPBattleContext.Shutdown()` 或明确退出流程中销毁/注销 Player FrameSync world，并清空本地引用
- [x] 3.2 确认 world 清理与 `CharacterModule.DestroyCharacter()` 的顺序不会让 frame systems 访问已销毁的 Player transform
- [x] 3.3 确认 shutdown 在 world 未创建、Player 未加载、camera target 为空时仍然幂等

## 4. PlayerFrameSyncEntry 退场

- [x] 4.1 删除 `TPBattleContext` 中 `GetComponent<PlayerFrameSyncEntry>()` / `AddComponent<PlayerFrameSyncEntry>()` 调用
- [x] 4.2 若资产引用复核通过，删除 `PlayerFrameSyncEntry.cs` 并同步 `GameLogic.csproj`
- [x] 4.3 若短期仍需兼容旧资产，改为 obsolete 兼容壳且不得创建或启动 `PlayerWorld`
- [x] 4.4 只在确认缺失脚本与本变更相关时，清理 Player prefab 上的 stale script 引用

## 5. 知识库更新

- [x] 5.1 更新 `.knowledge/modules/player-framesync-ecs.md`，把 Player FrameSync startup/cleanup 入口改为 `TPBattleContext`
- [x] 5.2 更新 `.knowledge/modules/player-controller.md` 或其他仍描述 `PlayerFrameSyncEntry` 为当前入口的文档
- [x] 5.3 如新增或删除模块级入口，按 AGENTS 约定同步更新 `.knowledge/INDEX.md`

## 6. 验证

- [x] 6.1 全仓搜索确认没有新的运行时路径依赖 `AddComponent<PlayerFrameSyncEntry>()` 启动 FrameSync
- [x] 6.2 运行可用的 C# 编译或 Unity 脚本编译验证，确认迁移后无编译错误
- [ ] 6.3 在 Unity 中进入 `Game` 场景，确认 `TPBattleContext.InitializeGameScene()` 能加载 Player、创建且只创建一个 `PlayerWorld`、绑定相机并启动 world
- [ ] 6.4 在 Unity 中验证本地玩家输入、移动和动画表现仍正常，且缺失 `PlayerAnimConfig` 时会出现明确日志
- [x] 6.5 运行 `openspec validate centralize-player-framesync-startup-in-tpbattlecontext --strict` 并修复所有规范问题
