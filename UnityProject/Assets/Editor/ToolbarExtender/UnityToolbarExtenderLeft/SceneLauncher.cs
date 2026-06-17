#if !UNITY_6000_3_OR_NEWER

using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityToolbarExtender;
using Debug = UnityEngine.Debug;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

namespace TEngine
{
    public partial class UnityToolbarExtenderLeft
    {
        private const string PreviousSceneKey = "TEngine_PreviousScenePath"; // 用于存储之前场景路径的键
        private const string IsLauncherBtn = "TEngine_IsLauncher"; // 用于存储之前是否按下launcher

        private static readonly string SceneMain = "main";

        private static readonly string ButtonStyleName = "Tab middle";
        private static GUIStyle _buttonGuiStyle;
        
        private static void OnToolbarGUI_SceneLauncher()
        {
            _buttonGuiStyle ??= new GUIStyle(ButtonStyleName)
            {
                padding = new RectOffset(2, 8, 2, 2),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(
                    new GUIContent("Launcher", EditorGUIUtility.FindTexture("PlayButton"), "Start Scene Launcher"),
                    _buttonGuiStyle))
                SceneHelper.StartScene(SceneMain);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                // 从 EditorPrefs 读取之前的场景路径
                var previousScenePath = EditorPrefs.GetString(PreviousSceneKey, string.Empty);
                if (!string.IsNullOrEmpty(previousScenePath) && EditorPrefs.GetBool(IsLauncherBtn))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            EditorSceneManager.OpenScene(previousScenePath);
                    };
                }

