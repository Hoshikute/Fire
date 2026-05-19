using System;
using NaughtyBezierCurves;
using UnityEngine;

#if EGAMEPLAY_ET
using AO;
using Unity.Mathematics;
using Vector3 = Unity.Mathematics.float3;
#else
using float3 = UnityEngine.Vector3;
#endif

namespace EGamePlay.Combat
{
    public class AbilityItemPathMoveComponent : Component
    {
        public IPosition PositionEntity { get; set; }
        public bool Rotate { get; set; }
        public float RotateRadian { get; set; }
        public float Duration { get; set; }
        public float Speed { get; set; } = 0.05f;
        public float Progress { get; set; }
        public BezierCurve3D BezierCurve { get; set; }
        public IPosition OriginEntity { get; set; }
        public Vector3 ExecutePoint { get; set; }

        private int _segmentIndex;
        private float _segmentElapsed;
        private float _segmentDuration;
        private Vector3 _segmentStart;
        private Vector3 _segmentEnd;
        private bool _isMoving;

        public Vector3 OriginPoint
        {
            get
            {
                if (OriginEntity != null)
                {
                    return OriginEntity.Position;
                }

                return ExecutePoint;
            }
        }

        public override void Update()
        {
#if EGAMEPLAY_ET
            var abilityItem = GetEntity<AbilityItem>();
            var itemUnit = abilityItem.GetComponent<CombatUnitComponent>().Unit;
            if (itemUnit != null && !PositionEntity.Position.Equals(itemUnit.MapUnit().Position))
            {
                PositionEntity.Position = itemUnit.MapUnit().Position;
            }
#endif

            if (!_isMoving)
            {
                return;
            }

            _segmentElapsed += Time.deltaTime;
            float progress = _segmentDuration <= 0f ? 1f : Mathf.Clamp01(_segmentElapsed / _segmentDuration);
            PositionEntity.Position = Vector3.Lerp(_segmentStart, _segmentEnd, progress);

            if (progress >= 1f)
            {
                _segmentIndex++;
                SetupSegment();
            }
        }

        public float3[] GetPathPoints()
        {
            var points = GetPathLocalPoints();
            for (int i = 0; i < points.Length; i++)
            {
                points[i] += OriginPoint;
            }

            return points;
        }

        public float3[] GetPathLocalPoints()
        {
            var abilityItem = GetEntity<AbilityItem>();
            var pathPoints = new float3[BezierCurve.Sampling];
            var perc = 1f / BezierCurve.Sampling;
            for (int i = 1; i <= BezierCurve.Sampling; i++)
            {
                var endValue = BezierCurve.GetPoint(perc * i);
                var v = endValue;

                if (Rotate)
                {
                    var x = v.x;
                    var y = v.y;
                    var x1 = x * math.cos(RotateRadian) - y * math.sin(RotateRadian);
                    var y1 = y * math.cos(RotateRadian) + x * math.sin(RotateRadian);
                    v = new float3(x1, y1, v.z);
                }

                pathPoints[i - 1] = v;
            }

            var length = math.distance(pathPoints[0], abilityItem.LocalPosition);
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                length += math.distance(pathPoints[i + 1], pathPoints[i]);
            }

            Speed = length / Duration;
            return pathPoints;
        }

        public void FollowMove()
        {
#if !EGAMEPLAY_ET
            var localPos = GetEntity<AbilityItem>().LocalPosition;
            PositionEntity.Position = OriginPoint + localPos;
#endif
        }

        public void DOMove()
        {
            _segmentIndex = 0;
            _isMoving = true;
            SetupSegment();
        }

        private void SetupSegment()
        {
            if (BezierCurve == null || _segmentIndex >= BezierCurve.Sampling)
            {
                _isMoving = false;
                return;
            }

            Progress = Mathf.Clamp01((float)(_segmentIndex + 1) / Math.Max(1, BezierCurve.Sampling));
            var localPos = BezierCurve.GetPoint(Progress);
            GetEntity<AbilityItem>().LocalPosition = localPos;

            _segmentStart = PositionEntity.Position;
            _segmentEnd = OriginPoint + localPos;
            float distance = math.distance(_segmentEnd, _segmentStart);
            _segmentDuration = Speed <= 0f ? 0f : distance / Speed;
            _segmentElapsed = 0f;

            if (_segmentDuration <= 0f)
            {
                PositionEntity.Position = _segmentEnd;
            }
        }
    }
}
