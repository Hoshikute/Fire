using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 同步规则
    /// </summary>
    public enum ChangeStatus
    {
        Add,
        Remove,
        Replace
    }

    #region 消息定义

    /// <summary>
    /// 同步开始消息
    /// </summary>
    public class StartSyncMsg
    {
        public int frame;
        public int advanceCount;
        public int intervalTime;
        public int createEntityIndex;
        public SyncRule SyncRule;
    }

    /// <summary>
    /// 追帧消息
    /// </summary>
    public class PursueMsg
    {
        public int id;
        public int recalcFrame;
        public int frame;
        public int advanceCount;
        public int serverTime;
    }

    /// <summary>
    /// 实体同步消息
    /// </summary>
    public class SyncEntityMsg
    {
        public int frame;
        public List<EntityInfo> infos;
        public List<int> destroyList;
    }

    /// <summary>
    /// 销毁实体消息
    /// </summary>
    public class DestroyEntityMsg
    {
        public int frame;
        public int id;
    }

    /// <summary>
    /// 组件变更消息
    /// </summary>
    public class ChangeComponentMsg
    {
        public int frame;
        public int id;
        public ComponentInfo info;
    }

    /// <summary>
    /// 单例组件变更消息
    /// </summary>
    public class ChangeSingletonComponentMsg
    {
        public int frame;
        public ComponentInfo info;
    }

    /// <summary>
    /// 确认消息
    /// </summary>
    public class AffirmMsg
    {
        public int frame;
        public int time;
        public int id;
    }

    /// <summary>
    /// 调试消息
    /// </summary>
    public class DebugMsg
    {
        public int frame;
        public List<EntityInfo> infos;
    }

    #endregion

    #region 数据结构

    /// <summary>
    /// 实体信息
    /// </summary>
    public class EntityInfo
    {
        public int id;
        public List<ComponentInfo> infos;
    }

    /// <summary>
    /// 组件信息
    /// </summary>
    public class ComponentInfo
    {
        public string m_compName;
        public string content;
    }

    /// <summary>
    /// 命令消息
    /// </summary>
    public class CommandMsg
    {
        public int frame;
        public int serverTime;
        public List<CommandInfo> msg;
    }

    /// <summary>
    /// 命令信息
    /// </summary>
    public class CommandInfo
    {
        public int frame;
        public int id;

        public SyncVector3 moveDir = new SyncVector3();
        public SyncVector3 skillDir = new SyncVector3();

        public int element1;
        public int element2;

        public bool isFire = false;

        public void FromCommand(CommandComponent comp)
        {
            moveDir = comp.moveDir.DeepCopy();
            skillDir = comp.skillDir.DeepCopy();
            element1 = comp.element1;
            element2 = comp.element2;
            isFire = comp.isFire;
            frame = comp.frame;
            id = comp.id;
        }

        public CommandComponent ToCommand()
        {
            CommandComponent cmd = new CommandComponent();
            cmd.moveDir = moveDir.DeepCopy();
            cmd.skillDir = skillDir.DeepCopy();
            cmd.element1 = element1;
            cmd.element2 = element2;
            cmd.isFire = isFire;
            cmd.frame = frame;
            cmd.id = id;
            return cmd;
        }
    }

    #endregion

    #region 消息类型

    /// <summary>
    /// 同步消息类型枚举
    /// </summary>
    public enum SyncMessageType
    {
        SyncEntity,
        ChangeSingletonComponent,
        Command,
    }

    /// <summary>
    /// 服务消息信息
    /// </summary>
    public class ServiceMessageInfo
    {
        public int m_frame;
        public SyncMessageType m_type;
        public object m_msg;
    }

    #endregion
}
