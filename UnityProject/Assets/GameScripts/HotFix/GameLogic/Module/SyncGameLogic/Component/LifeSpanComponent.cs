using GameLogic;

namespace GameLogic.SyncGameLogic.Component
{
    public class LifeSpanComponent : MomentComponentBase
    {
        public int lifeTime = 0;

        public override MomentComponentBase DeepCopy()
        {
            LifeSpanComponent lsc = new LifeSpanComponent();
            lsc.lifeTime = lifeTime;
            return lsc;
        }
    }
}
