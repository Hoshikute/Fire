
using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家动画剪辑配置（ScriptableObject）。
    /// 集中管理所有逻辑状态对应的 Animancer ClipTransition，
    /// 替代原 ThirdPersonController 里散落在各 PlayerStateDataSO 中的动画字段。
    ///
    /// 对齐参考项目 A:\animator-third-person-controller 的动画映射：
    ///   - MoveStart 按 8 方向选 clip（F/R45/R90/R135/R180/L135/L90/L45）
    ///   - MoveEnd 按左右脚选 clip（moveEnd_L / moveEnd_R）
    ///   - PlatformerUp 三阶段（start → loop → downLoop）
    ///
    /// 使用方式：
    ///   1. 在 Project 里 Create → GameLogic → PlayerAnimConfig 创建 SO 资产。
    ///   2. 在 Inspector 里把各状态的动画剪辑拖入对应字段。
    ///   3. 资源地址约定为 "PlayerAnimConfig"，由 TPBattleContext 加载后注入 PlayerViewComponent。
    ///
    /// 命名规范：字段名与 PlayerLogicState 枚举值一一对应，方便检索。
    /// 可选字段（nullable）不赋值时表现系统会跳过播放并打 Log.Warning 提示。
    /// </summary>
    [CreateAssetMenu(menuName = "GameLogic/PlayerAnimConfig", fileName = "PlayerAnimConfig")]
    public class PlayerAnimConfig : ScriptableObject
    {
        [Header("地面常态")]
        [Tooltip("待机（Idle）")]
        public TransitionAsset idle;

        [Header("移动启步（MoveStart）— 按 8 方向选择")]
        [Tooltip("起步-正前（夹角 <22.5°）")]
        public TransitionAsset moveStart_F;
        [Tooltip("起步-右前 45°（22.5°~67.5°）")]
        public TransitionAsset moveStart_R45;
        [Tooltip("起步-右 90°（67.5°~112.5°）")]
        public TransitionAsset moveStart_R90;
        [Tooltip("起步-右后 135°（112.5°~157.5°）")]
        public TransitionAsset moveStart_R135;
        [Tooltip("起步-正后 180°（>157.5° 或 <-157.5°）")]
        public TransitionAsset moveStart_R180;
        [Tooltip("起步-左后 135°（-157.5°~-112.5°）")]
        public TransitionAsset moveStart_L135;
        [Tooltip("起步-左 90°（-112.5°~-67.5°）")]
        public TransitionAsset moveStart_L90;
        [Tooltip("起步-左前 45°（-67.5°~-22.5°）")]
        public TransitionAsset moveStart_L45;

        [Header("移动循环 / 结束")]
        [Tooltip("移动循环（MoveLoop）")]
        public TransitionAsset moveLoop;

        [Tooltip("移动结束—左脚在前（MoveEnd_L）")]
        public TransitionAsset moveEnd_L;

        [Tooltip("移动结束—右脚在前（MoveEnd_R）")]
        public TransitionAsset moveEnd_R;

        [Header("靠墙")]
        [Tooltip("靠墙过渡（MoveToWall）；为空时复用 moveEnd_L")]
        public TransitionAsset moveToWall;

        [Header("锁定模式")]
        [Tooltip("锁定待机（LockIdle）；为空时回退使用 idle")]
        public TransitionAsset lockIdle;

        [Header("空中")]
        [Tooltip("前跳起跳（Jump）")]
        public TransitionAsset jumpForward;

        [Tooltip("就地跳起跳（JumpInPlace）")]
        public TransitionAsset jumpInPlace;

        [Tooltip("下落开始（Fall 入场）")]
        public TransitionAsset fallStart;

        [Tooltip("下落循环（Fall 持续）")]
        public TransitionAsset fallLoop;

        [Tooltip("落地（Land）")]
        public TransitionAsset land;

        [Header("平台跳（PlatformerUp）— 三阶段")]
        [Tooltip("平台跳起跳（start）")]
        public TransitionAsset platformerUpStart;

        [Tooltip("平台跳上升循环（loop）")]
        public TransitionAsset platformerUpLoop;

        [Tooltip("平台跳下落（downLoop）")]
        public TransitionAsset platformerDownLoop;

        [Header("交互 / 攀爬")]
        [Tooltip("翻越矮障碍物（Vault）")]
        public TransitionAsset vault;

        [Tooltip("攀爬高障碍物（Climb）")]
        public TransitionAsset climb;

        [Tooltip("边缘攀上（LedgeClimb）")]
        public TransitionAsset ledgeClimb;

        public bool HasRequiredBaseTransitions(out string missingFields)
        {
            List<string> missing = new List<string>();

            AddIfNull(missing, idle, nameof(idle));
            AddIfNull(missing, moveStart_F, nameof(moveStart_F));
            AddIfNull(missing, moveStart_R45, nameof(moveStart_R45));
            AddIfNull(missing, moveStart_R90, nameof(moveStart_R90));
            AddIfNull(missing, moveStart_R135, nameof(moveStart_R135));
            AddIfNull(missing, moveStart_R180, nameof(moveStart_R180));
            AddIfNull(missing, moveStart_L135, nameof(moveStart_L135));
            AddIfNull(missing, moveStart_L90, nameof(moveStart_L90));
            AddIfNull(missing, moveStart_L45, nameof(moveStart_L45));
            AddIfNull(missing, moveLoop, nameof(moveLoop));
            AddIfNull(missing, moveEnd_L, nameof(moveEnd_L));
            AddIfNull(missing, moveEnd_R, nameof(moveEnd_R));
            AddIfNull(missing, GetMoveToWallTransition(), $"{nameof(moveToWall)} or {nameof(moveEnd_L)} fallback");
            AddIfNull(missing, GetLockIdleTransition(), $"{nameof(lockIdle)} or {nameof(idle)} fallback");
            AddIfNull(missing, jumpForward, nameof(jumpForward));
            AddIfNull(missing, jumpInPlace, nameof(jumpInPlace));
            AddIfNull(missing, fallLoop, nameof(fallLoop));
            AddIfNull(missing, land, nameof(land));

            missingFields = string.Join(", ", missing);
            return missing.Count == 0;
        }

        public TransitionAsset GetMoveToWallTransition()
        {
            return moveToWall != null ? moveToWall : moveEnd_L;
        }

        public TransitionAsset GetLockIdleTransition()
        {
            return lockIdle != null ? lockIdle : idle;
        }

        private static void AddIfNull(List<string> missing, TransitionAsset asset, string fieldName)
        {
            if (asset == null)
            {
                missing.Add(fieldName);
            }
        }
    }
}
