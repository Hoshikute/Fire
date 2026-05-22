using UnityEngine;
using GameLogic;

namespace GameLogic.SyncGameLogic.Component
{
    public class BlowFlyComponent : MomentComponentBase
    {
        public bool isBlow = false;
        public int blowTime = 0;
        public string blowFlyID;
        public SyncVector3 blowDir = new SyncVector3();

        public override MomentComponentBase DeepCopy()
        {
            BlowFlyComponent mc = new BlowFlyComponent();
            mc.isBlow = isBlow;
            mc.blowFlyID = blowFlyID;
            mc.blowTime = blowTime;
            mc.blowDir = blowDir.DeepCopy();
            return mc;
        }

        public void SetBlowFly(Vector3 attackerPos, Vector3 selfPos, Vector3 attackerDir)
        {
            // 简化版本，完整逻辑需要配置数据
            Vector3 dir = (selfPos - attackerPos);
            dir.y = 0;
            dir = dir.normalized;
            blowDir.FromVector(dir);
        }
    }

    public enum DirectionEnum
    {
        Forward,
        Backward,
        Close,
        Leave
    }
}
