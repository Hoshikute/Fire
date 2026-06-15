## MODIFIED Requirements

### Requirement: FrameConfig 集中配置逻辑帧间隔

系统 SHALL 提供一个 `FrameConfig` 静态类，在 `GameLogic.Module.FrameSync` 目录下，集中定义逻辑帧间隔毫秒数。默认值 SHALL 为 200ms。

#### Scenario: 读取默认帧间隔
- **WHEN** 系统启动且未覆盖配置
- **THEN** `FrameConfig.LogicFrameIntervalMs` 返回 200

#### Scenario: FrameConfig 位于 FrameSync 目录
- **WHEN** 开发者查找帧同步配置
- **THEN** 在 `Module/FrameSync/FrameConfig.cs` 找到该文件
