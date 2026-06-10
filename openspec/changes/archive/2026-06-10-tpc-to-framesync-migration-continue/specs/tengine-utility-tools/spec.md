# tengine-utility-tools

四个工具类从 `ThirdPersonController` 命名空间迁移到 TEngine 通用 Utility，去 TPC 耦合。

## ADDED Requirements

### Requirement: MonoSingleton 迁移到 TEngine Utility
`MonoSingleton.cs` MUST 从 `ThirdPersonController.Tool.Singleton` 命名空间移动到 `TEngine.Utility` 目录，namespace 改为 `TEngine.Utility`。文件内类型名和逻辑保持一致。所有原 `using ThirdPersonController.Tool.Singleton` 的引用点 MUST 更新为 `using TEngine.Utility`。

#### Scenario: MonoSingleton 命名空间更新
- **WHEN** 编译项目
- **THEN** 所有引用 `MonoSingleton<T>` 的代码通过新的 `using TEngine.Utility` 解析成功，无编译错误

#### Scenario: MonoSingleton 行为不变
- **WHEN** 任何继承 `MonoSingleton<T>` 的类在运行时被访问
- **THEN** 单例行为（DontDestroyOnLoad、唯一实例）与迁移前完全一致

### Requirement: NoMonoSingleton 迁移到 TEngine Utility
`NoMonoSingleton.cs` MUST 从 `ThirdPersonController.Tool.Singleton` 命名空间移动到 `TEngine.Utility` 目录，namespace 改为 `TEngine.Utility`。所有引用点 MUST 更新。

#### Scenario: NoMonoSingleton 引用更新
- **WHEN** 编译项目
- **THEN** 所有引用 `NoMonoSingleton<T>` 的代码解析成功

### Requirement: BindableProperty 迁移到 TEngine Utility
`BindableProperty.cs` MUST 从 `ThirdPersonController.Tool.BindableProperty` 命名空间移动到 `TEngine.Utility` 目录，namespace 改为 `TEngine.Utility`。类型名和逻辑保持。所有引用点 MUST 更新。帧同步逻辑层（`GameLogic` 命名空间）SHALL NOT 使用 `BindableProperty`（含闭包/事件机制，非确定性），仅在表现层使用。

#### Scenario: BindableProperty 在表现层可用
- **WHEN** `PlayerFrameSyncEntry` 或 `PlayerViewSystem` 中使用 `BindableProperty<T>`
- **THEN** 编译通过，运行时值变更回调正常触发

#### Scenario: BindableProperty 不在逻辑层使用
- **WHEN** 审查 `GameLogic` 命名空间下的所有 `.cs` 文件
- **THEN** 无任何 `BindableProperty` 的引用或 using

### Requirement: ToolFunction 迁移到 TEngine Utility
`ToolFunction.cs` MUST 从 `ThirdPersonController.Tool.ToolFunction` 命名空间移动到 `TEngine.Utility` 目录，namespace 改为 `TEngine.Utility`。其中 `GetDeltaAngle` 和 `GetJumpInitVelocity` 等数学函数保留，UI 颜色辅助函数也一并迁移。帧同步逻辑层如需使用角度/速度计算 SHALL 使用定点数版本，而非原浮点版本。

#### Scenario: ToolFunction 数学函数可用
- **WHEN** 任意代码调用 `ToolFunction.GetDeltaAngle(transform, dir)`
- **THEN** 返回正确的角度差值，与迁移前一致

#### Scenario: 旧命名空间不可解析
- **WHEN** 编译项目
- **THEN** `using ThirdPersonController.Tool.ToolFunction` 在所有文件中已不存在
