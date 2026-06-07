using Animancer;
using System;
using UnityEngine;

namespace ThirdPersonController
{
    /**************************************************************************
    作者: HuHu
    邮箱: 3112891874@qq.com
    功能: 逻辑算法复用类
    **************************************************************************/
    public class PlayerReusableLogic
    {
        Player player { get; set; }
        PlayerReusableData reusableData;
        AnimancerComponent animator;
        PlayerSO playerSO;
        PlayerMovementData playerMovementData;
        public PlayerReusableLogic(Player player)
        {
            this.player = player;
            playerSO = player.playerSO;
            playerMovementData = playerSO.playerMovementData;
            animator = player.Animancer;
            reusableData = player.ReusableData;
        }
        public void InitIldeState()
        {
            var state = animator.Play(playerMovementData.PlayerIdleData.idle);
            bool isUpdateIdleState =( reusableData.isLockIdle && reusableData.lockValueParameter.TargetValue == 0 )|| (!reusableData.isLockIdle && reusableData.lockValueParameter.TargetValue == 1);
            if (reusableData.standIdleMixerState == null|| isUpdateIdleState)
            {
                if (reusableData.lockValueParameter.TargetValue == 1)//锁敌
                {
                    reusableData.standIdleMixerState = state.GetChild(1).GetChild(1) as ManualMixerState;
                    reusableData.isLockIdle = true;
                }
                else
                {
                    reusableData.standIdleMixerState = state.GetChild(0).GetChild(1) as ManualMixerState;
                    reusableData.isLockIdle = false;
                }
              
            }
            if (reusableData.crouchIdleMixerState == null ||isUpdateIdleState)
            {
                if (reusableData.lockValueParameter.TargetValue == 1)//锁敌
                {
                    reusableData.crouchIdleMixerState = state.GetChild(1).GetChild(0) as ManualMixerState;
                    reusableData.isLockIdle = true;
                }
                else
                {
                    reusableData.crouchIdleMixerState = state.GetChild(0).GetChild(0) as ManualMixerState;
                    reusableData.isLockIdle = false;
                }
            }
    
            if (reusableData.standIdleMixerState != null &&(reusableData.standIdleList.Count != reusableData.standIdleMixerState.ChildCount|| isUpdateIdleState))//拿到idleStandStates
            {
                reusableData.standIdleList.Clear();
                AnimancerState animancerState;
                for (int i = 0; i < reusableData.standIdleMixerState.ChildCount; i++)
                {
                    animancerState = reusableData.standIdleMixerState.GetChild(i);
                    animancerState.Events(player).OnEnd = PlayNextState;
                    reusableData.standIdleList.Add(animancerState);
                }
                reusableData.standIdleList[0].Weight = 1;
            }
            if (reusableData.crouchIdleMixerState != null && (reusableData.crouchIdleList.Count != reusableData.crouchIdleMixerState.ChildCount || isUpdateIdleState))//拿到idleCrouchStates
            {
                reusableData.crouchIdleList.Clear();
                AnimancerState animancerState;
                for (int i = 0; i < reusableData.crouchIdleMixerState.ChildCount; i++)
                {
                    animancerState = reusableData.crouchIdleMixerState.GetChild(i);
                    if (reusableData.crouchIdleMixerState.ChildCount != 1)
                    {
                        animancerState.Events(player).OnEnd = PlayNextState;
                    }
                    reusableData.crouchIdleList.Add(animancerState);
                }
                reusableData.crouchIdleList[0].Weight = 1;
            }
        }
    
