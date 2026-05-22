using System;
using System;
using GameLogic;

namespace GameLogic.SyncGameLogic.System
{
    public class MoveSystem : SystemBase
    {
        public override Type[] GetFilter()
        {
            return new Type[] { typeof(MoveComponent) };
        }

        public override void Update(int deltaTime)
        {
            // 移动逻辑
        }
    }
}
