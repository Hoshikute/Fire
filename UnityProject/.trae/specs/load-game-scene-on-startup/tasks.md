# Tasks
- [x] Task 1: 确认启动入口与场景定位名
  - [x] 核对 `ProcedureStartGame` 是否为默认进入游戏的最终主包流程节点
  - [x] 核对 `Game` 主场景的资源定位名与实际资源配置是否一致
  - [x] 核对场景切换是否经由 `GameModule.Scene` / `ISceneModule`，不绕过框架模块

- [x] Task 2: 固化启动默认进入 Game 场景的行为
  - [x] 若现状未满足规格，则调整启动流程，使其在 `ProcedureStartGame` 中默认进入 `Game` 主场景
  - [x] 保持通过异步场景加载接口切换主场景，不引入同步加载或直接调用原生场景管理 API
  - [x] 保持启动器 UI 在进入主场景前被隐藏，避免界面残留

- [x] Task 3: 验证启动流程回归
  - [x] 验证启动流程仍遵循既有 `Procedure` 链路，不绕过资源初始化与热更加载
  - [x] 按用户要求放弃本轮 Unity 运行验证，改为静态核对 `Game` 主场景资源定位名、加载调用与资源收集配置的一致性
  - [x] 静态确认启动器 UI 隐藏时机与主场景切换顺序正确，且未发现新的场景路径配置冲突

# Task Dependencies
- Task 2 depends on Task 1
- Task 3 depends on Task 2
