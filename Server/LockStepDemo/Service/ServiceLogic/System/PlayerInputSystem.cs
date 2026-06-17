using Protocol;
using System;
using System.Collections.Generic;


public class PlayerInputSystem : ServiceSystem /*where T : PlayerCommandBase, new()*/
{
    public override Type[] GetFilter()
    {
        return new Type[] {
                typeof(CommandComponent),
                typeof(ConnectionComponent),
            };
    }

    public override void NoRecalcBeforeFixedUpdate(int deltaTime)
    {
        List<EntityBase> list = GetEntityList();

        for (int i = 0; i < list.Count; i++)
        {
            ConnectionComponent comp = list[i].GetComp<ConnectionComponent>();
            CommandComponent cmd = (CommandComponent)comp.GetCommand(m_world.FrameCount);
            cmd.id = list[i].ID;
            cmd.frame = m_world.FrameCount;
            cmd.time = ServiceTime.GetServiceTime();

            list[i].ChangeComp(cmd);

            // 服务端每帧只广播自己最终选择的权威命令：真实输入或缺帧预测。
            for (int j = 0; j < list.Count; j++)
            {
                ConnectionComponent conn = list[j].GetComp<ConnectionComponent>();
                lock (conn.unConfirmFrame)
                {
                    if (!conn.unConfirmFrame.Contains(cmd.frame))
                    {
                        conn.unConfirmFrame.Add(cmd.frame);
                    }
                }

                ProtocolAnalysisService.SendMsg(conn.m_session, cmd);
            }
        }
    }
}
