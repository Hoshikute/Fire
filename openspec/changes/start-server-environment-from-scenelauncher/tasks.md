## 1. Server 启动脚本准备

- [x] 1.1 修正 `Server/scripts/start-server.ps1` 的 server 根路径定位，移除 `D:\UGitD\Fire\Server` 硬编码，支持从脚本路径或参数定位当前 checkout 的 `Server` 目录。
- [ ] 1.2 确认 `Server/scripts/start-server.bat` 和 `.ps1` 在当前仓库路径下都能找到 `LockStepDemo.sln`、`LockStepDemo.exe`、MySQL 配置和 MSBuild。
- [x] 1.3 确认本地 server 的监听协议和端口来源，记录 `Server/LockStepDemo/App.config` 的 `7500` 与 `Udp` 配置。
- [x] 1.4 对齐客户端登录协议与服务端监听模式，决定并实施 TCP/UDP 的最小修正或启动前阻断提示策略。

## 2. SceneLauncher 接入

- [x] 2.1 在 `UnityProject/Assets/Editor/ToolbarExtender/UnityToolbarExtenderLeft/SceneLauncher.cs` 中增加 Editor-only server 环境检查入口，只从工具栏 `Launcher` 按钮路径触发。
- [x] 2.2 在 `EditorSceneManager.OpenScene(scenePath)` 成功后、`EditorApplication.isPlaying = true` 前调用 server 环境准备逻辑。
- [x] 2.3 实现本地 server 端口就绪检查，已监听时跳过脚本启动并复用现有 server。
- [x] 2.4 实现脚本启动逻辑，调用仓库内 `Server/scripts/start-server.*`，并避免等待 server 长生命周期进程退出。
- [x] 2.5 实现端口等待与超时控制，server 在超时内就绪才进入 Play Mode。

## 3. 日志与失败处理

- [x] 3.1 为 server 环境准备过程添加统一日志前缀，输出检查、复用、启动、就绪和失败状态。
- [x] 3.2 当脚本路径缺失、server 方案缺失、MySQL 缺失、MSBuild 缺失、构建失败或端口超时时，取消本次自动进入 Play Mode。
- [x] 3.3 在失败日志中输出可操作信息，包括脚本路径、目标端口、目标协议和下一步排查建议。
- [x] 3.4 确保普通 Unity Play 按钮不会触发 server 启动脚本或 server 环境检查。

## 4. 验证与文档

- [ ] 4.1 验证 server 已运行时点击 `SceneLauncher`：不启动第二个 server，直接打开 `main` 并进入 Play Mode。
- [ ] 4.2 验证 server 未运行但环境完整时点击 `SceneLauncher`：脚本启动 server，端口就绪后进入 Play Mode。
- [ ] 4.3 验证 server 启动失败路径：故意使用缺失脚本或不可用依赖时，自动 Play Mode 被取消且 Unity Console 输出明确错误。
- [x] 4.4 验证 `ProcedureLaunch`、`GameApp.Entrance`、`SelectServerWindow`、`LoginWindow` 的运行时职责没有被 server 启动逻辑污染。
- [x] 4.5 运行 `openspec validate start-server-environment-from-scenelauncher --strict`。
- [x] 4.6 刷新 `Temp/openspec-html/start-server-environment-from-scenelauncher/index.html` 供审阅。
