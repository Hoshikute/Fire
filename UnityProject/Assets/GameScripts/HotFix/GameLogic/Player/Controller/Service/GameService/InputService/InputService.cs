using TEngine;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 第三人称控制器输入兼容层。
    /// 统一转发到 TEngine InputModule，避免再维护独立 InputMap 生命周期。
    /// </summary>
    public sealed class InputService
    {
        private static readonly InputService _instance = new InputService();

        public static InputService Instance => _instance;

        private IInputModule InputModule => GameModule.Input;

        public Vector2 GetMoveHorizontalValue
        {
            get
            {
#if UNITY_ANDROID
                return InputModule.Move;
#else
                Vector2 dir = InputModule.Move;
                bool isShift = Shift;

                if (dir != Vector2.zero && isShift)
                {
                    dir.y = 0;
                    return dir.normalized;
                }
                else if (dir != Vector2.zero && !isShift)
                {
                    dir.y = 0;
                    return dir.normalized;
                }
                else
                {
                    return Vector2.zero;
                }
#endif
            }
        }

        public Vector2 GetMoveVerticalValue
        {
            get
            {
                Vector2 dir = InputModule.Move;
                if (dir != Vector2.zero)
                {
                    dir.x = 0;
                    return dir.normalized;
                }
                return Vector2.zero;
            }
        }

        public bool Interactive => InputModule.GetButton(InputButtonType.Interactive);

        public bool Shift => InputModule.GetButton(InputButtonType.Shift);

        public Vector2 Move
        {
            get
            {
                Vector2 vector2 = InputModule.Move;
                vector2.x = vector2.x switch
                {
                    > 0 => 1,
                    < 0 => -1,
                    _ => 0
                };
                vector2.y = vector2.y switch
                {
                    > 0 => 1,
                    < 0 => -1,
                    _ => 0
                };
                return vector2;
            }
        }

        public Vector2 Look => InputModule.Look;

        public Vector2 Scroll => InputModule.Scroll;

        public bool GetButton(InputButtonType buttonType)
        {
            return InputModule.GetButton(buttonType);
        }

        public bool GetButtonDown(InputButtonType buttonType)
        {
            return InputModule.GetButtonDown(buttonType);
        }

        public bool GetButtonUp(InputButtonType buttonType)
        {
            return InputModule.GetButtonUp(buttonType);
        }
    }
}
