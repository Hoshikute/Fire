# Animancer 第三人称角色控制器详解

> 本文档介绍项目中基于 Animancer 实现的第三人称角色控制系统。

---

## 一、整体架构

### 1.1 架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                          Player (核心入口)                        │
│  挂载于角色GameObject，包含 AnimancerComponent + CharacterController │
└─────────────────────────────────────────────────────────────────┘
         │
         ├──► PlayerStateMachine (状态机)
         │         │
         │         ├── idleState (待机)
         │         ├── moveStartState (起步)
         │         ├── moveLoopState (移动循环)
         │         ├── moveEndState (停步)
         │         ├── jumpState (跳跃)
         │         ├── climbState (攀爬)
         │         ├── fallLoopState (下落)
         │         └── landState (落地) ...
         │
         ├──► PlayerReusableData (运行时数据缓存)
         │
         ├──► PlayerReusableLogic (复用逻辑算法)
         │
         └──► InputService / TimerService (服务层)
```

### 1.2 核心文件路径

| 模块 | 路径 |
|------|------|
| 玩家入口 | `Assets/AssetRaw/ThirdPersonController/AnimancerController/Scripts/AnimancerController/Core/Player/Player.cs` |
| 状态机基类 | `Assets/AssetRaw/ThirdPersonController/AnimancerController/Scripts/AnimancerController/Core/StateMachine/StateMachine/StateMachineBase.cs` |
| 状态基类 | `Assets/AssetRaw/ThirdPersonController/AnimancerController/Scripts/AnimancerController/Core/StateMachine/State/StateBase.cs` |
| 角色基类 | `Assets/AssetRaw/ThirdPersonController/AnimancerController/Scripts/AnimancerController/Core/CharacterBase/CharacterBase.cs` |
| 共享数据 | `Assets/AssetRaw/ThirdPersonController/AnimancerController/Scripts/AnimancerController/Core/Player/Data/PlayerReusableData/PlayerReusableData.cs` |
| 复用逻辑 | `Assets/AssetRaw/ThirdPersonController/AnimancerController/Scripts/AnimancerController/Core/Player/State/PlayerReusableLogic.cs` |

---

## 二、核心组件

### 2.1 Player - 角色入口类

**文件**: `Player.cs`

**职责**: 作为角色核心入口，初始化并协调各子系统。

```csharp
public class Player : CharacterBase
{
    public PlayerSO playerSO;                   // 配置数据
    public AnimancerComponent animancer;        // Animancer动画组件
    public PlayerStateMachine StateMachine;     // 状态机
    public PlayerReusableData ReusableData;     // 共享数据
    public PlayerReusableLogic ReusableLogic;   // 复用逻辑
    public Transform camTransform;              // 摄像机Transform

    public InputService InputService;           // 输入服务
    public TimerService TimerService;           // 计时器服务

    protected override void Awake()
    {
        base.Awake();
        InputService = InputService.Instance;
        TimerService = TimerService.Instance;

        camTransform = Camera.main.transform;
        animancer = GetComponent<AnimancerComponent>();

        // 初始化共享数据
        ReusableData = new PlayerReusableData(animancer, playerSO);
        // 初始化复用逻辑
        ReusableLogic = new PlayerReusableLogic(this);
        // 初始化状态机
        StateMachine = new PlayerStateMachine(this);
        // 进入默认状态
        StateMachine.ChangeState(StateMachine.idleState);
    }

    protected override void Update()
    {
        base.Update();
        StateMachine?.OnUpdate();
    }

    protected override void OnAnimatorMove()
    {
        base.OnAnimatorMove();
        StateMachine?.OnAnimationUpdate();
    }

    public void AnimationEnd()
    {
        StateMachine?.OnAnimationEnd();
    }
}
```

---

### 2.2 状态机系统

#### StateMachineBase - 状态机基类

**文件**: `StateMachineBase.cs`

```csharp
public class StateMachineBase
{
    public IState currentState;  // 当前状态
    public IState lastState;     // 上一状态

