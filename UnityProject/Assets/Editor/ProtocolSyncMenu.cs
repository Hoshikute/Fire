using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TEngine.Editor
{
    internal static class ProtocolSyncMenu
    {
        private const string LogPrefix = "[CODEX_LOG]";
        private const string MenuPath = "TEngine/Protocol/同步协议到Server运行目录";
        private const string ServerProtocolDirectoryRelativePath = "Server/LockStepDemo/Network";
        private const string ClientProtocolDirectoryRelativePath = "UnityProject/Assets/Resources/Protocol";
        private const string ServerRuntimeProtocolDirectoryRelativePath = "Server/LockStepDemo/bin/Debug/Network";
        private const string ServerConfigRelativePath = "Server/LockStepDemo/App.config";
        private const string ProtocolInfoFileName = "ProtocolInfo.txt";
        private const string MethodInfoFileName = "MethodInfo.txt";

        [MenuItem(MenuPath, false, 101)]
        private static void SyncProtocolToServerRuntime()
        {
            try
            {
                if (!TryCreateContext(out var context, out var error))
                {
                    Debug.LogError($"{LogPrefix} 协议同步失败。{error}");
                    return;
                }

                if (!TryValidateProtocolPair(context, ProtocolInfoFileName, out error) ||
                    !TryValidateProtocolPair(context, MethodInfoFileName, out error))
                {
                    Debug.LogError($"{LogPrefix} 协议同步失败。{error}");
                    return;
                }

                Directory.CreateDirectory(context.ServerRuntimeProtocolDirectory);
                CopyProtocolFile(context, ProtocolInfoFileName);
                CopyProtocolFile(context, MethodInfoFileName);

                Debug.Log($"{LogPrefix} 协议同步完成。文件：{ProtocolInfoFileName}, {MethodInfoFileName}。目标目录：{context.ServerRuntimeProtocolDirectory}");

                if (TryReadServerEndpoint(context.ServerConfigPath, out var protocol, out var port, out error))
                {
                    if (IsEndpointListening(protocol, port))
                    {
                        Debug.LogWarning($"{LogPrefix} 协议文件已同步，但检测到本地 server 正在监听 {protocol}/{port}。正在运行的 server 不会自动重新加载协议文件，请重启 server 后再本地联机。");
                    }
                }
                else
                {
                    Debug.LogWarning($"{LogPrefix} 协议文件已同步，但无法检查 server 是否正在运行。{error}");
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} 协议同步失败。{exception.Message}");
            }
        }

        private static bool TryCreateContext(out ProtocolSyncContext context, out string error)
        {
            context = new ProtocolSyncContext();
            error = null;

            var repositoryRoot = ResolveRepositoryRoot();
            if (string.IsNullOrEmpty(repositoryRoot))
            {
                error = $"无法从 Unity Application.dataPath 定位仓库根目录。Application.dataPath={Application.dataPath}";
                return false;
            }

            context.RepositoryRoot = repositoryRoot;
            context.ServerProtocolDirectory = ResolveRepositoryPath(repositoryRoot, ServerProtocolDirectoryRelativePath);
            context.ClientProtocolDirectory = ResolveRepositoryPath(repositoryRoot, ClientProtocolDirectoryRelativePath);
            context.ServerRuntimeProtocolDirectory = ResolveRepositoryPath(repositoryRoot, ServerRuntimeProtocolDirectoryRelativePath);
            context.ServerConfigPath = ResolveRepositoryPath(repositoryRoot, ServerConfigRelativePath);
            return true;
        }

        private static bool TryValidateProtocolPair(ProtocolSyncContext context, string fileName, out string error)
        {
            error = null;

            var serverPath = Path.Combine(context.ServerProtocolDirectory, fileName);
            var clientPath = Path.Combine(context.ClientProtocolDirectory, fileName);

            if (!File.Exists(serverPath))
            {
                error = $"未找到 server 源协议文件：{serverPath}";
                return false;
            }

            if (!File.Exists(clientPath))
            {
                error = $"未找到客户端源协议文件：{clientPath}";
                return false;
            }

            var serverText = ReadNormalizedText(serverPath);
            var clientText = ReadNormalizedText(clientPath);
            if (!string.Equals(serverText, clientText, StringComparison.Ordinal))
            {
                error = $"客户端与 server 源协议文件不一致，已中止复制。server={serverPath}；client={clientPath}";
                return false;
            }

            return true;
        }

        private static void CopyProtocolFile(ProtocolSyncContext context, string fileName)
        {
            var sourcePath = Path.Combine(context.ServerProtocolDirectory, fileName);
            var targetPath = Path.Combine(context.ServerRuntimeProtocolDirectory, fileName);
            File.Copy(sourcePath, targetPath, true);
        }

        private static string ReadNormalizedText(string path)
        {
            string text;
            using (var reader = new StreamReader(path, Encoding.UTF8, true))
            {
                text = reader.ReadToEnd();
            }

            if (!string.IsNullOrEmpty(text) && text[0] == '\uFEFF')
            {
                text = text.Substring(1);
            }

            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static string ResolveRepositoryRoot()
        {
            var assetsDirectory = new DirectoryInfo(Application.dataPath);
            var unityProjectDirectory = assetsDirectory.Parent;
            return unityProjectDirectory == null ? null : unityProjectDirectory.Parent?.FullName;
        }

        private static string ResolveRepositoryPath(string repositoryRoot, string relativePath)
        {
            return Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static bool TryReadServerEndpoint(string configPath, out string protocol, out int port, out string error)
        {
            protocol = null;
            port = 0;
            error = null;

            if (!File.Exists(configPath))
            {
                error = $"未找到 server 配置文件：{configPath}";
                return false;
            }

            var document = new XmlDocument();
            document.Load(configPath);

            var serverNode = document.SelectSingleNode("/configuration/superSocket/servers/server");
            if (serverNode?.Attributes == null)
            {
                error = $"server 配置缺少 /configuration/superSocket/servers/server 节点：{configPath}";
                return false;
            }

            var portAttribute = serverNode.Attributes["port"];
            var modeAttribute = serverNode.Attributes["mode"];
            if (portAttribute == null || modeAttribute == null)
            {
                error = $"server 配置缺少 port 或 mode 属性：{configPath}";
                return false;
            }

            if (!int.TryParse(portAttribute.Value, out port))
            {
                error = $"server port 不是合法整数：{portAttribute.Value}";
                return false;
            }

            if (!TryNormalizeProtocol(modeAttribute.Value, out protocol))
            {
                error = $"server mode 不是支持的协议：{modeAttribute.Value}";
                return false;
            }

            return true;
        }

        private static bool TryNormalizeProtocol(string value, out string protocol)
        {
            if (string.Equals(value, "Tcp", StringComparison.OrdinalIgnoreCase))
            {
                protocol = "Tcp";
                return true;
            }

            if (string.Equals(value, "Udp", StringComparison.OrdinalIgnoreCase))
            {
                protocol = "Udp";
                return true;
            }

            protocol = null;
            return false;
        }

        private static bool IsEndpointListening(string protocol, int port)
        {
            try
            {
                var properties = IPGlobalProperties.GetIPGlobalProperties();
                if (string.Equals(protocol, "Udp", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var endpoint in properties.GetActiveUdpListeners())
                    {
                        if (endpoint.Port == port) return true;
                    }
                }
                else
                {
                    foreach (var endpoint in properties.GetActiveTcpListeners())
                    {
                        if (endpoint.Port == port) return true;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{LogPrefix} 协议同步端口检查失败：{protocol}/{port}。错误：{exception.Message}");
            }

            return false;
        }

        private struct ProtocolSyncContext
        {
            public string RepositoryRoot;
            public string ServerProtocolDirectory;
            public string ClientProtocolDirectory;
            public string ServerRuntimeProtocolDirectory;
            public string ServerConfigPath;
        }
    }
}
