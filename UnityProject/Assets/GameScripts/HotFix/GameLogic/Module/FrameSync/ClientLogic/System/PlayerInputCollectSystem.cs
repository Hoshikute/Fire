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
    /// 完整对齐原 ThirdPersonController 的输入语义：
    ///   - moveDir：经相机朝向修正后归一化的水平定点方向（原 GetTargetDir 逻辑）
    ///   - jump：跳跃边沿触发
    ///   - toggleLock：锁定模式切换边沿触发
    ///   - platformJump：平台跳边沿触发（由交互逻辑写入）
    ///   - speedGear：Shift 键写入 PlayerInputComponent.speedGear（1 走 / 2 跑）
    ///
    /// 为什么放表现层：采集 Unity 输入本身依赖真实帧率和 UnityEngine.Input（非确定性来源），
    /// 必须隔离在表现层。逻辑层只读取「已定点化的输入意图」，保持确定性。
    /// </summary>
    public class PlayerInputCollectSystem : ViewSystemBase
    {
        public override void Update(int deltaTime)
        {
            PlayerInputComponent input = m_world.GetSingletonComp<PlayerInputComponent>();

            // ── 1. 移动方向（含相机朝向修正）─────────────────────────────
            Vector2 move = GameModule.Input.Move;

            // 取相机的水平朝向（去掉 Y 分量），用于把摇杆方向转成世界空间
            // GameModule.Camera.MainCamera 若为 null，回退到 Camera.main
            Camera cam = GameModule.Camera.MainCamera ?? Camera.main;
            if (cam != null && move.sqrMagnitude > 0.0001f)
            {
                input.moveDir = PlayerMathUtil.InputToSyncDir(move, cam.transform.forward, cam.transform.right);
            }
            else
            {
                // 无相机或无输入时：直接把摇杆映射到 XZ 平面
                SyncVector3 rawDir = SyncVector3.FromVector3(new Vector3(move.x, 0f, move.y));
                input.moveDir = rawDir.Normalized();
            }

            // ── 2. 跳跃（边沿触发）────────────────────────────────────────
            // 用 |= 防止两个逻辑帧之间多个渲染帧里的按下被漏掉
            if (GameModule.Input.GetButtonDown(InputButtonType.Jump))
            {
                input.jump = true;
            }

            // ── 3. 锁定模式切换（边沿触发）────────────────────────────────
            if (GameModule.Input.GetButtonDown(InputButtonType.Lock))
            {
                input.toggleLock = true;
            }

            // ── 4. 速度档位（Shift = 跑，持续性输入）
            // speedGear 写在 PlayerInputComponent（SingletonComponent，不参与回滚快照），
            // 避免渲染帧直写 MomentComponentBase 污染回滚数据。
            input.speedGear = GameModule.Input.GetButton(InputButtonType.Shift) ? 2 : 1;
        }
    }
}