        public void PlayNextState()
        {
            if (reusableData.standValueParameter.TargetValue == 1)
            {
                if (reusableData.standIdleList.Count == 0) return;
                reusableData.currentStandIdleIndex = (reusableData.currentStandIdleIndex + 1) % reusableData.standIdleList.Count;
    
                for (int i = 0; i < reusableData.standIdleList.Count; i++)
                {
                    if (i == reusableData.currentStandIdleIndex)
                    {
                        reusableData.standIdleList[i].SetWeight(1);
                        reusableData.standIdleList[i].Play();
                    }
                    else
                    {
                        reusableData.standIdleList[i].SetWeight(0);
                        reusableData.standIdleList[i].Stop();
                    }
                }
            }
            else if (reusableData.standValueParameter.TargetValue == 0)
            {
                if (reusableData.crouchIdleList.Count == 0) return;
                reusableData.currentCrouchIdleIndex = (reusableData.currentCrouchIdleIndex + 1) % reusableData.crouchIdleList.Count;
    
                for (int i = 0; i < reusableData.crouchIdleList.Count; i++)
                {
                    if (i == reusableData.currentCrouchIdleIndex)
                    {
                        reusableData.crouchIdleList[i].SetWeight(1);
                        reusableData.crouchIdleList[i].Play();
                    }
                    else
                    {
                        reusableData.crouchIdleList[i].SetWeight(0);
                        reusableData.crouchIdleList[i].Stop();
                    }
                }
            }
    
        }
    
        #region 按跳跃时的逻辑
        float detectionAngle = 45;
        float detectionDistance = 1;
        float vaultMaxDistance = 0.45f;
        float canClimbMaxHight = 3.2f;
        float canClimbMinHeight = 0.3f;
        int detectionSamplingCount = 30;
        /// <summary>
        /// 玩家按下跳跃键时
        /// </summary>
        public void OnJump()
        {
            float vaultHeight = 0;
            float obstructHeight = 0;
            RaycastHit hit = GetWallHight(player.transform.forward, canClimbMinHeight, canClimbMaxHight,detectionDistance, ref vaultHeight,ref obstructHeight,detectionSamplingCount);
            float angle = Vector3.Angle(-player.transform.forward, hit.normal);
            Debug.Log($"[JumpDebug] DECIDE: OnJump triggered | obstructHeight={obstructHeight:F2} angle={angle:F1} hitPoint=({hit.point.x:F2},{hit.point.y:F2},{hit.point.z:F2})");
            if (hit.point.y ==0 || angle > detectionAngle)//判断这个墙如果太高也不做攀爬//判断人物与墙面角度是否符合要求：TODO玩家的输入是否影响
            {
                Debug.Log($"[JumpDebug] EXEC: → JumpState (no wall or angle too big, angle={angle:F1})");
                player.SwitchState<PlayerJumpState>();
                return;
            }
            Vector3 vaultStartPos = new Vector3(hit.point.x, vaultHeight, hit.point.z);
            reusableData.vaultPos = vaultStartPos;
            reusableData.hit = hit;
            // 按障碍高度分类决策：攀爬 / 翻越 / 普通跳跃
            // 注意 VaultOrClimb 内部会通过 player.SwitchState<PlayerClimbState> 切换状态，此处不要重复调用
            if (obstructHeight >= 2f)//中高及以上：不做攀爬，普通跳跃
            {
                Debug.Log($"[JumpDebug] EXEC: → JumpState (too high, obstructHeight={obstructHeight:F2})");
                player.SwitchState<PlayerJumpState>();
                return;
            }
            else if (obstructHeight >= 1f && obstructHeight < 1.7f)//中攀
            {
                reusableData.ObstructHeight = ObstructHeight.medium;
                VaultOrClimb(vaultStartPos, hit);
                return;
            }
            else if (obstructHeight >= 0.35f && obstructHeight < 1f)//低中攀
            {
                reusableData.ObstructHeight = ObstructHeight.lowMedium;
                VaultOrClimb(vaultStartPos, hit);
                return;
            }
            else if (obstructHeight > 0)//低攀:只能爬不能翻越
            {
                reusableData.ObstructHeight = ObstructHeight.low;
                reusableData.ClimbType = ClimbType.Climb;
                Debug.Log($"[JumpDebug] EXEC: → ClimbState (low, obstructHeight={obstructHeight:F2})");
                player.SwitchState<PlayerClimbState>();
                return;
            }
            else
            {
                // 无有效障碍物，普通跳跃
                Debug.Log($"[JumpDebug] EXEC: → JumpState (no valid obstacle)");
                player.SwitchState<PlayerJumpState>();
                return;
            }
        }
    
