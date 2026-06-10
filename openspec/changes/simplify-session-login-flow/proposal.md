## Why

当前 demo 的登录入口仍带有“账号输入”和客户端自带 `playerID` 的假设，但实际目标只是进入帧同步对局，并不需要真实用户体系或持久化用户数据。这会让客户端和服务端职责边界变得模糊，也会让后续把流程简化为纯 session 驱动时出现不必要的兼容负担。

## What Changes

- 将 demo 登录流调整为匿名入场流程，由服务端在连接后的登录握手阶段创建临时玩家上下文。
- 客户端 `LoginWindow` 点击进入后不再上传自定义 `playerID` 作为身份来源。
- 服务端登录协议改为以当前连接 session 为准分配临时玩家标识，并在响应中返回该标识与展示信息。
- 匹配、进房和帧同步输入继续基于已绑定的 session/player 上下文运行，不引入注册、密码或用户资料持久化。
- 明确该方案仅面向 demo 最简链路，不包含跨重启身份恢复和正式账号安全能力。

## Capabilities

### New Capabilities
- `session-based-demo-login`: 定义 demo 中基于 session 的匿名登录、玩家上下文创建和客户端入场行为。

### Modified Capabilities

## Impact

- 客户端登录入口与 UI 文案：`UnityProject/Assets/GameScripts/HotFix/GameLogic/UI/LoginWindow/LoginWindow.cs`
- 客户端登录协议调用点和进入游戏流程
- 服务端登录协议与登录服务：`Server/LockStepDemo/Service/Message/ProtocolMessage.cs`、`Server/LockStepDemo/Service/Service/Login/LoginService.cs`
- 服务端匹配与会话绑定逻辑：`Server/LockStepDemo/Service/Service/Match/MatchService.cs`
- 可能影响重连策略，因为纯 session 最简方案不保证断线后认回原匿名玩家
