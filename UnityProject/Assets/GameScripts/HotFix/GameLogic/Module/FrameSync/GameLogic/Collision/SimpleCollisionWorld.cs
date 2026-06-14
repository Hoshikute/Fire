
using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 简单确定性碰撞世界：AABB 包围盒列表 + 胶囊碰撞检测。
    ///
    /// 胶囊碰撞简化模型：
    ///   竖直胶囊建模为底部半球中心到顶部半球中心的线段 + 半径 r。
    ///   AABB 碰撞 = 找 AABB 表面到线段的最短距离 &lt; r。
    ///
    /// 当前阶段仅提供基础碰撞检测框架；后续可扩展 BVH 加速或更精确几何。
    ///
    /// 确定性铁律：全程定点整数，无 float/Physics。
    /// </summary>
    public class SimpleCollisionWorld : ICollisionWorld
    {
        private const int MinSweepStep = 100;

        private readonly List<AABB> m_boxes = new List<AABB>();

        public void AddBox(SyncVector3 min, SyncVector3 max)
        {
            m_boxes.Add(new AABB { min = min, max = max });
        }

        public void Clear()
        {
            m_boxes.Clear();
        }

        public bool CapsuleOverlap(SyncVector3 center, int radius, int height)
        {
            // 胶囊底部半球中心（胶囊中点下移半个高度）
            int halfHeight = height / 2;
            SyncVector3 bottom = SyncVector3.FromRaw(center.x, center.y - halfHeight, center.z);
            SyncVector3 top    = SyncVector3.FromRaw(center.x, center.y + halfHeight, center.z);

            for (int i = 0; i < m_boxes.Count; i++)
            {
                AABB box = m_boxes[i];
                if (CapsuleAABBOverlap(bottom, top, radius, box))
                {
                    return true;
                }
            }
            return false;
        }

        public CollisionResult CapsuleSweep(SyncVector3 from, SyncVector3 to, int radius, int height)
        {
            int maxDelta = Max3(Abs(to.x - from.x), Abs(to.y - from.y), Abs(to.z - from.z));
            int stepSize = radius > 0 ? Max(MinSweepStep, radius / 2) : MinSweepStep;
            int steps = Max(1, (maxDelta + stepSize - 1) / stepSize);

            for (int step = 0; step <= steps; step++)
            {
                SyncVector3 sample = SyncVector3.FromRaw(
                    from.x + (int)((long)(to.x - from.x) * step / steps),
                    from.y + (int)((long)(to.y - from.y) * step / steps),
                    from.z + (int)((long)(to.z - from.z) * step / steps));

                CollisionResult result = GetCapsulePenetration(sample, radius, height);
                if (result.hit)
                {
                    return result;
                }
            }

            return CollisionResult.None;
        }

        private CollisionResult GetCapsulePenetration(SyncVector3 center, int radius, int height)
        {
            int halfHeight = height / 2;
            SyncVector3 bottom = SyncVector3.FromRaw(center.x, center.y - halfHeight, center.z);
            SyncVector3 top    = SyncVector3.FromRaw(center.x, center.y + halfHeight, center.z);

            for (int i = 0; i < m_boxes.Count; i++)
            {
                AABB box = m_boxes[i];
                CollisionResult? result = CapsuleAABBPenetration(bottom, top, radius, box);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }

            return CollisionResult.None;
        }

        // ── 内部：胶囊 vs AABB ──────────────────────────────────────

        /// <summary>
        /// 竖直胶囊（线段+半径）与 AABB 的重叠检测。
        /// 算法：找线段上距 AABB 最近的点，检查距离 &lt; 半径。
        /// 线段参数：bottom → top（均为定点）。
        /// </summary>
        private static bool CapsuleAABBOverlap(
            SyncVector3 bottom, SyncVector3 top, int radius, AABB box)
        {
            // 最近点 = clamp(线段中点/采样点, box.min, box.max)
            // 简化：用胶囊中点
            int midX = (bottom.x + top.x) / 2;
            int midY = (bottom.y + top.y) / 2;
            int midZ = (bottom.z + top.z) / 2;

            int closestX = Clamp(midX, box.min.x, box.max.x);
            int closestY = Clamp(midY, box.min.y, box.max.y);
            int closestZ = Clamp(midZ, box.min.z, box.max.z);

            long dx = midX - closestX;
            long dy = midY - closestY;
            long dz = midZ - closestZ;
            long distSq = dx * dx + dy * dy + dz * dz;

            long radiusSq = (long)radius * radius;
            return distSq < radiusSq;
        }

        /// <summary>
        /// 胶囊 vs AABB 穿透信息：返回碰撞法线和穿透深度。
        /// </summary>
        private static CollisionResult? CapsuleAABBPenetration(
            SyncVector3 bottom, SyncVector3 top, int radius, AABB box)
        {
            int midX = (bottom.x + top.x) / 2;
            int midY = (bottom.y + top.y) / 2;
            int midZ = (bottom.z + top.z) / 2;

            int closestX = Clamp(midX, box.min.x, box.max.x);
            int closestY = Clamp(midY, box.min.y, box.max.y);
            int closestZ = Clamp(midZ, box.min.z, box.max.z);

            long dx = midX - closestX;
            long dy = midY - closestY;
            long dz = midZ - closestZ;
            long distSq = dx * dx + dy * dy + dz * dz;

            long radiusSq = (long)radius * radius;
            if (distSq >= radiusSq)
            {
                return null;
            }

            if (distSq == 0)
            {
                return BuildInsidePenetration(midX, midY, midZ, radius, box);
            }

            // 定点距离（开平方用整数牛顿迭代或近似）
            int dist = SqrtFixed(distSq);
            if (dist <= 0) dist = 1; // 避免除零

            int penetration = radius - dist;

            // 法线 = (mid - closest) / dist（定点单位向量 = 归一化 × ONE）
            int nx = (int)(dx * SyncVector3.ONE / dist);
            int ny = (int)(dy * SyncVector3.ONE / dist);
            int nz = (int)(dz * SyncVector3.ONE / dist);

            SyncVector3 normal = SyncVector3.FromRaw(nx, ny, nz);
            SyncVector3 point  = SyncVector3.FromRaw(closestX, closestY, closestZ);

            return new CollisionResult
            {
                hit         = true,
                normal      = normal,
                penetration = penetration,
                point       = point,
            };
        }

        // ── 工具 ────────────────────────────────────────────────────

        private static int Clamp(int val, int min, int max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        private static CollisionResult BuildInsidePenetration(int midX, int midY, int midZ, int radius, AABB box)
        {
            int bestDistance = midX - box.min.x;
            SyncVector3 normal = SyncVector3.FromRaw(-SyncVector3.ONE, 0, 0);
            SyncVector3 point = SyncVector3.FromRaw(box.min.x, midY, midZ);

            TryUseFace(box.max.x - midX, SyncVector3.FromRaw(SyncVector3.ONE, 0, 0), SyncVector3.FromRaw(box.max.x, midY, midZ), ref bestDistance, ref normal, ref point);
            TryUseFace(midY - box.min.y, SyncVector3.FromRaw(0, -SyncVector3.ONE, 0), SyncVector3.FromRaw(midX, box.min.y, midZ), ref bestDistance, ref normal, ref point);
            TryUseFace(box.max.y - midY, SyncVector3.FromRaw(0, SyncVector3.ONE, 0), SyncVector3.FromRaw(midX, box.max.y, midZ), ref bestDistance, ref normal, ref point);
            TryUseFace(midZ - box.min.z, SyncVector3.FromRaw(0, 0, -SyncVector3.ONE), SyncVector3.FromRaw(midX, midY, box.min.z), ref bestDistance, ref normal, ref point);
            TryUseFace(box.max.z - midZ, SyncVector3.FromRaw(0, 0, SyncVector3.ONE), SyncVector3.FromRaw(midX, midY, box.max.z), ref bestDistance, ref normal, ref point);

            return new CollisionResult
            {
                hit = true,
                normal = normal,
                penetration = radius + bestDistance,
                point = point,
            };
        }

        private static void TryUseFace(int distance, SyncVector3 normal, SyncVector3 point, ref int bestDistance, ref SyncVector3 bestNormal, ref SyncVector3 bestPoint)
        {
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestNormal = normal;
                bestPoint = point;
            }
        }

        private static int Abs(int val)
        {
            return val < 0 ? -val : val;
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }

        private static int Max3(int a, int b, int c)
        {
            return Max(Max(a, b), c);
        }

        /// <summary>定点整数开平方（牛顿迭代）。</summary>
        private static int SqrtFixed(long n)
        {
            if (n <= 1) return (int)n;
            long x = n;
            long y = (x + 1) / 2;
            while (y < x)
            {
                x = y;
                y = (x + n / x) / 2;
            }
            return (int)x;
        }

        // ── 内部结构 ─────────────────────────────────────────────────

        private struct AABB
        {
            public SyncVector3 min;
            public SyncVector3 max;
        }
    }
}
