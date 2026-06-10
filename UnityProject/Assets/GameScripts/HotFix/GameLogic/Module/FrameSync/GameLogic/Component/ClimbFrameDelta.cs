// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 程序化定点攀爬轨迹数据。
    /// 每一逻辑帧的位移增量（SyncVector3 转 Vector3Int 序列化表示）。
    /// 替代老 TPC 的 Animancer 动画曲线驱动位移。
    ///
    /// 轨迹数据从老 TPC 动画曲线提取关键位移量，转为定点数逐帧存储。
    /// </summary>
    [Serializable]
    public struct ClimbFrameDelta
    {
        /// <summary>该帧的位移增量 x 分量（定点，毫单位）。</summary>
        public int x;

        /// <summary>该帧的位移增量 y 分量（定点，毫单位）。</summary>
        public int y;

        /// <summary>该帧的位移增量 z 分量（定点，毫单位）。</summary>
        public int z;

        public SyncVector3 ToDelta()
        {
            return SyncVector3.FromRaw(x, y, z);
        }
    }
}
