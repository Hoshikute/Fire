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
    }
}
