# framesync-movement-polish

移动手感打磨——定点惯性、转向插值、加减速曲线，使 FrameSync ECS 角色的操作手感接近老 TPC。

## ADDED Requirements

### Requirement: 定点惯性系统
角色在停止水平输入后 SHALL 不立即停止，而是按指数衰减曲线减速到零。惯性参数（衰减系数、最小速度阈值）MUST 以定点整数形式存储在 `PlayerMoveComponent` 中，在 `PlayerMoveSystem.Step` 中应用。

#### Scenario: 松开方向键后逐渐减速
- **WHEN** 角色从跑步状态松开方向键
- **THEN** 角色在 0.5-1 秒内逐渐减速到零，而非立即停止

### Requirement: 定点转向插值
角色朝向改变时 SHALL 使用定点旋转插值（`SyncQuaternion.Slerp` 或等效定点算法），而非突变。转向速度（每逻辑帧最大旋转角，定点）MUST 可配置。

#### Scenario: 角色平滑转身
- **WHEN** 输入方向从前方突变为左方
- **THEN** `faceDir` 在若干逻辑帧内逐步过渡到新方向，视觉上平滑转身

### Requirement: 定点加减速曲线
角色从静止到满速（或从满速到停止）SHALL 遵循加速曲线而非瞬时达到。加速/减速参数（加速率、减速率，定点整数）MUST 在 `PlayerMoveComponent` 或配置 SO 中可配置。

#### Scenario: 起跑有加速过程
- **WHEN** 角色从 Idle 状态开始移动
- **THEN** `moveSpeed` 从 0 逐步增加到 WalkSpeed/RunSpeed，加速耗时约 0.3 秒（~2 个逻辑帧）

### Requirement: 空中惯性保持
角色离地后 SHALL 保留离地前一帧的水平速度（惯性），空中可用输入以降低的比例微调方向。空中控制系数（`AirControlFixed`）已在 `PlayerMoveSystem` 中定义，本需求确保其数值和手感可配置、可调优。

#### Scenario: 跳跃中微调方向
- **WHEN** 角色在空中且有水平输入
- **THEN** 角色在空中移动速度约为地面速度的 60%（AirControlFixed = 600/1000），可微调方向但不如地面灵敏
