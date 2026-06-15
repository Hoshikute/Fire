## 为什么

FrameSync 玩家动画现在已经能成功加载 `PlayerAnimConfig`，也已经脱离 T-Pose 路径，但进入 Game 场景后，角色仍可能在逻辑状态为 `Idle` 时显示成蹲伏或偏低的待机姿态。运行日志已经显示 `PlayerAnimViewSystem` 正在播放 `Idle`，所以问题不是状态机切错状态，而是待机过渡资源和默认 mixer 参数选到了不符合预期的姿态。

这个问题需要从之前的 `fix-player-tpose-missing-anim-config` 中独立出来处理。前一个变更解决的是资源发现和缺失配置诊断；当前剩余问题是“站立待机”的表现契约：加载到的 Idle 资源必须默认呈现站立姿态，并且不能把表现层专用的动画参数写入确定性的 FrameSync 逻辑。

## 变更内容

- 修正 FrameSync 玩家 Idle 动画映射，使 `PlayerLogicState.Idle` 默认显示站立待机姿态。
- 保留现有 TEngine/YooAsset 的 `PlayerAnimConfig` 资源加载路径，不恢复旧 `ThirdPersonController` 运行时接线。
- 如果站立待机需要设置 Animancer mixer 参数，该参数只能由表现层驱动，不能存入回滚快照或确定性逻辑组件。
- 保留蹲伏、锁定、备用待机等资源，给后续明确状态使用；但它们不能成为非锁定 Idle 的默认姿态。
- 补充有针对性的诊断或校验，让后续 Idle 映射回退时能从日志或资源检查里快速定位。

## 能力

### 新增能力
- `player-idle-animation-pose`：定义 FrameSync 玩家默认 Idle 姿态行为，以及表现层如何从 Animancer 过渡资源中选择站立待机。

### 修改能力

## 影响范围

- 运行时动画表现：`UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/FrameSync/ClientLogic/System/PlayerAnimViewSystem.cs`。
- 动画配置资产：`UnityProject/Assets/AssetRaw/Configs/PlayerAnimConfig.asset`。
- 相关 Animancer 过渡资源：`UnityProject/Assets/AssetRaw/ThirdPersonController/AnimancerController/Resources/Config/PlayerAnimacer/`。
- 验证范围：Game 场景启动、首次 Idle 播放、短暂移动切换、回到 Idle。
