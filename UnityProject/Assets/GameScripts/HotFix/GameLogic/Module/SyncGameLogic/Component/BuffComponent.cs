using System.Collections.Generic;
using GameLogic;

namespace GameLogic.SyncGameLogic.Component
{
    public class BuffComponent : MomentComponentBase
    {
        public List<BuffInfo> buffList = new List<BuffInfo>();

        public override MomentComponentBase DeepCopy()
        {
            BuffComponent bc = new BuffComponent();
            bc.buffList.Clear();
            for (int i = 0; i < buffList.Count; i++)
            {
                bc.buffList.Add(buffList[i].DeepCopy());
            }
            return bc;
        }
    }

    public class BuffInfo
    {
        public string buffID;
        public int creater;
        public int buffTime;
        public int buffCount;
        public int hitTime;

        public BuffInfo DeepCopy()
        {
            BuffInfo bi = new BuffInfo();
            bi.buffID = buffID;
            bi.creater = creater;
            bi.buffTime = buffTime;
            bi.buffCount = buffCount;
            bi.hitTime = hitTime;
            return bi;
        }
    }
}
