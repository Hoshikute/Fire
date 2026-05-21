using CDatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase;

public class LoginService : ServiceBase
{
    const string c_playerTableName = "PlayerTable";

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

        //保存玩家数据
        SavePlayerData(session.player);

        //玩家退出登陆
        m_service.OnPlayerLogout(session.player);
    }

    public void RecevicePlayerLogin(SyncSession session, PlayerLoginMsg_s e)
    {
        Debug.Log("RecevicePlayerLogin");

        if(session.player != null)
        {
            Debug.Log(""+ session.player.playerID +" 已经登录，不需要重复登录！ ");
        }

        if (DataBaseService.IsAvailable)
        {
            try
            {
                LoadPlayerFromDatabase(session, e.playerID);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
                Debug.LogWarning("读取数据库玩家失败，改用临时玩家数据。");
                session.player = GetNewPlayer(e.playerID);
            }
        }
        else
        {
            session.player = GetNewPlayer(e.playerID);
        }

        session.player.playerID = e.playerID;
        session.player.session = session;

        PlayerLoginMsg_c msg = new PlayerLoginMsg_c();
        ProtocolAnalysisService.SendMsg(session,msg);

        //派发玩家登陆事件
        m_service.OnPlayerLogin(session.player);
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

    void LoadPlayerFromDatabase(SyncSession session, string playerID)
    {
        string clauseContent = "ID ='" + playerID + "'";
        var result = DataBaseService.database.Query(c_playerTableName, null, clauseContent, null, null, null, null);

        if (result.MoveToNext())
        {
            Debug.Log("查询到记录！ ");
            session.player = GetOldPlayer(result);
            result.Close();
        }
        else
        {
            result.Close();
            Debug.Log("未查询到记录！");

            session.player = GetNewPlayer(playerID);

            Dictionary<string, string> value = new Dictionary<string, string>();
            value.Add("ID", playerID);
            DataBaseService.database.Insert(c_playerTableName, null, value);
        }
    }

    Player GetOldPlayer(ICursor data)
    {
        Player player = new Player();

        player.characterID = data.GetString("CharacterID");
        player.OwnCharacter = data.GetString("OwnCharacter");
        player.nickName = data.GetString("NickName");

        return player;
    }

    Player GetNewPlayer(string playerID)
    {
        Player player = new Player();

        player.playerID = playerID;
        player.characterID = "1";
        player.OwnCharacter = "1";
        player.nickName = playerID;

        return player;
    }

    void SavePlayerData(Player player)
    {
        if (!DataBaseService.IsAvailable)
        {
            return;
        }

        string clauseContent = "ID ='" + player.playerID + "'";

        Dictionary<string, string> value = new Dictionary<string, string>();
        value.Add("ID", player.playerID);
        value.Add("NickName", player.nickName);
        value.Add("CharacterID", player.characterID);
        value.Add("OwnCharacter", player.OwnCharacter);

        try
        {
            DataBaseService.database.Update(c_playerTableName, value, clauseContent, null);
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }
}
