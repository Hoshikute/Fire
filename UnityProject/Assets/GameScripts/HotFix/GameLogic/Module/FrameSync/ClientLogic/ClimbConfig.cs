
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 攀爬/翻越轨迹配置（ScriptableObject）。
    /// 每种攀爬类型（Vault/Climb/LedgeClimb/PlatformerUp）各有一组逐帧位移表。
    ///
    /// 使用方式：
    ///   1. 在 Unity Editor 中创建 ClimbConfig 资产。
    ///   2. 从老 TPC 动画曲线提取关键位移量，填入各轨迹表的帧数据。
    ///   3. 挂到 PlayerWorld / PlayerMoveSystem 的序列化字段上。
    ///
    /// 轨迹推进：
    ///   逻辑帧索引 i → ClimbFrameDelta[i].ToDelta() → 叠加到 PlayerMoveComponent.pos。
    ///   索引超出轨迹表长度时，攀爬结束，切回 Idle/Fall。
    /// </summary>
    [CreateAssetMenu(fileName = "ClimbConfig", menuName = "Game/Climb Config")]
    public class ClimbConfig : ScriptableObject
    {
        [Header("Vault 翻越（矮障碍物）")]
        [Tooltip("Vault 翻越的逐帧位移表。")]
        public List<ClimbFrameDelta> vaultTrajectory = new List<ClimbFrameDelta>();

        [Header("Climb 攀爬（高障碍物）")]
        [Tooltip("Climb 攀爬的逐帧位移表。")]
        public List<ClimbFrameDelta> climbTrajectory = new List<ClimbFrameDelta>();

        [Header("LedgeClimb 边缘攀上")]
        [Tooltip("LedgeClimb 边缘攀上的逐帧位移表。")]
        public List<ClimbFrameDelta> ledgeClimbTrajectory = new List<ClimbFrameDelta>();

        [Header("PlatformerUp 平台跳上")]
        [Tooltip("PlatformerUp 平台跳上的逐帧位移表。")]
        public List<ClimbFrameDelta> platformerUpTrajectory = new List<ClimbFrameDelta>();

        /// <summary>
        /// 根据状态枚举获取对应的轨迹表。
        /// </summary>
        public List<ClimbFrameDelta> GetTrajectory(PlayerLogicState state)
        {
            switch (state)
            {
                case PlayerLogicState.Vault:         return vaultTrajectory;
                case PlayerLogicState.Climb:         return climbTrajectory;
                case PlayerLogicState.LedgeClimb:    return ledgeClimbTrajectory;
                case PlayerLogicState.PlatformerUp:  return platformerUpTrajectory;
                default: return null;
            }
        }
    }
}