        /// <summary>
        /// 检测障碍物的最大高度
        /// </summary>
        private RaycastHit GetWallHight(Vector3 targetDir,float startDetectionHight,float maxDetectionHight, float detectionLength,ref float vaultHight,ref float obstructHeight,int detectionSamplingCount)
        {
            RaycastHit currentHit = new RaycastHit() ;
            Vector3 h = Vector3.zero;
            Vector3 startPos = player.transform.position + Vector3.up * startDetectionHight;
            float sampleQuantityPerUnit = (maxDetectionHight - startDetectionHight) / detectionSamplingCount;
    
            for (int i = 0; i <= detectionSamplingCount+1; i++)
            {
                Vector3 currentPos = startPos + Vector3.up * sampleQuantityPerUnit * i;
              
                if (Physics.Raycast(currentPos, targetDir, out var hitInfo, detectionLength, player.whatIsGround))
                {
                    currentHit = hitInfo;//更新最高墙面碰撞点
                    h = currentPos;
                }
                Debug.DrawLine(currentPos, currentPos - hitInfo.normal * detectionDistance, Color.red);
            }
            obstructHeight = currentHit.point.y - player.transform.position.y;
            if (obstructHeight >= canClimbMaxHight)//认为检测点高于最高爬的高度
            {
                return default;
            }
            else if (obstructHeight<=0)//认为没有检测到任何障碍物
            {
                return default;
            }
            else
            {
                if (Physics.Raycast(h, -currentHit.normal, out var hitInfo, detectionDistance, player.whatIsGround))
                {
                    currentHit.point = hitInfo.point;
                    Debug.DrawLine(h, h - hitInfo.normal * detectionDistance, Color.blue);
                    //翻越高度 = 最高碰撞点高度 + 每单位采样高度
                    vaultHight = currentHit.point.y + sampleQuantityPerUnit;
                    return currentHit;
                }
                return default;
            }
        }
        private void VaultOrClimb(Vector3 VaultStart,RaycastHit wallHit)
        {
            //根据这个高度再次检测判断是否能翻越还是攀爬
            if (Physics.Raycast(VaultStart, -wallHit.normal, vaultMaxDistance, player.whatIsGround))//先判断翻越空间有没有其他物体遮挡
            {
                reusableData.ClimbType = ClimbType.Climb;
                Debug.Log($"[JumpDebug] DECIDE: Climb (space blocked) ObstructHeight={reusableData.ObstructHeight}");
            }
            else
            {
                Vector3 vaultDetectionPos = VaultStart  +(- wallHit.normal * vaultMaxDistance);
                Debug.DrawLine(vaultDetectionPos, vaultDetectionPos+Vector3.down*0.25f,Color.cyan,2);
                if (Physics.Raycast(vaultDetectionPos, Vector3.down, 0.25f))
                {
                    reusableData.ClimbType = ClimbType.Climb;
                    Debug.Log($"[JumpDebug] DECIDE: Climb (wall too thick) ObstructHeight={reusableData.ObstructHeight}");
                }
                else
                {
                    reusableData.ClimbType = ClimbType.Vault;
                    Debug.Log($"[JumpDebug] DECIDE: Vault (can vault over) ObstructHeight={reusableData.ObstructHeight}");
                }
            }
            // 统一使用 PlayerClimbState，ClimbType 决定播放攀爬还是翻越动画
            Debug.Log($"[JumpDebug] EXEC: → ClimbState (ClimbType={reusableData.ClimbType})");
            player.SwitchState<PlayerClimbState>();
        }
        #endregion
    
