## Context

`LoginWindow` 当前只有匿名联网登录流程：点击 `m_btn_Login` 后会生成或读取显示名，调用 `EnsureNetworkInitialized()`，连接 `Game.GameData.ServerAddress:Game.GameData.ServerPort`，连接成功后发送 `playerloginmsg`，收到服务端返回后再调用 `LoadGameScene()`。`LoadGameScene()` 已经会关闭 `LoginWindow`、加载 `"Game"` 场景，并执行 `GameModule.TPBattleContext.InitializeGameScene()`。

项目里已经存在两个可复用事实：`LoginUI.LoadGameScene()` 展示了不等待服务端响应也能加载 `"Game"` 并初始化场景的路径；`Game.GameData` 有 `PlayerName`、`PlayerId`、`PlayerCharacterId` 默认字段，可作为单机进入时的最小玩家数据。`Assets/AssetRaw/UI/LoginWindow.prefab` 当前只包含 `m_btn_Login` 与 `m_btn_RandomName` 两个按钮节点，因此实现需要同步更新 prefab 和 `LoginWindow.cs` 的组件绑定。

## Goals / Non-Goals

**Goals:**

- 在 `LoginWindow` 上新增“单机”按钮，点击后不依赖 server 直接进入 `Game` 场景。
- 单机入口复用现有 `"Game"` 场景加载和 `TPBattleContext.InitializeGameScene()`，避免创建第二套场景初始化流程。
- 单机入口写入稳定的本地玩家数据，保证后续读取 `Game.GameData.PlayerName`、`PlayerId`、`PlayerCharacterId` 时不为空。
- 联网登录入口保持原行为：仍连接 server、发送 `playerloginmsg`、等待服务端返回后进入游戏。
- 实现保持窄范围，优先修改 `LoginWindow` 与对应 prefab，不新增网络协议或服务端接口。

**Non-Goals:**

- 不重做登录 UI 架构，也不合并 `LoginWindow`、`LoginUI`、`SelectServerWindow`。
- 不修改 `playerloginmsg` 协议、服务端返回字段或网络模块实现。
- 不要求单机模式模拟完整帧同步服务端。
- 不改变 `Game.unity` 场景内容和 `TPBattleContext` 的 Player 加载规则。

## Decisions

### 决策一：在 `LoginWindow` 增加独立按钮和独立点击处理

实现应在 prefab 中新增可查找的按钮节点，例如 `m_btn_Standalone`，并在 `LoginWindow.ScriptGenerator()` 中按现有 `transform.Find(...)` 模式绑定。`RegisterEvent()` 中为该按钮注册 `OnStandaloneClick()`，让单机入口和现有 `OnLoginClick()` 在事件层面清晰分离。

替代方案：

- 复用现有 `m_btn_Login` 并用配置切换模式。这个方案会让按钮语义模糊，也容易误触联网流程。
- 在 `LoginUI` 里实现单机按钮。`LoginUI` 有直接加载场景的代码，但用户明确指定当前 `LoginWindow` 需要新增入口，迁移到另一个窗口会偏离目标。

### 决策二：提取或复用一个“直接进场”内部方法

单机点击流程应生成或读取玩家名，写入 `Game.GameData`，然后调用现有 `LoadGameScene()` 或一个更明确的内部方法加载 `"Game"`。如果复用现有 `LoadGameScene()`，需要确保它的日志在单机模式下不误导为“服务器登录”；如果拆出共享方法，应保持私有方法范围，避免为一次 UI 行为创造新的公共接口。

替代方案：

- 为单机模式新建场景加载服务。当前已有 `GameModule.Scene.LoadSceneAsync("Game")` 和 `GameModule.TPBattleContext.InitializeGameScene()`，新增服务会扩大改动面。
- 直接复制 `LoginUI.LoadGameScene()` 到 `LoginWindow`。复制能快速实现，但会让两个窗口的场景初始化逻辑继续分叉；更好的方式是在 `LoginWindow` 内保持单一加载入口，必要时只区分日志和进入来源。

### 决策三：单机入口必须绕过网络模块

单机点击处理不能调用 `EnsureNetworkInitialized()`、`GameModule.Network.Connect(...)` 或 `GameModule.Network.SendMessage("playerloginmsg", ...)`。它只应设置本地数据并进入场景。若实现时需要表达同步模式，应优先复用现有 `GameLogic.Game.RouteRule.Local` / `SyncService.ServiceType`，但不新增网络协议、消息类型或服务端假实现。

替代方案：

- 连接 `127.0.0.1` 作为“单机”。这仍要求本地 server 存在，不满足“不需要链接 server”的目标。
- 在客户端伪造 `playerloginmsg` 网络回包。这样会把网络登录和单机入口混在一起，后续排查登录问题时容易误判。

### 决策四：玩家数据使用确定性本地默认值

单机进入应至少设置 `Game.GameData.PlayerName`、`PlayerId`、`PlayerCharacterId`。`PlayerName` 可复用输入框显示名或随机名；`PlayerId` 可用稳定的本地 ID（例如 `standalone-player` 或同名派生值）；`PlayerCharacterId` 保持现有默认 `"1"`。这样既不依赖 server 分配，也避免进入场景后出现空玩家标识。

替代方案：

- 只设置 `PlayerName`。当前 `LoginWindow` 联网成功后会写入 `PlayerId` 和 `PlayerCharacterId`，单机路径不写可能留下空值风险。
- 随机生成 `PlayerId`。调试日志和复现场景会不稳定，除非后续业务明确需要多本地玩家区分。

## Risks / Trade-offs

- [Risk] prefab 节点名和代码查找名不一致，导致按钮不响应。→ Mitigation：按现有 `m_btn_*` 命名方式新增节点，并在验证中确认 `OnStandaloneClick()` 能被触发。
- [Risk] 单机路径复用 `LoadGameScene()` 时日志仍打印服务器信息，误导排查。→ Mitigation：为进入来源补充明确日志，或拆出共享加载方法并让联网/单机各自记录自己的上下文。
- [Risk] `Game` 场景后续逻辑仍隐式依赖 server 数据。→ Mitigation：单机入口写入最小 `GameData`，实现和验证时观察 `TPBattleContext.InitializeGameScene()` 后是否有网络相关异常。
- [Risk] prefab 手工修改可能影响现有登录按钮布局。→ Mitigation：保持现有按钮不改名、不移除，只增加“单机”按钮并在 Unity 中验证两个入口都可点击。
- [Risk] 若 `SyncService.ServiceType` 当前无消费方，设置它只能表达意图但不能改变行为。→ Mitigation：实现前用搜索确认消费方；没有消费方时不为了单机入口新增同步系统改造。
