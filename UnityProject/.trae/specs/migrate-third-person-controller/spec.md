# 第三人称控制器迁移 Spec

## Why
将外部工程 animator-third-person-controller 的第三人称控制器（含模型等 Assets）迁入当前 TEngine + EGame 项目，并按项目既有资源与热更框架规范组织，降低后续维护与打包成本。

## What Changes
- 导入 `D:\UGitD\animator-third-person-controller\Assets` 中的资源与脚本到本项目，并消除缺失依赖/脚本引用丢失导致的报错
- 对迁入资源进行目录重构，使其符合项目现有的资源分层：
  - 美术类可共享资源进入 `Assets/AssetArt/...`
  - 角色/动画/Prefab/场景等进入 `Assets/AssetRaw/...`
- 在热更侧新增“角色/玩家”模块承载第三人称控制器运行时代码，避免把业务逻辑散落在通用目录中
- 将控制器输入映射接入项目现有输入体系（基于 InputSystem），避免工程内出现重复/冲突的 InputActions 配置
- 提供一个最小可验证场景/入口，用于验证角色可正常控制与播放动画（不要求与战斗系统强耦合）

## Impact
- Affected specs:
  - 资源目录与打包收集规则需要与 TEngine 现有体系对齐（AssetArt / AssetRaw）
  - 热更模块边界：运行时代码进入 `GameScripts/HotFix` 下的新模块
  - 输入系统：需要与现有 InputSystem Actions/输入模块集成
- Affected code:
  - `Assets/GameScripts/HotFix/GameLogic/Module/*`（新增 Character/Player 模块）
  - `Assets/TEngine/Extension/InputModule/*`（InputSystem Actions 适配/扩展）
  - `Assets/AssetArt/*`、`Assets/AssetRaw/*`（新增迁入资源目录）
  - 可能涉及项目 Package（若源工程依赖 Cinemachine/URP 等，需对齐安装/版本）

## ADDED Requirements
### Requirement: ThirdPerson 资源迁移
系统 SHALL 将外部工程第三人称控制器的 Assets 迁入本项目，并按现有资源体系落盘与可构建。

#### Scenario: 资源完整导入
- **WHEN** 执行迁移流程并完成导入
- **THEN** Unity 打开工程无 Missing Script / Missing GUID 报错（允许个别可选特性被禁用，但需明确替代方案）

#### Scenario: 资源目录符合规范
- **WHEN** 迁移完成
- **THEN** 迁入内容按约定落盘到 `AssetArt` 与 `AssetRaw`，且不在根目录新增与现有体系冲突的资源目录

### Requirement: 新增热更角色模块
系统 SHALL 在 `GameScripts/HotFix/GameLogic/Module` 下新增角色模块用于承载第三人称控制器运行时代码，并提供最小示例入口以便验证。

#### Scenario: 模块可被调用
- **WHEN** 游戏启动并进入验证场景
- **THEN** 角色模块完成初始化，角色可响应输入并驱动动画状态（移动/转向/基础动作）

### Requirement: 输入体系对齐
系统 SHALL 复用项目现有 InputSystem 输入体系接入第三人称控制器输入映射，避免重复定义与冲突。

#### Scenario: 输入映射可工作
- **WHEN** 用户在验证场景中进行移动/视角/跳跃等操作
- **THEN** 角色行为与动画表现符合控制器预期，且工程内仅保留一套权威的 InputActions 配置入口（允许以扩展方式追加 ActionMap）

## MODIFIED Requirements
无。

## REMOVED Requirements
无。

