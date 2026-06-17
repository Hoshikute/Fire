## Context

Fire 的客户端协议读取入口在 `UnityProject/Assets/Resources/Protocol/`，server 运行时从当前工作目录下的 `Network/ProtocolInfo.txt` 和 `Network/MethodInfo.txt` 读取协议描述。开发时如果只更新了源协议文件，或者 SceneLauncher 复用了一个已经监听端口的 server，server 运行目录可能继续保留旧协议文件，导致客户端按新协议字段解析旧消息。

当前 `SceneLauncher` 已经在 Unity Editor 侧具备仓库根目录解析、server 配置读取、端口监听检查和 `[CODEX_LOG]` 日志约定。本变更在 Editor 菜单中补一个显式同步入口，优先解决“开发者知道要同步什么、同步到哪里、失败原因是什么”的问题。

## Goals / Non-Goals

**Goals:**

- 在 Unity Editor 菜单中提供“同步协议”的可点击入口。
- 在复制前检查客户端与 server 源协议文件内容一致，避免把不一致协议推进运行目录。
- 将 server 源协议文件复制到 server 运行目录 `Server/LockStepDemo/bin/Debug/Network/`。
- 对同步成功、校验失败、文件缺失、server 已运行等情况输出清晰日志。

**Non-Goals:**

- 不新增或修改协议生成器。
- 不改变协议字段格式、消息序列化规则或 TCP/UDP 通信实现。
- 不自动提交或追踪 `bin/Debug` 运行目录文件。
- 不自动停止、重启或热更新正在运行的 server。
- 不改变 SceneLauncher 的启动流程；后续如需要自动同步，可基于本菜单工具抽取复用。

## Decisions

### 菜单入口放在 TEngine/Protocol 下

新增 Editor MenuItem，例如 `TEngine/Protocol/同步协议到Server运行目录`。项目已有 `TEngine/Settings/*` 与 `TEngine/查找资产引用` 菜单，继续放在 `TEngine` 下比新增顶层 `Fire` 菜单更贴近当前编辑器工具分组。

替代方案是放到 `Tools/Fire/*`。该方案更通用，但会新增一个项目内没有使用过的顶层菜单分组，查找成本略高。

### 以源协议一致性作为复制前置条件

菜单执行时比较以下两组源文件：

- `Server/LockStepDemo/Network/ProtocolInfo.txt` 对比 `UnityProject/Assets/Resources/Protocol/ProtocolInfo.txt`
- `Server/LockStepDemo/Network/MethodInfo.txt` 对比 `UnityProject/Assets/Resources/Protocol/MethodInfo.txt`

比较时读取文本并归一化 UTF-8 BOM 与换行符，避免 Windows 换行差异造成误报。只要任意一组不一致，工具中止复制并通过 `Debug.LogError` 输出两端路径。

替代方案是无条件复制 server 源文件到运行目录。该方案可以修复运行目录滞后，但会掩盖 client/server 源协议本身已经分叉的问题。

### 运行目录只作为同步目标

同步目标固定为：

- `Server/LockStepDemo/bin/Debug/Network/ProtocolInfo.txt`
- `Server/LockStepDemo/bin/Debug/Network/MethodInfo.txt`

工具负责创建 `Network` 目录并覆盖目标文件。`bin/Debug` 仍然是本地运行产物，不作为 Git 源文件管理。

### 运行中的 server 只提示重启

菜单可读取 `Server/LockStepDemo/App.config` 的协议与端口，并检查本地监听状态。如果目标端口已经监听，工具同步磁盘文件后输出 warning，提示当前进程不会自动重新加载协议，需要重启 server。

替代方案是自动杀进程或重启 server。该方案副作用较大，且当前需求只是 Editor 菜单同步协议，因此不纳入本次范围。

## Risks / Trade-offs

- 运行中的 server 不会自动加载新文件 -> 同步完成后检测到端口监听时输出明确重启提示。
- `bin/Debug` 目录不存在 -> 工具创建目标目录；如果写入失败则输出错误并保持 Play Mode/运行流程不受影响。
- 源协议文件本身不一致 -> 工具中止复制，要求先重新生成或手动修正协议源文件。
- 与 SceneLauncher 仍是两个入口 -> 本次只提供手动同步能力；后续可将同步逻辑抽成公共 Editor 工具并在 SceneLauncher 启动前调用。

## Migration Plan

1. 新增 Editor 菜单工具。
2. 在 Unity Editor 点击菜单，验证运行目录协议文件被覆盖为当前 server 源协议。
3. 如果 server 已经运行，手动重启 server 后再进行本地联机验证。

回滚时删除该 Editor 菜单工具即可，不影响协议格式、server 工程或客户端运行时代码。

## Open Questions

无。
