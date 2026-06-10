# character-module-generic

`GameModule.Character` 模块 API 去 TPC 化，从专用于 `ThirdPersonPlayer` 改为通用角色加载接口。

## ADDED Requirements

### Requirement: Character 模块 API 通用化
`ICharacterModule` 接口 MUST 将 `SetThirdPersonPlayerPrefab(string prefabName, GameObject prefab)` 重命名为 `SetCharacterPrefab(string location, GameObject prefab)`，将 `LoadThirdPersonPlayerAsync(string prefabName)` 重命名为 `LoadCharacterAsync(string location)`。参数名从 `prefabName` 改为 `location`，语义从"TPC 专属"改为"通用角色标识"。`CharacterModule` 实现 MUST 同步更新。

#### Scenario: FrameSync 调用通用 API 加载角色
- **WHEN** `GameModule.Character.LoadCharacterAsync("hero_01")` 被调用
- **THEN** 按 location 异步加载对应 prefab，返回 GameObject，行为与旧 `LoadThirdPersonPlayerAsync` 一致

#### Scenario: 旧 API 不存在
- **WHEN** 编译项目
- **THEN** `SetThirdPersonPlayerPrefab` 和 `LoadThirdPersonPlayerAsync` 不存于 `ICharacterModule` 和 `CharacterModule` 中

### Requirement: Character 模块内部成员去 TPC 化
`CharacterModule` 内部所有以 `ThirdPersonPlayer` 为前缀/后缀的私有字段、方法、局部变量 MUST 改为通用命名（如 `_playerPrefab` → `_characterPrefab`、`m_ThirdPersonPlayerCache` → `m_characterCache`）。

#### Scenario: 模块内无 TPC 残留
- **WHEN** 在 `Module/Character/` 目录下全文搜索 `ThirdPersonPlayer`
- **THEN** 无匹配结果

### Requirement: GameModule 门面注册不变
`GameModule.cs` 中对 `CharacterModule` 的注册和属性暴露 MUST 保持原有结构，仅属性类型中的 API 名称跟随接口变化。

#### Scenario: GameModule.Character 可访问
- **WHEN** 代码引用 `GameModule.Character.LoadCharacterAsync(...)`
- **THEN** 编译通过，调用成功
