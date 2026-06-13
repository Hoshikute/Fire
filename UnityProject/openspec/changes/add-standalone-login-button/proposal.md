## Why

当前 `LoginWindow` 的进入路径依赖连接 `Game.GameData.ServerAddress:ServerPort` 并等待 `playerloginmsg` 返回；当开发者只想本机验证 `Game` 场景、相机和 Player 初始化时，仍需要启动或连接服务端，调试成本偏高。新增单机入口可以在不连接 server 的情况下直接进入 `Game.unity`，补齐登录窗口的离线开发与本地演示路径。

## What Changes

- 在 `LoginWindow` 增加“单机”按钮入口，和现有联网/本地入口并列展示。
- 点击“单机”时不初始化网络、不调用 `GameModule.Network.Connect`、不发送 `playerloginmsg`，直接进入 `Game` 场景。
- 单机进入时复用现有 `GameModule.Scene.LoadSceneAsync("Game")` 与 `GameModule.TPBattleContext.InitializeGameScene()` 初始化链路，保证场景、相机和 Player 的启动行为与现有直接进场路径一致。
- 单机进入前写入必要的本地玩家数据，例如 `Game.GameData.PlayerName`、`PlayerId`、`PlayerCharacterId`，避免后续逻辑读取空值。
- 保留现有联网登录行为，不改变服务端登录、服务器地址、端口和 `playerloginmsg` 的协议契约。

## Capabilities

### New Capabilities
- `standalone-login-entry`: 约束 `LoginWindow` 中不依赖 server 的单机进入能力，包括按钮入口、离线进入流程、必要玩家数据和场景初始化行为。

### Modified Capabilities
- 无。

## Impact

- 影响代码区域：
  - `Assets/GameScripts/HotFix/GameLogic/UI/LoginWindow/LoginWindow.cs`
  - 可能涉及 `Assets/GameScripts/HotFix/GameLogic/UI/LoginUI/LoginUI.cs` 中已有直接进场逻辑的复用或抽取。
- 影响资源：
  - `Assets/AssetRaw/UI/LoginWindow.prefab` 需要新增并绑定“单机”按钮节点。
  - 如使用 Unity prefab 文本节点，需要同步对应 `.meta` 保持资源引用稳定。
- 影响运行行为：
  - 单机按钮点击后可以不启动 server 直接加载 `Assets/AssetRaw/Scenes/Game.unity` 对应的 `Game` 场景。
  - 联网登录按钮仍按原有流程连接 server 并等待登录响应。
- 验证方式：
  - 在未启动 server 的情况下点击“单机”，确认不会输出连接 server 的日志，且能进入 `Game` 场景并完成 `TPBattleContext.InitializeGameScene()`。
  - 在正常 server 可用时点击现有登录按钮，确认匿名登录流程不被单机入口影响。
