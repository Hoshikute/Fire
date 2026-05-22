using System;
using System.Collections.Generic;
using GameLogic;

namespace GameLogic.SyncGameLogic.Component
{
    public class CollisionComponent : ComponentBase
    {
        public bool isCanAcross = false;
        public Area area = new Area();

        public bool isEnterCollision = false;

        private List<EntityBase> collisionList = new List<EntityBase>();

        public List<EntityBase> CollisionList
        {
            get { return collisionList; }
            set { collisionList = value; }
        }
    }

    /// <summary>
    /// 区域类 - 简化版本，完整版本在 Game/Calculate.cs
    /// </summary>
    [global::System.Serializable]
    public class Area
    {
        public AreaType areaType = AreaType.Circle;
        public UnityEngine.Vector3 position;
        public UnityEngine.Vector3 direction;
        public float radius;
        public float length;
        public float Width;
        public float angle;
    }

    public enum AreaType
    {
        Circle,
        Rectangle,
        Sector
    }
}
