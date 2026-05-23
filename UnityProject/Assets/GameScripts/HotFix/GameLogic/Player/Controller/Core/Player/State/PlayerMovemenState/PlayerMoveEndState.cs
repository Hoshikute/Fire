using TEngine;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家移动结束状态 - TEngine FSM 版本。
    /// </summary>
    public class PlayerMoveEndState : PlayerMovementFsmState
    {
        private PlayerMoveEndData moveEndData;
        private float angle;
        private float speed;
        private int _fallCheckTid;

        protected internal override void OnInit(IFsm<Player> fsm)
        {
            base.OnInit(fsm);
            moveEndData = playerSO.playerMovementData.PlayerMoveEndData;
        }

        protected internal override void OnEnter(IFsm<Player> fsm)
        {
            base.OnEnter(fsm);
            angle = reusableData.rotationValueParameter.CurrentValue;
            speed = reusableData.speedValueParameter.CurrentValue;

            if (CheckWall())
            {
                return;
            }
            CheckLeftOrRightFoot();
        }

        private bool CheckWall()
        {
            if (Physics.Raycast(player.transform.position + Vector3.up, player.transform.forward, out var hitInfo, reusableData.checkWallDistance, player.whatIsGround))
            {
                float distance = Vector3.Distance(player.transform.position + Vector3.up, hitInfo.point);
                if (distance > 0.45f && distance < reusableData.checkWallDistance)
                {
                    animancer.Play(moveEndData.moveToWall).Events(player).OnEnd = () => SwitchState<PlayerIdleState>();
                    return true;
                }
            }
            return false;
        }

        private void CheckLeftOrRightFoot()
        {
            Transform leftFoot = player.Animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = player.Animator.GetBoneTransform(HumanBodyBones.RightFoot);

            Vector3 leftFootLocalPos = player.transform.InverseTransformPoint(leftFoot.position);
            Vector3 rightFootLocalPos = player.transform.InverseTransformPoint(rightFoot.position);

            if (leftFootLocalPos.z > rightFootLocalPos.z)
            {
                Debug.Log("左腿在前");
                animancer.Play(moveEndData.moveEnd_L).Events(player).OnEnd = () => SwitchState<PlayerIdleState>();
            }
            else
            {
                Debug.Log("右腿在前");
                animancer.Play(moveEndData.moveEnd_R).Events(player).OnEnd = () => SwitchState<PlayerIdleState>();
            }
        }

        protected override void AddEventListening()
        {
            base.AddEventListening();
            player.IsOnGround.ValueChanged += OnCheckFall;
        }

        protected override void RemoveEventListening()
        {
            base.RemoveEventListening();
            player.IsOnGround.ValueChanged -= OnCheckFall;
            if (_fallCheckTid != 0)
            {
                GameModule.Timer.RemoveTimer(_fallCheckTid);
                _fallCheckTid = 0;
            }
        }

        protected internal override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);

            if (inputServer.GetButtonDown(InputButtonType.Jump))
            {
                OnJumpStart();
                return;
            }

            if (inputServer.GetButtonDown(InputButtonType.Crouch))
            {
                OnCrouch();
            }

            if (inputServer.Move != Vector2.zero)
            {
                SwitchState<PlayerMoveStartState>();
                return;
            }

            reusableData.rotationValueParameter.TargetValue = angle;
            reusableData.speedValueParameter.TargetValue = speed;
        }

        private void OnCheckFall(bool isGround)
        {
            if (!isGround)
            {
                _fallCheckTid = GameModule.Timer.AddTimer((timer) =>
                {
                    if (!player.IsOnGround.Value)
                    {
                        SwitchState<PlayerFallLoopState>();
                    }
                }, time: 0.05f);
            }
        }
    }
}
