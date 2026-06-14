using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 玩家命令记录组件
    /// </summary>
    public class PlayerCommandRecordComponent : ComponentBase
    {
        public PlayerCommandBase m_defaultInput;
        public List<PlayerCommandBase> m_inputCache = new List<PlayerCommandBase>();
        public bool m_isConflict = false;
        public int lastInputFrame = -1;

        public void EnsureDefaultCommand(int entityId)
        {
            if (m_defaultInput != null)
            {
                return;
            }

            m_defaultInput = new CommandComponent
            {
                id = entityId,
                frame = 0,
                time = 0,
                moveDir = SyncVector3.Zero,
                skillDir = SyncVector3.Zero,
                speedGear = 1,
            };
        }

        public PlayerCommandBase GetOrForecastInput(int frame)
        {
            PlayerCommandBase command = GetInputCache(frame);
            if (command != null)
            {
                return command;
            }

            return GetForecastInput(frame);
        }

        public PlayerCommandBase RecordForecastIfMissing(int frame)
        {
            PlayerCommandBase command = GetInputCache(frame);
            if (command != null)
            {
                return command;
            }

            command = GetForecastInput(frame);
            if (command != null)
            {
                RecordCommand(command);
            }
            return command;
        }

        public PlayerCommandBase GetInputCache(int frame)
        {
            for (int i = 0; i < m_inputCache.Count; i++)
            {
                if (m_inputCache[i].frame == frame)
                {
                    return m_inputCache[i].DeepCopy();
                }
            }
            return null;
        }

        public PlayerCommandBase GetForecastInput(int frame)
        {
            PlayerCommandBase record = GetInputCache(frame - 1);
            if (record == null)
            {
                record = m_defaultInput;
            }
            if (record == null)
            {
                return null;
            }

            PlayerCommandBase cmd = record.DeepCopy();
            cmd.frame = frame;
            cmd.id = Entity.ID;
            if (cmd is CommandComponent playerCommand)
            {
                playerCommand.ClearOneShotInputs();
            }
            return cmd;
        }

        public void RecordCommand(PlayerCommandBase cmd)
        {
            PlayerCommandBase copy = cmd.DeepCopy();
            for (int i = 0; i < m_inputCache.Count; i++)
            {
                if (m_inputCache[i].frame == copy.frame)
                {
                    m_inputCache[i] = copy;
                    if (copy.frame > lastInputFrame)
                    {
                        lastInputFrame = copy.frame;
                    }
                    return;
                }
            }
            m_inputCache.Add(copy);
            if (copy.frame > lastInputFrame)
            {
                lastInputFrame = copy.frame;
            }
        }

        public void ClearCache(int frame)
        {
            for (int i = 0; i < m_inputCache.Count; i++)
            {
                if (m_inputCache[i].frame < frame)
                {
                    m_inputCache.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
