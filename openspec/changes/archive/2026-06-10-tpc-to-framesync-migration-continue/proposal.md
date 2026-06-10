## Why

老 ThirdPersonController（44 个 .cs，命名空间 `ThirdPersonController`）基于 Animancer Root Motion + Unity Physics + TEngine FSM 状态机，与帧同步"确定性定点运算 + 可回滚"架构根本对立。ADR 0002 已定下"完全删除 TPC，迁移到 TEngine + FrameSync"的完整路线，P0-P2 第 1-2 轮已完成（接地抽象、角色状态机），但剩余的斜坡/碰撞/攀爬/翻越/动画降级等核心轮次尚未推进。继续推进迁移，最终完全移除旧 TPC，实现"逻辑确定可回滚、表现只读不写"的纯 ECS 架构。

## What Changes

- **P0 工具类迁移**：`MonoSingleton`、`NoMonoSingleton`、`BindableProperty`、`ToolFunction` 四个工具从 `ThirdPersonController` 命名空间迁移到 TEngine 通用 Utility，去 TPC 耦合
- **P1 Character 模块去 TPC 化**：`GameModule.Character` 改名通用 `LoadCharacterAsync(location)`，脱离 `ThirdPersonPlayer` 前缀，供 FrameSync 复用加载角色 prefab **BREAKING**：API 重命名
- **P2 第 3 轮 斜坡**：确定性地面法线查询 + 坡度限制 + 沿斜面投影位移
- **P2 第 4 轮 碰撞阻挡**：确定性碰撞体系（AABB/胶囊 vs 定点静态几何），角色不穿墙
- **P2 第 5 轮 攀爬/翻越**：把动画曲线位移改成确定性程序化位移（按逻辑帧推进的固定轨迹）
- **P2 第 6 轮 表现层动画**：`PlayerAnimViewSystem` 读 `PlayerStateComponent` 枚举驱动 Animancer 播放（纯表现，不回写逻辑）
- **P2 第 7 轮 手感打磨**：惯性、转向插值、加减速曲线（定点近似）
- **P3 老 TPC 动画降为纯表现**：确认所有 Animancer 调用只读逻辑状态，不再反向驱动位移
- **P4 TPBattleContext 切到 FrameSync 角色**：业务入口从 `Character.SetThirdPersonPlayerPrefab` 切到 `PlayerFrameSyncEntry`
- **P5 删除老 TPC**：删除 `Player/Controller/` 44 文件、`Player.prefab` 解绑 `Player.cs`、清理过时注释 **BREAKING**：旧 API 全部移除

## Capabilities

### New Capabilities
- `tengine-utility-tools`: 四个通用工具类（MonoSingleton/NoMonoSingleton/BindableProperty/ToolFunction）从 TPC 迁移到 TEngine Utility，去 ThirdPersonController 命名空间
- `character-module-generic`: Character 模块 API 去 TPC 化，改为通用 `LoadCharacterAsync(location)`，支持 FrameSync 和任何未来角色类型
- `framesync-slope`: 确定性斜坡处理——地面法线查询、坡度限制、沿斜面投影位移
- `framesync-collision`: 确定性碰撞体系——AABB/胶囊碰撞体 vs 定点静态几何，阻挡/滑动
- `framesync-climb-vault`: 确定性攀爬/翻越——程序化定点轨迹替代动画曲线位移
- `framesync-anim-presentation`: 表现层动画驱动——PlayerAnimViewSystem 只读逻辑状态枚举，驱动 Animancer 播放，不回写
- `framesync-movement-polish`: 移动手感打磨——定点惯性、转向插值、加减速曲线
- `battle-framesync-switch`: TPBattleContext 切换到 FrameSync 角色入口
- `tpc-removal`: 完全删除 ThirdPersonController 模块（44 文件 + prefab 解绑 + 清理）

### Modified Capabilities
<!-- No existing specs to modify -->

## Impact

- **受影响的代码**：`GameLogic/Player/Controller/`（44 文件，将删除）、`Module/Character/`（API 重命名）、`Context/TPBattleContext.cs`（入口切换）、`Module/FrameSync/GameLogic/`（新增 System/Component）、`Module/FrameSync/ClientLogic/`（新增表现 System）、`Assets/AssetRaw/Actor/Player.prefab`（解绑旧脚本）
- **API 变更**：`ICharacterModule` 的 `SetThirdPersonPlayerPrefab` / `LoadThirdPersonPlayerAsync` 改名 `SetCharacterPrefab` / `LoadCharacterAsync`（**BREAKING**）
- **依赖关系**：P0→P1→P2(轮次 3-7)→P3→P4→P5，每一阶段独立可编译验证，不破坏已完成的轮次
- **风险**：P2 第 4 轮碰撞需要确定性场景几何数据来源（待确认方案）；P2 第 5 轮攀爬需要提取老动画曲线关键位移量转定点轨迹表
