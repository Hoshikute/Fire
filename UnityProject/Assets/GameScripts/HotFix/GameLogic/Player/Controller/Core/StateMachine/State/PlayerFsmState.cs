using Animancer;
using TEngine;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 基于 TEngine FSM 的玩家状态基类。
    /// 适配现有 StateBase 功能到 FsmState<Player>。
    /// </summary>
    public abstract class PlayerFsmState : FsmState<Player>
    {
        protected Player player;
        protected AnimancerComponent animancer;
        protected PlayerReusableData reusableData;
        protected Transform cam;
        private PlayerReusableLogic _reusableLogic;

        /// <summary>
        /// 当前 FSM 引用 - 用于在事件回调中切换状态。
        /// </summary>
        protected IFsm<Player> currentFsm;

        public PlayerReusableLogic reusableLogic
        {
            get
            {
                if (_reusableLogic == null)
                {
                    _reusableLogic = player.ReusableLogic;
                }
                return _reusableLogic;
            }
        }

        /// <summary>
        /// 状态初始化 - 从 FSM Owner 获取依赖。
        /// </summary>
        protected internal override void OnInit(IFsm<Player> fsm)
        {
            player = fsm.Owner;
            reusableData = player.ReusableData;
            cam = player.CamTransform;
            animancer = player.Animancer;
            currentFsm = fsm;

            // 添加空检查警告
            if (cam == null)
            {
                Debug.LogWarning($"[PlayerFsmState] cam 为 null，玩家旋转功能可能异常");
            }
        }

        /// <summary>
        /// 进入状态 - 子类实现。
        /// </summary>
        protected internal override void OnEnter(IFsm<Player> fsm)
        {
            currentFsm = fsm;
            AddEventListening();
        }

        /// <summary>
        /// 离开状态 - 移除事件监听。
        /// </summary>
        protected internal override void OnLeave(IFsm<Player> fsm, bool isShutdown)
        {
            RemoveEventListening();
        }

        /// <summary>
        /// 动画帧更新 - 由 Player 调用。
        /// </summary>
        public virtual void OnAnimationUpdate()
        {
        }

        /// <summary>
        /// 动画结束回调 - 由 Player 调用。
        /// </summary>
        public virtual void OnAnimationEnd()
        {
        }

        /// <summary>
        /// 添加事件监听 - 子类实现。
        /// </summary>
        protected abstract void AddEventListening();

        /// <summary>
        /// 移除事件监听 - 子类实现。
        /// </summary>
        protected abstract void RemoveEventListening();

        /// <summary>
        /// 切换状态 - 使用保存的 FSM 引用。
        /// </summary>
        protected void SwitchState<TState>() where TState : PlayerFsmState
        {
            if (currentFsm != null)
            {
                ChangeState<TState>(currentFsm);
            }
        }

        /// <summary>
        /// 切换状态 - 显式传入 FSM。
        /// </summary>
        protected void SwitchState<TState>(IFsm<Player> fsm) where TState : PlayerFsmState
        {
            ChangeState<TState>(fsm);
        }
    }
}
