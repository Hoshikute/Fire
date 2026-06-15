## ADDED Requirements

### Requirement: FrameConfig 集中配置逻辑帧间隔

系统 SHALL 提供一个 `FrameConfig` 静态类，在 `GameLogic.Module` 命名空间下，集中定义逻辑帧间隔毫秒数。默认值 SHALL 为 200ms。

#### Scenario: 读取默认帧间隔
- **WHEN** 系统启动且未覆盖配置
- **THEN** `FrameConfig.LogicFrameIntervalMs` 返回 200

### Requirement: FrameSyncModule 从 FrameConfig 读取帧间隔

`FrameSyncModule` SHALL 在 `OnInit()` 时从 `FrameConfig.LogicFrameIntervalMs` 读取逻辑帧间隔，替代硬编码的 200。`IntervalTime` 属性 SHALL 继续可用，但默认值来源变为 `FrameConfig`。

#### Scenario: 帧同步模块以配置的间隔推进
- **WHEN** `FrameConfig.LogicFrameIntervalMs` 配置为 100ms
- **THEN** `FrameSyncModule` 的 `FixedUpdateWorlds` 每 100ms 调用一次

### Requirement: PlayerStateSystem 帧阈值基于配置计算

`PlayerStateSystem` 中的帧计数阈值（`MoveStartFrames`、`MoveEndFrames`、`LandBufferFrames`、`InteractMinFrames`）SHALL 以毫秒意图常量形式表达，实际帧数 SHALL 在运行时根据 `FrameConfig.LogicFrameIntervalMs` 计算。

#### Scenario: MoveStart 持续时间不随帧率改变
- **WHEN** `FrameConfig.LogicFrameIntervalMs` 配置为 100ms
- **THEN** MoveStart 状态持续 2 帧（200ms ÷ 100ms = 2），与原 200ms/帧 时持续 1 帧的时间长度一致

### Requirement: PlayerViewSystem 插值时长从 FrameConfig 读取

`PlayerViewSystem` SHALL 从 `FrameConfig` 获取逻辑帧时长用于表现层插值计算，不再使用硬编码的 `LogicFrameDuration = 0.2f` 常量。

#### Scenario: 插值计算适配新帧率
- **WHEN** `FrameConfig.LogicFrameIntervalMs` 配置为 100ms
- **THEN** `PlayerViewSystem` 的插值分母 `LogicFrameDuration` 为 0.1f，每个逻辑帧之间的表现插值在 100ms 完成

### Requirement: FrameConfig 提供全局逻辑帧计数器

`FrameConfig` SHALL 提供 `public static long FrameId` 属性，记录自系统启动以来经过的逻辑帧总数。`FrameSyncModule` SHALL 在每次 `FixedUpdateWorlds` 调用时递增 `FrameConfig.FrameId`。

#### Scenario: FrameId 随逻辑帧递增
- **WHEN** `FrameSyncModule` 执行一次 `FixedUpdateWorlds`
- **THEN** `FrameConfig.FrameId` 的值比调用前大 1

#### Scenario: 无需 World 引用即可获取帧号
- **WHEN** 任意代码需要获取当前逻辑帧号
- **THEN** 通过 `FrameConfig.FrameId` 即可读取，无需持有 `WorldBase` 引用
