using System;
using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 记录系统泛型实现
    /// </summary>
    public class RecordSystem<T> : RecordSystemBase where T : MomentComponentBase, new()
    {
        public override Type[] GetFilter()
        {
            return new Type[] { typeof(T) };
        }

        public override void Record(int frame)
        {
            RecordComponent<T> rc = m_world.GetSingletonComp<RecordComponent<T>>();

            List<EntityBase> list = GetEntityList();
            for (int i = 0; i < list.Count; i++)
            {
                T record = (T)list[i].GetComp<T>().DeepCopy();
                record.Frame = frame;
                record.ID = list[i].ID;

                rc.m_record.Add(record);
            }
        }

        public override void RevertToFrame(int frame)
        {
            RecordComponent<T> rc = m_world.GetSingletonComp<RecordComponent<T>>();
            List<T> list = rc.GetRecordList(frame);

            for (int i = 0; i < list.Count; i++)
            {
                if (m_world.GetEntityIsExist(list[i].ID))
                {
                    EntityBase entity = m_world.GetEntity(list[i].ID);
                    entity.ChangeComp((T)list[i].DeepCopy());
                }
                else if (m_world.GetIsExistCreateRollbackCache(list[i].ID))
                {
                    EntityBase entity = m_world.GetCreateRollbackCache(list[i].ID);
                    entity.ChangeComp((T)list[i].DeepCopy());
                }
                else if (m_world.GetIsExistDestroyRollbackCache(list[i].ID))
                {
                    EntityBase entity = m_world.GetDestroyRollbackCache(list[i].ID);
                    entity.ChangeComp((T)list[i].DeepCopy());
                }
            }
        }

        public override void ClearAfter(int frame)
        {
            RecordComponent<T> rc = m_world.GetSingletonComp<RecordComponent<T>>();
            rc.ClearAfter(frame);
        }

        public override void ClearBefore(int frame)
        {
            RecordComponent<T> rc = m_world.GetSingletonComp<RecordComponent<T>>();
            rc.ClearBefore(frame);
        }

        public override MomentComponentBase GetRecord(int id, int frame)
        {
            RecordComponent<T> rc = m_world.GetSingletonComp<RecordComponent<T>>();
            for (int i = 0; i < rc.m_record.Count; i++)
            {
                if (rc.m_record[i].ID == id && rc.m_record[i].Frame == frame)
                {
                    return rc.m_record[i];
                }
            }
            return null;
        }

        public override void PrintRecord(int id)
        {
            RecordComponent<T> rc = m_world.GetSingletonComp<RecordComponent<T>>();

            string content = "compName : " + typeof(T).Name + "\n";
            for (int i = 0; i < rc.m_record.Count; i++)
            {
                if (id == -1 || rc.m_record[i].ID == id)
                {
                    content += $" ID:{rc.m_record[i].ID} Frame:{rc.m_record[i].Frame}\n";
                }
            }
            Log.Warning("PrintRecord:" + content);
        }

        public override void Record(int frame, EntityBase entity)
        {
            throw new NotImplementedException();
        }
    }
}
