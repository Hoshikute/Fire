using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 确定性向量3，使用整数避免浮点误差
    /// </summary>
    public struct SyncVector3
    {
        public int x;
        public int y;
        public int z;

        private const float SCALE = 1000f;

        public Vector3 ToVector()
        {
            return new Vector3(x / SCALE, y / SCALE, z / SCALE);
        }

        public void FromVector(Vector3 v)
        {
            x = (int)(v.x * SCALE);
            y = (int)(v.y * SCALE);
            z = (int)(v.z * SCALE);
        }

        public static SyncVector3 FromVector3(Vector3 v)
        {
            SyncVector3 sv = new SyncVector3();
            sv.FromVector(v);
            return sv;
        }

        public SyncVector3 DeepCopy()
        {
            SyncVector3 sv = new SyncVector3
            {
                x = x,
                y = y,
                z = z
            };
            return sv;
        }

        public bool Equals(SyncVector3 sv)
        {
            return sv.x == x && sv.y == y && sv.z == z;
        }

        public override string ToString()
        {
            return $"SyncVector3({x}, {y}, {z})";
        }

        #region 确定性整数运算（全程 int，禁止经过 float 以保证跨端一致）

        /// <summary>定点数 1.0 对应的整数值（与 SCALE 一致）。</summary>
        public const int ONE = 1000;

        public static readonly SyncVector3 Zero = new SyncVector3 { x = 0, y = 0, z = 0 };

        /// <summary>由原始定点整数构造（不经过 float）。</summary>
        public static SyncVector3 FromRaw(int rawX, int rawY, int rawZ)
        {
            return new SyncVector3 { x = rawX, y = rawY, z = rawZ };
        }

        public static SyncVector3 operator +(SyncVector3 a, SyncVector3 b)
        {
            return new SyncVector3 { x = a.x + b.x, y = a.y + b.y, z = a.z + b.z };
        }

        public static SyncVector3 operator -(SyncVector3 a, SyncVector3 b)
        {
            return new SyncVector3 { x = a.x - b.x, y = a.y - b.y, z = a.z - b.z };
        }

        /// <summary>
        /// 定点数缩放：以 ONE(=1000) 为 1.0 的乘数。
        /// 例如 scaleFixed = 500 表示乘以 0.5。用 long 中转防止溢出。
        /// </summary>
        public static SyncVector3 MulFixed(SyncVector3 v, int scaleFixed)
        {
            return new SyncVector3
            {
                x = (int)((long)v.x * scaleFixed / ONE),
                y = (int)((long)v.y * scaleFixed / ONE),
                z = (int)((long)v.z * scaleFixed / ONE),
            };
        }

        /// <summary>整数倍乘（非定点，直接放大整数倍）。</summary>
        public static SyncVector3 operator *(SyncVector3 v, int k)
        {
            return new SyncVector3 { x = v.x * k, y = v.y * k, z = v.z * k };
        }

        /// <summary>平方长度（定点空间，long 防溢出）。比较距离时优先用它，避免开方。</summary>
        public long SqrMagnitude()
        {
            return (long)x * x + (long)y * y + (long)z * z;
        }

        /// <summary>
        /// 长度（定点整数，结果与 x/y/z 同量纲）。用整数牛顿迭代开方，确定性。
        /// </summary>
        public int Magnitude()
        {
            return Sqrt(SqrMagnitude());
        }

        /// <summary>
        /// 归一化为单位向量（长度≈ONE 的定点向量）。零向量返回 Zero。
        /// 全程整数，跨端一致。
        /// </summary>
        public SyncVector3 Normalized()
        {
            int len = Magnitude();
            if (len <= 0)
                return Zero;

            return new SyncVector3
            {
                x = (int)((long)x * ONE / len),
                y = (int)((long)y * ONE / len),
                z = (int)((long)z * ONE / len),
            };
        }

        /// <summary>确定性整数平方根（牛顿迭代），跨端结果一致。</summary>
        public static int Sqrt(long value)
        {
            if (value <= 0)
                return 0;

            long x0 = value;
            long x1 = (x0 + 1) / 2;
            while (x1 < x0)
            {
                x0 = x1;
                x1 = (x0 + value / x0) / 2;
            }
            return (int)x0;
        }

        #endregion
    }
}
