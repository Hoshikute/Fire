## Why

当前客户端协议资源与 server 运行目录协议文件存在漂移风险：协议源文件已经更新后，如果 server 运行目录仍保留旧的 `Network/ProtocolInfo.txt` 或 `Network/MethodInfo.txt`，客户端会按新字段解析旧消息，出现类似 `playerloginmsg.nickname` 读取越界的运行时错误。

需要在 Unity Editor 内提供一个明确、可点击的同步入口，让开发者在启动本地联机或排查协议问题前，可以把协议文件同步到 server 运行目录，并得到一致性检查结果。

## What Changes

- 新增 Unity Editor 菜单项，用于一键同步协议文件。
- 菜单项执行时检查客户端与 server 源协议文件是否一致，不一致时中止并提示具体文件。
- 菜单项将 server 源协议文件同步到 server 运行目录的 `bin/Debug/Network/`。
- 同步完成后输出 `[CODEX_LOG]` 日志，列出同步文件与目标目录。
- 如果检测到本地 server 端口已监听，提示正在运行的 server 需要重启后才能加载新的协议文件。

## Capabilities

### New Capabilities

- `editor-protocol-sync`: Unity Editor 内的协议文件同步能力，覆盖菜单入口、一致性检查、运行目录复制和结果提示。

### Modified Capabilities

无。

## Impact

- 影响 Unity Editor 扩展代码，预计新增或修改 `UnityProject/Assets/Editor/` 下的编辑器工具类。
- 读取协议源文件：
  - `Server/LockStepDemo/Network/ProtocolInfo.txt`
  - `Server/LockStepDemo/Network/MethodInfo.txt`
  - `UnityProject/Assets/Resources/Protocol/ProtocolInfo.txt`
  - `UnityProject/Assets/Resources/Protocol/MethodInfo.txt`
- 写入 server 运行目录：
  - `Server/LockStepDemo/bin/Debug/Network/ProtocolInfo.txt`
  - `Server/LockStepDemo/bin/Debug/Network/MethodInfo.txt`
- 不改变协议生成器、网络协议格式、SceneLauncher 启动流程或运行时通信协议。
