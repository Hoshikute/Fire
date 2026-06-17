## Context

`SceneLauncher` 是 Unity Editor 工具栏扩展，当前点击 `Launcher` 后只会打开名为 `main` 的场景并设置 `EditorApplication.isPlaying = true`。进入 Play Mode 后，运行时才由 TEngine `ProcedureSetting` 启动 `ProcedureLaunch`，再通过 `ProcedureModule` / `FsmModule` 跑热更、资源、程序集加载和 `GameApp.Entrance`。

联机入口已经在热更层拆成 `SelectServerWindow` 和 `LoginWindow`：选服写入 `Game.GameData.ServerAddress` / `ServerPort`，登录窗口通过 `GameModule.Network.Connect(...)` 连接服务端，之后加载 `Game` 场景并调用 `BattleContext.InitializeGameScene()`。当前仓库也已有 `Server/LockStepDemo` 与 `Server/scripts/start-server.*`，但 `start-server.ps1` 仍硬编码 `D:\UGitD\Fire\Server`，而服务端配置为 `7500 + Udp`，客户端登录初始化仍使用 `ProtocolType.Tcp`。

这次变更的边界应保持在 Editor 工作流和本地 server 启动脚本，不改变正式包启动流程，不把 server 启动逻辑塞进 TEngine Procedure FSM。

## Goals / Non-Goals

**Goals:**

- 点击 `SceneLauncher` 的 `Launcher` 后，在进入 Play Mode 前准备本地 server 环境。
- 已有本地 server 正在监听时直接复用，避免重复启动。
- 未监听时复用仓库内 server 启动脚本，完成 MySQL 检查、服务端构建和服务端进程启动。
- 去掉启动脚本对旧 checkout 的硬编码路径，保证从当前仓库相对定位 `Server`。
- 给 Unity Console 提供清晰、可搜索的启动结果、失败原因和排查入口。
- 在实现阶段明确处理客户端协议与服务端监听模式不一致的问题。

**Non-Goals:**

- 不改变 TEngine `Procedure` FSM 链路。
- 不改变 `GameApp`、`SelectServerWindow`、`LoginWindow` 的业务入口职责，除非协议对齐需要最小调整。
- 不在 Unity Editor 代码中重写 MySQL、MSBuild、服务端构建或服务端主循环。
- 不把该能力带入玩家正式包或运行时热更程序集。
- 不实现复杂的 server 进程托管面板、日志查看器或停止/重启 UI。

## Decisions

### Decision: 在 `SceneLauncher` 进入 Play 前启动 server

`SceneHelper.OnUpdate()` 已经是从按钮点击到打开 `main` 场景、进入 Play Mode 的唯一收束点。server 环境检查应插入在 `EditorSceneManager.OpenScene(scenePath)` 成功之后、`EditorApplication.isPlaying = true` 之前。

这样做的好处是：只有通过编辑器 `Launcher` 按钮进入时才触发本地 server 准备，普通 Unity Play 按钮、运行时 Procedure FSM、正式包都不受影响。

备选方案是在 `ProcedureLaunch` 中启动 server。放弃该方案，因为 `ProcedureLaunch` 属于运行时流程，会污染正式包，并把编辑器开发环境依赖带进 TEngine 流程 FSM。

### Decision: 复用 `Server/scripts`，Editor 只负责编排和检测

Editor 侧只做三件事：

- 解析仓库根路径和脚本路径。
- 检查目标 server 端口是否已经监听。
- 启动脚本并等待端口就绪或超时。

MySQL 启动、MSBuild 查找、server 构建和 `LockStepDemo.exe` 启动继续交给 `Server/scripts/start-server.*`。脚本需要支持从自身路径或传参定位仓库内 `Server` 目录，不能再硬编码 `D:\UGitD\Fire\Server`。

备选方案是在 C# Editor 代码里直接调用 MSBuild、启动 MySQL 和 `LockStepDemo.exe`。放弃该方案，因为它会复制脚本职责，后续维护两套启动逻辑。