    /// <summary>
    /// 状态切换API
    /// </summary>
    public virtual void ChangeState(IState targetState)
    {
        currentState?.OnExit();      // 退出旧状态
        lastState = currentState;
        currentState = targetState;
        currentState?.OnEnter();     // 进入新状态
    }

    /// <summary>
    /// Update驱动
    /// </summary>
    public void OnUpdate()
    {
        currentState?.OnUpdate();
    }

    /// <summary>
    /// 动画帧更新
    /// </summary>
    public void OnAnimationUpdate()
    {
        currentState?.OnAnimationUpdate();
    }

    /// <summary>
    /// 动画结束回调
    /// </summary>
    public void OnAnimationEnd()
    {
        currentState.OnAnimationEnd();
    }
}
```

#### PlayerStateMachine - 玩家状态机

**文件**: `PlayerStateMachine.cs`

预创建所有状态实例，避免运行时内存分配：

```csharp
public class PlayerStateMachine : StateMachineBase
{
    public Player player;

    // 所有状态实例
    public PlayerIdleState idleState;
    public PlayerMoveStartState moveStartState;
    public PlayerMoveLoopState moveLoopState;
    public PlayerMoveEndState moveEndState;
    public PlayerJumpState jumpState;
    public PlayerClimbState climbState;
    public PlayerLedgeClimbState ledgeClimbState;
    public PlayerMoveToWallState moveWallState;
    public PlayerFallLoopState fallLoopState;
    public PlayerPlatformerUpState platformerUpState;
    public PlayerLandState landState;

    public PlayerStateMachine(Player player)
    {
        this.player = player;
        idleState = new PlayerIdleState(this);
        moveStartState = new PlayerMoveStartState(this);
        moveLoopState = new PlayerMoveLoopState(this);
        moveEndState = new PlayerMoveEndState(this);
        jumpState = new PlayerJumpState(this);
        climbState = new PlayerClimbState(this);
        ledgeClimbState = new PlayerLedgeClimbState(this);
        moveWallState = new PlayerMoveToWallState(this);
        fallLoopState = new PlayerFallLoopState(this);
        platformerUpState = new PlayerPlatformerUpState(this);
        landState = new PlayerLandState(this);
    }

    public override void ChangeState(IState targetState)
    {
        base.ChangeState(targetState);
        // 同步当前状态名称到共享数据
        player.ReusableData.currentState.Value = targetState.GetType().Name;
    }
}
```

---

### 2.3 状态基类层次

```
IState (接口)
    │
    └──► StateBase (抽象基类)
              │  - 持有 Player、InputService、AnimancerComponent 引用
              │  - 定义生命周期方法
              │
              └──► PlayerMovementState (移动状态基类)
                        │  - 旋转、速度、锁敌等通用移动逻辑
                        │
                        ├── PlayerIdleState (待机)
                        ├── PlayerMoveStartState (起步)
                        ├── PlayerMoveLoopState (移动循环)
                        ├── PlayerMoveEndState (停步)
                        ├── PlayerJumpState (跳跃)
                        ├── PlayerFallLoopState (下落)
                        ├── PlayerLandState (落地)
                        ├── PlayerClimbState (攀爬)
                        └── ... 其他状态
```

#### StateBase - 状态基类

**文件**: `StateBase.cs`

```csharp
public abstract class StateBase : IState
{
    protected InputService inputServer;
    protected TimerService timerServer;
    protected Player player;
    protected AnimancerComponent animancer;
    public PlayerReusableData reusableData;
    public Transform cam;
    public PlayerReusableLogic reusableLogic;

    public StateBase(Player player)
    {
        this.player = player;
        inputServer = player.InputService;
        timerServer = player.TimerService;
        reusableData = player.ReusableData;
        cam = player.camTransform;
        animancer = player.animancer;
    }

    public abstract void OnEnter();
    public abstract void OnExit();
    protected abstract void AddEventListening();
    protected abstract void RemoveEventListening();
    public abstract void OnUpdate();
    public abstract void OnAnimationUpdate();
    public abstract void OnAnimationEnd();
}
```

#### PlayerMovementState - 移动状态基类

**文件**: `PlayerMovementState.cs`

提供所有移动相关状态的通用功能：

```csharp
public class PlayerMovementState : StateBase
{
    protected PlayerStateMachine playerStateMachine;
    protected PlayerSO playerSO;

