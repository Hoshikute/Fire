## 1. 代码重命名

- [x] 1.1 重命名 `ITPBattleContext` → `IBattleContext`（文件 + 接口名）
- [x] 1.2 重命名 `TPBattleContext` → `BattleContext`（文件 + 类名 + TraceHeader）
- [x] 1.3 更新 `GameModule.cs` 中 `TPBattleContext` 属性的名称和返回类型
- [x] 1.4 更新 `LoginUI.cs` 和 `LoginWindow.cs` 的调用
- [x] 1.5 更新 `PlayerViewComponent.cs` 和 `PlayerAnimConfig.cs` 注释

## 2. 知识库更新

- [x] 2.1 更新 `.knowledge/` 下所有文档中 `TPBattleContext` → `BattleContext`

## 3. 验证

- [x] 3.1 确认编译通过，零残留 `TPBattleContext` 引用
