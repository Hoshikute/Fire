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
    public class CameraModule : Module, ICameraModule
    {
        private Camera _mainCamera;
        private Camera _uiCamera;
        private CinemachineVirtualCamera _virtualCamera;

        // 保留 Depth 作为运行时兜底排序，实际叠加关系由 URP Camera Stack 决定。
        private const int UI_CAMERA_DEPTH = 2;
        private const int GAME_CAMERA_DEPTH = 0;

        public Camera MainCamera => _mainCamera;

        public override void OnInit()
        {
            FindUICamera();
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
                return;

            _mainCamera = camera;

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

                // 从主相机子对象查找 CinemachineVirtualCamera
                var virtualCameras = _mainCamera.GetComponentsInChildren<CinemachineVirtualCamera>(true);
                _virtualCamera = virtualCameras.Length > 0 ? virtualCameras[0] : null;

                ConfigureMainCameraStack();
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

        public void BindCinemachineToPlayer(Transform playerTransform)
        {
            if (_virtualCamera != null && playerTransform != null)
            {
                _virtualCamera.Follow = playerTransform;
                _virtualCamera.LookAt = playerTransform;
            }
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
            _mainCamera = null;
            _uiCamera = null;
            _virtualCamera = null;
        }
    }
}
