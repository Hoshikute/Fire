using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 服务器配置数据
    /// </summary>
    public class ServerData
    {
        public string Id { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return $"[{Name}] {Address}:{Port}";
        }
    }

    /// <summary>
    /// 服务器配置加载器
    /// 从 ServerData.txt 加载服务器列表配置
    /// </summary>
    public static class ServerDataLoader
    {
        private static List<ServerData> _serverList;
        private static bool _loaded;

        /// <summary>
        /// 获取所有服务器列表
        /// </summary>
        public static List<ServerData> ServerList
        {
            get
            {
                if (!_loaded)
                {
                    Load();
                }
                return _serverList;
            }
        }

        /// <summary>
        /// 根据 ID 获取服务器
        /// </summary>
        public static ServerData GetServer(string id)
        {
            return ServerList.Find(s => s.Id == id);
        }

        /// <summary>
        /// 获取本地服务器
        /// </summary>
        public static ServerData GetLocalServer()
        {
            return GetServer("Address") ?? new ServerData
            {
                Id = "local",
                Address = "127.0.0.1",
                Port = 7500,
                Name = "本地"
            };
        }

        /// <summary>
        /// 获取内网服务器
        /// </summary>
        public static ServerData GetLanServer()
        {
            return GetServer("1") ?? new ServerData
            {
                Id = "lan",
                Address = "192.168.89.146",
                Port = 7500,
                Name = "内网"
            };
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        public static void Load()
        {
            if (_loaded) return;

            _serverList = new List<ServerData>();

            // 配置文件路径
            string configPath = Path.Combine(Application.dataPath, "AssetRaw/FrameSync/Configs/Data/ServerData.txt");

            if (!File.Exists(configPath))
            {
                Debug.LogWarning($"[ServerDataLoader] 配置文件不存在: {configPath}");
                AddDefaultServers();
                _loaded = true;
                return;
            }

            try
            {
                var lines = File.ReadAllLines(configPath);

                // 跳过前4行（标题、类型、注释、默认值）
                for (int i = 4; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    var parts = line.Split('\t');
                    if (parts.Length >= 4)
                    {
                        var server = new ServerData
                        {
                            Id = parts[0].Trim(),
                            Address = parts[1].Trim(),
                            Port = int.TryParse(parts[2].Trim(), out int port) ? port : 7500,
                            Name = parts[3].Trim()
                        };
                        _serverList.Add(server);
                    }
                }

                Debug.Log($"[ServerDataLoader] 加载 {_serverList.Count} 个服务器配置");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ServerDataLoader] 加载失败: {e.Message}");
                AddDefaultServers();
            }

            _loaded = true;
        }

        /// <summary>
        /// 添加默认服务器配置
        /// </summary>
        private static void AddDefaultServers()
        {
            _serverList.Add(new ServerData { Id = "Address", Address = "127.0.0.1", Port = 7500, Name = "本地" });
            _serverList.Add(new ServerData { Id = "1", Address = "192.168.89.146", Port = 7500, Name = "内网" });
        }

        /// <summary>
        /// 重置加载状态（用于热重载）
        /// </summary>
        public static void Reset()
        {
            _loaded = false;
            _serverList = null;
        }
    }
}