### Decision: 端口检查必须理解 TCP/UDP 差异

服务端当前 `App.config` 是 `port="7500"`、`mode="Udp"`，而 `LoginWindow` 当前使用 `ProtocolType.Tcp` 初始化网络。实现时不能只检查 TCP 端口就认为 server 可用，也不能在协议不一致时静默进入 Play。

需要在启动前确定本地联机使用的协议策略：

- 要么把客户端匿名登录初始化改为与服务端一致的 UDP；
- 要么把服务端配置改为 TCP；
- 要么在 `SceneLauncher` 启动前检查到不一致时给出明确错误并阻止“已准备好”的误报。

备选方案是只检查 `127.0.0.1:7500` 是否有任意监听。放弃该方案，因为 TCP/UDP 不一致时登录仍会失败，问题会被误导成业务登录失败。

### Decision: 启动失败时不盲目进入 Play Mode

如果脚本路径缺失、MySQL 不存在或启动失败、MSBuild 找不到、构建失败、server 端口未在超时内就绪，`SceneLauncher` 应取消本次自动进入 Play Mode，并在 Unity Console 中输出错误。这样能让“点击 Launcher 后就是可联机调试环境”的承诺保持可信。

备选方案是失败后仍进入 Play Mode。放弃该方案，因为用户会在登录界面才看到连接失败，排查成本更高。

## Risks / Trade-offs

- [Risk] 启动脚本是长生命周期进程，Editor 若等待脚本结束会卡住。→ Mitigation: Editor 只等待端口就绪，不等待 server 进程退出；脚本/进程启动应以外部进程方式运行。
- [Risk] 端口已监听但不是当前仓库的 `LockStepDemo`。→ Mitigation: 第一阶段只把端口监听视为“已有 server 环境”，并在日志中说明复用；后续若需要再加握手验证。
- [Risk] Windows PowerShell 执行策略阻止 `.ps1`。→ Mitigation: 优先支持 `.bat` 或用 `powershell.exe -ExecutionPolicy Bypass -File` 启动 `.ps1`，并在失败日志中提示执行策略。
- [Risk] MySQL 或 MSBuild 缺失导致启动失败。→ Mitigation: 复用脚本现有检查，并把脚本退出码/日志路径写入 Unity Console。
- [Risk] 客户端 TCP 与服务端 UDP 不一致。→ Mitigation: 在实现任务中先做协议对齐决策，再把检查或调整纳入验收。
- [Risk] 每次点击 Launcher 都构建 server 影响速度。→ Mitigation: 先检查端口，已监听时不触发构建；脚本内部可继续保留构建逻辑以保证首次启动正确。

## Migration Plan

1. 先修正 `Server/scripts/start-server.ps1` 的路径定位，确保脚本从当前仓库运行。
2. 在 `SceneLauncher` 增加 Editor-only server 环境检查和脚本启动流程。
3. 增加端口/协议检查，明确处理 `7500 + Udp` 与客户端初始化协议的关系。
4. 验证三条路径：server 已运行、server 未运行但可启动、server 启动失败。
5. 回归普通 Unity Play 按钮和运行时 Procedure 链路，确认不会触发 server 启动。

回退策略：移除 `SceneLauncher` 中的 server 环境检查调用即可恢复旧行为；脚本路径修正可保留，因为它只让脚本更适配当前 checkout。

## Open Questions

- 本地联机调试最终应该统一使用 TCP 还是 UDP？当前服务端配置和客户端初始化不一致，需要在实现前确认或由本 change 一并修正。
- `Launcher` 点击时 server 启动窗口应保持可见还是隐藏？如果仍依赖按 `q` 停止 server，可见窗口更利于手动停止；如果后续改成后台服务，则需要额外停止机制。
- 是否需要为 server 启动增加 EditorPrefs 开关？当前 proposal 默认点击 `Launcher` 就准备 server 环境，后续如果单机调试频繁，可再加显式开关。
