## ADDED Requirements

### Requirement: Unity Editor 提供协议同步菜单

系统 SHALL 在 Unity Editor 中提供一个可点击菜单项，用于触发协议同步操作。菜单项 SHALL 位于现有编辑器工具菜单分组下，并以中文明确表达同步协议到 server 运行目录的用途。

#### Scenario: 开发者点击同步协议菜单
- **WHEN** 开发者在 Unity Editor 中点击协议同步菜单
- **THEN** 系统执行协议同步流程

### Requirement: 协议同步前校验客户端与 server 源协议一致

系统 SHALL 在复制文件前校验客户端与 server 的源协议文件内容一致。校验范围 SHALL 包含 `ProtocolInfo.txt` 与 `MethodInfo.txt`。如果任意源文件不存在或内容不一致，系统 MUST 中止同步，并输出包含问题文件路径的错误日志。

#### Scenario: 源协议文件一致
- **WHEN** 客户端与 server 的 `ProtocolInfo.txt` 和 `MethodInfo.txt` 内容一致
- **THEN** 系统继续执行运行目录同步

#### Scenario: 源协议文件不一致
- **WHEN** 客户端与 server 的任意源协议文件内容不一致
- **THEN** 系统中止同步并输出错误日志

### Requirement: 协议同步写入 server 运行目录

系统 SHALL 将 server 源协议文件复制到 `Server/LockStepDemo/bin/Debug/Network/`。复制范围 SHALL 包含 `ProtocolInfo.txt` 与 `MethodInfo.txt`。目标目录不存在时，系统 SHALL 创建目标目录。

#### Scenario: server 运行目录协议文件滞后
- **WHEN** server 源协议文件与运行目录协议文件不同
- **THEN** 点击同步菜单后，运行目录协议文件内容与 server 源协议文件一致

#### Scenario: server 运行目录 Network 目录不存在
- **WHEN** `Server/LockStepDemo/bin/Debug/Network/` 不存在
- **THEN** 系统创建该目录并写入协议文件

### Requirement: 协议同步输出可诊断日志

系统 SHALL 使用 `[CODEX_LOG]` 前缀输出协议同步结果。同步成功时日志 SHALL 包含同步文件名和目标目录。同步失败时日志 MUST 包含失败原因。

#### Scenario: 协议同步成功
- **WHEN** 协议文件成功写入 server 运行目录
- **THEN** Unity Console 显示带 `[CODEX_LOG]` 前缀的成功日志

#### Scenario: 协议同步失败
- **WHEN** 文件缺失、内容不一致或写入失败
- **THEN** Unity Console 显示带 `[CODEX_LOG]` 前缀的错误日志

### Requirement: server 已运行时提示需要重启

系统 SHALL 在同步操作中检查本地 server 配置的协议与端口是否已经监听。如果目标端口已经监听，系统 SHALL 在同步完成后输出 warning，提示正在运行的 server 需要重启后才能加载新的协议文件。

#### Scenario: server 已经监听目标端口
- **WHEN** 协议同步完成且本地 server 目标端口已监听
- **THEN** Unity Console 显示需要重启 server 的 warning 日志
