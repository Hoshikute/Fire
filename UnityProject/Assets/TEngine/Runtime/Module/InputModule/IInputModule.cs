using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TEngine
{
    /// <summary>
    /// 输入按键类型。
    /// </summary>
    public enum InputButtonType
    {
        Move,
        Look,
        Attack,
        Interact,
        Crouch,
        Jump,
        Previous,
        Next,
        Sprint,
        Interactive,
        Shift,
        Scroll,
        Lock
    }

    /// <summary>
    /// 输入事件监听器接口。
    /// </summary>
    public interface IInputActionListener
    {
        void OnMove(InputAction.CallbackContext context, Vector2 value);
        void OnLook(InputAction.CallbackContext context, Vector2 value);
        void OnAttack(InputAction.CallbackContext context);
        void OnInteract(InputAction.CallbackContext context);
        void OnCrouch(InputAction.CallbackContext context);
        void OnJump(InputAction.CallbackContext context);
        void OnSprint(InputAction.CallbackContext context);
        void OnInteractive(InputAction.CallbackContext context);
        void OnShift(InputAction.CallbackContext context);
        void OnScroll(InputAction.CallbackContext context, Vector2 value);
        void OnLock(InputAction.CallbackContext context);
        void OnPrevious(InputAction.CallbackContext context);
        void OnNext(InputAction.CallbackContext context);
    }

    /// <summary>
    /// 输入模块接口。
    /// </summary>
    public interface IInputModule
    {
        #region 状态属性

        /// <summary>
        /// 当前激活的控制方案名称。
        /// </summary>
        string CurrentControlScheme { get; }

        /// <summary>
        /// Player 输入映射是否启用。
        /// </summary>
        bool PlayerEnabled { get; set; }

        /// <summary>
        /// UI 输入映射是否启用。
        /// </summary>
        bool UIEnabled { get; set; }

        /// <summary>
        /// 输入是否被全局锁定（如打开模态窗口时）。
        /// </summary>
        bool InputLocked { get; set; }

        #endregion

        #region 输入值访问

        /// <summary>
        /// 获取移动输入值。
        /// </summary>
        Vector2 Move { get; }

        /// <summary>
        /// 获取视角输入值。
        /// </summary>
        Vector2 Look { get; }

        /// <summary>
        /// 获取滚轮输入值。
        /// </summary>
        Vector2 Scroll { get; }

        /// <summary>
        /// 检测按键是否按下。
        /// </summary>
        bool GetButtonDown(InputButtonType buttonType);

        /// <summary>
        /// 检测按键是否持续按下。
        /// </summary>
        bool GetButton(InputButtonType buttonType);

        /// <summary>
        /// 检测按键是否抬起。
        /// </summary>
        bool GetButtonUp(InputButtonType buttonType);

        #endregion

        #region 事件订阅

        /// <summary>
        /// 注册输入事件监听器。
        /// </summary>
        void AddListener(IInputActionListener listener);

        /// <summary>
        /// 移除输入事件监听器。
        /// </summary>
        void RemoveListener(IInputActionListener listener);

        /// <summary>
        /// 注册指定按键的回调。
        /// </summary>
        void RegisterButtonCallback(InputButtonType buttonType, UnityEngine.InputSystem.InputActionPhase phase, Action callback);

        /// <summary>
        /// 取消注册指定按键的回调。
        /// </summary>
        void UnregisterButtonCallback(InputButtonType buttonType, UnityEngine.InputSystem.InputActionPhase phase, Action callback);

        #endregion

        #region 控制方案切换

        /// <summary>
        /// 切换控制方案。
        /// </summary>
        void SwitchControlScheme(string schemeName);

        /// <summary>
        /// 获取所有可用的控制方案。
        /// </summary>
        string[] GetAvailableControlSchemes();

        #endregion

        #region 向后兼容

        /// <summary>
        /// 获取原始 InputSystem_Actions 实例（用于向后兼容）。
        /// </summary>
        InputSystem_Actions Actions { get; }

        #endregion
    }
}
