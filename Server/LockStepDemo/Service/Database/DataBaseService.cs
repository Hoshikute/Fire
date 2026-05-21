//using CDatabase;
using CDatabase;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class DataBaseService
{
    public static IDatabase database;

    public static bool IsAvailable
    {
        get { return database != null; }
    }

    public static void Init()
    {
        Debug.Log("开始连接数据库~~~");
        long time = ServiceTime.GetServiceTime();

        DbConfig config = new DbConfig();
        config.Server = GetAppSetting("DbServer", "127.0.0.1");
        config.User = GetAppSetting("DbUser", "lockstep");
        config.Password = GetAppSetting("DbPassword", "lockstep_dev");
        config.Database = GetAppSetting("DbName", "ElementCraft");

        try
        {
            database = DatabaseFactory.CreateDatabase(config, DbConfig.DbType.MYSQL);
            database.Open();

            time = ServiceTime.GetServiceTime() - time;

            Debug.Log("数据库连接成功 用时" + time +"ms");
        }
        catch (DatabaseException e)
        {
            database = null;
            Debug.LogError("错误代码：" + e.GetErrorCode() + "，错误信息：" + e.GetErrorMsg());
            Debug.LogWarning("数据库不可用，服务端将使用临时玩家数据运行。");
        }
        catch (Exception e)
        {
            database = null;
            Debug.LogError(e.ToString());
            Debug.LogWarning("数据库不可用，服务端将使用临时玩家数据运行。");
        }
    }

    private static string GetAppSetting(string key, string defaultValue)
    {
        string value = ConfigurationManager.AppSettings[key];
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }
}
