// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家输入采集系统（表现层，渲染帧驱动）。
    /// 每个渲染帧把 Unity 的输入（Vector2 / 按键）转成定点数写进单例 PlayerInputComponent，
    /// 供逻辑层在固定逻辑帧消费。
    ///
    /// 为什么放表现层：采集 Unity 输入本身依赖真实帧率和 UnityEngine.Input（非确定性来源），
    /// 必须隔离在表现层。逻辑层只读取「已定点化的输入意图」，保持确定性。
    /// 这是「表现 → 逻辑」唯一合法的写入：写的是玩家指令，不是世界状态。
    /// </summary>
    public class PlayerInputCollectSystem : ViewSystemBase
    {
        public override void Update(int deltaTime)
        {
            PlayerInputComponent input = m_world.GetSingletonComp<PlayerInputComponent>();

            // 1) 移动方向：Unity Vector2 → 世界平面 (x, 0, z) → 定点 → 归一化。
            Vector2 move = GameModule.Input.Move;
            SyncVector3 dir = SyncVector3.FromVector3(new Vector3(move.x, 0f, move.y));
            input.moveDir = dir.Normalized();

            // 2) 跳跃：边沿触发，置位后由逻辑帧消费清空。
            //    用 |= 防止两个逻辑帧之间的多个渲染帧里按下被漏掉。
            if (GameModule.Input.GetButtonDown(InputButtonType.Jump))
            {
                input.jump = true;
            }
        }
    }
}
