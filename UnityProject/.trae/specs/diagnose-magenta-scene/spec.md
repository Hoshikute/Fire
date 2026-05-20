# Game 场景洋红色诊断与修复工具 Spec

## Why
Game 视图出现大量洋红色通常意味着材质引用的 Shader 丢失/编译失败或渲染管线不匹配。需要一个可重复执行、可定位根因并可选择性修复的编辑器工具，降低排查成本。

## What Changes
- 新增一个可从 Unity 顶部菜单 Tools 下拉调用的“洋红色诊断/修复”工具
- 支持扫描“当前已加载场景”的 Renderer 与材质，输出可定位的报告（对象路径/材质/Shader/原因）
- 提供最小可控的修复能力（默认不改动；修复前二次确认；支持 Undo）

## Impact
- Affected specs: Editor 工具、渲染/材质诊断、资源引用稳定性
- Affected code: 新增 Editor 脚本（Assets/TEngine/Editor 下），可能使用 UnityEditor 的 Shader/Material/RenderPipeline 相关 API

## ADDED Requirements
### Requirement: 场景洋红色诊断
系统 SHALL 提供一个 Tools 菜单入口，用于对“当前已加载场景”的可渲染对象进行洋红色原因分析，并输出报告。

#### Scenario: 成功诊断
- **WHEN** 用户在编辑器中执行 Tools 菜单的“洋红色诊断”
- **THEN** 工具扫描所有已加载场景中的 Renderer（含 inactive，可配置）
- **AND** 对每个 Renderer 的 sharedMaterials 进行检查并分类原因：
  - Shader 引用为空/缺失
  - Shader 为 InternalError/编译失败（可用 Editor API 判定时）
  - 渲染管线不匹配（Built-in vs URP vs HDRP，与材质 Shader 归属不一致时）
  - 材质引用丢失（Renderer 上出现 Missing/Null material slot）
- **AND** 输出诊断结果，包含：
  - 统计汇总（对象数/Renderer 数/材质数/问题材质数、按原因分类计数）
  - 明细列表（场景名、GameObject 层级路径、Renderer 类型、材质 asset 路径或实例标识、Shader 名称、原因）

### Requirement: 最小修复能力（可选）
系统 SHALL 提供一个在明确告知影响范围后才执行的修复流程，且修复可 Undo。

#### Scenario: 修复前确认
- **WHEN** 用户触发“修复”动作
- **THEN** 工具在执行任何资产写入前弹出确认对话框
- **AND** 对话框展示将被修改的材质数量与修复类型
- **AND** 用户取消时不应产生任何变更

#### Scenario: 修复执行（最小集合）
- **WHEN** 用户确认修复
- **THEN** 工具至少支持以下修复动作（按可实现性从易到难，优先实现靠前项）：
  - 对问题材质/Shader 相关资产执行 Reimport/刷新（用于处理导入顺序/编译未触发类问题）
  - 对明显的渲染管线不匹配提供“安全映射修复”（例如项目处于 URP 且材质使用 Built-in Standard 时，可将其切换到 URP/Lit 并做最小属性迁移：主贴图与颜色）
- **AND** 对所有被修改的材质记录 Undo，并标记为 dirty 以便保存
- **AND** 修复结束输出变更报告（哪些材质被改动、从哪个 Shader 切换到哪个 Shader）

## MODIFIED Requirements
无

## REMOVED Requirements
无

