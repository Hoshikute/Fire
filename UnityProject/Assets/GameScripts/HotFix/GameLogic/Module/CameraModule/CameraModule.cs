using Cinemachine;
using TEngine;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic
{
    /// <summary>
    /// 相机管理模块。
    /// 负责管理游戏相机、UI 相机的生命周期和渲染顺序。
    /// UI相机保持为Base类型，通过Depth和ClearFlags实现与游戏画面叠加。
    /// </summary>
    public class CameraModule : Module, ICameraModule
    {
        private Camera _mainCamera;
        private Camera _uiCamera;
        private CinemachineVirtualCamera _virtualCamera;

        // 渲染顺序：游戏相机先渲染(Depth小)，UI相机后渲染(Depth大)
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
                    // UI相机配置：
                    // - Depth = 2 (后渲染，叠加在游戏画面上)
                    // - ClearFlags = Depth Only (不清除颜色缓冲，保留游戏画面)
                    // - CullingMask = UI层 (只渲染UI)
                    _uiCamera.depth = UI_CAMERA_DEPTH;
                    _uiCamera.clearFlags = CameraClearFlags.Depth;
                    int uiLayer = LayerMask.NameToLayer("UI");
                    if (uiLayer >= 0)
                    {
                        _uiCamera.cullingMask = 1 << uiLayer;
                    }
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
            }
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
