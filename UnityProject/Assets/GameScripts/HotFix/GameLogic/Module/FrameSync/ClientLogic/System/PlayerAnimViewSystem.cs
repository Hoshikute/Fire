// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

using System;
using Animancer;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家动画表现系统（表现层，渲染帧驱动）。
    /// 读取逻辑层的 PlayerStateComponent（只读），驱动 Animancer 播放对应动画。
    ///
    /// 逻辑与表现分离铁律：
    ///   - 本系统对所有逻辑组件「只读不写」。
    ///   - 动画过渡、混合树参数、idle 轮播等表现细节全在此处，不进逻辑层。
    ///   - 旋转插值（朝向）由 PlayerViewSystem 负责，本系统只管动画剪辑。
    ///
    /// 动画剪辑通过 PlayerAnimConfig（ScriptableObject）注入，不硬编码路径。
    /// PlayerViewComponent 持有 Animancer 实例引用（animancer 字段）。
    /// </summary>
    public class PlayerAnimViewSystem : ViewSystemBase
    {
        public override Type[] GetFilter()
        {
            return new Type[]
            {
                typeof(PlayerStateComponent),
                typeof(PlayerViewComponent),
            };
        }

        public override void Update(int deltaTime)
        {
            var entities = GetEntityList();
            for (int i = 0; i < entities.Count; i++)
            {
                PlayerStateComponent st   = entities[i].GetComp<PlayerStateComponent>();
                PlayerViewComponent  view = entities[i].GetComp<PlayerViewComponent>();

                if (view.animancer == null || view.animConfig == null)
                    continue;

                // 状态未发生切换时不重复触发 Play（Animancer 内部幂等，但避免无谓开销）
                if (st.state == st.prevState)
                    continue;

                PlayAnim(st, view);
            }
        }

        // ── 动画分发 ────────────────────────────────────────────────────

        private void PlayAnim(PlayerStateComponent st, PlayerViewComponent view)
        {
            AnimancerComponent animancer = view.animancer;
            PlayerAnimConfig   cfg       = view.animConfig;

            switch (st.state)
            {
                // ── 地面常态 ────────────────────────────────────────────
                case PlayerLogicState.Idle:
                    PlayIfNotNull(animancer, cfg.idle);
                    break;

                case PlayerLogicState.MoveStart:
                    PlayIfNotNull(animancer, cfg.moveStart);
                    break;

                case PlayerLogicState.MoveLoop:
                    PlayIfNotNull(animancer, cfg.moveLoop);
                    break;

                case PlayerLogicState.MoveEnd:
                    PlayIfNotNull(animancer, cfg.moveEnd);
                    break;

                // ── 锁定模式 ────────────────────────────────────────────
                case PlayerLogicState.LockIdle:
                    PlayIfNotNull(animancer, cfg.lockIdle ?? cfg.idle);
                    break;

                // ── 空中 ────────────────────────────────────────────────
                case PlayerLogicState.Jump:
                    PlayIfNotNull(animancer, cfg.jumpForward);
                    break;

                case PlayerLogicState.JumpInPlace:
                    PlayIfNotNull(animancer, cfg.jumpInPlace);
                    break;

                case PlayerLogicState.Fall:
                    // 先播 fallStart，OnEnd 时切到 fallLoop（通过 Animancer Events）
                    if (cfg.fallStart != null)
                    {
                        animancer.Play(cfg.fallStart).Events(view.viewRoot).OnEnd = () =>
                        {
                            if (cfg.fallLoop != null)
                                animancer.Play(cfg.fallLoop);
                        };
                    }
                    else
                    {
                        PlayIfNotNull(animancer, cfg.fallLoop);
                    }
                    break;

                case PlayerLogicState.Land:
                    PlayIfNotNull(animancer, cfg.land);
                    break;

                // ── 交互 / 攀爬 ─────────────────────────────────────────
                case PlayerLogicState.MoveToWall:
                    PlayIfNotNull(animancer, cfg.moveToWall ?? cfg.idle);
                    break;

                case PlayerLogicState.Vault:
                    PlayIfNotNull(animancer, cfg.vault);
                    break;

                case PlayerLogicState.Climb:
                    PlayIfNotNull(animancer, cfg.climb);
                    break;

                case PlayerLogicState.LedgeClimb:
                    PlayIfNotNull(animancer, cfg.ledgeClimb);
                    break;

                case PlayerLogicState.PlatformerUp:
                    PlayIfNotNull(animancer, cfg.platformerUp);
                    break;

                default:
                    Log.Warning($"[PlayerAnimViewSystem] 未处理的状态：{st.state}");
                    break;
            }
        }

        private static void PlayIfNotNull(AnimancerComponent animancer, ClipTransition clip)
        {
            if (clip != null)
                animancer.Play(clip);
        }
    }
}
