# Animancer 角色控制器 - 快速参考

## 状态列表

| 状态名 | 类名 | 触发条件 |
|--------|------|----------|
| 待机 | `PlayerIdleState` | 默认状态 |
| 起步 | `PlayerMoveStartState` | 按下移动键 |
| 移动循环 | `PlayerMoveLoopState` | 起步动画结束 |
| 停步 | `PlayerMoveEndState` | 松开移动键 |
| 跳跃 | `PlayerJumpState` | 按下跳跃键(无障碍) |
| 下落 | `PlayerFallLoopState` | 离开地面 |
| 落地 | `PlayerLandState` | 接触地面 |
| 攀爬 | `PlayerClimbState` | 按跳跃时检测到可攀爬障碍 |
| 翻越 | `PlayerVaultState` | 低障碍翻越 |
| 悬挂攀爬 | `PlayerLedgeClimbState` | 空中抓墙 |

## 核心API

### 状态切换
```csharp
playerStateMachine.ChangeState(playerStateMachine.idleState);
```

### 播放动画
```csharp
animancer.Play(animationClip);
var state = animancer.Play(transition);
```

### 根运动控制
```csharp
// 完全根运动 (攀爬等)
player.applyFullRootMotion = true;

// 部分根运动 (普通移动)
player.applyFullRootMotion = false;
player.animatorDeltaPositionOffset = extraMovement;
```

### 输入获取
```csharp
Vector2 move = inputServer.Move;      // 移动方向
bool shift = inputServer.Shift;       // 奔跑
bool interactive = inputServer.Interactive; // 交互
```

### 平滑参数
```csharp
reusableData.speedValueParameter.TargetValue = 2;  // 设置目标值
// 自动平滑过渡到目标值
```

## 文件路径速查

```
Assets/AssetRaw/ThirdPersonController/AnimancerController/Scripts/
├── AnimancerController/
│   ├── Core/
│   │   ├── Player/
│   │   │   ├── Player.cs              # 入口
│   │   │   ├── StateMachine/          # 状态机
│   │   │   ├── State/                 # 状态实现
│   │   │   └── Data/                  # 数据定义
│   │   ├── CharacterBase/             # 物理基类
│   │   └── Animation/                 # 动画工具
│   └── Camera/
│       └── CameraController.cs        # 摄像机
└── Service/
    └── GameService/
        ├── InputService/              # 输入服务
        └── TimerService/              # 计时器
```

## 常用事件

```csharp
// 注册输入监听
inputServer.inputMap.Player.Move.started += OnMoveStart;
inputServer.inputMap.Player.Jump.started += OnJumpStart;

// 监听状态变化
player.isOnGround.ValueChanged += OnGroundChanged;
reusableData.lockValueParameter.Parameter.OnValueChanged += OnLockChanged;
```

## 调试技巧

1. **查看当前状态**: `player.ReusableData.currentState.Value`
2. **地面检测 Gizmos**: Scene视图中显示球体
3. **动画事件**: 通过 `AnimationEnd()` 回调
