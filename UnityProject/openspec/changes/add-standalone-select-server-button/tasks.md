## 1. 现状确认

- [ ] 1.1 确认 `SelectServerWindow.cs` 当前服务器项点击链路中 `OnServerSelected`、`Game.GameData.ServerAddress`、`Game.GameData.ServerPort` 和 `GameModule.UI.ShowUIAsync<LoginWindow>()` 的调用边界。
- [ ] 1.2 确认 `Assets/AssetRaw/UI/SelectServerWindow.prefab` 现有标题、滚动列表、服务器项模板布局，确定新增“单机”按钮的节点名、文本和位置。
- [ ] 1.3 检查当前工作树中是否已有 `LoginWindow` 单机入口改动，决定应用本变更时需要迁移、复用还是移除以避免重复入口。

## 2. SelectServerWindow 代码实现

- [ ] 2.1 在 `SelectServerWindow` 中新增 `m_btn_Standalone` 组件绑定，并在 `RegisterEvent()` 中注册单机按钮点击事件。
- [ ] 2.2 实现 `OnStandaloneClick()` / `BeginStandaloneGame()`，阻止重复点击进入，并让单机流程独立于 `OnServerSelected`。
- [ ] 2.3 在单机进入前写入 `Game.GameData.PlayerName`、非空 `PlayerId` 和 `PlayerCharacterId`。
- [ ] 2.4 确保单机入口不调用 `GameModule.UI.ShowUIAsync<LoginWindow>()`、不修改 `ServerAddress` / `ServerPort`、不调用 `GameModule.Network.Connect(...)`、不发送 `playerloginmsg`。
- [ ] 2.5 让单机入口关闭 `SelectServerWindow` 后加载 `"Game"` 场景，并执行 `GameModule.TPBattleContext.InitializeGameScene()`。

## 3. SelectServerWindow Prefab 更新

- [ ] 3.1 在 `Assets/AssetRaw/UI/SelectServerWindow.prefab` 增加“单机”按钮节点，并保持现有服务器列表与服务器项模板不改名、不移除。
- [ ] 3.2 调整按钮布局和文本，确认“单机”按钮在 `SelectServerWindow` 打开后可见、可点击，且不遮挡服务器列表。
- [ ] 3.3 保存 prefab 后检查 Unity 资源序列化、fileID 引用和 `.meta` 状态，避免无关资源改动。

## 4. 入口迁移与去重

- [ ] 4.1 如果 `LoginWindow` 已存在“单机”入口，将最终展示位置统一到 `SelectServerWindow`，并撤回或不保留 `LoginWindow` 的重复按钮、绑定和点击逻辑。
- [ ] 4.2 用搜索确认最终只在 `SelectServerWindow` 暴露“单机”入口，`LoginWindow` 不再额外展示重复入口。

## 5. 验证

- [ ] 5.1 用代码搜索确认单机点击路径不包含 `GameModule.UI.ShowUIAsync<LoginWindow>()`、`GameModule.Network.Connect` 或 `playerloginmsg` 发送。
- [ ] 5.2 运行 C# 编译检查，确认 `SelectServerWindow` 相关代码没有编译错误。
- [ ] 5.3 在未启动 server 的情况下运行 `SelectServerWindow`，点击“单机”，确认能进入 `Game` 场景并完成 `TPBattleContext.InitializeGameScene()`。
- [ ] 5.4 点击现有服务器项，确认仍会保存服务器地址端口并打开 `LoginWindow`。
- [ ] 5.5 运行 `openspec validate add-standalone-select-server-button --strict`，确认变更工件有效。
