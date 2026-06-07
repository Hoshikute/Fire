using System;
using TEngine;
using UnityEngine;

namespace ThirdPersonController
{
    [RequireComponent(typeof(Animator), typeof(CharacterController))]
    public class CharacterBase : MonoBehaviour
    {
        public CharacterController Controller { get; private set; }
        public Animator Animator { get; private set; }

        [Header("Gravity Settings")]
        [SerializeField] public float gravity = -12;
        [SerializeField] public Vector2 velocityLimit = new Vector2(-20, 60);
        [SerializeField] public LayerMask whatIsGround;
        [SerializeField] private float groundDetectedOffset = -0.06f;
        [SerializeField] private float groundRadius = 1.2f;
        private Vector3 detectedOrigin;
        public BindableProperty<bool> IsOnGround { set; get; } = new BindableProperty<bool>();

        public float VerticalSpeed { get; set; }
        private Vector3 verticalVelocity;
        private Vector3 horizontalVelocityInAir;
        private Vector3 animationVelocity;
        public Vector3 AnimationVelocity => animationVelocity;
        private Vector3 moveDir;
        public Vector3 AnimatorDeltaPositionOffset { get; set; }
        public bool ApplyFullRootMotion { get; set; } = false;

        [SerializeField, Range(0.1f, 10)] public float moveSpeedMult = 1;
        public bool DisEnableRootMotion { get; set; }
        public bool IgnoreRootMotionY { get; set; } = false;
        public bool DisEnableGravity { get; set; } = false;
        public bool IgnoreRotationRootMotion { get; set; } = false;

        protected virtual void Awake()
        {
            Animator = GetComponent<Animator>();
            Controller = GetComponent<CharacterController>();
        }

        protected virtual void Update()
        {
            CheckOnGround();
            CharacterGravity();
            CharacterVerticalVelocity();
            ResetHorizontalVelocity();
        }

        #region Gravity Handling

        private bool CheckOnGround()
        {
            detectedOrigin = transform.position - groundDetectedOffset * Vector3.up;
            var isHit = Physics.CheckSphere(detectedOrigin, groundRadius, whatIsGround, QueryTriggerInteraction.Ignore);
            var prevGround = IsOnGround.Value;
            IsOnGround.Value = isHit && VerticalSpeed < 0;

            // ★ 诊断日志：离地/着地变化
            if (prevGround != IsOnGround.Value)
            {
                Log.Warning($"[Claude] IsOnGround changed: {prevGround} → {IsOnGround.Value} | isHit={isHit} VS={VerticalSpeed:F2} gravity={gravity} | pos=({transform.position.x:F2},{transform.position.y:F2},{transform.position.z:F2}) | groundLayer={whatIsGround.value} | detectedOrigin=({detectedOrigin.x:F2},{detectedOrigin.y:F2},{detectedOrigin.z:F2}) groundRadius={groundRadius:F2}");
            }

            return IsOnGround.Value;
        }

        private void CharacterGravity()
        {
            if (DisEnableGravity) return;

            if (IsOnGround.Value)
            {
                VerticalSpeed = -2;
            }
            else
            {
                VerticalSpeed += Time.deltaTime * gravity;
                VerticalSpeed = Mathf.Clamp(VerticalSpeed, velocityLimit.x, velocityLimit.y);
            }
            verticalVelocity = new Vector3(0, VerticalSpeed, 0);
        }

        #endregion

        #region Player Movement

        private void ResetHorizontalVelocity()
        {
            if (IsOnGround.Value && horizontalVelocityInAir != Vector3.zero)
            {
                horizontalVelocityInAir = Vector3.zero;
            }
        }

        private void CharacterVerticalVelocity()
        {
            if (DisEnableGravity)
            {
                verticalVelocity = Vector3.zero;
            }
            if (Controller.enabled)
            {
                Controller.Move((verticalVelocity + horizontalVelocityInAir) * Time.deltaTime);
            }
        }

        protected virtual void OnAnimatorMove()
        {
            if (DisEnableRootMotion) return;

            if (ApplyFullRootMotion)
            {
                Animator.ApplyBuiltinRootMotion();
            }
            else
            {
                Vector3 animationMovement = Animator.deltaPosition + AnimatorDeltaPositionOffset;
                if (IgnoreRootMotionY)
                {
                    animationMovement.y = 0;
                }
                moveDir = SetDirOnSlop(animationMovement) * moveSpeedMult;
                UpdateCharacterMove(moveDir, Animator.deltaRotation);
            }
        }

        public void UpdateCharacterMove(Vector3 deltaDir, Quaternion deltaRotation)
        {
            if (!IgnoreRotationRootMotion && deltaRotation != Quaternion.identity)
            {
                transform.rotation = deltaRotation * transform.rotation;
            }
            if (Controller.enabled)
            {
                animationVelocity = deltaDir;
                Controller.Move(deltaDir);
            }
        }

        public float ChangeVerticalSpeed(float verticalSpeed)
        {
            return VerticalSpeed = verticalSpeed;
        }

        public void AddHorizontalVelocityInAir(Vector3 vector3)
        {
            horizontalVelocityInAir = new Vector3(vector3.x, 0, vector3.z);
        }

        public void ClearHorizontalVelocity()
        {
            horizontalVelocityInAir = Vector3.zero;
        }

        #endregion

        #region Slope Handling

        private Vector3 SetDirOnSlop(Vector3 dir)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out var hitInfo, 1))
            {
                if (Vector3.Dot(hitInfo.normal, Vector3.up) != 1)
                {
                    return Vector3.ProjectOnPlane(dir, hitInfo.normal);
                }
            }
            return dir;
        }

        #endregion

        private void OnDrawGizmos()
        {
            Gizmos.color = CheckOnGround() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position - groundDetectedOffset * Vector3.up, groundRadius);
        }
    }
}
