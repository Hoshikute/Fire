using Cinemachine;
using TEngine;
using UnityEngine;
#if ENABLE_URP
using UnityEngine.Rendering.Universal;
#endif
using Object = UnityEngine.Object;

namespace GameLogic
{
    /// <summary>
    /// 相机管理模块。
    /// 负责管理游戏相机、UI 相机的生命周期和 URP 相机堆栈。
    /// </summary>
    public class CameraModule : Module, ICameraModule, IUpdateModule
    {
        private const string TRACE_HEADER = "[TPC_CAM_TRACE][CameraModule]";
        private const float LOOK_HORIZONTAL_SENSITIVITY = 0.2f;
        private const float LOOK_VERTICAL_SENSITIVITY = 0.2f;
        private const float DEFAULT_VERTICAL_MIN = -70f;
        private const float DEFAULT_VERTICAL_MAX = 70f;
        private const float DEFAULT_HORIZONTAL_MIN = -180f;
        private const float DEFAULT_HORIZONTAL_MAX = 180f;

        private Camera _mainCamera;
        private Camera _uiCamera;
        private CinemachineVirtualCamera _virtualCamera;
        private CinemachinePOV _lookPov;
        private bool _gameplayCameraBound;

        // 保留 Depth 作为运行时兜底排序，实际叠加关系由 URP Camera Stack 决定。
        private const int UI_CAMERA_DEPTH = 2;
        private const int GAME_CAMERA_DEPTH = 0;

        public Camera MainCamera => _mainCamera;

        public override void OnInit()
        {
            FindUICamera();
            string uiCameraName = _uiCamera != null ? _uiCamera.name : "null";
            string mainCameraName = Camera.main != null ? Camera.main.name : "null";
            Log.Info($"{TRACE_HEADER}[OnInit] uiCamera={uiCameraName}, cameraMain={mainCameraName}");
        }

        private void FindUICamera()
        {
            var uiRoot = GameObject.Find("UIRoot");
            if (uiRoot != null)
            {
                _uiCamera = uiRoot.GetComponentInChildren<Camera>();
                if (_uiCamera != null)
                {
                    ConfigureUICamera();
                }
            }
        }

        public void SetMainCamera(Camera camera)
        {
            if (_mainCamera == camera)
            {
                Log.Info($"{TRACE_HEADER}[SetMainCamera] skip duplicate registration. camera={GetCameraName(camera)}");
                if (_mainCamera != null)
                {
                    EnsureSingleAudioListener(_mainCamera);
                }

                return;
            }

            _mainCamera = camera;
            ResetLookBridgeState();
            Log.Info($"{TRACE_HEADER}[SetMainCamera] incoming camera={GetCameraName(camera)}, tag={camera?.tag ?? "null"}");

            if (_mainCamera != null)
            {
                _mainCamera.tag = "MainCamera";
                _mainCamera.depth = GAME_CAMERA_DEPTH;

                // 游戏相机排除UI层
                int uiLayer = LayerMask.NameToLayer("UI");
                if (uiLayer >= 0)
                {
                    _mainCamera.cullingMask = ~(1 << uiLayer);
                }

                // 优先从主相机子对象查找，其次全局搜索
                var virtualCameras = _mainCamera.GetComponentsInChildren<CinemachineVirtualCamera>(true);
                _virtualCamera = virtualCameras.Length > 0 ? virtualCameras[0] : null;

                // 如果子对象没找到，尝试全局搜索（CinemachineBrain 会自动控制场景中的虚拟相机）
                if (_virtualCamera == null)
                {
                    var allVirtualCameras = Object.FindObjectsOfType<CinemachineVirtualCamera>(true);
                    if (allVirtualCameras.Length > 0)
                    {
                        _virtualCamera = allVirtualCameras[0];
                        Log.Info($"{TRACE_HEADER}[SetMainCamera] virtual camera found globally. vcam={_virtualCamera.name}");
                        EnsureLookPov();
                    }
                    else
                    {
                        _lookPov = null;
                        Log.Warning($"{TRACE_HEADER}[SetMainCamera] no virtual camera found in scene. Please ensure CameraController prefab is instantiated.");
                    }
                }
                else
                {
                    Log.Info($"{TRACE_HEADER}[SetMainCamera] virtual camera registered from child. vcam={_virtualCamera.name}");
                    EnsureLookPov();
                }

                EnsureSingleAudioListener(_mainCamera);
                ConfigureMainCameraStack();
            }
            else
            {
                _virtualCamera = null;
                ResetLookBridgeState();
                Log.Warning($"{TRACE_HEADER}[SetMainCamera] received null main camera.");
            }
        }