                EditorPrefs.SetBool(IsLauncherBtn, false);
            }
        }

        private static void OnEditorQuit()
        {
            EditorPrefs.SetString(PreviousSceneKey, "");
            EditorPrefs.SetBool(IsLauncherBtn, false);
        }

        private static class SceneHelper
        {
            private const string LogPrefix = "[CODEX_LOG]";
            private const int ServerStartupTimeoutSeconds = 120;
            private const string ServerConfigRelativePath = "Server/LockStepDemo/App.config";
            private const string LoginWindowRelativePath = "UnityProject/Assets/GameScripts/HotFix/GameLogic/UI/LoginWindow/LoginWindow.cs";
            private const string ServerBatchRelativePath = "Server/scripts/start-server.bat";
            private const string ServerPowerShellRelativePath = "Server/scripts/start-server.ps1";

            private static string _sceneToOpen;
            private static string _pendingScenePath;
            private static ServerLaunchContext _pendingServerContext;
            private static double _serverStartupBeginTime;
            private static Process _serverScriptProcess;

            public static void StartScene(string sceneName)
            {
                if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;

                // 记录当前场景路径到 EditorPrefs
                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.isLoaded && activeScene.name != SceneMain)
                {
                    EditorPrefs.SetString(PreviousSceneKey, activeScene.path);
                    EditorPrefs.SetBool(IsLauncherBtn, true);
                }

                _sceneToOpen = sceneName;
                EditorApplication.update += OnUpdate;
            }

            private static void OnUpdate()
            {
                if (_pendingScenePath != null)
                {
                    PollServerStartup();
                    return;
                }

                if (_sceneToOpen == null ||
                    EditorApplication.isPlaying || EditorApplication.isPaused ||
                    EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                EditorApplication.update -= OnUpdate;

                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    string[] guids = AssetDatabase.FindAssets("t:scene " + _sceneToOpen, null);
                    if (guids.Length == 0)
                    {
                        Debug.LogWarning("Couldn't find scene file");
                    }
                    else
                    {
                        string scenePath = null;
                        // 优先打开完全匹配_sceneToOpen的场景
                        for (var i = 0; i < guids.Length; i++)
                        {
                            scenePath = AssetDatabase.GUIDToAssetPath(guids[i]);
                            if (scenePath.EndsWith("/" + _sceneToOpen + ".unity")) break;
                        }

                        // 如果没有完全匹配的场景，默认显示找到的第一个场景
                        if (string.IsNullOrEmpty(scenePath)) scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);

                        EditorSceneManager.OpenScene(scenePath);
                        BeginServerPreparation(scenePath);
                    }
                }

                _sceneToOpen = null;
            }

            private static void BeginServerPreparation(string scenePath)
            {
                if (!TryCreateServerLaunchContext(out var context, out var error))
                {
                    Debug.LogError($"{LogPrefix} SceneLauncher server 环境检查失败，取消自动 Play Mode。{error}");
                    return;
                }

                Debug.Log($"{LogPrefix} SceneLauncher 检查本地 server：{context.Protocol}/{context.Port}，脚本：{context.ScriptPath}");

                if (IsEndpointListening(context.Protocol, context.Port))
                {
                    Debug.Log($"{LogPrefix} SceneLauncher 检测到本地 server 已监听 {context.Protocol}/{context.Port}，复用现有 server 并进入 Play Mode。");
                    EditorApplication.isPlaying = true;
                    return;
                }

                if (!TryValidateServerDependencies(context, out error))
                {
                    Debug.LogError($"{LogPrefix} SceneLauncher server 依赖检查失败，取消自动 Play Mode。{error}");
                    return;
                }

                try
                {
                    _serverScriptProcess = StartServerScript(context);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"{LogPrefix} SceneLauncher 启动 server 脚本失败，取消自动 Play Mode。脚本：{context.ScriptPath}。错误：{exception.Message}");
                    return;
                }

                if (_serverScriptProcess == null)
                {
                    Debug.LogError($"{LogPrefix} SceneLauncher 启动 server 脚本失败，取消自动 Play Mode。脚本：{context.ScriptPath} 未返回进程句柄。");
                    return;
                }

                _pendingScenePath = scenePath;
                _pendingServerContext = context;
                _serverStartupBeginTime = EditorApplication.timeSinceStartup;
                EditorApplication.update += OnUpdate;

                Debug.Log($"{LogPrefix} SceneLauncher 已启动 server 脚本，等待 {context.Protocol}/{context.Port} 就绪，最长 {ServerStartupTimeoutSeconds} 秒。请查看脚本窗口确认 MySQL、MSBuild 和 server 构建输出。");
            }

            private static void PollServerStartup()
            {
                if (IsEndpointListening(_pendingServerContext.Protocol, _pendingServerContext.Port))
                {
                    Debug.Log($"{LogPrefix} SceneLauncher server 已就绪：{_pendingServerContext.Protocol}/{_pendingServerContext.Port}，进入 Play Mode。");
                    ClearPendingServerStartup();
                    EditorApplication.isPlaying = true;
                    return;
                }

                if (HasServerScriptExited())
                {
                    Debug.LogError($"{LogPrefix} SceneLauncher server 脚本已退出，但 {_pendingServerContext.Protocol}/{_pendingServerContext.Port} 未就绪，取消自动 Play Mode。请查看脚本窗口中的 MySQL、MSBuild、构建或 server 启动错误。脚本：{_pendingServerContext.ScriptPath}");
                    ClearPendingServerStartup();
                    return;
                }

                if (EditorApplication.timeSinceStartup - _serverStartupBeginTime >= ServerStartupTimeoutSeconds)
                {
                    Debug.LogError($"{LogPrefix} SceneLauncher 等待 server 超时，取消自动 Play Mode。目标：{_pendingServerContext.Protocol}/{_pendingServerContext.Port}。请确认 MySQL 已启动、MSBuild 可用、server 构建成功，或手动运行脚本：{_pendingServerContext.ScriptPath}");
                    ClearPendingServerStartup();
                }
            }

            private static bool TryCreateServerLaunchContext(out ServerLaunchContext context, out string error)
            {
                context = new ServerLaunchContext();
                error = null;

                var repositoryRoot = ResolveRepositoryRoot();
                if (string.IsNullOrEmpty(repositoryRoot))
                {
                    error = $"无法从 Unity Application.dataPath 定位仓库根目录。Application.dataPath={Application.dataPath}";
                    return false;
                }

                var serverConfigPath = ResolveRepositoryPath(repositoryRoot, ServerConfigRelativePath);
                if (!TryReadServerEndpoint(serverConfigPath, out var serverProtocol, out var serverPort, out error))
                {
                    return false;
                }

                var loginWindowPath = ResolveRepositoryPath(repositoryRoot, LoginWindowRelativePath);
                if (!TryReadClientProtocol(loginWindowPath, out var clientProtocol, out error))
                {
                    return false;
                }

                if (!string.Equals(serverProtocol, clientProtocol, StringComparison.OrdinalIgnoreCase))
                {
                    error = $"协议不一致：server 配置为 {serverProtocol}/{serverPort}，LoginWindow 初始化为 {clientProtocol}。请保持两端协议一致。";
                    return false;
                }

                var powerShellScriptPath = ResolveRepositoryPath(repositoryRoot, ServerPowerShellRelativePath);
                var batchScriptPath = ResolveRepositoryPath(repositoryRoot, ServerBatchRelativePath);
                var scriptPath = File.Exists(powerShellScriptPath) ? powerShellScriptPath : batchScriptPath;

                if (!File.Exists(scriptPath))
                {
                    error = $"未找到 server 启动脚本。已检查：{powerShellScriptPath}；{batchScriptPath}";
                    return false;
                }

                context.ServerRoot = ResolveRepositoryPath(repositoryRoot, "Server");
                context.ScriptPath = scriptPath;
                context.Protocol = serverProtocol;
                context.Port = serverPort;
                return true;
            }

            private static bool TryValidateServerDependencies(ServerLaunchContext context, out string error)
            {
                error = null;

                var solutionPath = Path.Combine(context.ServerRoot, "LockStepDemo.sln");
                if (!File.Exists(solutionPath))
                {
                    error = $"未找到 server 方案文件：{solutionPath}";
                    return false;
                }

                if (!IsEndpointListening("Tcp", 3306))
                {
                    var mysqlBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"LockStep\mysql");
                    var mysqld = Path.Combine(mysqlBase, @"mysql-8.4.9-winx64\bin\mysqld.exe");
                    var mysqlConfig = Path.Combine(mysqlBase, "my.ini");
                    if (!File.Exists(mysqld) || !File.Exists(mysqlConfig))
                    {
                        error = $"MySQL 未监听 3306，且本地 MySQL 文件不完整。mysqld={mysqld}，config={mysqlConfig}";
                        return false;
                    }
                }

                if (!TryFindMSBuild(out var msbuildPath))
                {
                    error = "未找到 MSBuild.exe。请安装 Visual Studio 或 Build Tools，并确保包含 MSBuild。";
                    return false;
                }

                Debug.Log($"{LogPrefix} SceneLauncher server 依赖检查通过。MSBuild={msbuildPath}");
                return true;
            }

            private static Process StartServerScript(ServerLaunchContext context)
            {
                var extension = Path.GetExtension(context.ScriptPath);
                var startInfo = new ProcessStartInfo
                {
                    WorkingDirectory = Path.GetDirectoryName(context.ScriptPath),
                    UseShellExecute = true
                };

                if (string.Equals(extension, ".ps1", StringComparison.OrdinalIgnoreCase))
                {
                    startInfo.FileName = "powershell.exe";
                    startInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{context.ScriptPath}\" -ServerRoot \"{context.ServerRoot}\"";
                }
                else
                {
                    startInfo.FileName = context.ScriptPath;
                }

                return Process.Start(startInfo);
            }

            private static bool HasServerScriptExited()
            {
                try
                {
                    return _serverScriptProcess != null && _serverScriptProcess.HasExited;
                }
                catch
                {
                    return false;
                }
            }

            private static void ClearPendingServerStartup()
            {
                EditorApplication.update -= OnUpdate;
                _pendingScenePath = null;
                _pendingServerContext = new ServerLaunchContext();
                _serverStartupBeginTime = 0d;

                if (_serverScriptProcess != null)
                {
                    _serverScriptProcess.Dispose();
                    _serverScriptProcess = null;
                }
            }

            private static string ResolveRepositoryRoot()
            {
                var assetsDirectory = new DirectoryInfo(Application.dataPath);
                var unityProjectDirectory = assetsDirectory.Parent;
                return unityProjectDirectory?.Parent?.FullName;
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

            private static bool TryReadClientProtocol(string loginWindowPath, out string protocol, out string error)
            {
                protocol = null;
                error = null;

                if (!File.Exists(loginWindowPath))
                {
                    error = $"未找到客户端登录代码：{loginWindowPath}";
                    return false;
                }

                var source = File.ReadAllText(loginWindowPath);
                var match = Regex.Match(source, @"GameModule\.Network\.Init<ProtocolService>\s*\(\s*ProtocolType\.(Tcp|Udp)\s*\)");
                if (!match.Success)
                {
                    error = $"无法从 LoginWindow 读取 ProtocolService 初始化协议：{loginWindowPath}";
                    return false;
                }

                if (!TryNormalizeProtocol(match.Groups[1].Value, out protocol))
                {
                    error = $"LoginWindow 使用了不支持的协议：{match.Groups[1].Value}";
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
                    Debug.LogWarning($"{LogPrefix} SceneLauncher 端口检查失败：{protocol}/{port}。错误：{exception.Message}");
                }

                return false;
            }

            private static bool TryFindMSBuild(out string msbuildPath)
            {
                msbuildPath = null;

                var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                var vswhere = Path.Combine(programFilesX86, @"Microsoft Visual Studio\Installer\vswhere.exe");
                if (File.Exists(vswhere))
                {
                    try
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = vswhere,
                            Arguments = "-latest -requires Microsoft.Component.MSBuild -find \"MSBuild\\**\\Bin\\MSBuild.exe\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        };

                        using (var process = Process.Start(startInfo))
                        {
                            if (process != null && process.WaitForExit(5000))
                            {
                                var output = process.StandardOutput.ReadToEnd();
                                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var line in lines)
                                {
                                    if (File.Exists(line))
                                    {
                                        msbuildPath = line;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore and continue with known install locations.
                    }
                }

                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var candidates = new[]
                {
                    Path.Combine(programFiles, @"Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"),
                    Path.Combine(programFiles, @"Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"),
                    Path.Combine(programFiles, @"Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"),
                    Path.Combine(programFilesX86, @"Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"),
                    Path.Combine(programFilesX86, @"Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"),
                    Path.Combine(programFilesX86, @"Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe")
                };

                foreach (var candidate in candidates)
                {
                    if (File.Exists(candidate))
                    {
                        msbuildPath = candidate;
                        return true;
                    }
                }

                return false;
            }

            private struct ServerLaunchContext
            {
                public string ServerRoot;
                public string ScriptPath;
                public string Protocol;
                public int Port;
            }
        }
    }
}

#endif
