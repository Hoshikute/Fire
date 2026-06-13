## 1. 预加载链路回退

- [x] 1.1 移除 `Player.Awake()` 中的 `PreloadAllAnimations()` 调用
- [x] 1.2 删除 `Player.PreloadAllAnimations()`、`DIAG_HEADER`、`_frameHeartbeatCounter` 以及预加载/heartbeat/dt spike 临时日志
- [x] 1.3 删除 `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Core/Player/Data/IAnimationProvider.cs` 及其 `.meta`

## 2. 动画数据类还原

- [x] 2.1 从 `PlayerMovementData` 和 `PlayerLockMovementData` 移除 `IAnimationProvider` 实现、`CollectTransitions()` 方法和仅为该方法新增的 using
- [x] 2.2 从 `PlayerIdleData`、`PlayerMoveStartData`、`PlayerMoveLoopData`、`PlayerMoveEndData` 移除 `IAnimationProvider` 实现、`CollectTransitions()` 方法和仅为该方法新增的 using
- [x] 2.3 从 `PlayerJumpFallAndLandData`、`PlayerClimbData`、`PlayerHangWallData` 移除 `IAnimationProvider` 实现、`CollectTransitions()` 方法和仅为该方法新增的 using
- [x] 2.4 确认所有 serialized animation reference 字段保持不变，不改 `PlayerSO` 资源结构

## 3. 边界保护

- [x] 3.1 确认不回退 `PlayerMoveStartState` 的提前衔接、输入释放、Animancer OnEnd 延迟兜底逻辑
- [x] 3.2 确认不回退 `PlayerMovementFsmState` 的非接地下落兜底逻辑
- [x] 3.3 确认不回退 `Assets/AssetRaw/Actor/Player.prefab` 的 `whatIsGround` 配置修复

## 4. 验证

- [x] 4.1 用 `rg` 确认 `PreloadAllAnimations`、`IAnimationProvider`、`CollectTransitions`、`[Preload]` 不再残留
- [x] 4.2 运行可用的 C# 编译或 Unity 相关静态检查；如果本机缺少目标框架，记录阻塞原因
- [x] 4.3 运行 `openspec validate revert-animation-preload-changes --strict` 并修复所有规范问题
- [x] 4.4 刷新 `Temp/openspec-html/revert-animation-preload-changes/index.html`
- [ ] 4.5 在 Unity 中运行 `Game.unity`，确认初始化阶段没有 `[Preload]` 日志，玩家基础 Idle/MoveStart/MoveLoop/Fall/Land 状态仍可用