    public PlayerMovementState(PlayerStateMachine stateMachine) : base(stateMachine.player)
    {
        playerStateMachine = stateMachine;
        playerSO = player.playerSO;
    }

    public override void OnEnter()
    {
        AddEventListening();
    }

    public override void OnExit()
    {
        RemoveEventListening();
    }

    public override void OnUpdate()
    {
        // 锁敌旋转
        if (reusableData.lockValueParameter.TargetValue == 1)
        {
            UpdateLockRotation(5, null);
            UpdateLockValue();
        }
        // 处理打断委托
        reusableData.inputInterruptionCB?.Invoke();
    }

    // 更新速度 (走/跑)
    protected float UpdateSpeed()
    {
        return reusableData.speedValueParameter.TargetValue = inputServer.Shift ? 2 : 1;
    }

    // 更新旋转
    protected float UpdateRotation(bool isUpdateRotationParameter = true,
        float rotationSmoothTime = 0.7f, bool isRotationCompensation = true, float rotationSize = 1.4f)
    {
        float angle = GetTargetAngle();
        if (isUpdateRotationParameter)
        {
            reusableData.rotationValueParameter.SmoothTime = rotationSmoothTime;
            reusableData.rotationValueParameter.TargetValue = angle * Mathf.Deg2Rad;
        }
        if (inputServer.Move != Vector2.zero)
        {
            if (isRotationCompensation)
            {
                player.transform.rotation = Quaternion.Slerp(
                    player.transform.rotation,
                    Quaternion.LookRotation(reusableData.targetDir),
                    Time.deltaTime * rotationSize);
            }
            return angle;
        }
        return 0;
    }

    // 获取目标方向 (相对于摄像机)
    protected Vector3 GetTargetDir()
    {
        return Quaternion.Euler(0, cam.eulerAngles.y, 0) *
            new Vector3(inputServer.Move.x, 0, inputServer.Move.y);
    }

    // 空中移动
    protected void InAirMove()
    {
        if (player.isOnGround.Value) return;
        reusableData.horizontalSpeed = Mathf.Lerp(
            reusableData.horizontalSpeed,
            inputServer.Move != Vector2.zero ? 2 : 0,
            1 - Mathf.Exp(-8 * Time.deltaTime));

        if (reusableData.lockValueParameter.TargetValue == 1) // 锁敌
        {
            player.AddHorizontalVelocityInAir(
                GetTargetDir() * reusableData.horizontalSpeed * reusableData.currentMidInAirMultiplier);
        }
        else
        {
            player.AddHorizontalVelocityInAir(
                player.transform.forward * reusableData.horizontalSpeed * reusableData.currentMidInAirMultiplier);
        }
    }
}
```

---

### 2.4 具体状态实现示例

#### PlayerIdleState - 待机状态

**文件**: `PlayerMovemenState/PlayerIdleState.cs`

```csharp
public class PlayerIdleState : PlayerMovementState
{
    PlayerIdleData idleData;

    public PlayerIdleState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        idleData = playerSO.playerMovementData.PlayerIdleData;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        reusableData.currentCrouchIdleIndex = -1;
        reusableData.currentStandIdleIndex = -1;
        reusableLogic.InitIldeState();    // 初始化Idle动画
        reusableLogic.PlayNextState();     // 播放
    }

    protected override void AddEventListening()
    {
        base.AddEventListening();
        // 监听输入事件
        inputServer.inputMap.Player.Move.started += MoveStart;
        inputServer.inputMap.Player.Jump.started += OnJumpStart;
        inputServer.inputMap.Player.Crouch.started += OnCrouch;
        // 监听状态变化
        player.isOnGround.ValueChanged += OnCheckFall;
        reusableData.lockValueParameter.Parameter.OnValueChanged += LockValueChange;
    }

    protected override void RemoveEventListening()
    {
        base.RemoveEventListening();
        inputServer.inputMap.Player.Move.started -= MoveStart;
        inputServer.inputMap.Player.Jump.started -= OnJumpStart;
        inputServer.inputMap.Player.Crouch.started -= OnCrouch;
        player.isOnGround.ValueChanged -= OnCheckFall;
        reusableData.lockValueParameter.Parameter.OnValueChanged -= LockValueChange;
    }

    private void MoveStart(InputAction.CallbackContext context)
    {
        playerStateMachine.ChangeState(playerStateMachine.moveStartState);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        UpdateCashVelocity(player.AnimationVelocity);
        UpdateSpeed();
    }
}
```

---

### 2.5 CharacterBase - 角色物理基类

**文件**: `CharacterBase.cs`

处理重力、地面检测、根运动等物理相关逻辑：

```csharp
[RequireComponent(typeof(Animator), typeof(CharacterController))]
public class CharacterBase : MonoBehaviour
{
    public CharacterController controller;
    public Animator animator;

