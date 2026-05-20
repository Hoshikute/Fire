using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TEngine
{
    /// <summary>
    /// 输入模块实现。
    /// </summary>
    internal sealed class InputModule : Module, IInputModule, IUpdateModule,
        InputSystem_Actions.IPlayerActions
    {
        #region 常量

        private const string KEYBOARD_MOUSE_SCHEME = "Keyboard&Mouse";
        private const string GAMEPAD_SCHEME = "Gamepad";

        #endregion

        #region 私有字段

        private InputSystem_Actions _actions;
        private string _currentControlScheme;
        private bool _inputLocked;

        private Vector2 _moveValue;
        private Vector2 _lookValue;
        private Vector2 _scrollValue;

        private readonly Dictionary<InputButtonType, bool> _buttonStates = new Dictionary<InputButtonType, bool>();
        private readonly Dictionary<InputButtonType, bool> _buttonDownThisFrame = new Dictionary<InputButtonType, bool>();
        private readonly Dictionary<InputButtonType, bool> _buttonUpThisFrame = new Dictionary<InputButtonType, bool>();

        private readonly List<IInputActionListener> _listeners = new List<IInputActionListener>();

        private readonly Dictionary<InputButtonType, Dictionary<UnityEngine.InputSystem.InputActionPhase, List<Action>>> _buttonCallbacks
            = new Dictionary<InputButtonType, Dictionary<UnityEngine.InputSystem.InputActionPhase, List<Action>>>();

        private readonly List<InputButtonType> _buttonsToClearDown = new List<InputButtonType>();
        private readonly List<InputButtonType> _buttonsToClearUp = new List<InputButtonType>();

        #endregion

        #region Module 生命周期

        public override int Priority => 5;

        public override void OnInit()
        {
            _actions = new InputSystem_Actions();
            _actions.Player.AddCallbacks(this);

            foreach (InputButtonType buttonType in Enum.GetValues(typeof(InputButtonType)))
            {
                _buttonStates[buttonType] = false;
                _buttonDownThisFrame[buttonType] = false;
                _buttonUpThisFrame[buttonType] = false;
                _buttonCallbacks[buttonType] = new Dictionary<UnityEngine.InputSystem.InputActionPhase, List<Action>>
                {
                    { UnityEngine.InputSystem.InputActionPhase.Started, new List<Action>() },
                    { UnityEngine.InputSystem.InputActionPhase.Performed, new List<Action>() },
                    { UnityEngine.InputSystem.InputActionPhase.Canceled, new List<Action>() }
                };
            }

            EnablePlayerInput(true);

            Log.Info("[InputModule] Initialized");
        }

        public override void Shutdown()
        {
            _listeners.Clear();

            foreach (var callbacks in _buttonCallbacks.Values)
            {
                foreach (var list in callbacks.Values)
                {
                    list.Clear();
                }
            }
            _buttonCallbacks.Clear();

            _actions?.Player.RemoveCallbacks(this);
            _actions?.Disable();
            _actions?.Dispose();
            _actions = null;

            Log.Info("[InputModule] Shutdown");
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (_inputLocked)
            {
                return;
            }

            for (int i = _buttonsToClearDown.Count - 1; i >= 0; i--)
            {
                _buttonDownThisFrame[_buttonsToClearDown[i]] = false;
            }
            _buttonsToClearDown.Clear();

            for (int i = _buttonsToClearUp.Count - 1; i >= 0; i--)
            {
                _buttonUpThisFrame[_buttonsToClearUp[i]] = false;
            }
            _buttonsToClearUp.Clear();
        }

        #endregion

        #region IInputModule 实现 - 状态属性

        public string CurrentControlScheme => _currentControlScheme;

        public bool PlayerEnabled
        {
            get => _actions?.Player.enabled ?? false;
            set
            {
                if (_actions == null) return;
                if (value)
                    _actions.Player.Enable();
                else
                    _actions.Player.Disable();
            }
        }

        public bool UIEnabled
        {
            get => _actions?.UI.enabled ?? false;
            set
            {
                if (_actions == null) return;
                if (value)
                    _actions.UI.Enable();
                else
                    _actions.UI.Disable();
            }
        }

        public bool InputLocked
        {
            get => _inputLocked;
            set => _inputLocked = value;
        }

        #endregion

        #region IInputModule 实现 - 输入值访问

        public Vector2 Move => _inputLocked ? Vector2.zero : _moveValue;
        public Vector2 Look => _inputLocked ? Vector2.zero : _lookValue;
        public Vector2 Scroll => _inputLocked ? Vector2.zero : _scrollValue;

        public bool GetButtonDown(InputButtonType buttonType)
        {
            return !_inputLocked && _buttonDownThisFrame.GetValueOrDefault(buttonType, false);
        }

        public bool GetButton(InputButtonType buttonType)
        {
            return !_inputLocked && _buttonStates.GetValueOrDefault(buttonType, false);
        }

        public bool GetButtonUp(InputButtonType buttonType)
        {
            return !_inputLocked && _buttonUpThisFrame.GetValueOrDefault(buttonType, false);
        }

        #endregion

        #region IInputModule 实现 - 事件订阅

        public void AddListener(IInputActionListener listener)
        {
            if (listener != null && !_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public void RemoveListener(IInputActionListener listener)
        {
            _listeners.Remove(listener);
        }

        public void RegisterButtonCallback(InputButtonType buttonType, UnityEngine.InputSystem.InputActionPhase phase, Action callback)
        {
            if (callback == null) return;
            if (_buttonCallbacks.TryGetValue(buttonType, out var phaseCallbacks))
            {
                if (phaseCallbacks.TryGetValue(phase, out var callbacks))
                {
                    if (!callbacks.Contains(callback))
                    {
                        callbacks.Add(callback);
                    }
                }
            }
        }

        public void UnregisterButtonCallback(InputButtonType buttonType, UnityEngine.InputSystem.InputActionPhase phase, Action callback)
        {
            if (callback == null) return;
            if (_buttonCallbacks.TryGetValue(buttonType, out var phaseCallbacks))
            {
                if (phaseCallbacks.TryGetValue(phase, out var callbacks))
                {
                    callbacks.Remove(callback);
                }
            }
        }

        #endregion

        #region IInputModule 实现 - 控制方案

        public void SwitchControlScheme(string schemeName)
        {
            if (_actions == null) return;

            var schemeIndex = _actions.asset.FindControlSchemeIndex(schemeName);
            if (schemeIndex >= 0)
            {
                _actions.bindingMask = InputBinding.MaskByGroup(schemeName);
                _currentControlScheme = schemeName;
                Log.Info($"[InputModule] Switched to control scheme: {schemeName}");
            }
            else
            {
                Log.Warning($"[InputModule] Control scheme not found: {schemeName}");
            }
        }

        public string[] GetAvailableControlSchemes()
        {
            if (_actions == null) return Array.Empty<string>();

            var schemes = _actions.controlSchemes;
            var result = new string[schemes.Count];
            for (int i = 0; i < schemes.Count; i++)
            {
                result[i] = schemes[i].name;
            }
            return result;
        }

        #endregion

        #region IInputModule 实现 - 向后兼容

        public InputSystem_Actions Actions => _actions;

        #endregion

        #region IPlayerActions 回调实现

        public void OnMove(InputAction.CallbackContext context)
        {
            if (_inputLocked) return;

            _moveValue = context.ReadValue<Vector2>();
            DispatchToListeners(listener => listener.OnMove(context, _moveValue));
            DispatchButtonCallbacks(InputButtonType.Move, context.phase);
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            if (_inputLocked) return;

            _lookValue = context.ReadValue<Vector2>();
            DispatchToListeners(listener => listener.OnLook(context, _lookValue));
            DispatchButtonCallbacks(InputButtonType.Look, context.phase);
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (_inputLocked) return;

            HandleButtonState(InputButtonType.Attack, context);
            DispatchToListeners(listener => listener.OnAttack(context));
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (_inputLocked) return;

            HandleButtonState(InputButtonType.Interact, context);
            DispatchToListeners(listener => listener.OnInteract(context));
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (_inputLocked) return;

            HandleButtonState(InputButtonType.Crouch, context);
            DispatchToListeners(listener => listener.OnCrouch(context));
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (_inputLocked) return;

            HandleButtonState(InputButtonType.Jump, context);
            DispatchToListeners(listener => listener.OnJump(context));
        }

        public void OnPrevious(InputAction.CallbackContext context)
        {
            if (_inputLocked) return;

            HandleButtonState(InputButtonType.Previous, context);
            DispatchToListeners(listener => listener.OnPrevious(context));
        }

        public void OnNext(InputAction.CallbackContext context)
        {
            if (_inputLocked) return;

            HandleButtonState(InputButtonType.Next, context);
            DispatchToListeners(listener => listener.OnNext(context));
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (_inputLocked) return;

            HandleButtonState(InputButtonType.Sprint, context);
            DispatchToListeners(listener => listener.OnSprint(context));
        }

        public void OnInteractive(InputAction.CallbackContext context)
        {
            if (_inputLocked) return;

            HandleButtonState(InputButtonType.Interactive, context);
            DispatchToListeners(listener => listener.OnInteractive(context));
        }

        public void OnShift(InputAction.CallbackContext context)
        {
            if (_inputLocked) return;

            HandleButtonState(InputButtonType.Shift, context);
            DispatchToListeners(listener => listener.OnShift(context));
        }

        public void OnScroll(InputAction.CallbackContext context)
        {
            if (_inputLocked) return;

            _scrollValue = context.ReadValue<Vector2>();
            DispatchToListeners(listener => listener.OnScroll(context, _scrollValue));
            DispatchButtonCallbacks(InputButtonType.Scroll, context.phase);
        }

        public void OnLock(InputAction.CallbackContext context)
        {
            if (_inputLocked) return;

            HandleButtonState(InputButtonType.Lock, context);
            DispatchToListeners(listener => listener.OnLock(context));
        }

        #endregion

        #region 私有方法

        private void EnablePlayerInput(bool enable)
        {
            if (_actions == null) return;

            if (enable)
            {
                _actions.Player.Enable();
                _currentControlScheme = KEYBOARD_MOUSE_SCHEME;
            }
            else
            {
                _actions.Player.Disable();
            }
        }

        private void HandleButtonState(InputButtonType buttonType, InputAction.CallbackContext context)
        {
            bool isPressed = context.action.IsPressed();
            bool wasPressed = _buttonStates[buttonType];

            _buttonStates[buttonType] = isPressed;

            if (context.phase == UnityEngine.InputSystem.InputActionPhase.Performed && !wasPressed)
            {
                _buttonDownThisFrame[buttonType] = true;
                _buttonsToClearDown.Add(buttonType);
            }
            else if (context.phase == UnityEngine.InputSystem.InputActionPhase.Canceled && wasPressed)
            {
                _buttonUpThisFrame[buttonType] = true;
                _buttonsToClearUp.Add(buttonType);
            }

            DispatchButtonCallbacks(buttonType, context.phase);
        }

        private void DispatchButtonCallbacks(InputButtonType buttonType, UnityEngine.InputSystem.InputActionPhase phase)
        {
            if (_buttonCallbacks.TryGetValue(buttonType, out var phaseCallbacks))
            {
                if (phaseCallbacks.TryGetValue(phase, out var callbacks))
                {
                    for (int i = 0; i < callbacks.Count; i++)
                    {
                        callbacks[i]?.Invoke();
                    }
                }
            }
        }

        private void DispatchToListeners(Action<IInputActionListener> dispatchAction)
        {
            for (int i = 0; i < _listeners.Count; i++)
            {
                dispatchAction?.Invoke(_listeners[i]);
            }
        }

        #endregion
    }
}
