using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace ThirdPersonController
{
    public sealed class InputMap : IInputActionCollection2, IDisposable
    {
        private readonly TEngine.InputSystem_Actions actions;
    
        public InputActionAsset asset => actions.asset;
    
        public InputMap()
        {
            actions = new TEngine.InputSystem_Actions();
        }
    
        public void Dispose()
        {
            actions.Dispose();
        }
    
        public InputBinding? bindingMask
        {
            get => actions.bindingMask;
            set => actions.bindingMask = value;
        }
    
        public ReadOnlyArray<InputDevice>? devices
        {
            get => actions.devices;
            set => actions.devices = value;
        }
    
        public ReadOnlyArray<InputControlScheme> controlSchemes => actions.controlSchemes;
    
        public bool Contains(InputAction action)
        {
            return actions.Contains(action);
        }
    
        public IEnumerator<InputAction> GetEnumerator()
        {
            return actions.GetEnumerator();
        }
    
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    
        public void Enable()
        {
            actions.Enable();
        }
    
        public void Disable()
        {
            actions.Disable();
        }
    
        public IEnumerable<InputBinding> bindings => actions.bindings;
    
        public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
        {
            return actions.FindAction(actionNameOrId, throwIfNotFound);
        }
    
        public int FindBinding(InputBinding bindingMask, out InputAction action)
        {
            return actions.FindBinding(bindingMask, out action);
        }
    
        public PlayerActions Player => new PlayerActions(actions);
    
        public struct PlayerActions
        {
            private readonly TEngine.InputSystem_Actions wrapper;
    
            public PlayerActions(TEngine.InputSystem_Actions wrapper)
            {
                this.wrapper = wrapper;
            }
    
            private TEngine.InputSystem_Actions.PlayerActions Inner => wrapper.Player;
    
            public InputAction Move => Inner.Move;
            public InputAction Jump => Inner.Jump;
            public InputAction Interactive => Inner.Interactive;
            public InputAction Shift => Inner.Shift;
            public InputAction Scroll => Inner.Scroll;
            public InputAction Look => Inner.Look;
            public InputAction Crouch => Inner.Crouch;
            public InputAction Lock => Inner.Lock;
            public InputActionMap Get() { return Inner.Get(); }
            public void Enable() { Inner.Enable(); }
            public void Disable() { Inner.Disable(); }
            public bool enabled => Inner.enabled;
            public static implicit operator InputActionMap(PlayerActions set) { return set.Get(); }
        }
    }
}
