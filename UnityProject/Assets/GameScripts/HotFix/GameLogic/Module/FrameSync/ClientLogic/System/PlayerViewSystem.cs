
using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家表现系统（表现层，渲染帧驱动）。
    /// 只读逻辑层的 PlayerMoveComponent（确定性状态），把它「渲染」到 Unity Transform：
    ///   - 位置：在两个连续逻辑快照间线性插值（prevLogicPos → lastLogicPos），
    ///           超出插值窗口后用速度方向外推（dead reckoning），视觉顺滑。
    ///   - 朝向：把定点 faceDir 转成 Vector3，平滑旋转。
    ///
    /// 逻辑与表现分离铁律：本系统对逻辑组件「只读不写」。
    /// 逻辑帧 200ms 一跳，渲染帧 60+ 帧，中间用插值补，所以画面平滑而逻辑确定。
    /// </summary>
    public class PlayerViewSystem : ViewSystemBase
    {
        /// <summary>朝向插值速度（每秒系数）。</summary>
        private const float RotLerpSpeed = 12f;

        /// <summary>逻辑帧时长（秒），对应 FrameSyncModule.IntervalTime = 200ms。</summary>
        private const float LogicFrameDuration = 0.2f;

        /// <summary>
        /// 位置跳变保护阈值（米）。逻辑位置跳变超过此值时直接 snap，
        /// 不插值追逐（避免回滚/传送后"飞过去"）。
        /// </summary>
        private const float PosSnapThreshold = 3.0f;

        /// <summary>
        /// 外推系数（0~1）。超过插值窗口时，用速度方向做多大比例的外推。
        /// 0.3 = 30% 外推，避免画面明显比逻辑"超前"太多导致视觉抖动。
        /// </summary>
        private const float ExtrapolationFactor = 0.3f;

        public override Type[] GetFilter()
        {
            return new Type[] { typeof(PlayerMoveComponent), typeof(PlayerViewComponent) };
        }

        public override void Update(int deltaTime)
        {
            float dt = deltaTime / 1000f; // 渲染帧 deltaTime（秒），仅用于表现插值
            var entities = GetEntityList();
            for (int i = 0; i < entities.Count; i++)
            {
                PlayerMoveComponent move = entities[i].GetComp<PlayerMoveComponent>();
                PlayerViewComponent view = entities[i].GetComp<PlayerViewComponent>();
                if (view.viewRoot == null)
                {
                    continue;
                }

                Vector3 logicPos = move.pos.ToVector();

                // ── 首帧对齐 ──────────────────────────────────────────
                if (!view.initialized)
                {
                    view.viewRoot.position  = logicPos;
                    view.lastLogicPos       = move.pos;
                    view.prevLogicPos       = move.pos;
                    view.interpT            = 1.1f;
                    view.initialized        = true;
                    ApplyRotation(view, move, dt);
                    continue;
                }

                // ── 检测逻辑帧更新 ────────────────────────────────────
                // SyncVector3 值比较（struct equality 逐字段比）
                if (!move.pos.Equals(view.lastLogicPos))
                {
                    // 逻辑帧推进：旧值 → prev，新值 → last，重置插值计时
                    view.prevLogicPos = view.lastLogicPos;
                    view.lastLogicPos = move.pos;
                    view.interpT      = 0f;
                }

                // ── 跳变保护 ──────────────────────────────────────────
                float distanceToCurrent = Vector3.Distance(
                    view.viewRoot.position, logicPos);

                if (distanceToCurrent > PosSnapThreshold)
                {
                    // 大跳变（回滚 / 传送）：直接 snap，不插值追逐
                    view.viewRoot.position = logicPos;
                    view.prevLogicPos      = move.pos;
                    view.lastLogicPos      = move.pos;
                    view.interpT           = 1.1f;
                    ApplyRotation(view, move, dt);
                    continue;
                }

                // ── 插值推进 ──────────────────────────────────────────
                view.interpT += dt / LogicFrameDuration;

                Vector3 fromPos = view.prevLogicPos.ToVector();
                Vector3 toPos   = logicPos;

                // ── 外推（Dead Reckoning）─────────────────────────────
                // 超出插值窗口时，用 faceDir × currentSpeed 向前推一小段
                if (view.interpT > 1.0f && move.currentSpeed > 0)
                {
                    float extra = view.interpT - 1.0f;
                    Vector3 faceDir = move.faceDir.ToVector();
                    float speedMps  = move.currentSpeed / 1000f; // 毫单位/秒 → 米/秒
                    Vector3 extrapDelta = faceDir * speedMps * extra * LogicFrameDuration;
                    toPos += extrapDelta * ExtrapolationFactor;
                }

                float t = Mathf.Clamp01(view.interpT);
                view.viewRoot.position = Vector3.Lerp(fromPos, toPos, t);

                // ── 朝向 ──────────────────────────────────────────────
                ApplyRotation(view, move, dt);
            }
        }

        /// <summary>平滑旋转朝向。</summary>
        private static void ApplyRotation(PlayerViewComponent view, PlayerMoveComponent move, float dt)
        {
            Vector3 face = move.faceDir.ToVector();
            if (face.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(face, Vector3.up);
                view.viewRoot.rotation = Quaternion.Slerp(
                    view.viewRoot.rotation, target, dt * RotLerpSpeed);
            }
        }
    }
}
