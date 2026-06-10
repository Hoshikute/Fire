# tpc-removal

完全删除 ThirdPersonController 模块——44 个 .cs 文件 + Player.prefab 脚本解绑 + 所有过时注释和引用清理。

## ADDED Requirements

### Requirement: 删除 Player/Controller/ 目录
`GameLogic/Player/Controller/` 目录及其 44 个 .cs 文件 MUST 被完全删除。目录 `GameLogic/Player/` 下的其他非 Controller 文件（如有）SHALL 保留。

#### Scenario: Controller 目录不存在
- **WHEN** 编译项目（`dotnet build` 0 错误）
- **THEN** `GameLogic/Player/Controller/` 路径不存在

### Requirement: Player.prefab 解绑 Player.cs
`Assets/AssetRaw/Actor/Player.prefab` 中挂载的 `Player.cs`（ThirdPersonController.Player）MUST 被移除。若 prefab 仍需角色入口组件，SHALL 替换为 `PlayerFrameSyncEntry`（GameLogic.PlayerFrameSyncEntry）。

#### Scenario: Player.prefab 无 Missing Script
- **WHEN** 在 Unity Editor 中打开 Player.prefab
- **THEN** Inspector 中无 `Missing Script` 警告，挂载的是 `PlayerFrameSyncEntry`

### Requirement: 清理 ThirdPersonController 命名空间引用
项目中所有 `using ThirdPersonController` 和 `using ThirdPersonController.Tool.*` MUST 被删除或替换。仅保留迁移到 TEngine Utility 的工具类引用（`using TEngine.Utility`）。

#### Scenario: 无 TPC 命名空间残留
- **WHEN** 在 `UnityProject/Assets/GameScripts/` 下全文搜索 `using ThirdPersonController`
- **THEN** 无匹配结果（除注释外）

### Requirement: 清理过时注释
ADR 0002 中标记的过时注释——`Module/FrameSync/ClientLogic/PlayerFrameSyncEntry.cs:18` 的"老 TPC 仍可独立存在"——MUST 被删除或更新为新描述。

#### Scenario: 注释反映现状
- **WHEN** 阅读 `PlayerFrameSyncEntry.cs` 头部注释
- **THEN** 不含"老 TPC 仍可独立存在"等描述两套系统并存的过时语句

### Requirement: Character 模块旧 API 移除
`ICharacterModule` 和 `CharacterModule` 中的 `SetThirdPersonPlayerPrefab` / `LoadThirdPersonPlayerAsync` API MUST 不存在（由 character-module-generic 的通用 API 替代后，旧 API 残留需在此阶段清理）。

#### Scenario: 旧 API 编译报错
- **WHEN** 编译项目
- **THEN** 不存在 `SetThirdPersonPlayerPrefab` 或 `LoadThirdPersonPlayerAsync` 符号
