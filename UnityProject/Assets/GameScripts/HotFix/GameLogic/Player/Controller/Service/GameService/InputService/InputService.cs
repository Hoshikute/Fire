using System;
using UnityEngine;

namespace ThirdPersonController
{
    public class InputService : MonoSingleton<InputService>
    {
        public InputMap inputMap;

        protected override void Awake()
        {
            base.Awake();
            if (inputMap == null)
            {
                inputMap = new InputMap();
            }
            inputMap.Enable();
        }

        private void OnDestroy()
        {
            inputMap.Disable();
        }

        public Vector2 GetMoveHorizontalValue
        {
            get
            {
#if UNITY_ANDROID
                return inputMap.Player.Move.ReadValue<Vector2>();
#else
                Vector2 dir = inputMap.Player.Move.ReadValue<Vector2>();
                bool isShift = inputMap.Player.Shift.ReadValue<float>() != 0;

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
                Vector2 dir = inputMap.Player.Move.ReadValue<Vector2>();
                if (dir != Vector2.zero)
                {
                    dir.x = 0;
                    return dir.normalized;
                }
                return Vector2.zero;
            }
        }

        public bool Interactive => inputMap.Player.Interactive.ReadValue<float>() != 0;

        public bool Shift => inputMap.Player.Shift.ReadValue<float>() != 0;

        public Vector2 Move
        {
            get
            {
                Vector2 vector2 = inputMap.Player.Move.ReadValue<Vector2>();
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

        public Vector2 Scroll => inputMap.Player.Scroll.ReadValue<Vector2>();
    }
}
