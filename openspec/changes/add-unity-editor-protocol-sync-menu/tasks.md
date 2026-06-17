## 1. Editor 菜单入口

- [x] 1.1 在 `UnityProject/Assets/Editor/` 下新增协议同步 Editor 工具类
- [x] 1.2 添加 `TEngine/Protocol/同步协议到Server运行目录` MenuItem 入口
- [x] 1.3 通过 `Application.dataPath` 解析仓库根目录，并组装协议源文件与运行目录路径

## 2. 协议一致性校验

- [x] 2.1 校验 server 与客户端源 `ProtocolInfo.txt` 是否存在且内容一致
- [x] 2.2 校验 server 与客户端源 `MethodInfo.txt` 是否存在且内容一致
- [x] 2.3 校验失败时中止同步，并输出带 `[CODEX_LOG]` 前缀的错误日志

## 3. 运行目录同步

- [x] 3.1 创建 `Server/LockStepDemo/bin/Debug/Network/` 目标目录
- [x] 3.2 将 server 源 `ProtocolInfo.txt` 覆盖复制到运行目录
- [x] 3.3 将 server 源 `MethodInfo.txt` 覆盖复制到运行目录
- [x] 3.4 同步成功后输出带 `[CODEX_LOG]` 前缀的同步摘要日志

## 4. server 运行态提示

- [x] 4.1 读取 `Server/LockStepDemo/App.config` 中的协议与端口
- [x] 4.2 检查目标协议端口是否已经监听
- [x] 4.3 如果 server 已运行，在同步完成后输出需要重启 server 的 warning 日志

## 5. 验证

- [x] 5.1 运行 `openspec validate add-unity-editor-protocol-sync-menu --strict`
- [ ] 5.2 在 Unity Editor 中确认菜单项可见
- [ ] 5.3 手动点击菜单，确认运行目录协议文件与 server 源协议文件一致
