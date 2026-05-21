using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using TEngine;

namespace ThirdPersonController
{
    public class CameraController : MonoBehaviour
    {
        public float defaultDistance;
        [Range(0.5f, 3)] public float minDistance;
        [Range(3, 10)] public float maxDistance;
        private float currentDistance;
        public float sensitivity;
        public float smoothness;

        private CinemachineFramingTransposer virtualCamera;
        private PlayableDirector playableDirector;
        private IInputModule inputModule;

        private void Awake()
        {
            inputModule = GameModule.Input;
            virtualCamera = GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineFramingTransposer>();
            playableDirector = transform.GetComponent<PlayableDirector>();
            currentDistance = defaultDistance;
            virtualCamera.m_CameraDistance = currentDistance;
        }

        private void Update()
        {
            GetMouseScroll();
        }

        private void LateUpdate()
        {
            UpdateCameraDistance();
        }

        private void GetMouseScroll()
        {
            if (inputModule == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            currentDistance -= inputModule.Scroll.y * Time.deltaTime * sensitivity;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }

        private void UpdateCameraDistance()
        {
            if (playableDirector != null && playableDirector?.state == PlayState.Playing)
            {
                return;
            }
            virtualCamera.m_CameraDistance = Mathf.Lerp(virtualCamera.m_CameraDistance, currentDistance, Time.deltaTime * smoothness);
        }
    }
}
