## 1. 现状确认

- [x] 1.1 确认 `LoginWindow.cs` 当前联网登录链路中 `EnsureNetworkInitialized`、`GameModule.Network.Connect`、`playerloginmsg` 和 `LoadGameScene` 的调用边界。
- [x] 1.2 确认 `Assets/AssetRaw/UI/LoginWindow.prefab` 现有按钮节点与布局，确定新增“单机”按钮的节点名、文本和位置。
- [x] 1.3 搜索 `SyncService.ServiceType` / `RouteRule.Local` 的当前消费方，决定单机入口是否需要设置现有本地同步模式字段。

## 2. LoginWindow 代码实现

- [x] 2.1 在 `LoginWindow` 中新增 `m_btn_Standalone` 组件绑定，并在 `RegisterEvent()` 中注册单机按钮点击事件。
- [x] 2.2 实现 `OnStandaloneClick()`，复用显示名输入或随机名生成逻辑，并阻止重复点击进入。
- [x] 2.3 在单机进入前写入 `Game.GameData.PlayerName`、非空 `PlayerId` 和 `PlayerCharacterId`，必要时复用现有 `RouteRule.Local` 表达本地模式。
- [x] 2.4 确保单机入口不调用 `EnsureNetworkInitialized()`、`GameModule.Network.Connect(...)` 或 `GameModule.Network.SendMessage("playerloginmsg", ...)`。
- [x] 2.5 复用或整理现有 `LoadGameScene()`，让单机和联网入口都通过 `"Game"` 场景加载与 `GameModule.TPBattleContext.InitializeGameScene()` 完成进场，并让日志能区分进入来源。

## 3. LoginWindow Prefab 更新

- [x] 3.1 在 `Assets/AssetRaw/UI/LoginWindow.prefab` 增加“单机”按钮节点，并保持现有登录按钮和随机名字按钮不改名、不移除。
- [x] 3.2 调整按钮布局和文本，确认“单机”按钮在 `LoginWindow` 打开后可见、可点击，且不遮挡输入框和现有按钮。
- [x] 3.3 保存 prefab 后检查 Unity 资源序列化和 `.meta` 状态，避免无关资源改动。

## 4. 验证

- [x] 4.1 用代码搜索确认单机点击路径不包含 `GameModule.Network.Connect`、`EnsureNetworkInitialized` 或 `playerloginmsg` 发送。
- [ ] 4.2 在未启动 server 的情况下运行 `LoginWindow`，点击“单机”，确认能进入 `Game` 场景并完成 `TPBattleContext.InitializeGameScene()`。
- [ ] 4.3 在 server 可用时点击现有登录按钮，确认仍会连接 `Game.GameData.ServerAddress:Game.GameData.ServerPort`、发送 `playerloginmsg` 并在登录成功后进入 `Game` 场景。
- [x] 4.4 运行 `openspec validate add-standalone-login-button --strict`，确认变更工件有效。
