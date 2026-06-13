## Context

`SelectServerWindow` 当前负责展示服务器列表。窗口创建时会通过 `ServerDataLoader.GetLocalServer()` 初始化选中服务器，通过 `ServerDataLoader.ServerList` 创建服务器项；点击某个服务器项后，`OnServerSelected(ServerData server)` 会保存 `Game.GameData.ServerAddress` / `ServerPort`，关闭 `SelectServerWindow`，再打开 `LoginWindow` 继续匿名联网登录。

单机入口的目标不是选择某个 server，而是在选服阶段直接绕过 server 和登录窗口进入 `Game` 场景。项目已有可复用的场景初始化链路：`GameModule.Scene.LoadSceneAsync("Game")` 加载场景后，调用 `GameModule.TPBattleContext.InitializeGameScene()` 设置相机并加载 Player。`Game.GameData` 也已有 `PlayerName`、`PlayerId`、`PlayerCharacterId` 字段，可作为单机进入时的最小本地玩家数据。

当前工作树里可能已经存在把单机按钮加到 `LoginWindow` 的在途改动。本变更的产品位置以 `SelectServerWindow` 为准，应用时需要避免两个窗口都保留“单机”入口造成重复入口和测试歧义。

## Goals / Non-Goals

**Goals:**

- 在 `SelectServerWindow` 上新增“单机”按钮，作为与服务器列表同级的进入方式。
- 点击“单机”时不保存 server 选择、不打开 `LoginWindow`、不初始化或连接网络、不发送 `playerloginmsg`。
- 单机入口写入稳定的本地玩家数据，并直接加载 `"Game"` 场景与执行 `TPBattleContext.InitializeGameScene()`。
- 现有服务器项点击行为保持不变：仍保存地址端口，并进入 `LoginWindow` 的联网登录流程。
- 如果实现阶段发现 `LoginWindow` 已经有单机按钮，需要迁移或移除该入口，使最终单机入口只出现在 `SelectServerWindow`。

**Non-Goals:**

- 不重做选服窗口的服务器列表架构。
- 不修改 `ServerDataLoader` 的配置格式或默认服务器数据。
- 不改 `playerloginmsg` 协议、网络模块或服务端逻辑。
- 不模拟完整单机帧同步服务端。
- 不改 `Game` 场景内容和 `TPBattleContext` 的初始化规则。

## Decisions

### 决策一：单机按钮挂在 `SelectServerWindow` 顶层

实现应在 `Assets/AssetRaw/UI/SelectServerWindow.prefab` 中新增可查找的按钮节点，例如 `m_btn_Standalone`，并在 `SelectServerWindow.ScriptGenerator()` 中按现有 `transform.Find(...)` 模式绑定。`RegisterEvent()` 中为按钮注册独立点击事件，保持服务器项点击和单机点击在事件入口上分离。

替代方案：

- 把“单机”作为一个伪服务器项塞进服务器列表。这个方案会让 `OnServerSelected` 的 server 保存逻辑和单机逻辑混在一起，后续容易误连 `LoginWindow`。
- 继续把单机按钮放在 `LoginWindow`。用户已明确要求入口在 `SelectServerWindow`，且选服阶段更符合“联网 / 单机”分流语义。

### 决策二：单机点击使用独立流程，不复用 `OnServerSelected`

`OnServerSelected` 的职责是保存 server 地址端口并打开 `LoginWindow`。单机入口应使用独立方法，例如 `OnStandaloneClick()` / `BeginStandaloneGame()`，只写入本地玩家数据并直接进入场景。这样可以保证单机路径不会经过 server 选择和登录窗口。

替代方案：

- 在 `OnServerSelected` 里判断特殊 server id。这个方案会扩大 `ServerData` 的语义，也会让配置数据承担 UI 模式开关职责。
- 打开 `LoginWindow` 后自动触发单机进入。这个方案仍让单机路径依赖登录窗口生命周期，不符合入口移动到选服窗口的目标。

### 决策三：复用现有 Game 场景初始化链路

单机进入不需要新建场景加载服务。实现应复用 `GameModule.Scene.LoadSceneAsync("Game")` 和 `GameModule.TPBattleContext.InitializeGameScene()`，并在加载前关闭 `SelectServerWindow`。如果代码需要避免重复，可以在 `SelectServerWindow` 内保留私有加载方法，而不是新增公共接口。

替代方案：

- 复制 `LoginWindow` 或 `LoginUI` 的完整加载代码到多个窗口。复制可行但容易造成日志和状态清理不一致。
- 引入全局“进入游戏服务”。当前变更范围只涉及一个 UI 入口，新增服务会增加不必要的架构成本。

### 决策四：本地玩家数据使用确定性默认值

`SelectServerWindow` 没有玩家名输入框，因此单机路径应使用确定性默认玩家名和非空 `PlayerId`，例如 `StandalonePlayer` / `standalone-player`，并设置 `PlayerCharacterId = "1"`。这样可以避免进入场景后依赖 server 分配字段。

替代方案：

- 在 `SelectServerWindow` 新增玩家名输入框。用户只要求加单机按钮，增加输入框会扩大 UI 范围。
- 随机生成玩家 ID。随机 ID 会降低日志复现稳定性，除非后续需求明确需要多单机玩家区分。

## Risks / Trade-offs

- [Risk] `SelectServerWindow.prefab` 新增按钮节点名和代码查找名不一致，导致按钮无响应。→ Mitigation：按 `m_btn_Standalone` 约定新增节点，并在验证中确认点击事件可触发。
- [Risk] 单机按钮和服务器列表布局重叠。→ Mitigation：实现时检查现有 `m_scroll_ServerList`、`m_text_Title` 和服务器 item 模板位置，单机按钮使用独立区域且不遮挡滚动列表。
- [Risk] 工作树中已有 `LoginWindow` 单机入口，导致最终出现两个单机按钮。→ Mitigation：应用本变更时把单机入口统一到 `SelectServerWindow`，必要时撤回或不保留 `LoginWindow` 单机入口。
- [Risk] `Game` 场景后续逻辑仍隐式依赖登录成功后的 server 字段。→ Mitigation：单机入口写入最小 `GameData`，并在未启动 server 时做运行时点击验证。
- [Risk] 直接在 YAML prefab 中修改可能引入 Unity 序列化细节错误。→ Mitigation：优先通过 Unity 保存；若手工补丁，必须检查 fileID 引用、children 关系、`.meta` 状态和 `git diff --check`。
