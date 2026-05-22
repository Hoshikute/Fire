using GameLogic;

namespace GameLogic.SyncGameLogic.Component
{
    public class CDComponent : MomentComponentBase
    {
        public int CD;

        public override MomentComponentBase DeepCopy()
        {
            CDComponent cc = new CDComponent();
            cc.ID = ID;
            cc.Frame = Frame;
            cc.CD = CD;
            return cc;
        }
    }
}
