# Implementation Plan

[Overview]
删除不符合 TEngine 架构规范的客户端 Service 目录，移除冗余的 `InputService`/`InputMap`/`KeyBoardUIController` 包装层，使项目完全遵循 TEngine Module 体系。

当前项目的 `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Service/` 目录下存在一个 `InputService` 体系，该体系使用手动单例模式、不继承 `TEngine.Module`，完全绕过了 `ModuleSystem` 的生命周期管理。经调查发现，所有调用方（PlayerMovementFsmState、PlayerIdleState 等状态机文件）已经直接使用 `GameModule.Input`（即 `IInputModule`），该 Service 目录已被架空，无任何外部引用。TEngine 的 `InputModule` 已正确实现 `Module` 基类并通过 `ModuleSystem.RegisterModule` 注册。本次重构为一纯删除操作，不涉及任何代码修改，零破坏性。

[Types]
无类型系统变更。本次为纯文件删除操作，不涉及接口、枚举、数据结构的新增或修改。现有的 `IInputModule` 接口和 `InputModule` 实现已完整覆盖原 `InputService` 的全部功能（Move/Look/GetButton/GetButtonDown/GetButtonUp）。

[Files]
删除 `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Service/` 目录及全部子内容。

**待删除文件清单：**

| 文件路径 | 说明 |
|---------|------|
| `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Service/GameService/InputService/InputService.cs` | 硬编码单例，绕过 ModuleSystem，无外部引用 |
| `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Service/GameService/InputService/InputService.cs.meta` | Unity meta 文件 |
| `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Service/GameService/InputService/InputMap.cs` | 对 InputSystem_Actions 的冗余包装，InputModule 已内置处理 |
| `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Service/GameService/InputService/InputMap.cs.meta` | Unity meta 文件 |
| `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Service/GameService/InputService/KeyBoardUIController.cs` | MonoBehaviour，使用原生 Input 绕过 TEngine 体系 |
| `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Service/GameService/InputService/KeyBoardUIController.cs.meta` | Unity meta 文件 |
| `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Service/GameService/InputService/` | 空目录（删除文件后） |
| `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Service/GameService/InputService/.meta` | Unity meta 文件 |
| `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Service/GameService/TimerService/` | 已是空目录 |
| `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Service/GameService/TimerService/.meta` | Unity meta 文件 |
| `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Service/GameService/` | 空目录（删除子目录后） |
| `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Service/GameService/.meta` | Unity meta 文件 |
| `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Service/` | 空目录（删除子目录后） |
| `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Service/.meta` | Unity meta 文件 |

**不涉及修改的现有文件：**
- `Assets/TEngine/Runtime/Module/InputModule/InputModule.cs` — 已符合 TEngine Module 规范，无需改动
- `Assets/TEngine/Runtime/Module/InputModule/IInputModule.cs` — 接口已完备，无需改动
- `Assets/GameScripts/HotFix/GameLogic/GameModule.cs` — 已通过 `GameModule.Input` 暴露 IInputModule，无需改动
- 所有状态机调用方文件 — 已直接使用 `GameModule.Input`，无需改动

[Functions]
无函数修改。`InputService` 中的辅助函数（`GetMoveHorizontalValue`、`GetMoveVerticalValue`、`Move` 属性等）经搜索确认无外部调用者，直接删除即可。

[Classes]
- **删除类**：
  - `ThirdPersonController.InputService` — 手动单例包装类，替换方案：调用方直接使用 `GameModule.Input`
  - `ThirdPersonController.InputMap` — 对 `TEngine.InputSystem_Actions` 的冗余包装，替换方案：`InputModule` 已内置
  - `ThirdPersonController.KeyboardUIController` — MonoBehaviour UI 键盘控制，替换方案：由 TEngine UIWindow 体系接管

- **保留类（无需修改）**：
  - `TEngine.InputModule` — 已正确继承 `Module`、实现 `IInputModule` 和 `IUpdateModule`
  - `TEngine.IInputModule` — 接口定义完备

[Dependencies]
无依赖变更。本次操作仅删除文件，不添加、升级或移除任何包/库。

[Testing]
编译验证即可。删除文件后通过 Unity Editor 编译检查，确认无编译错误。由于所有调用方已直接使用 `GameModule.Input`，不存在引用断裂风险。

[Implementation Order]
1. 删除 3 个 `.cs` 源文件（InputService.cs、InputMap.cs、KeyBoardUIController.cs）
2. 删除对应的 3 个 `.cs.meta` 文件
3. 自上而下清理空目录及 meta 文件：
   - 删除 `InputService/` 目录及 .meta
   - 删除 `TimerService/` 目录及 .meta（已为空）
   - 删除 `GameService/` 目录及 .meta
   - 删除 `Service/` 目录及 .meta
4. 触发 Unity 编译（使用 unity-compile skill 或 coplay-unity MCP）
5. 确认编译成功，无 error