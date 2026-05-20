using Animancer;
using System;
using TEngine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家悬崖攀爬状态 - TEngine FSM 版本。
    /// </summary>
    public class PlayerLedgeClimbState : PlayerMovementFsmState
    {
        private PlayerHangWallData HangWallData;
        private BindableProperty<bool> isHandingRotation = new BindableProperty<bool>();
        private Action handRotaionTask;
        private Action climbUpTask;
        private Vector3 startDetectionPos;
        private bool isInitMatchTargeting;
        private bool isClimbUp;
        private bool isClimbUpCancel;
        private bool isHangOut;
        private float handHight;
        private float detectionOffset = -0.02f;
        private float CCRadiusMult = 0.5f;
        private Vector3 targetPoint;
        private float maxError = 0.08f;
        private CapsuleCollider capsuleCollider;
        private Rigidbody rigidbody;
        private Vector3 matchSpeed = Vector3.zero;

        protected internal override void OnInit(IFsm<Player> fsm)
        {
            base.OnInit(fsm);
            HangWallData = playerSO.playerMovementData.PlayerHangWallData;
        }

        protected internal override void OnEnter(IFsm<Player> fsm)
        {
            player.Controller.enabled = false;
            player.ApplyFullRootMotion = true;
            player.DisEnableGravity = true;

            if (!player.transform.TryGetComponent(out capsuleCollider))
            {
                capsuleCollider = player.gameObject.AddComponent<CapsuleCollider>();
                capsuleCollider.radius = player.Controller.radius * CCRadiusMult;
                capsuleCollider.height = player.Controller.height / 2f;
                capsuleCollider.center = player.Controller.center;
            }
            if (!player.transform.TryGetComponent(out rigidbody))
            {
                rigidbody = player.gameObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.constraints = RigidbodyConstraints.FreezeRotationZ;
                rigidbody.constraints = RigidbodyConstraints.FreezeRotationX;
            }
            capsuleCollider.enabled = true;
            rigidbody.isKinematic = false;

            isInitMatchTargeting = true;
            isHandingRotation.Value = false;
            isClimbUp = false;
            isHangOut = false;

            handHight = Mathf.Abs(HangWallData.hightAndForwardOffSet.x) + detectionOffset;
            targetPoint = reusableData.hit.point + Vector3.up * HangWallData.hightAndForwardOffSet.x + reusableData.hit.normal * (HangWallData.hightAndForwardOffSet.y);

            base.OnEnter(fsm);
        }

        protected override void AddEventListening()
        {
            base.AddEventListening();
            inputServer.inputMap.Player.Move.started += OnMove;
            inputServer.inputMap.Player.Move.canceled += MoveEnd;
            inputServer.inputMap.Player.Jump.started += OnJump;
            isHandingRotation.ValueChanged += HandRotaion;
        }

        protected override void RemoveEventListening()
        {
            base.RemoveEventListening();
            inputServer.inputMap.Player.Move.started -= OnMove;
            inputServer.inputMap.Player.Move.canceled -= MoveEnd;
            inputServer.inputMap.Player.Jump.started -= OnJump;
            isHandingRotation.ValueChanged -= HandRotaion;
            reusableLogic.RemoveClimbTarget_Y_Task();
        }

        protected internal override void OnLeave(IFsm<Player> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);
            isHangOut = false;
            player.DisEnableGravity = false;
            player.Controller.enabled = true;
            player.ApplyFullRootMotion = false;
            capsuleCollider.enabled = false;
            GameObject.Destroy(rigidbody);
        }

        private void HangWallStartEnd()
        {
            if (inputServer.Move == Vector2.zero)
            {
                animancer.Play(HangWallData.hang_wall_idle);
            }
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            if (isInitMatchTargeting) return;
            if (isClimbUpCancel) return;
            if (isClimbUp) return;

            float angle = GetTargetAngle();
            if (angle < 45 && angle >= -45)
            {
                isClimbUp = true;
                AnimancerState animancerState = animancer.Play(HangWallData.hang_wall_climb_up);
                ClimbTargetMatchInfo climbTargetMatchInfo = new ClimbTargetMatchInfo(reusableData.hit.point + Vector3.up * 0.25f);
                reusableLogic.SetClimbTarget_Y_Task(animancerState, ref climbTargetMatchInfo, 0.6f, 1);
                animancerState.Events(player).SetCallback(playerSO.playerParameterData.moveInterruptEvent, () => OnInputInterruption(currentFsm));
                animancerState.Events(player).OnEnd = () => SwitchState<PlayerIdleState>();
                climbUpTask = () =>
                {
                    if (animancerState.NormalizedTime > 0.6f)
                    {
                        climbUpTask = null;
                    }
                    if (inputServer.Move == Vector2.zero)
                    {
                        animancer.Play(HangWallData.hang_wall_idle_inertia_01);
                        targetPoint = reusableData.hit.point + Vector3.up * HangWallData.hightAndForwardOffSet.x + reusableData.hit.normal * (HangWallData.hightAndForwardOffSet.y);
                        reusableLogic.RemoveAnimationMotionCompensationTask();
                        climbUpTask = null;
                        isClimbUp = false;
                        isClimbUpCancel = true;
                    }
                };
            }
            else if (angle < 135 && angle >= 45)
            {
                animancer.Play(HangWallData.hand_wallMove_Mixer);
                reusableData.rotationValueParameter.TargetValue = 90 * Mathf.Deg2Rad;
            }
            else if (angle < -45 && angle >= -135)
            {
                animancer.Play(HangWallData.hand_wallMove_Mixer);
                reusableData.rotationValueParameter.TargetValue = -90 * Mathf.Deg2Rad;
            }
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            float angle = GetTargetAngle();
            if (inputServer.Move == Vector2.zero)
            {
                HangJumpOut();
                reusableData.currentInertialVelocity = Vector3.zero;
                animancer.Play(HangWallData.hang_wall_idle_jump_out_up).Events(player).OnEnd = OnJumpFall;
            }
            else if (angle < -45 && angle >= -135)
            {
                HangJumpOut();
                reusableData.currentInertialVelocity = 1.5f * GetTargetDir().normalized * Time.deltaTime;
                animancer.Play(HangWallData.hang_wall_idle_jump_out_left).Events(player).OnEnd = OnJumpFall;
            }
            else if (angle < 135 && angle >= 45)
            {
                HangJumpOut();
                reusableData.currentInertialVelocity = 1.5f * GetTargetDir().normalized * Time.deltaTime;
                animancer.Play(HangWallData.hang_wall_idle_jump_out_right).Events(player).OnEnd = OnJumpFall;
            }
        }

        private void HangJumpOut()
        {
            isHangOut = true;
            player.Controller.enabled = true;
            player.ApplyFullRootMotion = false;
            player.DisEnableGravity = true;
            rigidbody.isKinematic = true;
            RemoveEventListening();
        }

        private void OnJumpFall()
        {
            player.VerticalSpeed = -2;
            if (!player.IsOnGround.Value)
            {
                SwitchState<PlayerFallLoopState>();
            }
        }

        private void MoveEnd(InputAction.CallbackContext context)
        {
            if (isClimbUp) return;
            if (isInitMatchTargeting) return;
            if (isClimbUpCancel) return;

            if (reusableData.rotationValueParameter.CurrentValue > 0)
            {
                animancer.Play(HangWallData.hang_wall_idle_right_inertia).Events(player).OnEnd = HangWallStartEnd;
            }
            else
            {
                animancer.Play(HangWallData.hang_wall_idle_left_inertia).Events(player).OnEnd = HangWallStartEnd;
            }
            reusableData.rotationValueParameter.TargetValue = 0;
        }

        protected internal override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);
            climbUpTask?.Invoke();
            handRotaionTask?.Invoke();
            if (isClimbUp) return;
            if (isHangOut) return;

            if (isInitMatchTargeting)
            {
                player.transform.position = Vector3.SmoothDamp(player.transform.position, targetPoint, ref matchSpeed, 0.05f, 200);
                if (Vector3.Distance(player.transform.position, targetPoint) < 0.02f)
                {
                    animancer.Play(HangWallData.hang_wall_idle_frond).Events(player).OnEnd = HangWallStartEnd;
                    isInitMatchTargeting = false;
                }
            }
            else if (isClimbUpCancel)
            {
                player.transform.position = Vector3.SmoothDamp(player.transform.position, targetPoint, ref matchSpeed, 0.08f, 80);
                if (Vector3.Distance(player.transform.position, targetPoint) < 0.01f)
                {
                    isClimbUpCancel = false;
                }
            }
            else
            {
                startDetectionPos = (player.transform.position + Vector3.up * handHight - player.transform.forward * 0.2f);
                if (Physics.Raycast(startDetectionPos, player.transform.forward, out var ray, 1.0f, player.whatIsGround))
                {
                    if (Physics.Raycast(startDetectionPos + Vector3.up * maxError, player.transform.forward, out var hitInfo, 0.5f, player.whatIsGround) && !isHandingRotation.Value)
                    {
                        reusableData.hit = hitInfo;
                    }
                    else
                    {
                        reusableData.hit = ray;
                    }
                    Vector3 target = reusableData.hit.point + (Vector3.up * HangWallData.hightAndForwardOffSet.x) + reusableData.hit.normal * (HangWallData.hightAndForwardOffSet.y);
                    if (Vector3.Distance(player.transform.position, target) > 0.04f)
                    {
                        player.transform.position = Vector3.Lerp(player.transform.position, target, Time.deltaTime * 8f);
                    }
                }
                else
                {
                    if (Physics.Raycast(startDetectionPos + Vector3.down * maxError, player.transform.forward, out var hitInfo, 0.5f, player.whatIsGround) && !isHandingRotation.Value)
                    {
                        reusableData.hit = hitInfo;
                        Vector3 target = reusableData.hit.point + (Vector3.up * HangWallData.hightAndForwardOffSet.x) + reusableData.hit.normal * (HangWallData.hightAndForwardOffSet.y);
                        if (Vector3.Distance(player.transform.position, target) > 0.04f)
                        {
                            player.transform.position = Vector3.Lerp(player.transform.position, target, Time.deltaTime * 8);
                        }
                    }
                    else
                    {
                        isHandingRotation.Value = true;
                    }
                }
            }
            UpdateLockRotation(8f, -reusableData.hit.normal);
        }

        private void HandRotaion(bool noWall)
        {
            if (isInitMatchTargeting) return;
            if (isClimbUp) return;
            if (isClimbUpCancel) return;
            if (isHangOut) return;

            if (noWall)
            {
                startDetectionPos += player.transform.forward * 0.5f;
                Vector3 detectionDir = reusableData.rotationValueParameter.CurrentValue >= 0 ? -player.transform.right : player.transform.right;
                if (Physics.Raycast(startDetectionPos, detectionDir, out var hitInfo, 0.8f, player.whatIsGround))
                {
                    reusableData.hit = hitInfo;
                    Vector3 target = reusableData.hit.point + (Vector3.up * HangWallData.hightAndForwardOffSet.x) + reusableData.hit.normal * (HangWallData.hightAndForwardOffSet.y);
                    handRotaionTask = () =>
                    {
                        player.transform.position = Vector3.Lerp(player.transform.position, target, Time.deltaTime * 6f);
                        if (Vector3.Distance(player.transform.position, target) < 0.1f)
                        {
                            isHandingRotation.Value = false;
                            handRotaionTask = null;
                        }
                    };
                }
                else
                {
                    SwitchState<PlayerFallLoopState>();
                }
            }
        }
    }
}
