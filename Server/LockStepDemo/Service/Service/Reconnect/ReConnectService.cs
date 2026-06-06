using SuperSocket.SocketBase;

public class ReConnectService : ServiceBase
{
    public override void OnInit()
    {
    }

    public override void OnPlayerLogin(Player player)
    {
        Debug.Log("ReConnectService 已禁用：纯 session demo 不支持掉线身份恢复。");
    }

    public override void OnSessionClose(SyncSession session, CloseReason reason)
    {
        if (session.m_connect != null)
        {
            session.m_connect.Entity.World.eventSystem.DispatchEvent(ServiceEventDefine.c_playerExit, session.m_connect.Entity);
            session.m_connect = null;
        }
    }
}
