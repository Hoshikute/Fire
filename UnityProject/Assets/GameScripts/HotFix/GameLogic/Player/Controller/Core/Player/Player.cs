using Animancer;
using GameLogic;
using TEngine;
using UnityEngine;

namespace ThirdPersonController
{
    [RequireComponent(typeof(AnimancerComponent))]
    public class Player : CharacterBase
    {
        public PlayerSO playerSO;

        [Header("Camera")]
        [SerializeField] private Transform _cameraTransform;

        public AnimancerComponent Animancer { get; private set; }
        public IFsm<Player> StateMachine { get; private set; }
        public PlayerReusableData ReusableData { get; private set; }
        public PlayerReusableLogic ReusableLogic { get; private set; }
        public Transform CamTransform { get; private set; }

        public InputService InputService { get; private set; }
        public TimerService TimerService { get; private set; }

        private PlayerFsmState _currentState;

        protected override void Awake()
        {
            base.Awake();
            InputService = InputService.Instance;
            TimerService = TimerService.Instance;

            // 三层回退逻辑，确保 CamTransform 不为 null
            if (_cameraTransform != null)
            {
                CamTransform = _cameraTransform;
            }
            else if (GameModule.Camera.MainCamera != null)
            {
                CamTransform = GameModule.Camera.MainCamera.transform;
            }
            else
            {
                // 回退到场景中的主相机
                CamTransform = Camera.main?.transform;

                if (CamTransform == null)
                {
                    Debug.LogError("[Player] 无法获取相机引用，请检查场景中是否有 MainCamera");
                }
            }

            // 绑定 Cinemachine 到 Player（仅当相机有效时）
            if (CamTransform != null)
            {
                GameModule.Camera.BindCinemachineToPlayer(transform);
            }

            Animancer = GetComponent<AnimancerComponent>();
            if (Animancer == null)
            {
                Debug.LogError("Animancer component not found, unable to play animations");
                return;
            }
            ReusableData = new PlayerReusableData(Animancer, playerSO);
            ReusableLogic = new PlayerReusableLogic(this);

            // 使用 TEngine FsmModule 创建状态机
            var fsmModule = ModuleSystem.GetModule<IFsmModule>();
            StateMachine = fsmModule.CreateFsm("PlayerFSM", this,
                new PlayerIdleState(),
                new PlayerMoveStartState(),
                new PlayerMoveLoopState(),
                new PlayerMoveEndState(),
                new PlayerJumpState(),
                new PlayerClimbState(),
                new PlayerLedgeClimbState(),
                new PlayerMoveToWallState(),
                new PlayerFallLoopState(),
                new PlayerPlatformerUpState(),
                new PlayerLandState(),
                new PlayerOutPlaceJumpState(),
                new PlayerLockIdleState()
            );

            StateMachine.Start<PlayerIdleState>();
        }

        protected override void Update()
        {
            base.Update();
            // FsmModule 会自动调用 OnUpdate，但动画更新需要手动处理
            UpdateAnimationState();
        }

        protected override void OnAnimatorMove()
        {
            base.OnAnimatorMove();
            // 调用当前状态的动画更新
            _currentState?.OnAnimationUpdate();
        }

        /// <summary>
        /// 动画结束回调 - 由动画事件调用。
        /// </summary>
        public void AnimationEnd()
        {
            _currentState?.OnAnimationEnd();
        }

        private void UpdateAnimationState()
        {
            if (StateMachine != null && StateMachine.CurrentState is PlayerFsmState state)
            {
                _currentState = state;
            }
        }

        /// <summary>
        /// 切换玩家状态 - 公开 API。
        /// </summary>
        public void SwitchState<T>() where T : PlayerFsmState
        {
            if (StateMachine != null)
            {
                var fsmImpl = (Fsm<Player>)StateMachine;
                fsmImpl.ChangeState<T>();
            }
        }

        private void OnDestroy()
        {
            if (StateMachine != null)
            {
                var fsmModule = ModuleSystem.GetModule<IFsmModule>();
                fsmModule?.DestroyFsm(StateMachine);
            }
        }
    }
}