    [Header("重力设置")]
    [SerializeField] public float gravity = -12;
    [SerializeField] public Vector2 velocityLimit = new Vector2(-20, 60);
    [SerializeField] public LayerMask whatIsGround;
    [SerializeField] private float groundDetectedOffset = -0.06f;
    [SerializeField] private float groundRadius = 1.2f;

    public BindableProperty<bool> isOnGround;  // 是否在地面
    public float verticalSpeed;                 // 垂直速度

    // 根运动控制
    public bool applyFullRootMotion = false;   // 完全使用根运动
    public bool disEnableRootMotion = false;   // 禁用根运动
    public bool ignoreRootMotionY = false;     // 忽略Y轴根运动
    public bool disEnableGravity = false;      // 禁用重力

    protected virtual void Update()
    {
        CheckOnGround();           // 地面检测
        CharacterGravity();        // 重力计算
        CharacterVerticalVelocity(); // 应用速度
        ResetHorizontalVelocity();
    }

    #region 地面检测
    private bool CheckOnGround()
    {
        detectedOrigin = transform.position - groundDetectedOffset * Vector3.up;
        var isHit = Physics.CheckSphere(detectedOrigin, groundRadius, whatIsGround, QueryTriggerInteraction.Ignore);
        isOnGround.Value = isHit && verticalSpeed < 0;
        return isOnGround.Value;
    }
    #endregion

    #region 重力处理
    private void CharacterGravity()
    {
        if (disEnableGravity) return;

        if (isOnGround.Value)
        {
            verticalSpeed = -2;  // 贴地速度
        }
        else
        {
            verticalSpeed += Time.deltaTime * gravity;
            verticalSpeed = Mathf.Clamp(verticalSpeed, velocityLimit.x, velocityLimit.y);
        }
    }
    #endregion

    #region 根运动处理
    protected virtual void OnAnimatorMove()
    {
        if (disEnableRootMotion) return;

        if (applyFullRootMotion)
        {
            // 完全使用动画根运动 (攀爬等)
            animator.ApplyBuiltinRootMotion();
        }
        else
        {
            // 部分采用根运动，可叠加额外位移
            Vector3 animationMovement = animator.deltaPosition + animatorDeltaPositionOffset;
            if (ignoreRootMotionY)
            {
                animationMovement.y = 0;
            }
            moveDir = SetDirOnSlop(animationMovement) * moveSpeedMult;
            UpdateCharacterMove(moveDir, animator.deltaRotation);
        }
    }
    #endregion

