using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 记录组件，用于存储历史帧数据
    /// </summary>
    public class RecordComponent<T> : SingletonComponent where T : MomentComponentBase, new()
    {
        public List<T> m_record = new List<T>();

        public void ClearBefore(int frame)
        {
            for (int i = 0; i < m_record.Count; i++)
            {
                if (m_record[i].Frame < frame)
                {
                    m_record.RemoveAt(i);
                    i--;
                }
            }
        }

        public void ClearAfter(int frame)
        {
            for (int i = 0; i < m_record.Count; i++)
            {
                if (m_record[i].Frame > frame)
                {
                    m_record.RemoveAt(i);
                    i--;
                }
            }
        }

        private List<T> m_list = new List<T>();

        public List<T> GetRecordList(int frame)
        {
            m_list.Clear();

            for (int i = 0; i < m_record.Count; i++)
            {
                if (m_record[i].Frame == frame)
                {
                    m_list.Add(m_record[i]);
                }
            }

            return m_list;
        }
    }
}
