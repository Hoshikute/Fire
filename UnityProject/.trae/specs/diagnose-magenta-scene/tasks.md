# Tasks
- [x] Task 1: 现状确认与技术路线
  - [x] 确认项目当前渲染管线（Built-in/URP/HDRP）与关键设置入口（Graphics/Quality）
  - [x] 盘点现有 Editor 工具菜单组织方式与 asmdef 约束，确定脚本落点与命名空间

- [x] Task 2: 实现“洋红色诊断”扫描与报告
  - [x] 新增 Tools 菜单入口（至少一个入口可直接执行诊断）
  - [x] 扫描所有已加载场景的 Renderer 与 sharedMaterials（可配置是否包含 inactive）
  - [x] 实现原因分类（Missing material slot / Missing shader / InternalError 或编译失败 / 管线不匹配）
  - [x] 输出报告（控制台摘要 + 可复制的明细；可选写入到工程内指定目录的文本/JSON）

- [x] Task 3: 实现“最小修复”流程（带确认与 Undo）
  - [x] 提供修复入口（菜单项或窗口按钮）
  - [x] 修复前汇总将修改的材质数量与修复类型，弹窗二次确认
  - [x] 实现修复动作：Reimport/刷新（优先）
  - [x] 实现修复动作：URP 项目下 Standard -> URP/Lit 的最小映射（可行则做；不可行则仅提供提示与跳过）
  - [x] 记录 Undo、标记 dirty，并输出修复结果报告

- [x] Task 4: 验证与回归
  - [x] 用 UnityMCP 执行诊断入口自检（包含空场景/空层级场景不抛异常）
  - [x] 在至少一个出现洋红的测试场景中验证：诊断能定位对象与材质（issues>0）
  - [x] 验证：取消修复不产生任何资产变更（代码路径审阅：修复前二次确认，取消即 return）
  - [x] 验证：修复逻辑可执行且可 Undo（用临时材质资产验证 Standard->URP/Lit 后 Undo 还原）

- [x] Task 5: UnityMCP 接入自检（非阻塞）
  - [x] telemetry_ping 连通
  - [x] execute_code 可正常在 Editor 内运行诊断/材质映射自检
  - [x] 临时资产创建与删除流程可闭环（测试用材质已清理）

# Task Dependencies
- Task 2 depends on Task 1
- Task 3 depends on Task 2
- Task 4 depends on Task 2, Task 3