    #region 斜坡处理
    private Vector3 SetDirOnSlop(Vector3 dir)
    {
        if (Physics.Raycast(transform.position, Vector3.down, out var hitInfo, 1))
        {
            if (Vector3.Dot(hitInfo.normal, Vector3.up) != 1)
            {
                return Vector3.ProjectOnPlane(dir, hitInfo.normal);
            }
        }
        return dir;
    }
    #endregion
}
```

---

## 三、Animancer 动画系统

### 3.1 什么是 Animancer？

Animancer 是一个**无状态机 Animator** 替代方案，特点：

- 直接用代码控制动画播放
- 支持动画混合树、过渡、事件
- 无需在 Animator Controller 中配置状态图
- 性能优于传统 Animator Controller

### 3.2 项目中的使用方式

#### 数据配置

通过 ScriptableObject 配置动画数据：

```csharp
// PlayerMovementData.cs
[System.Serializable]
public class PlayerMovementData
{
    public PlayerIdleData PlayerIdleData;
    public PlayerMoveStartData PlayerMoveStartData;
    public PlayerMoveLoopData PlayerMoveLoopData;
    public PlayerMoveEndData PlayerMoveEndData;
    public PlayerClimbData PlayerClimbData;
    public PlayerHangWallData PlayerHangWallData;
    public PlayerJumpFallAndLandData PlayerJumpFallAndLandData;
}
```

#### 播放动画

```csharp
// 直接播放
animancer.Play(animationClip);

// 获取状态并操作
var state = animancer.Play(playerMovementData.PlayerIdleData.idle);
var mixerState = state.GetChild(0).GetChild(1) as ManualMixerState;
```

#### 平滑参数

用于动画混合树的参数平滑过渡：

```csharp
public class PlayerReusableData
{
    public SmoothedFloatParameter standValueParameter;     // 站立/蹲下
    public SmoothedFloatParameter speedValueParameter;     // 移动速度
    public SmoothedFloatParameter rotationValueParameter;  // 旋转
    public SmoothedFloatParameter lockValueParameter;      // 锁敌状态

    public PlayerReusableData(AnimancerComponent animancerComponent, PlayerSO playerSO)
    {
        standValueParameter = new SmoothedFloatParameter(
            animancerComponent,
            playerSO.playerParameterData.standValueParameter,
            0.15f);  // 平滑时间
        // ...
    }
}
```

---

## 四、数据驱动设计

### 4.1 配置结构

```
PlayerSO (角色配置)
    │
    ├── playerMovementData (移动动画数据)
    │       ├── PlayerIdleData
    │       ├── PlayerMoveStartData
    │       ├── PlayerMoveLoopData
    │       ├── PlayerMoveEndData
    │       ├── PlayerClimbData
    │       └── PlayerHangWallData
    │
    └── playerParameterData (动画参数名配置)
            ├── standValueParameter
            ├── rotationValueParameter
            ├── speedValueParameter
            └── LockValueParameter
```

### 4.2 优点

- 策划可在 Inspector 中配置动画
- 配置与逻辑分离
- 便于热更新和维护

---

## 五、共享数据与复用逻辑

### 5.1 PlayerReusableData - 运行时数据缓存

**文件**: `PlayerReusableData.cs`

```csharp
public class PlayerReusableData
{
    // 动画参数
    public SmoothedFloatParameter standValueParameter;
    public SmoothedFloatParameter rotationValueParameter;
    public SmoothedFloatParameter speedValueParameter;
    public SmoothedFloatParameter lockValueParameter;

    // 锁敌
    public BindableProperty<Transform> lockTarget;

    // 移动
    public Vector3 targetDir;
    public BindableProperty<float> targetAngle;
    public BindableProperty<string> currentState;

    // Idle动画状态缓存
    public ManualMixerState standIdleMixerState;
    public ManualMixerState crouchIdleMixerState;
    public List<AnimancerState> standIdleList = new List<AnimancerState>();
    public List<AnimancerState> crouchIdleList = new List<AnimancerState>();

    // 攀爬
    public ObstructHeight ObstructHeight;
    public ClimbType ClimbType;
    public ClipTransition targetClimbClip;
    public Vector3 vaultPos;
    public RaycastHit hit;

    // 跳跃
    public float horizontalSpeed;
    public Vector3 currentInertialVelocity;
    public float jumpExternalForce = 15;

    // 打断回调
    public Action inputInterruptionCB;
}
```

### 5.2 PlayerReusableLogic - 复用逻辑

**文件**: `PlayerReusableLogic.cs`

封装可被多个状态共用的逻辑算法：

- `InitIldeState()` - 初始化Idle动画
- `PlayNextState()` - 播放下一个Idle变体
- `OnJump()` - 跳跃时的攀爬/翻越检测
- `GetWallHight()` - 墙壁高度检测
- `VaultOrClimb()` - 判断翻越还是攀爬
- `ClimbTargetMatch()` - 攀爬位置匹配
- `InAirMoveCheck()` - 空中移动检测

---

## 六、输入系统

### 6.1 InputService

**文件**: `Service/GameService/InputService/InputService.cs`

封装 Unity InputSystem，提供统一输入接口：

```csharp
public class InputService : MonoSingleton<InputService>
{
    public InputMap inputMap;

