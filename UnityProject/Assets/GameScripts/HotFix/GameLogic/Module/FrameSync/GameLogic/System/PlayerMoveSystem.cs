// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

using System;

namespace GameLogic
{
    /// <summary>
    /// 玩家移动系统（确定性逻辑层）。
    /// 在固定逻辑帧（200ms）里运行：读取单例 PlayerInputComponent 的输入意图，
    /// 用定点数推进每个带 PlayerMoveComponent 实体的位置 / 朝向 / 竖直速度。
    ///
    /// 确定性铁律：
    ///   - 全程 int / SyncVector3 整数运算，禁止 float、Time.deltaTime、Random。
    ///   - 步长用传入的 deltaTime（毫秒，恒为 FrameSyncModule.IntervalTime=200）。
    ///   - 跑在 FixedUpdate，回滚重算时会被重复执行，所以不得有任何表现层副作用。
    /// </summary>
    public class PlayerMoveSystem : SystemBase
    {
        /// <summary>重力加速度（定点，毫单位 / 秒²）。约 -9.8 m/s²。</summary>
        private const int Gravity = -9800;

        /// <summary>跳跃初速度（定点，毫单位 / 秒）。</summary>
        private const int JumpSpeed = 5000;

        public override Type[] GetFilter()
        {
            return new Type[] { typeof(PlayerMoveComponent) };
        }

        public override void FixedUpdate(int deltaTime)
        {
            // 单例输入：所有本地玩家共用一份输入意图（多人时应改为按玩家 ID 取指令）。
            PlayerInputComponent input = m_world.GetSingletonComp<PlayerInputComponent>();

            var entities = GetEntityList();
            for (int i = 0; i < entities.Count; i++)
            {
                PlayerMoveComponent move = entities[i].GetComp<PlayerMoveComponent>();
                Step(move, input, deltaTime);
            }

            // 边沿输入（跳跃）在本逻辑帧消费完后清空，避免跨帧重复触发。
            input.ConsumeOneShot();
        }

        /// <summary>
        /// 单个实体的一帧确定性推进。位移 = 速度 × 时间，定点整数：
        /// delta(毫单位) = speed(毫单位/秒) × deltaTimeMs / 1000。
        /// </summary>
        private void Step(PlayerMoveComponent move, PlayerInputComponent input, int deltaTimeMs)
        {
            // —— 水平移动 ——
            SyncVector3 dir = input.moveDir;
            if (dir.SqrMagnitude() > 0)
            {
                // 朝向跟随移动方向（定点单位向量）。
                move.faceDir = dir.Normalized();

                // 位移：speed × dt / 1000，再乘方向单位向量（方向已是 ONE 定点，用 MulFixed）。
                long stepLen = (long)move.moveSpeed * deltaTimeMs / 1000; // 毫单位
                SyncVector3 unit = move.faceDir; // 长度≈ONE
                SyncVector3 horizontal = SyncVector3.FromRaw(
                    (int)((long)unit.x * stepLen / SyncVector3.ONE),
                    0,
                    (int)((long)unit.z * stepLen / SyncVector3.ONE));
                move.pos = move.pos + horizontal;
            }

            // —— 跳跃 ——
            if (input.jump && move.isOnGround)
            {
                move.verticalSpeed = JumpSpeed;
                move.isOnGround = false;
            }

            // —— 重力 / 竖直积分 ——
            if (!move.isOnGround)
            {
                // v += g × dt / 1000
                move.verticalSpeed += (int)((long)Gravity * deltaTimeMs / 1000);

                // y += v × dt / 1000
                int dy = (int)((long)move.verticalSpeed * deltaTimeMs / 1000);
                move.pos = move.pos + SyncVector3.FromRaw(0, dy, 0);

                // 落地检测：y <= 0 视为接地（地面高度 0，简化处理）。
                if (move.pos.y <= 0)
                {
                    move.pos = SyncVector3.FromRaw(move.pos.x, 0, move.pos.z);
                    move.verticalSpeed = 0;
                    move.isOnGround = true;
                }
            }
        }
    }
}