        private void EnsureSingleAudioListener(Camera targetCamera)
        {
            if (targetCamera == null)
            {
                return;
            }

            var targetListener = targetCamera.GetComponent<AudioListener>();
            if (targetListener == null)
            {
                targetListener = targetCamera.gameObject.AddComponent<AudioListener>();
                Log.Warning($"[CameraModule] Main camera '{targetCamera.name}' was missing AudioListener. Added one automatically.");
            }

            if (!targetListener.enabled)
            {
                targetListener.enabled = true;
            }

            var listeners = Object.FindObjectsOfType<AudioListener>(true);
            foreach (var listener in listeners)
            {
                if (listener == null || !listener.gameObject.scene.IsValid())
                {
                    continue;
                }

                bool shouldEnable = listener == targetListener;
                if (listener.enabled != shouldEnable)
                {
                    listener.enabled = shouldEnable;
                }
            }
        }

        private void ConfigureUICamera()
        {
            if (_uiCamera == null)
            {
                return;
            }

            _uiCamera.depth = UI_CAMERA_DEPTH;
            _uiCamera.clearFlags = CameraClearFlags.Depth;
            _uiCamera.orthographic = true;

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                _uiCamera.cullingMask = 1 << uiLayer;
            }

#if ENABLE_URP
            var uiCameraData = _uiCamera.GetUniversalAdditionalCameraData();
            uiCameraData.renderType = CameraRenderType.Overlay;
            uiCameraData.renderPostProcessing = false;
#endif
        }

        private void ConfigureMainCameraStack()
        {
#if !ENABLE_URP
            return;
#else
            if (_mainCamera == null)
            {
                return;
            }

            if (_uiCamera == null)
            {
                FindUICamera();
            }

            if (_uiCamera == null || _uiCamera == _mainCamera)
            {
                return;
            }

            ConfigureUICamera();

            var mainCameraData = _mainCamera.GetUniversalAdditionalCameraData();
            mainCameraData.renderType = CameraRenderType.Base;

            var cameraStack = mainCameraData.cameraStack;
            for (int i = cameraStack.Count - 1; i >= 0; i--)
            {
                if (cameraStack[i] == null)
                {
                    cameraStack.RemoveAt(i);
                }
            }

            if (!cameraStack.Contains(_uiCamera))
            {
                cameraStack.Add(_uiCamera);
            }
#endif
        }

        public void BindCinemachineToPlayer(Transform followTarget, Transform lookAtTarget)
        {
            Log.Info($"{TRACE_HEADER}[Bind] mainCamera={GetCameraName(_mainCamera)}, virtualCamera={GetVirtualCameraName()}, follow={GetTransformName(followTarget)}, lookAt={GetTransformName(lookAtTarget)}");

            if (_virtualCamera == null)
            {
                Log.Error($"{TRACE_HEADER}[Bind] abort: virtual camera is null, SetMainCamera may never have been called.");
                return;
            }

            if (followTarget == null || lookAtTarget == null)
            {
                Log.Error($"{TRACE_HEADER}[Bind] abort: follow or lookAt target is null.");
                return;
            }

            if (_virtualCamera != null && followTarget != null && lookAtTarget != null)
            {
                _virtualCamera.Follow = followTarget;
                _virtualCamera.LookAt = lookAtTarget;
                ApplyGameplayCursorState(true);
                _gameplayCameraBound = true;
                EnsureLookPov();
                Log.Info($"{TRACE_HEADER}[Bind] success. follow={followTarget.name}, lookAt={lookAtTarget.name}, cursorLock={Cursor.lockState}, cursorVisible={Cursor.visible}");
            }
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            UpdateGameplayMouseLook();
        }

        private void UpdateGameplayMouseLook()
        {
            if (!_gameplayCameraBound || _virtualCamera == null)
            {
                return;
            }

            CinemachinePOV pov = EnsureLookPov();
            if (pov == null)
            {
                return;
            }

            var inputModule = GameModule.Input;
            string suppressedReason = GetLookSuppressedReason(inputModule);
            if (!string.IsNullOrEmpty(suppressedReason))
            {
                pov.m_HorizontalAxis.m_InputAxisValue = 0f;
                pov.m_VerticalAxis.m_InputAxisValue = 0f;
                return;
            }

            Vector2 look = inputModule.Look;
            if (look == Vector2.zero)
            {
                return;
            }

            float horizontalValue = pov.m_HorizontalAxis.Value + look.x * LOOK_HORIZONTAL_SENSITIVITY;
            float verticalValue = pov.m_VerticalAxis.Value - look.y * LOOK_VERTICAL_SENSITIVITY;

            pov.m_HorizontalAxis.Value = ClampAxisValue(pov.m_HorizontalAxis, horizontalValue);
            pov.m_VerticalAxis.Value = ClampAxisValue(pov.m_VerticalAxis, verticalValue);
            pov.m_HorizontalAxis.m_InputAxisValue = 0f;
            pov.m_VerticalAxis.m_InputAxisValue = 0f;
        }

