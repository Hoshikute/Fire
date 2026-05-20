using Cinemachine;
using TEngine;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic
{
    /// <summary>
    /// 相机管理模块。
    /// 负责管理游戏相机、UI 相机的生命周期和渲染顺序。
    /// </summary>
    public class CameraModule : Module, ICameraModule
    {
        private Camera _mainCamera;
        private Camera _uiCamera;
        private CinemachineVirtualCamera _virtualCamera;

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
                    _uiCamera.depth = UI_CAMERA_DEPTH;
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

                int uiLayer = LayerMask.NameToLayer("UI");
                if (uiLayer >= 0)
                {
                    _mainCamera.cullingMask = ~(1 << uiLayer);
                }

                _virtualCamera = Object.FindObjectOfType<CinemachineVirtualCamera>();
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
