using Animancer;
using GameLogic;
using TEngine;
using UnityEngine;

namespace ThirdPersonController
{
    [RequireComponent(typeof(AnimancerComponent))]
    public class Player : CharacterBase
    {
        private const string TRACE_HEADER = "[TPC_CAM_TRACE][Player]";
        public PlayerSO playerSO;

        [Header("Camera")]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform _lookAtTarget;

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
            Debug.Log($"{TRACE_HEADER}[Awake] moduleMainCamera={(GameModule.Camera.MainCamera != null ? GameModule.Camera.MainCamera.name : "null")}, cameraMain={(Camera.main != null ? Camera.main.name : "null")}");

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

            // 相机绑定延迟到 Start，确保 CameraModule 已完成初始化
            _pendingCameraBind = true;

            Animancer = GetComponent<AnimancerComponent>();
            if (Animancer == null)
            {
                Debug.LogError("Animancer component not found, unable to play animations");
                return;
            }
            ReusableData = new PlayerReusableData(Animancer, playerSO);
            ReusableLogic = new PlayerReusableLogic(this);

            // 使用 TEngine FsmModule 创建状态机
            StateMachine = GameModule.Fsm.CreateFsm("PlayerFSM", this,
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

        private bool _pendingCameraBind;

        protected virtual void Start()
        {
            // 在 Start 中执行相机绑定，此时场景加载已完成，CameraModule 应该已初始化
            if (_pendingCameraBind)
            {
                TryBindCamera();
            }
        }

        private void TryBindCamera()
        {
            Transform followTarget = ResolveCameraAnchor();
            Transform lookAtTarget = followTarget;
            if (followTarget != null && lookAtTarget != null)
            {
                Debug.Log($"{TRACE_HEADER}[BindRequest] follow={followTarget.name}, lookAt={lookAtTarget.name}, moduleMainCamera={(GameModule.Camera.MainCamera != null ? GameModule.Camera.MainCamera.name : "null")}");
                GameModule.Camera.BindCinemachineToPlayer(followTarget, lookAtTarget);
                _pendingCameraBind = false;
            }
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
                GameModule.Fsm.DestroyFsm(StateMachine);
            }
        }

        private Transform ResolveCameraAnchor()
        {
            if (_lookAtTarget != null)
            {
                Debug.Log($"{TRACE_HEADER}[ResolveCameraAnchor] use serialized LookAt target={_lookAtTarget.name}");
                return _lookAtTarget;
            }

            Transform lookAt = transform.Find("LookAt");
            if (lookAt != null)
            {
                _lookAtTarget = lookAt;
                Debug.Log($"{TRACE_HEADER}[ResolveCameraAnchor] use prefab LookAt target={_lookAtTarget.name}");
                return _lookAtTarget;
            }

            if (_cameraTransform != null)
            {
                Debug.LogWarning($"{TRACE_HEADER}[ResolveCameraAnchor] fallback to _cameraTransform={_cameraTransform.name}, LookAt anchor missing.");
                Debug.LogWarning("[Player] 未找到 LookAt 锚点，回退使用 _cameraTransform 作为相机锚点。");
                return _cameraTransform;
            }

            Debug.LogWarning($"{TRACE_HEADER}[ResolveCameraAnchor] fallback to player root={transform.name}, LookAt anchor missing.");
            Debug.LogWarning("[Player] 未找到 LookAt 锚点，回退绑定到玩家根节点。");
            return transform;
        }
    }
}