        private CinemachinePOV EnsureLookPov()
        {
            if (_virtualCamera == null)
            {
                _lookPov = null;
                Log.Warning("[CODEX_LOG] Camera mouse look cannot initialize because virtual camera is null.");
                return null;
            }

            if (_lookPov == null)
            {
                _lookPov = _virtualCamera.GetCinemachineComponent<CinemachinePOV>();
            }

            bool addedPov = false;
            if (_lookPov == null)
            {
                _lookPov = _virtualCamera.AddCinemachineComponent<CinemachinePOV>();
                addedPov = _lookPov != null;

                if (addedPov)
                {
                    ConfigureDefaultPovRange(_lookPov);
                }
            }

            if (_lookPov == null)
            {
                Log.Error($"[CODEX_LOG] Camera mouse look failed because CinemachinePOV is unavailable. vcam={_virtualCamera.name}");
                return null;
            }

            ConfigurePovInput(_lookPov);

            return _lookPov;
        }

        private static void ConfigureDefaultPovRange(CinemachinePOV pov)
        {
            pov.m_VerticalAxis.m_MinValue = DEFAULT_VERTICAL_MIN;
            pov.m_VerticalAxis.m_MaxValue = DEFAULT_VERTICAL_MAX;
            pov.m_HorizontalAxis.m_MinValue = DEFAULT_HORIZONTAL_MIN;
            pov.m_HorizontalAxis.m_MaxValue = DEFAULT_HORIZONTAL_MAX;
            pov.m_HorizontalAxis.m_Wrap = true;
        }

        private static void ConfigurePovInput(CinemachinePOV pov)
        {
            pov.m_HorizontalAxis.m_InputAxisName = string.Empty;
            pov.m_VerticalAxis.m_InputAxisName = string.Empty;
            pov.m_HorizontalAxis.m_InputAxisValue = 0f;
            pov.m_VerticalAxis.m_InputAxisValue = 0f;
            pov.m_HorizontalAxis.m_SpeedMode = AxisState.SpeedMode.MaxSpeed;
            pov.m_VerticalAxis.m_SpeedMode = AxisState.SpeedMode.MaxSpeed;
        }

        private static string GetLookSuppressedReason(IInputModule inputModule)
        {
            if (inputModule == null)
            {
                return "input-null";
            }

            if (inputModule.InputLocked)
            {
                return "input-locked";
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return $"cursor-{Cursor.lockState}";
            }

            return null;
        }

        private static float ClampAxisValue(AxisState axis, float value)
        {
            float min = axis.m_MinValue;
            float max = axis.m_MaxValue;
            if (Mathf.Approximately(min, max))
            {
                return value;
            }

            if (axis.m_Wrap)
            {
                float range = max - min;
                if (range <= 0f)
                {
                    return value;
                }

                while (value < min)
                {
                    value += range;
                }

                while (value > max)
                {
                    value -= range;
                }

                return value;
            }

            return Mathf.Clamp(value, min, max);
        }

        private void ResetLookBridgeState()
        {
            _lookPov = null;
            _gameplayCameraBound = false;
        }

        private void ApplyGameplayCursorState(bool lockCursor)
        {
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;
        }

        public void CleanupExtraCameras()
        {
            var allCameras = Object.FindObjectsOfType<Camera>();
            foreach (var cam in allCameras)
            {
                if (cam == _uiCamera)
                    continue;

                if (cam.tag == "MainCamera" && cam != _mainCamera)
                {
                    cam.enabled = false;
                }
            }
        }

        public override void Shutdown()
        {
            ApplyGameplayCursorState(false);
            _mainCamera = null;
            _uiCamera = null;
            _virtualCamera = null;
            ResetLookBridgeState();
        }

        private static string GetCameraName(Camera camera)
        {
            return camera != null ? camera.name : "null";
        }

        private string GetVirtualCameraName()
        {
            return _virtualCamera != null ? _virtualCamera.name : "null";
        }

        private static string GetTransformName(Transform target)
        {
            return target != null ? target.name : "null";
        }
    }
}
