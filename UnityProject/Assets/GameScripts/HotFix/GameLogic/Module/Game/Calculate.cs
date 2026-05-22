using System;
using UnityEngine;

namespace GameLogic.Game
{
    /// <summary>
    /// 范围类
    /// </summary>
    [Serializable]
    public class Area
    {
        public AreaType areaType = AreaType.Circle;
        public Vector3 position;
        public Vector3 direction;
        public float radius;
        public float length;
        public float Width;
        public float angle;

        /// <summary>
        /// 区域碰撞成功
        /// </summary>
        public bool AreaCollideSucceed(Area area)
        {
            area.position.y = 0;
            position.y = 0;

            switch (areaType)
            {
                case AreaType.Circle:
                    return Circle(area);
                case AreaType.Rectangle:
                    return Rectangle(area);
                case AreaType.Sector:
                    return Sector(area);
            }
            return true;
        }

        private bool Circle(Area area)
        {
            switch (area.areaType)
            {
                case AreaType.Circle:
                    return Circle_Circle(this, area);
                case AreaType.Rectangle:
                    return Circle_Rectangle(this, area);
                case AreaType.Sector:
                    return Circle_Sector(this, area);
            }
            return true;
        }

        private bool Rectangle(Area area)
        {
            switch (area.areaType)
            {
                case AreaType.Circle:
                    return Circle_Rectangle(area, this);
                case AreaType.Rectangle:
                    return Rectangle_Rectangle(area, this);
                case AreaType.Sector:
                    return Sector_Rectangle(area, this);
            }
            return true;
        }

        private bool Sector(Area area)
        {
            switch (area.areaType)
            {
                case AreaType.Circle:
                    return Circle_Sector(area, this);
                case AreaType.Rectangle:
                    return Sector_Rectangle(this, area);
                case AreaType.Sector:
                    return Sector_Sector(area, this);
            }
            return true;
        }

        private bool Circle_Circle(Area area1, Area area2)
        {
            return Vector3.Distance(area1.position, area2.position) < (area1.radius + area2.radius);
        }

        private bool Circle_Rectangle(Area areaCircle, Area areaRectangle)
        {
            return PointInRectangle(areaCircle.position, areaRectangle, areaCircle.radius * 2, areaCircle.radius * 2);
        }

        private bool Circle_Sector(Area areaCircle, Area areaSector)
        {
            return PointInSector(areaCircle.position, areaSector, areaCircle.radius);
        }

        private bool Sector_Sector(Area area1, Area area2)
        {
            return Vector3.Distance(area1.position, area2.position) < (area1.radius + area2.radius);
        }

        private bool Sector_Rectangle(Area areaSector, Area areaRectangle)
        {
            return PointInRectangle(areaSector.position, areaRectangle, areaSector.radius, areaSector.radius);
        }

        private bool Rectangle_Rectangle(Area area1, Area area2)
        {
            return Vector3.Distance(area1.position, area2.position) < (area1.length + area2.length) * 0.5f;
        }

        public bool PointInRectangle(Vector3 point, Area area, float lenghAdd = 0, float widthAdd = 0)
        {
            Vector3 l_v3_newPos = GetPointPosInRectangle(area, point);
            if (Mathf.Abs(l_v3_newPos.x) < (area.length + lenghAdd) * 0.5f
                && Mathf.Abs(l_v3_newPos.z) < (area.Width + widthAdd) * 0.5f)
            {
                return true;
            }
            return false;
        }

        private bool PointInSector(Vector3 point, Area area, float radiusAdd = 0)
        {
            Vector3 l_v3_forward = area.direction;
            Vector3 l_v3_v = point - area.position;

            if (l_v3_v == Vector3.zero) return true;

            float l_angle = Vector3.Angle(l_v3_forward, l_v3_v);
            float l_newRadius = area.radius + radiusAdd;

            if (l_angle < area.angle * 0.5f || l_angle < 0.05f)
            {
                if (l_v3_v.magnitude < l_newRadius)
                {
                    return true;
                }
            }
            return false;
        }

        private Vector3 GetPointPosInRectangle(Area area, Vector3 point)
        {
            Vector3 m_v3_newPos = new Vector3();
            Vector3 l_v3_v = point - area.position;
            Vector3 l_v3_forward = area.direction;
            float l_angle = Vector3.Angle(l_v3_v, l_v3_forward);
            if (l_angle > 90) l_angle = 180 - l_angle;

            float l_magnitude = l_v3_v.magnitude;
            l_angle = l_angle * Mathf.Deg2Rad;

            m_v3_newPos.x = l_magnitude * Mathf.Cos(l_angle);
            m_v3_newPos.z = l_magnitude * Mathf.Sin(l_angle);

            return m_v3_newPos;
        }
    }

    public enum AreaType
    {
        Circle,
        Rectangle,
        Sector
    }

    public enum DirectionEnum
    {
        Forward,
        Backward,
        Close,
        Leave
    }
}
