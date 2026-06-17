## Why

目前 Unity 编辑器工具栏的 `Launcher` 只负责打开 `main` 场景并进入 Play Mode，本地联机调试还需要开发者手动启动 `Server/LockStepDemo` 及其 MySQL 依赖。随着客户端已接入选服、登录和服务端权威帧同步流程，缺少一键准备 server 环境会让 `SceneLauncher` 的“从任意场景直接进入联机验证”体验断在登录前。

## What Changes

- 在 `SceneLauncher` 的 Editor-only 启动流程中增加“启动 server 环境”的能力，在进入 Play Mode 前确保本地 server 环境可用。
- 复用仓库现有 `Server/scripts/start-server.*` 脚本，避免在 Unity 编辑器代码里重写构建、MySQL、服务端进程启动逻辑。
- 启动前检查本地 server 端口状态，已运行时直接进入 Play Mode，未运行时再触发脚本。
- 修正 server 启动脚本的仓库路径依赖，使它能从当前 checkout 相对定位 `Server` 目录。
- 在启动失败时给出清晰的 Editor 日志，说明是 MySQL、MSBuild、服务端构建、端口占用还是脚本路径问题。
- 明确客户端登录协议和服务端监听配置需要对齐，避免 server 已启动但登录连接失败。

## Capabilities

### New Capabilities

- `scenelauncher-server-environment`: 定义 `SceneLauncher` 在进入 Play Mode 前准备本地 server 环境的行为、可见反馈和失败处理。

### Modified Capabilities

- 无。

## Impact

- 影响 Unity Editor 工具：`UnityProject/Assets/Editor/ToolbarExtender/UnityToolbarExtenderLeft/SceneLauncher.cs`。
- 影响本地 server 启动脚本：`Server/scripts/start-server.bat`、`Server/scripts/start-server.ps1`。
- 需要核对服务端监听配置：`Server/LockStepDemo/App.config` 的 `port="7500"` / `mode="Udp"`。
- 需要核对客户端登录网络初始化：`LoginWindow` 当前使用 `ProtocolType.Tcp`，实现时必须决定是调整客户端协议、调整服务端模式，还是把协议对齐作为启动前检查/提示的一部分。
- 不改变运行时 TEngine `Procedure` FSM、不改变正式包行为、不改变 `BattleContext` 的 Game 场景初始化职责。
