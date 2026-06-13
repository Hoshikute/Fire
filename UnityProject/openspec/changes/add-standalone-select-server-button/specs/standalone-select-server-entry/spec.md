## ADDED Requirements

### Requirement: SelectServerWindow 单机按钮入口
`SelectServerWindow` SHALL 提供一个可点击的“单机”按钮入口，并与现有服务器列表同时存在。

#### Scenario: 显示单机按钮
- **WHEN** `SelectServerWindow` 创建并完成 UI 组件绑定
- **THEN** 界面 SHALL 显示“单机”按钮
- **AND** 现有服务器列表 SHALL 保持可见和可选

#### Scenario: 点击单机按钮触发单机流程
- **WHEN** 玩家点击“单机”按钮
- **THEN** `SelectServerWindow` SHALL 执行单机进入流程
- **AND** 客户端 SHALL NOT 执行服务器项选择流程

### Requirement: 单机入口绕过服务器选择和登录窗口
单机进入流程 MUST NOT 保存 server 选择，也 MUST NOT 打开 `LoginWindow`。

#### Scenario: 单机点击不进入 LoginWindow
- **WHEN** 玩家点击 `SelectServerWindow` 上的“单机”按钮
- **THEN** 客户端 SHALL NOT 调用 `GameModule.UI.ShowUIAsync<LoginWindow>()`
- **AND** 客户端 SHALL NOT 因单机点击修改 `Game.GameData.ServerAddress`
- **AND** 客户端 SHALL NOT 因单机点击修改 `Game.GameData.ServerPort`

#### Scenario: 单机点击不走网络登录
- **WHEN** 玩家点击 `SelectServerWindow` 上的“单机”按钮
- **THEN** 客户端 SHALL NOT 调用 `GameModule.Network.Connect`
- **AND** 客户端 SHALL NOT 发送 `playerloginmsg`

### Requirement: 单机玩家数据初始化
单机进入流程 SHALL 在加载 `Game` 场景前写入必要的本地玩家数据。

#### Scenario: 写入本地玩家标识
- **WHEN** 玩家点击“单机”按钮
- **THEN** `Game.GameData.PlayerName` SHALL 被设置为非空本地玩家名
- **AND** `Game.GameData.PlayerId` SHALL 被设置为非空本地玩家标识
- **AND** `Game.GameData.PlayerCharacterId` SHALL 被设置为可用角色标识

### Requirement: 单机场景加载与初始化
单机进入流程 SHALL 直接加载 `Game` 场景并完成现有游戏场景初始化。

#### Scenario: 进入 Game 场景
- **WHEN** 单机玩家数据初始化完成
- **THEN** 客户端 SHALL 关闭 `SelectServerWindow`
- **AND** 客户端 SHALL 加载 `Game` 场景
- **AND** 客户端 SHALL 执行 `GameModule.TPBattleContext.InitializeGameScene()`

### Requirement: 服务器选择行为保持不变
新增单机入口 MUST NOT 改变现有服务器项点击后的联网登录流程。

#### Scenario: 点击服务器项
- **WHEN** 玩家点击现有服务器项
- **THEN** 客户端 SHALL 保存该服务器的 `Address` 和 `Port` 到 `Game.GameData`
- **AND** 客户端 SHALL 关闭 `SelectServerWindow`
- **AND** 客户端 SHALL 打开 `LoginWindow`

### Requirement: 单机入口唯一展示位置
单机入口 SHALL 以 `SelectServerWindow` 为最终展示位置，避免同时在 `SelectServerWindow` 和 `LoginWindow` 暴露重复单机按钮。

#### Scenario: 窗口入口不重复
- **WHEN** 玩家从选服流程进入 UI
- **THEN** “单机”入口 SHALL 出现在 `SelectServerWindow`
- **AND** `LoginWindow` SHALL NOT 额外展示重复的“单机”入口
