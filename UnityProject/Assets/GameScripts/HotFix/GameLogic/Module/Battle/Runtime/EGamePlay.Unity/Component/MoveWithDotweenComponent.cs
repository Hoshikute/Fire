using System;
using UnityEngine;

namespace EGamePlay.Combat
{
    public enum MoveType
    {
        TargetMove,
        PathMove,
    }

    public enum SpeedType
    {
        Speed,
        Duration,
    }

    public class MoveWithDotweenComponent : Component
    {
        public SpeedType SpeedType { get; set; }
        public float Speed { get; set; }
        public float Duration { get; set; }
        public float ElapsedTime { get; set; }
        public IPosition PositionEntity { get; set; }
        public IPosition TargetPositionEntity { get; set; }
        public Entity TargetEntity { get; set; }
        public Vector3 Destination { get; set; }

        private Vector3 _moveStart;
        private Vector3 _moveEnd;
        private float _moveDuration;
        private float _moveElapsed;
        private bool _singleMoveActive;
        private Action MoveFinishAction { get; set; }

        public override void Awake()
        {
            PositionEntity = (IPosition)Entity;
            ElapsedTime = 0f;
        }

        public override void Update()
        {
            if (_singleMoveActive)
            {
                _moveElapsed += Time.deltaTime;
                float progress = _moveDuration <= 0f ? 1f : Mathf.Clamp01(_moveElapsed / _moveDuration);
                PositionEntity.Position = Vector3.Lerp(_moveStart, _moveEnd, progress);
                if (progress >= 1f)
                {
                    _singleMoveActive = false;
                    OnMoveFinish();
                }
            }

            if (TargetPositionEntity == null)
            {
                return;
            }

            if (TargetEntity != null && TargetEntity.IsDisposed)
            {
                TargetEntity = null;
                TargetPositionEntity = null;
                Entity.Destroy(Entity);
                return;
            }

            if (SpeedType == SpeedType.Speed)
            {
                float step = Math.Max(Speed, 0f) * Time.deltaTime;
                PositionEntity.Position = Vector3.MoveTowards(PositionEntity.Position, TargetPositionEntity.Position, step);
            }

            if (SpeedType == SpeedType.Duration)
            {
                DoTimeMove(MathF.Max(0f, Duration - ElapsedTime));
            }

            ElapsedTime += Time.deltaTime;
        }

        public MoveWithDotweenComponent DoMoveTo(Vector3 destination, float duration)
        {
            Destination = destination;
            _moveStart = PositionEntity.Position;
            _moveEnd = destination;
            _moveDuration = Math.Max(duration, 0.0001f);
            _moveElapsed = 0f;
            _singleMoveActive = true;
            return this;
        }

        public void DoMoveToWithSpeed(IPosition targetPositionEntity, float speed = 1f)
        {
            Speed = speed;
            SpeedType = SpeedType.Speed;
            TargetPositionEntity = targetPositionEntity;
            TargetEntity = targetPositionEntity as Entity;
        }

        public void DoMoveToWithTime(IPosition targetPositionEntity, float time = 1f)
        {
            Duration = time;
            SpeedType = SpeedType.Duration;
            TargetPositionEntity = targetPositionEntity;
            TargetEntity = targetPositionEntity as Entity;
            DoTimeMove(time);
        }

        private void DoTimeMove(float time)
        {
            if (TargetPositionEntity == null)
            {
                return;
            }

            _moveStart = PositionEntity.Position;
            _moveEnd = TargetPositionEntity.Position;
            _moveDuration = Math.Max(time, 0.0001f);
            _moveElapsed = 0f;
            _singleMoveActive = true;
        }

        public void OnMoveFinish(Action action)
        {
            MoveFinishAction = action;
        }

        private void OnMoveFinish()
        {
            MoveFinishAction?.Invoke();
        }
    }
}