        #region 在空中的检测
        public void InAirMoveCheck(Vector3 targetDir)
        {
            float vaultHeight = 0;
            float obstructHeight = 0;
            RaycastHit hit = GetWallHight(targetDir,1.6f,1.9f,1.5f, ref vaultHeight, ref obstructHeight, 8);
            float angle = Vector3.Angle(-player.transform.forward, hit.normal);
            if (hit.point.y == 0 || angle > detectionAngle)
            {
                return;
            }
            reusableData.hit = hit;
            if (obstructHeight > 1.6f && obstructHeight < 1.9f)
            {
                player.SwitchState<PlayerLedgeClimbState>();
                animator.Play(playerSO.playerMovementData.PlayerHangWallData.hang_wall_idle_frond);
            }
        }
        #endregion
        /// <summary>
        /// 攀爬的位置（x\y\z）匹配,放在OnAnimationUpdate中调用，确保cc组件禁用，开启角色根运动
        /// </summary>
        public void ClimbTargetMatch(AnimancerState animancerState,ref ClimbTargetMatchInfo climbTargetMatchInfo, float startNormalizedTime, float endNormalizedTime)
        {
            float currentTime = animancerState.NormalizedTime;
            if (!climbTargetMatchInfo.setTargetMatchInitPos && animator.States.Current.NormalizedTime > startNormalizedTime)
            {
                climbTargetMatchInfo.setTargetMatchInitPos = true;
                climbTargetMatchInfo.InitPos = player.transform.position;
            }
            if (currentTime > startNormalizedTime && currentTime < endNormalizedTime)
            {
                float t = (currentTime - startNormalizedTime) / (endNormalizedTime - startNormalizedTime);
                player.transform.position = Vector3.Lerp(climbTargetMatchInfo.InitPos, climbTargetMatchInfo.TargetPos, t);
            }
        }
        /// <summary>
        /// 攀爬的位置(只对Y)匹配,放在OnAnimationUpdate中调用，确保cc组件禁用，开启角色根运动
        /// </summary>
        public void ClimbTargetMatch_Y(AnimancerState animancerState,ref ClimbTargetMatchInfo climbTargetMatchInfo, float startNormalizedTime, float endNormalizedTime)
        {
            float currentTime = animancerState.NormalizedTime;
            if (!climbTargetMatchInfo.setTargetMatchInitPos && animator.States.Current.NormalizedTime > startNormalizedTime)
            {
                climbTargetMatchInfo.setTargetMatchInitPos = true;
                climbTargetMatchInfo.InitPos = player.transform.position;
            }
            if (currentTime > startNormalizedTime && currentTime < endNormalizedTime)
            {
                float t = (currentTime - startNormalizedTime) / (endNormalizedTime - startNormalizedTime);
                Vector3 targetPos = new Vector3(player.transform.position.x, Mathf.Lerp(climbTargetMatchInfo.InitPos.y, climbTargetMatchInfo.TargetPos.y, t), player.transform.position.z);
                player.transform.position = targetPos;
            }
        }
        public void SetClimbTarget_Y_Task(AnimancerState animancerState, ref ClimbTargetMatchInfo climbTargetMatchInfo, float startNormalizedTime, float endNormalizedTime)
        {
            var localClimbTargetMatchInfo = climbTargetMatchInfo;
    
            animationTask = () =>
            {
                ClimbTargetMatch_Y(animancerState, ref localClimbTargetMatchInfo, startNormalizedTime, endNormalizedTime);
            };
    
            climbTargetMatchInfo = localClimbTargetMatchInfo;
    
        }
    
        public void RemoveClimbTarget_Y_Task()
        {
            animationTask = null;
        }
    
        Action animationTask;
        public void SetAnimationMotionCompensationTask(Vector3 compensationMovement,AnimancerState targetAnimation, float startNormalTime=0,float endNormalizedTime = 1)
        {
            animationTask = () =>
            {
                if (!targetAnimation.IsPlaying)
                {
                    return;
                }
                if (player.ApplyFullRootMotion)
                {
                    player.ApplyFullRootMotion = true;
                    player.Controller.enabled = true;
                }
                float time = targetAnimation.Duration;
                float startSeconds = time * startNormalTime;
                float endSeconds = time * endNormalizedTime;
                if (targetAnimation.NormalizedTime >= startNormalTime && targetAnimation.NormalizedTime <= endNormalizedTime)
                {
                    Vector3 frameCompensation = compensationMovement * (Time.deltaTime / (endSeconds - startSeconds));
                    player.AnimatorDeltaPositionOffset = frameCompensation;
                }
                else
                {
                    player.AnimatorDeltaPositionOffset = Vector3.zero;
                }
            };
        }
        public void RemoveAnimationMotionCompensationTask()
        {
            player.AnimatorDeltaPositionOffset = Vector3.zero;
            animationTask = null;
        }
    }
}