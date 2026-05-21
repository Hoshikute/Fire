using Cinemachine;
using TEngine;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 将 TEngine InputModule 的 Look 输入桥接到 CinemachinePOV。
    /// 保证第三人称相机只走一条输入链路。
    /// </summary>
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public sealed class CinemachineLookInputBridge : MonoBehaviour
    {
        private const string TRACE_HEADER = "[TPC_CAM_TRACE][LookBridge]";

        [Tooltip("可选：直接引用 CinemachinePOV 组件。如未配置，将自动在子对象中查找。")]
        [SerializeField]
        private CinemachinePOV _povReference;

        [Tooltip("水平旋转灵敏度（度/像素）")]
        [SerializeField]
        private float _horizontalSensitivity = 0.2f;

        [Tooltip("垂直旋转灵敏度（度/像素）")]
        [SerializeField]
        private float _verticalSensitivity = 0.2f;

        private CinemachinePOV _pov;
        private IInputModule _inputModule;
        private bool _initialized;

        private void Awake()
        {
            _inputModule = GameModule.Input;

            // 优先使用序列化引用，其次使用 GetComponentInChildren 查找子对象
            _pov = _povReference;
            if (_pov == null)
            {
                _pov = GetComponentInChildren<CinemachinePOV>();
            }

            // 如果仍未找到，尝试在虚拟相机上动态添加 POV 组件
            if (_pov == null)
            {
                var virtualCamera = GetComponent<CinemachineVirtualCamera>();
                if (virtualCamera != null)
                {
                    _pov = virtualCamera.AddCinemachineComponent<CinemachinePOV>();
                    Debug.Log($"{TRACE_HEADER} dynamically added CinemachinePOV to {virtualCamera.name}");
                }
            }

            if (_pov == null)
            {
                Debug.LogWarning("[CinemachineLookInputBridge] 未找到 CinemachinePOV，相机视角输入桥接将失效。");
                return;
            }

            // 禁用 POV 的输入轴，我们将手动控制
            _pov.m_HorizontalAxis.m_InputAxisName = string.Empty;
            _pov.m_VerticalAxis.m_InputAxisName = string.Empty;
            // 设置 SpeedMode 为 0 (MaxSpeed 模式)，输入值作为速度
            _pov.m_HorizontalAxis.m_SpeedMode = AxisState.SpeedMode.MaxSpeed;
            _pov.m_VerticalAxis.m_SpeedMode = AxisState.SpeedMode.MaxSpeed;

            _initialized = true;
            Debug.Log($"{TRACE_HEADER} initialized. pov={_pov.name}, H.sens={_horizontalSensitivity}, V.sens={_verticalSensitivity}");
        }

        private void LateUpdate()
        {
            if (!_initialized || _pov == null || _inputModule == null)
            {
                return;
            }

            if (_inputModule.InputLocked || Cursor.lockState != CursorLockMode.Locked)
            {
                // 清零输入轴值
                _pov.m_HorizontalAxis.m_InputAxisValue = 0f;
                _pov.m_VerticalAxis.m_InputAxisValue = 0f;
                return;
            }

            Vector2 look = _inputModule.Look;

            // 直接修改 POV 的 Value，绕过输入轴处理
            // 这样可以确保相机旋转，不受 AxisState 配置影响
            float horizontalDelta = look.x * _horizontalSensitivity;
            float verticalDelta = look.y * _verticalSensitivity;

            // 更新 POV 的轴值
            _pov.m_HorizontalAxis.Value += horizontalDelta;
            _pov.m_VerticalAxis.Value -= verticalDelta; // 垂直方向取反（鼠标向上 = 相机向上）

            // 钳制垂直角度
            _pov.m_VerticalAxis.Value = Mathf.Clamp(_pov.m_VerticalAxis.Value, -70f, 70f);
        }

        private void OnDisable()
        {
            if (_pov == null)
            {
                return;
            }

            _pov.m_HorizontalAxis.m_InputAxisValue = 0f;
            _pov.m_VerticalAxis.m_InputAxisValue = 0f;
        }
    }
}
