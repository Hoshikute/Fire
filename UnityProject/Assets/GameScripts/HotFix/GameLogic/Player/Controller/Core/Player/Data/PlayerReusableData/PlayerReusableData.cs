using Animancer;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public enum ObstructHeight
    {
        low = 0, lowMedium = 1, medium = 2, mediumHight = 3, Hight = 4,
    }

    public enum ClimbType
    {
        Vault, Climb
    }

    public enum MatchType
    {
        Root,
        RootY,
    }

    public struct ClimbTargetMatchInfo
    {
        public Vector3 TargetPos;
        public Vector3 InitPos;
        public bool setTargetMatchInitPos;

        public ClimbTargetMatchInfo(Vector3 targetPos)
        {
            TargetPos = targetPos;
            InitPos = Vector3.zero;
            setTargetMatchInitPos = false;
        }
    }

    public class PlayerReusableData
    {
        public float currentRotationTime;

        public SmoothedFloatParameter standValueParameter { get; set; }
        public SmoothedFloatParameter rotationValueParameter { get; set; }
        public SmoothedFloatParameter speedValueParameter { get; set; }
        public SmoothedFloatParameter lockValueParameter { get; set; }
        public SmoothedFloatParameter lock_X_ValueParameter { get; set; }
        public SmoothedFloatParameter lock_Y_ValueParameter { get; set; }

        public BindableProperty<Transform> lockTarget { get; set; } = new BindableProperty<Transform>();

        public int drawTargetId = -1;
        public int drawCurrentId = -1;
        public Vector3 targetDir;
        public BindableProperty<float> targetAngle = new BindableProperty<float>();
        public BindableProperty<string> currentState = new BindableProperty<string>();

        public ManualMixerState standIdleMixerState;
        public ManualMixerState crouchIdleMixerState;
        public List<AnimancerState> standIdleList = new List<AnimancerState>();
        public List<AnimancerState> crouchIdleList = new List<AnimancerState>();
        public int currentStandIdleIndex;
        public int currentCrouchIdleIndex;
        public bool isLockIdle = false;

        public ObstructHeight ObstructHeight;
        public ClimbType ClimbType;
        public ClipTransition targetClimbClip;

        public float horizontalSpeed;
        public Vector3 currentInertialVelocity;
        public int cashIndex = 0;
        public readonly static int cashSize = 3;
        public Vector3[] cashVelocity = new Vector3[cashSize];

        public float originalCCRadius;
        public Vector3 vaultPos;
        public RaycastHit hit;

        public Action inputInterruptionCB { get; set; }
        public float checkWallDistance = 0.6f;
        public bool isInPlaceJump;
        public float jumpExternalForce = 15;
        public bool platformJumpRequested;
        public float currentMidInAirMultiplier = 0.6f;

        public PlayerReusableData(AnimancerComponent animancerComponent, PlayerSO playerSO)
        {
            standValueParameter = new SmoothedFloatParameter(animancerComponent, playerSO.playerParameterData.standValueParameter, 0.15f);
            standValueParameter.Parameter.Value = 1;

            rotationValueParameter = new SmoothedFloatParameter(animancerComponent, playerSO.playerParameterData.rotationValueParameter, 0.2f);
            speedValueParameter = new SmoothedFloatParameter(animancerComponent, playerSO.playerParameterData.speedValueParameter, 1f);
            lockValueParameter = new SmoothedFloatParameter(animancerComponent, playerSO.playerParameterData.LockValueParameter, 0.1f);
            lock_X_ValueParameter = new SmoothedFloatParameter(animancerComponent, playerSO.playerParameterData.Lock_X_ValueParameter, 0.3f);
            lock_Y_ValueParameter = new SmoothedFloatParameter(animancerComponent, playerSO.playerParameterData.Lock_Y_ValueParameter, 0.3f);
        }
    }
}
