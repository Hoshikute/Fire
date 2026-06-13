## ADDED Requirements

### Requirement: LoginWindow 单机按钮入口
`LoginWindow` SHALL 提供一个可点击的“单机”按钮入口，并与现有联网登录按钮同时存在。

#### Scenario: 显示单机按钮
- **WHEN** `LoginWindow` 创建并完成 UI 组件绑定
- **THEN** 界面 SHALL 显示“单机”按钮，且现有登录按钮和随机名字按钮 SHALL 保持可用

#### Scenario: 点击单机按钮触发单机流程
- **WHEN** 玩家点击“单机”按钮
- **THEN** `LoginWindow` SHALL 执行单机进入流程，而不是执行联网登录流程

### Requirement: 单机进入绕过 server 连接
单机进入流程 MUST NOT 初始化或连接 server，也 MUST NOT 发送 `playerloginmsg`。

#### Scenario: 未启动 server 时进入单机
- **WHEN** 本地没有可连接的 server 且玩家点击“单机”按钮
- **THEN** 客户端 SHALL 不调用 `GameModule.Network.Connect`
- **AND** 客户端 SHALL 不发送 `playerloginmsg`
- **AND** 客户端 SHALL 继续进入 `Game` 场景

#### Scenario: 单机流程不等待网络回包
- **WHEN** 玩家点击“单机”按钮
- **THEN** 客户端 SHALL 不等待 `NetworkState.Connected`
- **AND** 客户端 SHALL 不等待服务端返回 `playerloginmsg`

### Requirement: 单机玩家数据初始化
单机进入流程 SHALL 在加载场景前写入必要的本地玩家数据。

#### Scenario: 使用输入显示名
- **WHEN** 玩家在显示名输入框填写名称并点击“单机”按钮
- **THEN** `Game.GameData.PlayerName` SHALL 使用该输入名称
- **AND** `Game.GameData.PlayerId` SHALL 被设置为非空本地玩家标识
- **AND** `Game.GameData.PlayerCharacterId` SHALL 被设置为可用角色标识

#### Scenario: 显示名为空时生成默认名称
- **WHEN** 玩家未填写显示名并点击“单机”按钮
- **THEN** `LoginWindow` SHALL 生成本地显示名
- **AND** `Game.GameData.PlayerName` SHALL 使用生成后的显示名

### Requirement: 单机场景加载与初始化
单机进入流程 SHALL 加载 `Game` 场景并完成现有游戏场景初始化。

#### Scenario: 进入 Game 场景
- **WHEN** 单机玩家数据初始化完成
- **THEN** 客户端 SHALL 调用现有场景加载链路进入 `Game`
- **AND** `GameModule.TPBattleContext.InitializeGameScene()` SHALL 被执行

#### Scenario: 单机进入后关闭登录窗口
- **WHEN** 单机进入流程开始加载 `Game` 场景
- **THEN** `LoginWindow` SHALL 被关闭

### Requirement: 联网登录行为保持不变
新增单机入口 MUST NOT 改变现有联网登录按钮的行为。

#### Scenario: 点击现有登录按钮
- **WHEN** 玩家点击现有登录按钮
- **THEN** 客户端 SHALL 按原有流程连接 `Game.GameData.ServerAddress:Game.GameData.ServerPort`
- **AND** 客户端 SHALL 在连接成功后发送 `playerloginmsg`
- **AND** 客户端 SHALL 在服务端返回成功后进入 `Game` 场景
