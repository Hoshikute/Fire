## Why

`SelectServerWindow` 是当前选择“本地 / 局域网”等服务器入口的位置，选中服务器后才进入 `LoginWindow`。把“单机”入口放在 `SelectServerWindow` 能让玩家在选服阶段直接选择“不连接 server 进入 `Game`”，避免把单机模式混入后续登录窗口，也让入口语义更清晰。

## What Changes

- 在 `SelectServerWindow` 增加“单机”按钮入口，和服务器列表入口同屏展示。
- 点击“单机”时不保存服务器地址、不打开 `LoginWindow`、不初始化或连接网络，也不发送 `playerloginmsg`。
- 单机入口直接加载 `Game` 场景，并执行 `GameModule.TPBattleContext.InitializeGameScene()`。
- 单机进入前写入必要的本地玩家数据，例如 `Game.GameData.PlayerName`、非空 `PlayerId` 和 `PlayerCharacterId`。
- 保留现有服务器选择行为：点击“本地 / 局域网”等服务器项后仍保存 `Game.GameData.ServerAddress` / `ServerPort`，关闭 `SelectServerWindow` 并打开 `LoginWindow`。
- 如果工作树中已有 `LoginWindow` 单机按钮实现，应用本变更时需要避免两个窗口重复暴露单机入口；最终入口以 `SelectServerWindow` 为准。

## Capabilities

### New Capabilities
- `standalone-select-server-entry`: 约束 `SelectServerWindow` 中不依赖 server 的单机入口，包括按钮展示、离线进入、玩家数据初始化、场景加载和现有服务器选择行为保持不变。

### Modified Capabilities
- 无。

## Impact

- 影响代码区域：
  - `Assets/GameScripts/HotFix/GameLogic/UI/SelectServerWindow/SelectServerWindow.cs`
  - 可能需要复用或迁移当前 `LoginWindow` 单机进入逻辑，避免重复实现。
- 影响资源：
  - `Assets/AssetRaw/UI/SelectServerWindow.prefab` 需要新增并绑定“单机”按钮节点。
  - 如需撤回错误位置的单机入口，可能同步影响 `Assets/AssetRaw/UI/LoginWindow.prefab` 与 `Assets/GameScripts/HotFix/GameLogic/UI/LoginWindow/LoginWindow.cs`。
- 影响运行行为：
  - 选服阶段可直接点击“单机”进入 `Assets/AssetRaw/Scenes/Game.unity` 对应的 `Game` 场景。
  - 选择现有服务器后进入 `LoginWindow` 的联网登录流程不变。
- 验证方式：
  - 在未启动 server 的情况下打开 `SelectServerWindow`，点击“单机”，确认不连接网络且能进入 `Game` 场景并完成 `TPBattleContext.InitializeGameScene()`。
  - 点击“本地 / 局域网”等服务器项，确认仍保存地址并打开 `LoginWindow`。
