# 启动默认加载 Game 场景 Spec

## Why
当前项目的启动流程已经通过 `Procedure` 链路进入游戏，但“默认进入 Game 主场景”尚未在规格中被明确约束。将该行为写入规格，可以避免后续改动绕过 TEngine 启动流程或改回同步/直连式场景切换。

## What Changes
- 明确规定主包启动流程在进入 `ProcedureStartGame` 后，默认加载 `Assets/Scenes/Game.unity` 对应的 `Game` 主场景
- 约束场景切换必须通过 `GameModule.Scene` 提供的异步接口完成，保持与 TEngine 场景模块一致
- 约束启动器 UI 在切入主场景前完成隐藏，避免启动界面残留到游戏场景
- 明确该能力不改变现有热更入口与流程链路，仅补充启动默认场景的行为要求

## Impact
- Affected specs:
  - 启动流程：`ProcedureLaunch -> ... -> ProcedureStartGame`
  - 场景管理：通过 `ISceneModule` 进行异步主场景切换
- Affected code:
  - `Assets/GameScripts/Procedure/ProcedureStartGame.cs`
  - `Assets/GameScripts/HotFix/GameLogic/GameModule.cs`
  - `Assets/TEngine/Runtime/Module/SceneModule/*`

## ADDED Requirements
### Requirement: 启动时默认进入 Game 主场景
系统 SHALL 在主包启动流程进入 `ProcedureStartGame` 后，默认切换到 `Assets/Scenes/Game.unity` 对应的 `Game` 主场景，作为游戏运行的首个主场景。

#### Scenario: 启动流程正常进入主场景
- **WHEN** 启动流程完成资源初始化、程序集加载并进入 `ProcedureStartGame`
- **THEN** 系统默认开始加载 `Game` 主场景，而不是停留在启动器界面或空场景

#### Scenario: 通过框架场景模块切换
- **WHEN** 系统执行默认主场景切换
- **THEN** 必须调用 `GameModule.Scene` 的异步场景加载接口完成加载，而不是直接调用 Unity 原生同步场景 API

#### Scenario: 启动器界面在切场景前隐藏
- **WHEN** 系统准备切换到 `Game` 主场景
- **THEN** 启动器相关 UI 已被隐藏，进入主场景后不会残留启动界面

## MODIFIED Requirements
无。

## REMOVED Requirements
无。
