using Animancer;
using TEngine;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家移动状态基类 - 继承 PlayerFsmState。
    /// 提供移动相关的基础功能。
    /// </summary>
    public abstract class PlayerMovementFsmState : PlayerFsmState
    {
        protected PlayerSO playerSO;
        protected int _fallCheckTimerId;

        protected internal override void OnInit(IFsm<Player> fsm)
        {
            base.OnInit(fsm);
            playerSO = player.playerSO;
        }

        protected internal override void OnEnter(IFsm<Player> fsm)
        {
            base.OnEnter(fsm);
        }

        protected internal override void OnLeave(IFsm<Player> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);
            if (_fallCheckTimerId != 0)
            {
                GameModule.Timer.RemoveTimer(_fallCheckTimerId);
                _fallCheckTimerId = 0;
            }
        }

        protected internal override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);

            if (GameModule.Input.GetButtonDown(InputButtonType.Lock))
            {
                ToggleLock();
            }

            // 检查平台跳跃请求
            if (reusableData.platformJumpRequested)
            {
                reusableData.platformJumpRequested = false;
                SwitchState<PlayerPlatformerUpState>();
                return;
            }

            // 锁定状态处理
            if (reusableData.lockValueParameter.TargetValue == 1)
            {
                UpdateLockRotation(5, null);
                UpdateLockValue();
            }

            // 输入中断回调
            reusableData.inputInterruptionCB?.Invoke();
        }

        #region 事件监听

        protected override void AddEventListening()
        {
        }

        protected override void RemoveEventListening()
        {
        }

        #endregion

        #region 锁定相关

        protected void ToggleLock()
        {
            reusableData.lockValueParameter.TargetValue = reusableData.lockValueParameter.TargetValue == 0 ? 1 : 0;
            if (reusableData.lockValueParameter.TargetValue == 1)
            {
                reusableData.lockTarget.Value = cam;
            }
            else
            {
                reusableData.lockTarget.Value = null;
            }
        }

        private void UpdateLockValue()
        {
            reusableData.lock_X_ValueParameter.TargetValue = GameModule.Input.Move.x * reusableData.speedValueParameter.TargetValue;
            reusableData.lock_Y_ValueParameter.TargetValue = GameModule.Input.Move.y * reusableData.speedValueParameter.TargetValue;
        }

        protected void UpdateLockRotation(float rotationSize, Transform lockTarget = null)
        {
            if (lockTarget == null)
            {
                if (cam == null) return;
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up)), Time.deltaTime * rotationSize);
            }
            else
            {
                Vector3 dir = (lockTarget.position - player.transform.position).normalized;
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(dir, Vector3.up)), Time.deltaTime * rotationSize);
            }
        }

        protected void UpdateLockRotation(float rotationSize, Vector3 normal = default)
        {
            if (normal == default)
            {
                if (cam == null) return;
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up)), Time.deltaTime * rotationSize);
            }
            else
            {
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(normal, Vector3.up)), Time.deltaTime * rotationSize);
            }
        }

        #endregion

        #region 移动与旋转

        protected float UpdateSpeed()
        {
            return reusableData.speedValueParameter.TargetValue = GameModule.Input.GetButton(InputButtonType.Shift) ? 2 : 1;
        }

        protected float UpdateRotation(bool isUpdateRotationParameter = true, float rotationSmoothTime = 0.7f, bool isRotationCompensation = true, float rotationSize = 1.4f)
        {
            float angle = GetTargetAngle();
            if (isUpdateRotationParameter)
            {
                reusableData.rotationValueParameter.SmoothTime = rotationSmoothTime;
                reusableData.rotationValueParameter.TargetValue = angle * Mathf.Deg2Rad;
            }
            if (GameModule.Input.Move != Vector2.zero)
            {
                if (isRotationCompensation)
                {
                    player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(reusableData.targetDir), Time.deltaTime * rotationSize);
                }
                return angle;
            }
            return 0;
        }

        protected float GetTargetAngle()
        {
            reusableData.targetDir = GetTargetDir();
            reusableData.targetAngle.Value = ToolFunction.GetDeltaAngle(player.transform, reusableData.targetDir);
            return reusableData.targetAngle.Value;
        }

        protected Vector3 GetTargetDir()
        {
            if (cam == null)
            {
                return new Vector3(GameModule.Input.Move.x, 0, GameModule.Input.Move.y);
            }
            return Quaternion.Euler(0, cam.eulerAngles.y, 0) * new Vector3(GameModule.Input.Move.x, 0, GameModule.Input.Move.y);
        }

        #endregion

        #region 跳跃与下落

        protected void OnJumpStart()
        {
            Debug.Log($"[JumpDebug] INPUT: OnJumpStart from {GetType().Name} | isGround={player.IsOnGround.Value}");
            reusableLogic.OnJump();
        }

        protected void OnEnterFall(IFsm<Player> fsm)
        {
            ChangeState<PlayerFallLoopState>(fsm);
        }

        protected void OnFallToLand(IFsm<Player> fsm, bool onGround)
        {
            if (onGround)
            {
                ChangeState<PlayerLandState>(fsm);
            }
        }

        protected void OnLandToFall(IFsm<Player> fsm)
        {
            if (!player.IsOnGround.Value)
            {
                OnEnterFall(fsm);
            }
            else
            {
                OnStateDefaultEnd(fsm);
            }
        }

        protected void CheckCurrentFall()
        {
            if (!player.IsOnGround.Value)
            {
                StartFallCheckTimer();
            }
        }

        protected void OnCheckFall(bool isGround)
        {
            if (!isGround)
            {
                StartFallCheckTimer();
            }
        }

        private void StartFallCheckTimer()
        {
            if (_fallCheckTimerId != 0)
            {
                return;
            }
            _fallCheckTimerId = GameModule.Timer.AddTimer((timer) =>
            {
                _fallCheckTimerId = 0;
                if (currentFsm.CurrentState == this && !player.IsOnGround.Value)
                {
                    SwitchState<PlayerFallLoopState>();
                }
            }, time: 0.05f);
        }

        protected void InAirMove()
        {
            if (player.IsOnGround.Value)
            {
                return;
            }
            reusableData.horizontalSpeed = Mathf.Lerp(reusableData.horizontalSpeed, GameModule.Input.Move != Vector2.zero ? 2 : 0, 1 - Mathf.Exp(-8 * Time.deltaTime));
            if (reusableData.lockValueParameter.TargetValue == 1)
            {
                player.AddHorizontalVelocityInAir(GetTargetDir() * reusableData.horizontalSpeed * reusableData.currentMidInAirMultiplier + reusableData.currentInertialVelocity / Time.deltaTime);
            }
            else
            {
                player.AddHorizontalVelocityInAir(player.transform.forward * reusableData.horizontalSpeed * reusableData.currentMidInAirMultiplier + reusableData.currentInertialVelocity / Time.deltaTime);
            }
        }

        #endregion

        #region 辅助方法

        protected void OnCrouch()
        {
            reusableData.standValueParameter.TargetValue = reusableData.standValueParameter.TargetValue == 0 ? 1 : 0;
        }

        protected void OnCheckFall(IFsm<Player> fsm, bool isGround)
        {
            if (!isGround)
            {
                _fallCheckTimerId = GameModule.Timer.AddTimer((timer) => OnLandToFall(fsm), time: 0.05f);
            }
        }

        public void UpdateCashVelocity(Vector3 horizontalSpeed)
        {
            reusableData.cashIndex = (reusableData.cashIndex + 1) % PlayerReusableData.cashSize;
            reusableData.cashVelocity[reusableData.cashIndex] = horizontalSpeed;
        }

        public Vector3 GetInertialVelocity()
        {
            Vector3 inertialVelocity = Vector3.zero;
            for (int i = 0; i < reusableData.cashVelocity.Length; i++)
            {
                inertialVelocity += reusableData.cashVelocity[i];
            }
            return inertialVelocity / reusableData.cashVelocity.Length;
        }

        protected void OnStateDefaultEnd(IFsm<Player> fsm)
        {
            ChangeState<PlayerIdleState>(fsm);
        }

        protected virtual void OnInputInterruption(IFsm<Player> fsm)
        {
            reusableData.inputInterruptionCB = () =>
            {
                if (GameModule.Input.Move != Vector2.zero)
                {
                    if (player.IsOnGround.Value)
                    {
                        ChangeState<PlayerMoveStartState>(fsm);
                        reusableData.inputInterruptionCB = null;
                    }
                }
            };
        }

        #endregion
    }
}