## 1. TEngine 资源契约确认

- [x] 1.1 对照 TEngine 资源规范，确认本修复继续使用 `GameModule.Resource`/`ResourceModule`，不新增 `Resources.Load`、AssetDatabase 运行时依赖或硬编码文件路径旁路。
- [x] 1.2 确认当前 `DefaultPackage` collector 仍然使用 `AddressByFileName` 收集 `Assets/AssetRaw/Configs`，并记录 `PackDirectory`、`CollectAll` 规则没有漂移。
- [x] 1.3 确认 `PlayerAnimConfig.asset` 作为文件名生成的 YooAsset location 为无后缀 `PlayerAnimConfig`，与 `TPBattleContext.PLAYER_ANIM_CONFIG_PATH` 一致。

## 2. 资源资产契约

- [x] 2.1 创建 `Assets/AssetRaw/Configs/PlayerAnimConfig.asset` 和 `.meta`，类型为 `GameLogic.PlayerAnimConfig` ScriptableObject。
- [x] 2.2 从现有 `NoneLock` 过渡资产填入基础地面映射：Idle、MoveStart F/R45/R90/R135/R180/L135/L90/L45、MoveLoop、MoveEnd_L、MoveEnd_R。
- [x] 2.3 为 `moveToWall` 和 `lockIdle` 填入安全兜底，避免缺少专用过渡时整段动画播放被跳过。
- [x] 2.4 确认被引用的 `TransitionAsset` 资源也在 TEngine/YooAsset 可收集资源范围内，避免 `PlayerAnimConfig` 可加载但内部引用丢失。

## 3. 运行时校验与诊断

- [x] 3.1 增加 `PlayerAnimConfig` 校验 helper 或等价运行时检查，用于检查基础必填字段。
- [x] 3.2 在 `TPBattleContext` 加载 `PlayerAnimConfig` 后调用校验，并输出准确的缺失地址、加载返回空或缺失基础字段。
- [x] 3.3 确认 `TPBattleContext` 在启动 PlayerWorld 前仍会把加载到的配置注入 `PlayerViewComponent.animConfig`。
- [x] 3.4 保持启动逻辑确定性与表现层边界，不把动画数据写入参与回滚记录的逻辑组件。
- [x] 3.5 保留 TEngine 资源模块的失败语义：地址无效时能看到 `PlayerAnimConfig` location，加载失败时能区分 location 无效与 asset 返回 null。

## 4. PlayerAnimViewSystem 行为

- [x] 4.1 确认 `animInitialized == false` 且配置有效时，首帧会播放 Idle。
- [x] 4.2 确认移动状态切换会对配置好的基础过渡调用 `AnimancerComponent.Play`。
- [x] 4.3 为缺少过渡的可选高级状态增加或优化 warning，日志需写明状态或字段，并避免每帧刷屏。
- [x] 4.4 确认可选高级映射缺失不会影响 Idle 和地面移动映射继续播放。

## 5. 验证

- [x] 5.1 搜索 `PlayerAnimConfig missing`、`Could not found location [PlayerAnimConfig]`、`animConfig == null` 和基础配置字段引用，确认预期代码路径已覆盖。
- [ ] 5.2 在 EditorSimulateMode 下通过 YooAsset/TEngine 资源查询验证 `GameModule.Resource.CheckLocationValid("PlayerAnimConfig")` 为 true。
- [x] 5.3 运行可用的 GameLogic 编译/构建验证，或相关 Unity 生成项目验证。
- [ ] 5.4 在 Unity Editor 中运行 Game 场景，确认不再出现 `PlayerAnimConfig missing` 或 `Could not found location [PlayerAnimConfig]` 错误。
- [ ] 5.5 验证可见玩家进入场景后脱离 T-Pose，先播放 Idle，再在移动和停止时切过 MoveStart、MoveLoop、MoveEnd，并回到 Idle。
- [x] 5.6 运行 `openspec validate fix-player-tpose-missing-anim-config --strict`，并刷新 HTML 审阅页。
