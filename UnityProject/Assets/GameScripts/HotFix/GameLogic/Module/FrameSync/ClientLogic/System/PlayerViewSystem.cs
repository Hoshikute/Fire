// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家表现系统（表现层，渲染帧驱动）。
    /// 只读逻辑层的 PlayerMoveComponent（确定性状态），把它「渲染」到 Unity Transform：
    ///   - 位置：把定点 pos 转成 Vector3，向其平滑插值（视觉顺滑，不影响逻辑）。
    ///   - 朝向：把定点 faceDir 转成 Vector3，旋转表现根。
    ///
    /// 逻辑与表现分离铁律：本系统对逻辑组件「只读不写」。
    /// 逻辑帧 200ms 一跳，渲染帧 60+ 帧，中间用插值补，所以画面平滑而逻辑确定。
    /// </summary>
    public class PlayerViewSystem : ViewSystemBase
    {
        /// <summary>位置插值速度（每秒插值系数，越大越贴近逻辑值）。</summary>
        private const float PosLerpSpeed = 12f;

        /// <summary>朝向插值速度。</summary>
        private const float RotLerpSpeed = 12f;

        public override Type[] GetFilter()
        {
            return new Type[] { typeof(PlayerMoveComponent), typeof(PlayerViewComponent) };
        }

        public override void Update(int deltaTime)
        {
            float dt = deltaTime / 1000f; // 渲染帧 deltaTime（秒），仅用于表现插值，不进逻辑
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

                if (!view.initialized)
                {
                    // 首帧直接对齐，避免从原点飞过来。
                    view.viewRoot.position = logicPos;
                    view.initialized = true;
                }
                else
                {
                    view.viewRoot.position = Vector3.Lerp(
                        view.viewRoot.position, logicPos, dt * PosLerpSpeed);
                }

                // 朝向：定点 faceDir → Vector3，平滑旋转。
                Vector3 face = move.faceDir.ToVector();
                if (face.sqrMagnitude > 0.0001f)
                {
                    Quaternion target = Quaternion.LookRotation(face, Vector3.up);
                    view.viewRoot.rotation = Quaternion.Slerp(
                        view.viewRoot.rotation, target, dt * RotLerpSpeed);
                }

                view.lastLogicPos = move.pos;
            }
        }
    }
}