    // 移动输入
    public Vector2 Move { get; }

    // 奔跑
    public bool Shift { get; }

    // 交互
    public bool Interactive { get; }

    // 滚轮
    public Vector2 Scroll { get; }
}
```

### 6.2 InputMap

使用 Unity InputSystem 生成的输入映射类。

---

## 七、状态流转示例

### 7.1 Idle → Move 完整流程

```
1. PlayerIdleState.OnEnter()
   ├── 初始化Idle动画
   └── 注册输入监听: Move.started → MoveStart()

2. 用户按下移动键
   ├── InputService 触发事件
   └── PlayerIdleState.MoveStart() 被调用

3. 状态切换
   ├── PlayerIdleState.OnExit() → 移除事件监听
   ├── StateMachine.ChangeState(moveStartState)
   └── PlayerMoveStartState.OnEnter() → 播放起步动画

4. 起步动画播放完成
   ├── Animancer 触发 OnEnd 回调
   └── 状态切换到 moveLoopState

5. 用户持续按住移动键
   └── PlayerMoveLoopState.OnUpdate() → 更新移动、旋转

6. 用户松开移动键
   ├── 切换到 moveEndState
   └── 最终回到 idleState
```

### 7.2 跳跃与攀爬流程

```
1. 用户按下跳跃键
   └── PlayerIdleState.OnJumpStart()

2. PlayerReusableLogic.OnJump()
   ├── 检测前方障碍物高度
   ├── 判断障碍物类型
   │
   ├── 无障碍/太高 → jumpState (普通跳跃)
   ├── 高障碍 → jumpState (普通跳跃)
   ├── 中等高度 → climbState (攀爬)
   └── 低障碍 → climbState (翻越)
```

---

## 八、根运动控制模式

| 模式 | applyFullRootMotion | 使用场景 |
|------|---------------------|----------|
| **完全根运动** | `true` | 攀爬、翻越等需要精确位置匹配的动画 |
| **部分根运动** | `false` | 普通移动，可叠加程序位移、处理斜坡 |

### 8.1 完全根运动示例

```csharp
// 攀爬状态中
player.applyFullRootMotion = true;
player.controller.enabled = false;
animancer.Play(climbAnimation);
```

### 8.2 部分根运动示例

```csharp
// 普通移动
player.applyFullRootMotion = false;
// 可以叠加额外位移
player.animatorDeltaPositionOffset = extraMovement;
```

---

## 九、设计模式总结

| 模式 | 应用位置 | 说明 |
|------|---------|------|
| **状态模式** | StateMachine + StateBase | 角色行为由状态决定，状态间独立切换 |
| **单例模式** | InputService, TimerService | 全局访问服务 |
| **观察者模式** | BindableProperty, InputSystem | 事件驱动，解耦模块 |
| **数据驱动** | PlayerSO 系列 | 配置与逻辑分离 |
| **策略模式** | ClimbType 枚举 | 不同攀爬策略 |
| **模板方法** | StateBase 抽象类 | 定义状态生命周期模板 |

---

## 十、扩展指南

### 10.1 添加新状态

1. 在 `PlayerMovemenState/` 下创建新状态类，继承 `PlayerMovementState`
2. 在 `PlayerStateMachine` 中添加状态实例
3. 在 `PlayerMovementData` 中添加对应数据配置
4. 在其他状态中添加切换逻辑

### 10.2 添加新输入

1. 在 InputMap 中添加新输入
2. 在 `InputService` 中添加访问接口
3. 在需要的状态中注册监听

---

## 参考资料

- [Animancer 官方文档](https://kybernetik.com.au/animancer/)
- [Unity InputSystem](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest)
