using System;
using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 逻辑帧输入指令桥。
    /// 当前本地路径仍由 PlayerInputCollectSystem 采集 Unity 输入，但进入逻辑帧前先固化为
    /// 本地玩家实体的 CommandComponent；非本地实体只消费已缓存/预测的帧指令。
    /// </summary>
    public class PlayerInputCommandSystem : SystemBase
    {
        public override Type[] GetFilter()
        {
            return new Type[]
            {
                typeof(PlayerComponent),
                typeof(PlayerCommandRecordComponent),
            };
        }

        public override void NoRecalcBeforeFixedUpdate(int deltaTime)
        {
            PlayerInputComponent input = m_world.GetSingletonComp<PlayerInputComponent>();
            List<EntityBase> entities = GetEntityList();
            for (int i = 0; i < entities.Count; i++)
            {
                EntityBase entity = entities[i];
                PlayerComponent player = entity.GetComp<PlayerComponent>();
                PlayerCommandRecordComponent record = entity.GetComp<PlayerCommandRecordComponent>();
                record.EnsureDefaultCommand(entity.ID);

                // WorldBase.FixedLoop 先记录 FrameCount 快照，再递增为本次 FixedUpdate 的执行帧。
                int executionFrame = m_world.FrameCount;
                if (player.isLocal)
                {
                    if (record.GetInputCache(executionFrame) == null)
                    {
                        CommandComponent command = input.ToCommand(executionFrame, entity.ID, 0);
                        record.RecordCommand(command);
                    }
                }
                else
                {
                    record.RecordForecastIfMissing(executionFrame);
                }
            }

            // 本地输入已经进入本帧 CommandComponent，边沿输入留在命令记录中供 Move/State 读取。
            // 清空单例，避免没有渲染帧刷新时同一次按键被下一逻辑帧重复记录。
            input.ConsumeOneShot();
        }

        public override void OnlyCallByRecalc(int frame, int deltaTime)
        {
            List<EntityBase> entities = GetEntityList();
            for (int i = 0; i < entities.Count; i++)
            {
                EntityBase entity = entities[i];
                PlayerCommandRecordComponent record = entity.GetComp<PlayerCommandRecordComponent>();
                record.EnsureDefaultCommand(entity.ID);

                // WorldBase.Recalc 在调用 OnlyCallByRecalc 前已递增 FrameCount。
                int executionFrame = m_world.FrameCount;
                record.RecordForecastIfMissing(executionFrame);
            }
        }
    }
}
