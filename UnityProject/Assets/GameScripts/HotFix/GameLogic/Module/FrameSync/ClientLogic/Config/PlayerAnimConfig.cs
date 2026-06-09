// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

using Animancer;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家动画剪辑配置（ScriptableObject）。
    /// 集中管理所有逻辑状态对应的 Animancer ClipTransition，
    /// 替代原 ThirdPersonController 里散落在各 PlayerStateDataSO 中的动画字段。
    ///
    /// 使用方式：
    ///   1. 在 Project 里 Create → GameLogic → PlayerAnimConfig 创建 SO 资产。
    ///   2. 在 Inspector 里把各状态的动画剪辑拖入对应字段。
    ///   3. 把 SO 资产拖入 PlayerFrameSyncEntry.animConfig 字段。
    ///
    /// 命名规范：字段名与 PlayerLogicState 枚举值一一对应，方便检索。
    /// 可选字段（nullable）不赋值时表现系统会跳过播放并打 Log.Warning 提示。
    /// </summary>
    [CreateAssetMenu(menuName = "GameLogic/PlayerAnimConfig", fileName = "PlayerAnimConfig")]
    public class PlayerAnimConfig : ScriptableObject
    {
        [Header("地面常态")]
        [Tooltip("待机（Idle）")]
        public ClipTransition idle;

        [Tooltip("移动启步（MoveStart）")]
        public ClipTransition moveStart;

        [Tooltip("移动循环（MoveLoop）")]
        public ClipTransition moveLoop;

        [Tooltip("移动结束（MoveEnd）")]
        public ClipTransition moveEnd;

        [Header("锁定模式")]
        [Tooltip("锁定待机（LockIdle）；为空时回退使用 idle")]
        public ClipTransition lockIdle;

        [Header("空中")]
        [Tooltip("前跳起跳（Jump）")]
        public ClipTransition jumpForward;

        [Tooltip("就地跳起跳（JumpInPlace）")]
        public ClipTransition jumpInPlace;

        [Tooltip("下落开始（Fall 入场）")]
        public ClipTransition fallStart;

        [Tooltip("下落循环（Fall 持续）")]
        public ClipTransition fallLoop;

        [Tooltip("落地（Land）")]
        public ClipTransition land;

        [Header("交互 / 攀爬")]
        [Tooltip("靠墙过渡（MoveToWall）；为空时回退使用 idle")]
        public ClipTransition moveToWall;

        [Tooltip("翻越矮障碍物（Vault）")]
        public ClipTransition vault;

        [Tooltip("攀爬高障碍物（Climb）")]
        public ClipTransition climb;

        [Tooltip("边缘攀上（LedgeClimb）")]
        public ClipTransition ledgeClimb;

        [Tooltip("平台跳（PlatformerUp）")]
        public ClipTransition platformerUp;
    }
}
