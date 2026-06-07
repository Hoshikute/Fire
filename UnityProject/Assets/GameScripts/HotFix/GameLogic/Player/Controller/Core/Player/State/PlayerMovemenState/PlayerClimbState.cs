using Animancer;
using System;
using TEngine;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家攀爬状态 - TEngine FSM 版本。
    /// </summary>
    public class PlayerClimbState : PlayerMovementFsmState
    {
        private PlayerClimbData climbData;
        private AnimancerState animancerState;
        private ClimbTargetMatchInfo targetMatchInfo_Start;
        private ClimbTargetMatchInfo targetMatchInfo_Y;
        private PlayerClimbAnimationSettings animationSettings;
        private Action cancelClimbTask;

        protected internal override void OnInit(IFsm<Player> fsm)
        {
            base.OnInit(fsm);
            climbData = playerSO.playerMovementData.PlayerClimbData;
        }

        protected internal override void OnEnter(IFsm<Player> fsm)
        {
            var clip = GetClimbAnimation();
            if (clip == null)
            {
                SwitchState<PlayerJumpState>();
                return;
            }

            player.DisEnableGravity = true;
            player.Controller.enabled = false;
            player.ApplyFullRootMotion = true;
            animancerState = animancer.Play(clip);
            animancerState.ApplyFootIK = true;

            animationSettings = GetClimbTimeSetting();
            targetMatchInfo_Y = new ClimbTargetMatchInfo(reusableData.vaultPos + Vector3.up * animationSettings.targetHeightOffSet);
            targetMatchInfo_Start = new ClimbTargetMatchInfo(new Vector3(reusableData.hit.point.x, player.transform.position.y, reusableData.hit.point.z) + reusableData.hit.normal * (0.35f + animationSettings.startMatchDistanceOffset));

            base.OnEnter(fsm);
        }

        protected override void AddEventListening()
        {
            base.AddEventListening();
            var events = animancerState.Events(player);
            events.OnEnd = () => SwitchState<PlayerIdleState>();
            events.SetCallback(playerSO.playerParameterData.moveInterruptEvent, () => OnInputInterruption(currentFsm));
            events.Add(animationSettings.targetMatchTime.y + animationSettings.enableCCTimeOffset, ResetCC);
            SetCancelClimb();
        }

        protected override void RemoveEventListening()
        {
            base.RemoveEventListening();
            reusableData.inputInterruptionCB = null;
            cancelClimbTask = null;
        }

        protected internal override void OnLeave(IFsm<Player> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);
            // 安全恢复 CC：确保即使攀爬被中断也能正确恢复物理状态
            if (player.DisEnableGravity || !player.Controller.enabled || player.ApplyFullRootMotion)
            {
                Debug.Log("攀爬退出时安全恢复CC");
                ResetCC();
            }
            animancerState = null;
        }

        protected internal override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);
            cancelClimbTask?.Invoke();
        }

        public override void OnAnimationUpdate()
        {
            base.OnAnimationUpdate();
            reusableLogic.ClimbTargetMatch(animancerState, ref targetMatchInfo_Start, animationSettings.startMatchTime.x, animationSettings.startMatchTime.y);
            reusableLogic.ClimbTargetMatch_Y(animancerState, ref targetMatchInfo_Y, animationSettings.targetMatchTime.x, animationSettings.targetMatchTime.y);
        }

        private void SetCancelClimb()
        {
            if (reusableData.ClimbType != ClimbType.Climb)
            {
                return;
            }
            if (reusableData.ObstructHeight == ObstructHeight.mediumHight)
            {
                animancerState.Events(player).Add(0.15f, OnCancelClimb);
            }
        }

        private void OnCancelClimb()
        {
            if (animancerState.Speed == -1)
            {
                return;
            }
            Debug.Log("开启取消攀爬动作检测！");
            cancelClimbTask = () =>
            {
                if (animancerState.NormalizedTime >= animationSettings.targetMatchTime.y + animationSettings.enableCCTimeOffset)
                {
                    cancelClimbTask = null;
                }
                float angle = GetTargetAngle();
                if (GameModule.Input.Move != Vector2.zero && Mathf.Abs(angle) > 100)
                {
                    Debug.Log("开始取消攀爬");
                    float currentTime = animancerState.NormalizedTime;
                    animancerState.Speed = -1;
                    animancerState.Events(player).Add(currentTime - 0.12f, FinishCancelClimb);
                    cancelClimbTask = null;
                }
            };
        }

        private void FinishCancelClimb()
        {
            Debug.Log("完成取消攀爬");
            ResetCC();
            SwitchState<PlayerIdleState>();
        }

        private void ResetCC()
        {
            Debug.Log("恢复CC");
            player.DisEnableGravity = false;
            player.Controller.enabled = true;
            player.ApplyFullRootMotion = false;
        }

        public ClipTransition GetClimbAnimation()
        {
            int index = (int)reusableData.ObstructHeight;
            if (reusableData.ClimbType == ClimbType.Climb)
            {
                if (index >= climbData.climbs.Length)
                {
                    return null;
                }
                if (index < 0)
                {
                    return climbData.climbs[0];
                }
                return climbData.climbs[index];
            }
            else if (reusableData.ClimbType == ClimbType.Vault)
            {
                index--;
                if (index < 0)
                {
                    return climbData.vaults[0];
                }
                if (index >= climbData.vaults.Length)
                {
                    return null;
                }
                return climbData.vaults[index];
            }
            return null;
        }

        public PlayerClimbAnimationSettings GetClimbTimeSetting()
        {
            int index = (int)reusableData.ObstructHeight;
            if (reusableData.ClimbType == ClimbType.Climb)
            {
                if (index >= climbData.climbSettings.Length)
                {
                    return climbData.climbSettings[0];
                }
                PlayerClimbAnimationSettings targetSetting = climbData.climbSettings[index];
                if (targetSetting == null)
                {
                    return climbData.climbSettings[0];
                }
                return targetSetting;
            }
            else if (reusableData.ClimbType == ClimbType.Vault)
            {
                if (index >= climbData.vaults.Length)
                {
                    return climbData.vaultSettings[0];
                }
                PlayerClimbAnimationSettings targetSetting = climbData.vaultSettings[index];
                if (targetSetting == null)
                {
                    return climbData.vaultSettings[0];
                }
                return targetSetting;
            }
            return null;
        }
    }
}