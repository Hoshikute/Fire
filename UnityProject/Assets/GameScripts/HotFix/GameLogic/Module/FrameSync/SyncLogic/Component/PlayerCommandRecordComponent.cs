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

        public PlayerCommandBase GetInputCache(int frame)
        {
            for (int i = 0; i < m_inputCache.Count; i++)
            {
                if (m_inputCache[i].frame == frame)
                {
                    return m_inputCache[i];
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

            PlayerCommandBase cmd = record.DeepCopy();
            cmd.frame = frame;
            cmd.id = Entity.ID;
            return cmd;
        }

        public void RecordCommand(PlayerCommandBase cmd)
        {
            for (int i = 0; i < m_inputCache.Count; i++)
            {
                if (m_inputCache[i].frame == cmd.frame)
                {
                    m_inputCache[i] = cmd;
                    return;
                }
            }
            m_inputCache.Add(cmd);
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
