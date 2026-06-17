using System;
using SuperSocket.SocketBase;

public class LoginService : ServiceBase
{
    public override void OnInit()
    {
        EventService.AddTypeEvent<PlayerLoginMsg_s>(RecevicePlayerLogin);
        EventService.AddTypeEvent<PlayerRename_s>(RecevicePlayerRename);
    }

    public override void OnSessionClose(SyncSession session, CloseReason reason)
    {
        if (session.player == null)
        {
            return;
        }

        m_service.OnPlayerLogout(session.player);
    }

    public void RecevicePlayerLogin(SyncSession session, PlayerLoginMsg_s e)
    {
        Debug.Log("RecevicePlayerLogin");

        if (session.player != null)
        {
            Debug.Log("" + session.player.playerID + " 已经登录，不需要重复登录！ ");
            SendLoginSuccess(session, session.player);
            JoinSingletonBattleWorld(session.player);
            return;
        }

        session.player = CreateTemporaryPlayer(session);
        session.player.session = session;

        SendLoginSuccess(session, session.player);
        m_service.OnPlayerLogin(session.player);
        JoinSingletonBattleWorld(session.player);
    }

    public void RecevicePlayerRename(SyncSession session, PlayerRename_s e)
    {
        if (session.player == null)
        {
            Debug.LogError("玩家未登录");
            return;
        }

        session.player.nickName = e.newName;

        PlayerRename_c msg = new PlayerRename_c();
        msg.code = 0;
        msg.newName = e.newName;

        ProtocolAnalysisService.SendMsg(session, msg);
    }

    Player CreateTemporaryPlayer(SyncSession session)
    {
        string playerID = $"guest_{session.SessionID}_{DateTime.UtcNow.Ticks}";

        Player player = new Player();
        player.playerID = playerID;
        player.characterID = "1";
        player.OwnCharacter = "1";
        player.nickName = playerID;

        return player;
    }

    void SendLoginSuccess(SyncSession session, Player player)
    {
        PlayerLoginMsg_c msg = new PlayerLoginMsg_c();
        msg.code0 = 0;
        msg.content = "anonymous-session-ready";
        msg.playerID = player.playerID;
        msg.nickName = player.nickName;
        msg.characterID = player.characterID;
        ProtocolAnalysisService.SendMsg(session, msg);
    }

    void JoinSingletonBattleWorld(Player player)
    {
        if (player == null || player.session == null)
        {
            return;
        }

        if (player.session.m_connect != null)
        {
            Debug.Log("玩家已经在 BattleWorld 中: " + player.playerID);
            return;
        }

        WorldBase world = WorldManager.GetOrCreateSingletonWorld<DemoWorld>();
        world.IsStart = true;
        world.SyncRule = SyncRule.Frame;

        ConnectionComponent conn = new ConnectionComponent();
        conn.m_session = player.session;
        conn.playerID = player.playerID;

        SyncComponent sync = new SyncComponent();
        string entityKey = "Player" + player.playerID;
        world.CreateEntityImmediately(entityKey, conn, sync);
        EntityBase entity = world.GetEntity(entityKey.ToHash());
        player.session.m_connect = conn;

        world.eventSystem.DispatchEvent(ServiceEventDefine.c_playerJoin, entity);
        Debug.Log("玩家直接进入 BattleWorld: " + player.playerID + " entity " + entity.ID);
    }
}
