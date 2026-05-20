#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TEngine.Editor
{
    public sealed class MagentaSceneDiagnoseWindow : EditorWindow
    {
        private const string MenuRoot = "Tools/渲染/洋红色诊断与修复";
        private const string InternalErrorShaderName = "Hidden/InternalErrorShader";

        private bool _includeInactive = true;
        private Vector2 _scroll;

        private DiagnoseReport _lastReport;
        private string _lastReportText;

        [MenuItem(MenuRoot + "/打开面板")]
        public static void Open()
        {
            var window = GetWindow<MagentaSceneDiagnoseWindow>();
            window.titleContent = new GUIContent("洋红色诊断/修复");
            window.minSize = new Vector2(760, 520);
            window.Show();
        }

        [MenuItem(MenuRoot + "/立即诊断并输出到控制台")]
        public static void DiagnoseAndLog()
        {
            var report = Diagnose(includeInactive: true);
            var text = BuildTextReport(report);
            Debug.Log(text);
        }

        private void OnGUI()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scrollScope.scrollPosition;

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("当前渲染管线", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(GetPipelineLabel());
                EditorGUILayout.Space(8);

                EditorGUILayout.LabelField("诊断", EditorStyles.boldLabel);
                _includeInactive = EditorGUILayout.ToggleLeft("包含 Inactive 对象", _includeInactive);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("执行诊断", GUILayout.Height(28)))
                    {
                        _lastReport = Diagnose(_includeInactive);
                        _lastReportText = BuildTextReport(_lastReport);
                        Debug.Log(_lastReportText);
                        Repaint();
                    }

                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_lastReportText)))
                    {
                        if (GUILayout.Button("复制报告", GUILayout.Height(28)))
                        {
                            EditorGUIUtility.systemCopyBuffer = _lastReportText;
                        }

                        if (GUILayout.Button("保存报告...", GUILayout.Height(28)))
                        {
                            var path = EditorUtility.SaveFilePanelInProject("保存诊断报告", "MagentaReport", "txt", "选择保存位置");
                            if (!string.IsNullOrEmpty(path))
                            {
                                File.WriteAllText(path, _lastReportText, Encoding.UTF8);
                                AssetDatabase.Refresh();
                            }
                        }
                    }
                }

                EditorGUILayout.Space(10);
                DrawFixGUI();
                EditorGUILayout.Space(10);

                if (!string.IsNullOrEmpty(_lastReportText))
                {
                    EditorGUILayout.LabelField("报告预览（只读）", EditorStyles.boldLabel);
                    EditorGUILayout.TextArea(_lastReportText, GUILayout.MinHeight(240));
                }
            }
        }

        private void DrawFixGUI()
        {
            EditorGUILayout.LabelField("最小修复（可选）", EditorStyles.boldLabel);

            if (_lastReport == null)
            {
                EditorGUILayout.HelpBox("请先执行一次诊断，修复入口会基于诊断结果生成候选列表。", MessageType.Info);
                return;
            }

            var reimportTargets = CollectReimportTargets(_lastReport);
            var standardToUrpTargets = CollectStandardToUrpTargets(_lastReport);
            var isUrp = GetCurrentPipelineKind() == PipelineKind.URP;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Reimport/刷新", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"候选材质数：{reimportTargets.MaterialPaths.Count}，候选 Shader 数：{reimportTargets.ShaderPaths.Count}");

                using (new EditorGUI.DisabledScope(reimportTargets.MaterialPaths.Count == 0 && reimportTargets.ShaderPaths.Count == 0))
                {
                    if (GUILayout.Button("执行 Reimport", GUILayout.Height(26)))
                    {
                        var message = $"将对以下资产执行 Reimport：\n\n材质：{reimportTargets.MaterialPaths.Count}\nShader：{reimportTargets.ShaderPaths.Count}\n\n该操作会触发资产重新导入，可能耗时。是否继续？";
                        if (EditorUtility.DisplayDialog("确认 Reimport", message, "继续", "取消"))
                        {
                            ExecuteReimport(reimportTargets);
                        }
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("URP：Standard -> URP/Lit（最小迁移）", EditorStyles.boldLabel);
                if (!isUrp)
                {
                    EditorGUILayout.HelpBox("当前项目不是 URP（GraphicsSettings.currentRenderPipeline 为空或非 URP）。该修复入口将保持不可用。", MessageType.Info);
                }

                EditorGUILayout.LabelField($"候选材质数：{standardToUrpTargets.Count}");

                using (new EditorGUI.DisabledScope(!isUrp || standardToUrpTargets.Count == 0))
                {
                    if (GUILayout.Button("执行 Standard -> URP/Lit", GUILayout.Height(26)))
                    {
                        var message = $"将把 {standardToUrpTargets.Count} 个材质的 Shader 从 Standard 切换到 Universal Render Pipeline/Lit，并尝试迁移主贴图与颜色。\n\n是否继续？";
                        if (EditorUtility.DisplayDialog("确认 Shader 映射修复", message, "继续", "取消"))
                        {
                            ExecuteStandardToUrpLit(standardToUrpTargets);
                        }
                    }
                }
            }
        }

        private static void ExecuteReimport(ReimportTargets targets)
        {
            var paths = new HashSet<string>(targets.MaterialPaths);
            foreach (var shaderPath in targets.ShaderPaths)
            {
                paths.Add(shaderPath);
            }

            if (paths.Count == 0)
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Reimport Materials/Shaders");
            var undoGroup = Undo.GetCurrentGroup();

            if (targets.MaterialPaths.Count > 0)
            {
                var undoMaterials = new List<Material>(targets.MaterialPaths.Count);
                foreach (var matPath in targets.MaterialPaths)
                {
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat != null)
                    {
                        undoMaterials.Add(mat);
                    }
                }

                if (undoMaterials.Count > 0)
                {
                    Undo.RegisterCompleteObjectUndo(undoMaterials.ToArray(), "Reimport Materials");
                }
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in paths)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Undo.CollapseUndoOperations(undoGroup);
            AssetDatabase.Refresh();
            Debug.Log(BuildReimportReport(paths));
        }

        private static void ExecuteStandardToUrpLit(List<Material> materials)
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                EditorUtility.DisplayDialog("无法执行", "未找到 Shader：Universal Render Pipeline/Lit。请确认已安装并启用 URP。", "确定");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Standard -> URP/Lit");
            var undoGroup = Undo.GetCurrentGroup();

            var changed = new List<ShaderChangeRecord>();

            foreach (var mat in materials)
            {
                if (mat == null)
                {
                    continue;
                }

                var oldShader = mat.shader;
                var oldShaderName = oldShader != null ? oldShader.name : "<null>";
                if (oldShaderName != "Standard")
                {
                    continue;
                }

                Undo.RecordObject(mat, "Standard -> URP/Lit");

                var mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                var mainColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                var mainTexOffset = mat.HasProperty("_MainTex") ? mat.GetTextureOffset("_MainTex") : Vector2.zero;
                var mainTexScale = mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex") : Vector2.one;

                mat.shader = urpLit;

                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", mainTex);
                    mat.SetTextureOffset("_BaseMap", mainTexOffset);
                    mat.SetTextureScale("_BaseMap", mainTexScale);
                }

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", mainColor);
                }

                EditorUtility.SetDirty(mat);
                changed.Add(new ShaderChangeRecord(mat, oldShaderName, urpLit.name));
            }

            Undo.CollapseUndoOperations(undoGroup);

            if (changed.Count > 0)
            {
                AssetDatabase.SaveAssets();
            }

            Debug.Log(BuildShaderChangeReport(changed));
        }

        private static DiagnoseReport Diagnose(bool includeInactive)
        {
            var report = new DiagnoseReport
            {
                Pipeline = GetCurrentPipelineKind(),
                IncludeInactive = includeInactive
            };

            for (var i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                report.SceneCount++;
                report.SceneNames.Add(scene.name);

                var roots = scene.GetRootGameObjects();
                foreach (var root in roots)
                {
                    if (root == null)
                    {
                        continue;
                    }

                    var renderers = root.GetComponentsInChildren<Renderer>(includeInactive);
                    foreach (var renderer in renderers)
                    {
                        if (renderer == null)
                        {
                            continue;
                        }

                        report.RendererCount++;
                        var materials = renderer.sharedMaterials;
                        report.MaterialSlotCount += materials != null ? materials.Length : 0;

                        if (materials == null || materials.Length == 0)
                        {
                            continue;
                        }

                        for (var mi = 0; mi < materials.Length; mi++)
                        {
                            var material = materials[mi];
                            if (material == null)
                            {
                                report.Issues.Add(IssueEntry.CreateMissingMaterialSlot(scene.name, renderer, mi));
                                IncrementReasonCount(report.ReasonCounts, IssueReason.MissingMaterialSlot);
                                continue;
                            }

                            report.UniqueMaterialInstanceIds.Add(material.GetInstanceID());

                            var reason = ClassifyMaterial(report.Pipeline, material);
                            if (reason == IssueReason.None)
                            {
                                continue;
                            }

                            report.Issues.Add(IssueEntry.Create(scene.name, renderer, mi, material, reason));
                            IncrementReasonCount(report.ReasonCounts, reason);
                        }
                    }
                }
            }

            return report;
        }

        private static IssueReason ClassifyMaterial(PipelineKind currentPipeline, Material material)
        {
            if (material == null)
            {
                return IssueReason.MissingMaterialSlot;
            }

            var shader = material.shader;
            if (shader == null)
            {
                return IssueReason.MissingShader;
            }

            if (!shader.isSupported || shader.name == InternalErrorShaderName)
            {
                return IssueReason.InternalErrorOrUnsupported;
            }

            var materialPipeline = GetShaderPipelineKind(shader);
            if (currentPipeline != PipelineKind.Unknown && materialPipeline != PipelineKind.Unknown && currentPipeline != materialPipeline)
            {
                return IssueReason.PipelineMismatch;
            }

            return IssueReason.None;
        }

        private static string BuildTextReport(DiagnoseReport report)
        {
            var sb = new StringBuilder(16 * 1024);

            sb.AppendLine("=== 场景洋红色诊断报告 ===");
            sb.AppendLine($"时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"渲染管线：{report.Pipeline}");
            sb.AppendLine($"包含 Inactive：{report.IncludeInactive}");
            sb.AppendLine($"已加载场景数：{report.SceneCount}");
            if (report.SceneNames.Count > 0)
            {
                sb.AppendLine($"场景：{string.Join(", ", report.SceneNames.Distinct())}");
            }
            sb.AppendLine($"Renderer 数：{report.RendererCount}");
            sb.AppendLine($"材质槽位数：{report.MaterialSlotCount}");
            sb.AppendLine($"材质实例数（去重）：{report.UniqueMaterialInstanceIds.Count}");
            sb.AppendLine($"问题项数：{report.Issues.Count}");
            sb.AppendLine();

            sb.AppendLine("=== 按原因统计（问题项）===");
            AppendReasonLine(sb, report, IssueReason.MissingMaterialSlot, "Missing material slot（Renderer 材质槽为空）");
            AppendReasonLine(sb, report, IssueReason.MissingShader, "Missing shader（材质 Shader 为空）");
            AppendReasonLine(sb, report, IssueReason.InternalErrorOrUnsupported, "InternalError/Unsupported（编译失败或不支持）");
            AppendReasonLine(sb, report, IssueReason.PipelineMismatch, "Pipeline mismatch（渲染管线不匹配）");
            sb.AppendLine();

            sb.AppendLine("=== 明细（每个问题项一行）===");
            sb.AppendLine("Scene | GameObjectPath | Renderer | Slot | Material | MaterialPath | Shader | Reason");

            foreach (var issue in report.Issues.OrderBy(x => x.SceneName).ThenBy(x => x.GameObjectPath).ThenBy(x => x.MaterialSlotIndex))
            {
                sb.Append(issue.SceneName).Append(" | ");
                sb.Append(issue.GameObjectPath).Append(" | ");
                sb.Append(issue.RendererType).Append(" | ");
                sb.Append(issue.MaterialSlotIndex).Append(" | ");
                sb.Append(issue.MaterialName).Append(" | ");
                sb.Append(issue.MaterialAssetPath).Append(" | ");
                sb.Append(issue.ShaderName).Append(" | ");
                sb.Append(issue.Reason);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static void AppendReasonLine(StringBuilder sb, DiagnoseReport report, IssueReason reason, string label)
        {
            var count = GetReasonCount(report.ReasonCounts, reason);
            sb.AppendLine($"{label}：{count}");
        }

        private static int GetReasonCount(Dictionary<IssueReason, int> dict, IssueReason reason)
        {
            return dict.TryGetValue(reason, out var v) ? v : 0;
        }

        private static void IncrementReasonCount(Dictionary<IssueReason, int> dict, IssueReason reason)
        {
            if (dict.TryGetValue(reason, out var v))
            {
                dict[reason] = v + 1;
            }
            else
            {
                dict.Add(reason, 1);
            }
        }

        private static string BuildReimportReport(IEnumerable<string> paths)
        {
            var sb = new StringBuilder(4 * 1024);
            sb.AppendLine("=== Reimport 执行结果 ===");
            sb.AppendLine($"时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            foreach (var p in paths.OrderBy(x => x))
            {
                sb.AppendLine(p);
            }
            return sb.ToString();
        }

        private static string BuildShaderChangeReport(List<ShaderChangeRecord> records)
        {
            var sb = new StringBuilder(8 * 1024);
            sb.AppendLine("=== Shader 映射修复结果 ===");
            sb.AppendLine($"时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"变更材质数：{records.Count}");
            sb.AppendLine("MaterialPath | FromShader | ToShader");
            foreach (var r in records.OrderBy(x => x.MaterialAssetPath))
            {
                sb.Append(r.MaterialAssetPath).Append(" | ");
                sb.Append(r.FromShader).Append(" | ");
                sb.Append(r.ToShader);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static ReimportTargets CollectReimportTargets(DiagnoseReport report)
        {
            var targets = new ReimportTargets();
            foreach (var issue in report.Issues)
            {
                if (string.IsNullOrEmpty(issue.MaterialAssetPath))
                {
                    continue;
                }

                targets.MaterialPaths.Add(issue.MaterialAssetPath);

                if (!string.IsNullOrEmpty(issue.ShaderAssetPath))
                {
                    targets.ShaderPaths.Add(issue.ShaderAssetPath);
                }
            }

            return targets;
        }

        private static List<Material> CollectStandardToUrpTargets(DiagnoseReport report)
        {
            if (report.Pipeline != PipelineKind.URP)
            {
                return new List<Material>();
            }

            var list = new List<Material>();
            var visited = new HashSet<int>();
            foreach (var issue in report.Issues)
            {
                if (issue.Reason != IssueReason.PipelineMismatch)
                {
                    continue;
                }

                var mat = issue.Material;
                if (mat == null)
                {
                    continue;
                }

                if (mat.shader == null || mat.shader.name != "Standard")
                {
                    continue;
                }

                var id = mat.GetInstanceID();
                if (!visited.Add(id))
                {
                    continue;
                }

                var assetPath = AssetDatabase.GetAssetPath(mat);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                list.Add(mat);
            }

            return list;
        }

        private static PipelineKind GetShaderPipelineKind(Shader shader)
        {
            if (shader == null)
            {
                return PipelineKind.Unknown;
            }

            var name = shader.name ?? string.Empty;

            if (name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal))
            {
                return PipelineKind.URP;
            }

            if (name.StartsWith("HDRP/", StringComparison.Ordinal) || name.StartsWith("High Definition Render Pipeline/", StringComparison.Ordinal))
            {
                return PipelineKind.HDRP;
            }

            if (name == "Standard" || name.StartsWith("Legacy Shaders/", StringComparison.Ordinal))
            {
                return PipelineKind.BuiltIn;
            }

            return PipelineKind.Unknown;
        }

        private static PipelineKind GetCurrentPipelineKind()
        {
            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp == null)
            {
                return PipelineKind.BuiltIn;
            }

            var typeName = rp.GetType().FullName ?? string.Empty;
            if (typeName == "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset")
            {
                return PipelineKind.URP;
            }

            if (typeName == "UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset")
            {
                return PipelineKind.HDRP;
            }

            return PipelineKind.Unknown;
        }

        private static string GetPipelineLabel()
        {
            var pipeline = GetCurrentPipelineKind();
            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp == null)
            {
                return $"{pipeline}（GraphicsSettings.currentRenderPipeline = null）";
            }

            return $"{pipeline}（{rp.GetType().FullName}）";
        }

        private enum IssueReason
        {
            None = 0,
            MissingMaterialSlot = 1,
            MissingShader = 2,
            InternalErrorOrUnsupported = 3,
            PipelineMismatch = 4
        }

        private enum PipelineKind
        {
            Unknown = 0,
            BuiltIn = 1,
            URP = 2,
            HDRP = 3
        }

        private sealed class DiagnoseReport
        {
            public PipelineKind Pipeline;
            public bool IncludeInactive;
            public int SceneCount;
            public int RendererCount;
            public int MaterialSlotCount;
            public readonly HashSet<int> UniqueMaterialInstanceIds = new HashSet<int>();
            public readonly List<string> SceneNames = new List<string>();
            public readonly List<IssueEntry> Issues = new List<IssueEntry>();
            public readonly Dictionary<IssueReason, int> ReasonCounts = new Dictionary<IssueReason, int>();
        }

        private sealed class IssueEntry
        {
            public string SceneName;
            public string GameObjectPath;
            public string RendererType;
            public int MaterialSlotIndex;
            public string MaterialName;
            public string MaterialAssetPath;
            public string ShaderName;
            public string ShaderAssetPath;
            public IssueReason Reason;
            public Material Material;

            public static IssueEntry Create(string sceneName, Renderer renderer, int slotIndex, Material material, IssueReason reason)
            {
                var shader = material != null ? material.shader : null;
                var shaderName = shader != null ? shader.name : "<null>";
                var shaderPath = shader != null ? AssetDatabase.GetAssetPath(shader) : string.Empty;

                return new IssueEntry
                {
                    SceneName = sceneName,
                    GameObjectPath = GetGameObjectPath(renderer != null ? renderer.transform : null),
                    RendererType = renderer != null ? renderer.GetType().Name : "<null>",
                    MaterialSlotIndex = slotIndex,
                    MaterialName = material != null ? material.name : "<null>",
                    MaterialAssetPath = material != null ? AssetDatabase.GetAssetPath(material) : string.Empty,
                    ShaderName = shaderName,
                    ShaderAssetPath = shaderPath,
                    Reason = reason,
                    Material = material
                };
            }

            public static IssueEntry CreateMissingMaterialSlot(string sceneName, Renderer renderer, int slotIndex)
            {
                return new IssueEntry
                {
                    SceneName = sceneName,
                    GameObjectPath = GetGameObjectPath(renderer != null ? renderer.transform : null),
                    RendererType = renderer != null ? renderer.GetType().Name : "<null>",
                    MaterialSlotIndex = slotIndex,
                    MaterialName = "<null>",
                    MaterialAssetPath = string.Empty,
                    ShaderName = string.Empty,
                    ShaderAssetPath = string.Empty,
                    Reason = IssueReason.MissingMaterialSlot,
                    Material = null
                };
            }

            private static string GetGameObjectPath(Transform transform)
            {
                if (transform == null)
                {
                    return "<null>";
                }

                var sb = new StringBuilder(128);
                var stack = new Stack<string>();
                var current = transform;
                while (current != null)
                {
                    stack.Push(current.name);
                    current = current.parent;
                }

                while (stack.Count > 0)
                {
                    sb.Append(stack.Pop());
                    if (stack.Count > 0)
                    {
                        sb.Append('/');
                    }
                }

                return sb.ToString();
            }
        }

        private sealed class ReimportTargets
        {
            public readonly HashSet<string> MaterialPaths = new HashSet<string>();
            public readonly HashSet<string> ShaderPaths = new HashSet<string>();
        }

        private sealed class ShaderChangeRecord
        {
            public readonly string MaterialAssetPath;
            public readonly string FromShader;
            public readonly string ToShader;

            public ShaderChangeRecord(Material material, string from, string to)
            {
                MaterialAssetPath = material != null ? AssetDatabase.GetAssetPath(material) : string.Empty;
                FromShader = from;
                ToShader = to;
            }
        }
    }
}
#endif
