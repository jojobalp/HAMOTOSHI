using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PixelCamera.Editor
{
    /// <summary>
    /// Diagnóstico do projeto para o Pixel Camera.
    ///
    /// O pacote depende do Universal Render Pipeline (URP). Quando o URP não está instalado
    /// (ou o Asset de pipeline não está atribuído), a Unity reporta erros de compilação como:
    ///
    ///   error CS0234: The type or namespace name 'Universal' does not exist in the namespace
    ///                 'UnityEngine.Rendering' (are you missing an assembly reference?)
    ///
    /// Esta classe roda sozinha após cada compilação e mostra no Console exatamente o que está
    /// faltando e como resolver. Também pode ser aberta em Tools > Pixel Camera > Diagnóstico.
    /// </summary>
    public static class PixelCameraProjectDiagnostics
    {
        private const string UrpPackageName = "com.unity.render-pipelines.universal";
        private const string MenuPath = "Tools/Pixel Camera/Diagnóstico do Projeto (URP)";
        private const string AutoFixPath = "Tools/Pixel Camera/Corrigir Automaticamente (Adicionar Render Feature)";

        [InitializeOnLoadMethod]
        private static void DiagnoseOnLoad()
        {
            // Evita spam durante o Play Mode.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Log(BuildReport());
        }

        [MenuItem(MenuPath)]
        public static void ShowDiagnosticsDialog()
        {
            Report report = BuildReport();
            Log(report);

            string message = report.ToString();
            bool hasFixableIssue = report.RendererMissingFeatureCount > 0;

            if (hasFixableIssue)
            {
                int choice = EditorUtility.DisplayDialogComplex(
                    "Pixel Camera - Diagnóstico",
                    message + "\n\nDeseja adicionar automaticamente o Pixel Camera Render Feature ao Renderer ativo?",
                    "Corrigir Automaticamente", "OK", "Abrir Renderer Ativo");

                switch (choice)
                {
                    case 0:
                        AutoAddRenderFeatureToActiveRenderer();
                        break;
                    case 2:
                        PingActiveRenderer();
                        break;
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Pixel Camera - Diagnóstico", message, "OK");
            }
        }

        [MenuItem(AutoFixPath)]
        public static void AutoFixMenuItem()
        {
            bool success = AutoAddRenderFeatureToActiveRenderer();
            if (!success)
            {
                EditorUtility.DisplayDialog("Pixel Camera",
                    "Não foi possível adicionar o Render Feature automaticamente.\n\n" +
                    "Verifique se o URP está configurado e se há um Renderer ativo no projeto.",
                    "OK");
            }
        }

        /// <summary>
        /// Adiciona automaticamente o PixelCameraRenderFeature ao Renderer que está em uso
        /// pela pipeline ativa (com verificação para não duplicar caso já exista).
        /// </summary>
        public static bool AutoAddRenderFeatureToActiveRenderer()
        {
            try
            {
                List<ScriptableRendererData> activeRenderers = GetActiveRendererDataList();

                if (activeRenderers == null || activeRenderers.Count == 0)
                {
                    Debug.LogWarning("[PixelCamera] Nenhum Renderer ativo encontrado na pipeline. " +
                        "Verifique se o URP Asset está configurado em Project Settings > Graphics/Quality.");
                    return false;
                }

                bool anyAdded = false;
                foreach (ScriptableRendererData rendererData in activeRenderers)
                {
                    if (rendererData == null) continue;

                    // Verifica se já tem o PixelCameraRenderFeature
                    bool alreadyHas = false;
                    foreach (var feature in rendererData.rendererFeatures)
                    {
                        if (feature is PixelCameraRenderFeature)
                        {
                            alreadyHas = true;
                            break;
                        }
                    }

                    if (alreadyHas)
                    {
                        Debug.Log($"[PixelCamera] Renderer \"{rendererData.name}\" já tem Pixel Camera Render Feature.");
                        continue;
                    }

                    // Adiciona o Render Feature
                    var renderFeature = ScriptableObject.CreateInstance<PixelCameraRenderFeature>();
                    renderFeature.name = "Pixel Camera Render Feature";

                    rendererData.rendererFeatures.Add(renderFeature);
                    rendererData.SetDirty();

                    // Salva o asset
                    EditorUtility.SetDirty(rendererData);
                    AssetDatabase.AddObjectToAsset(renderFeature, rendererData);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    Debug.Log($"[PixelCamera] ✔ Pixel Camera Render Feature adicionado a \"{rendererData.name}\" " +
                        $"(caminho: {AssetDatabase.GetAssetPath(rendererData)})");
                    anyAdded = true;
                }

                if (anyAdded)
                {
                    EditorUtility.DisplayDialog("Pixel Camera - Sucesso!",
                        "Pixel Camera Render Feature adicionado com sucesso ao(s) Renderer(s) ativo(s)!\n\n" +
                        "Agora você pode usar o componente PixelCameraAutoSetup ou PixelCameraController na sua câmera.",
                        "OK");
                }
                else
                {
                    Debug.Log("[PixelCamera] Todos os Renderers ativos já têm o Pixel Camera Render Feature.");
                }

                return anyAdded || activeRenderers.All(r => r.rendererFeatures.Any(f => f is PixelCameraRenderFeature));
            }
            catch (Exception e)
            {
                Debug.LogError($"[PixelCamera] Erro ao adicionar Render Feature automaticamente: {e}");
                return false;
            }
        }

        /// <summary>
        /// Seleciona e destaca o Renderer ativo no Project window.
        /// </summary>
        private static void PingActiveRenderer()
        {
            List<ScriptableRendererData> activeRenderers = GetActiveRendererDataList();
            if (activeRenderers != null && activeRenderers.Count > 0 && activeRenderers[0] != null)
            {
                Selection.activeObject = activeRenderers[0];
                EditorGUIUtility.PingObject(activeRenderers[0]);
            }
        }

        private static void Log(Report report)
        {
            if (report.HasErrors)
            {
                Debug.LogError(report.ToString());
            }
            else if (report.HasWarnings)
            {
                Debug.LogWarning(report.ToString());
            }
            else
            {
                Debug.Log(report.ToString());
            }
        }

        /// <summary>
        /// Gera o relatório completo (URP instalado? pipeline atribuído? Render Feature presente?).
        /// </summary>
        public static Report BuildReport()
        {
            var report = new Report();

            CheckUrpPackage(report);
            CheckPipelineAsset(report);
            CheckRenderFeatureInProject(report);

            if (!report.HasErrors && !report.HasWarnings)
            {
                report.Ok("Projeto pronto: URP instalado, pipeline atribuído e Render Feature configurado.");
            }

            return report;
        }

        // ---------------------------------------------------------------- URP

        private static void CheckUrpPackage(Report report)
        {
            string installedVersion = TryGetUrpVersionFromAssembly() ?? TryGetUrpVersionFromManifest();

            if (string.IsNullOrEmpty(installedVersion))
            {
                report.Error(
                    "URP NÃO está instalado no projeto.\n" +
                    "É isso que causa o erro \"CS0234: The type or namespace name 'Universal' does not exist " +
                    "in the namespace 'UnityEngine.Rendering'\".\n\n" +
                    "Como resolver:\n" +
                    "  1. Window > Package Manager\n" +
                    "  2. Aba \"Unity Registry\" e busque por \"Universal RP\"\n" +
                    "  3. Clique em Install e aguarde a recompilação\n" +
                    "  Alternativa: edite Packages/manifest.json e adicione\n" +
                    $"   \"{UrpPackageName}\": \"14.0.11\" dentro de \"dependencies\".");
                return;
            }

            report.Ok($"URP instalado (versão {installedVersion}).");

            if (TryParseMajorVersion(installedVersion, out int major) && major < 12)
            {
                report.Warning(
                    $"A versão do URP ({installedVersion}) é anterior à 12.0. O Pixel Camera foi testado com " +
                    "URP 12+ (Unity 2021.3). Atualize o pacote se aparecerem erros de API.");
            }
        }

        private static string TryGetUrpVersionFromAssembly()
        {
            try
            {
                // Se o URP está instalado, o tipo existe na assembly dele e o PackageManager
                // consegue reportar a versão a partir dessa assembly.
                var urpType = Type.GetType(
                    "UnityEngine.Rendering.Universal.UniversalRenderPipeline, Unity.RenderPipelines.Universal.Runtime");

                if (urpType == null)
                {
                    return null;
                }

#if UNITY_2020_1_OR_NEWER
                UnityEditor.PackageManager.PackageInfo info =
                    UnityEditor.PackageManager.PackageInfo.FindForAssembly(urpType.Assembly);
                return info != null ? info.version : "instalado";
#else
                return "instalado";
#endif
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string TryGetUrpVersionFromManifest()
        {
            try
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");

                if (!File.Exists(manifestPath))
                {
                    return null;
                }

                foreach (string line in File.ReadAllLines(manifestPath))
                {
                    int index = line.IndexOf(UrpPackageName, StringComparison.Ordinal);

                    if (index < 0)
                    {
                        continue;
                    }

                    string tail = line.Substring(index + UrpPackageName.Length);
                    var quotes = new List<int>();

                    for (int i = 0; i < tail.Length && quotes.Count < 2; i++)
                    {
                        if (tail[i] == '\"')
                        {
                            quotes.Add(i);
                        }
                    }

                    if (quotes.Count == 2)
                    {
                        return tail.Substring(quotes[0] + 1, quotes[1] - quotes[0] - 1);
                    }

                    return "presente no manifest.json";
                }
            }
            catch (Exception)
            {
                // Ignorado: o diagnóstico nunca deve quebrar o editor.
            }

            return null;
        }

        private static bool TryParseMajorVersion(string version, out int major)
        {
            major = 0;

            if (string.IsNullOrEmpty(version))
            {
                return false;
            }

            int dot = version.IndexOf('.');
            string head = dot > 0 ? version.Substring(0, dot) : version;
            return int.TryParse(head, out major);
        }

        // ----------------------------------------------------------- Pipeline

        private static void CheckPipelineAsset(Report report)
        {
            RenderPipelineAsset graphicsPipeline = GraphicsSettings.currentRenderPipeline;
            RenderPipelineAsset qualityPipeline = QualitySettings.renderPipeline;
            RenderPipelineAsset active = qualityPipeline != null ? qualityPipeline : graphicsPipeline;

            if (active == null)
            {
                report.Error(
                    "Nenhum Render Pipeline Asset atribuído ao projeto.\n" +
                    "Mesmo com o URP instalado, sem isso o Render Feature não funciona e a cena fica rosa.\n\n" +
                    "Como resolver:\n" +
                    "  1. Edit > Project Settings > Graphics > \"Scriptable Render Pipeline Settings\"\n" +
                    "  2. Crie/atribua um \"Universal Render Pipeline Asset (Forward Renderer)\"\n" +
                    "  3. Repita em Project Settings > Quality (campo \"Render Pipeline Asset\").");
                return;
            }

            report.Ok($"Pipeline ativo: {active.name}.");

            if (graphicsPipeline == null)
            {
                report.Warning(
                    "O pipeline está atribuído apenas em Project Settings > Quality. Recomenda-se atribuir " +
                    "também em Project Settings > Graphics para valer em todas as plataformas.");
            }

            if (!LooksLikeUrp(active) && !LooksLikeUrp(graphicsPipeline))
            {
                report.Warning(
                    $"O pipeline atribuído (\"{active.name}\") não parece ser um UniversalRenderPipelineAsset. " +
                    "O Pixel Camera só funciona com URP (não funciona com Built-in nem HDRP).");
            }
        }

        private static bool LooksLikeUrp(RenderPipelineAsset asset)
        {
            if (asset == null)
            {
                return false;
            }

            string typeName = asset.GetType().FullName ?? string.Empty;
            return typeName.Contains("Universal");
        }

        // ------------------------------------------------------ Render Feature

        /// <summary>
        /// Obtém a lista de Renderer Data efetivamente em uso pela pipeline ativa
        /// (tanto em Graphics quanto em Quality settings).
        /// </summary>
        private static List<ScriptableRendererData> GetActiveRendererDataList()
        {
            var result = new List<ScriptableRendererData>();

            try
            {
                // URP 12+ tem a propriedade .m_RendererDataList em UniversalRenderPipelineAsset.
                // Usamos SerializedObject para acessá-la de forma compatível com múltiplas versões.
                var pipelines = new HashSet<RenderPipelineAsset>();

                if (GraphicsSettings.currentRenderPipeline != null)
                    pipelines.Add(GraphicsSettings.currentRenderPipeline);
                if (QualitySettings.renderPipeline != null)
                    pipelines.Add(QualitySettings.renderPipeline);

                foreach (RenderPipelineAsset pipeline in pipelines)
                {
                    CollectRenderersFromPipeline(pipeline, result);
                }
            }
            catch (Exception e)
            {
                Debug.Log($"[PixelCamera] Falha ao ler Renderers da pipeline: {e.Message}");
            }

            return result;
        }

        private static void CollectRenderersFromPipeline(RenderPipelineAsset pipeline, List<ScriptableRendererData> result)
        {
            if (pipeline == null) return;

            try
            {
                // UniversalRenderPipelineAsset tem um campo m_RendererDataList (ScriptableRendererData[])
                var serialized = new SerializedObject(pipeline);
                SerializedProperty listProp = serialized.FindProperty("m_RendererDataList");

                if (listProp == null || !listProp.isArray)
                {
                    // Fallback para versões mais antigas: campo m_DefaultRendererIndex + rendererData
                    SerializedProperty defaultProp = serialized.FindProperty("m_RendererData");
                    if (defaultProp != null && defaultProp.objectReferenceValue is ScriptableRendererData data)
                    {
                        if (!result.Contains(data))
                            result.Add(data);
                    }
                    return;
                }

                for (int i = 0; i < listProp.arraySize; i++)
                {
                    SerializedProperty elem = listProp.GetArrayElementAtIndex(i);
                    if (elem?.objectReferenceValue is ScriptableRendererData rd && !result.Contains(rd))
                    {
                        result.Add(rd);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log($"[PixelCamera] Falha ao ler Renderers de {pipeline.name}: {e.Message}");
            }
        }

        private static void CheckRenderFeatureInProject(Report report)
        {
            try
            {
                // Método CORRETO e ROBUSTO (igual ao usado no PixelCameraAutoSetup):
                // busca por t:ScriptableRendererData diretamente, sem filtrar por nome.
                string[] guids = AssetDatabase.FindAssets("t:ScriptableRendererData");

                int rendererAssets = 0;
                var foundIn = new List<string>();
                var missingList = new List<string>();

                // Primeiro verifica os Renderers ATIVOS (os que realmente importam)
                List<ScriptableRendererData> activeRenderers = GetActiveRendererDataList();
                bool activeRendererHasFeature = false;

                foreach (ScriptableRendererData active in activeRenderers)
                {
                    if (active == null) continue;

                    bool hasFeature = false;
                    foreach (var feat in active.rendererFeatures)
                    {
                        if (feat is PixelCameraRenderFeature)
                        {
                            hasFeature = true;
                            string p = AssetDatabase.GetAssetPath(active);
                            if (!foundIn.Contains(p)) foundIn.Add(p);
                            break;
                        }
                    }

                    if (hasFeature)
                        activeRendererHasFeature = true;
                    else
                        missingList.Add($"{active.name} (ATIVO)");
                }

                // Depois varre TODOS os Renderers do projeto (Assets + Packages)
                if (guids != null)
                {
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (string.IsNullOrEmpty(path))
                            continue;

                        // Inclui Assets E Packages (não só Assets/ como no código antigo)
                        if (!path.StartsWith("Assets") && !path.StartsWith("Packages"))
                            continue;

                        var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
                        if (rendererData == null)
                        {
                            // Fallback: tenta carregar como ScriptableObject genérico e ler via SerializedObject
                            var genericAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                            if (genericAsset != null && HasPixelCameraFeatureSerialized(genericAsset))
                            {
                                rendererAssets++;
                                foundIn.Add(path);
                            }
                            continue;
                        }

                        rendererAssets++;

                        // Usa a API PÚBLICA da URP (rendererFeatures), que é mais confiável que
                        // inspecionar m_RendererFeatures manualmente e já resolve sub-assets.
                        bool has = false;
                        if (rendererData.rendererFeatures != null)
                        {
                            foreach (var feature in rendererData.rendererFeatures)
                            {
                                if (feature is PixelCameraRenderFeature)
                                {
                                    has = true;
                                    break;
                                }
                            }
                        }

                        if (has)
                        {
                            if (!foundIn.Contains(path))
                                foundIn.Add(path);
                        }
                        else
                        {
                            // Evita duplicar na lista de faltantes se já marcamos como ATIVO
                            bool alreadyMarkedAsActive = false;
                            foreach (ScriptableRendererData active in activeRenderers)
                            {
                                if (active != null && AssetDatabase.GetAssetPath(active) == path)
                                {
                                    alreadyMarkedAsActive = true;
                                    break;
                                }
                            }
                            if (!alreadyMarkedAsActive)
                                missingList.Add($"{rendererData.name} ({path})");
                        }
                    }
                }

                if (rendererAssets == 0)
                {
                    report.Warning(
                        "Nenhum Renderer Asset do URP encontrado no projeto. Crie um em " +
                        "Assets > Create > Rendering > URP Asset (with Universal Renderer) para poder " +
                        "adicionar o \"Pixel Camera Render Feature\".");
                    return;
                }

                report.RendererTotalCount = rendererAssets;
                report.RendererConfiguredCount = foundIn.Count;
                report.RendererMissingFeatureCount = rendererAssets - foundIn.Count;

                if (activeRendererHasFeature)
                {
                    report.Ok($"Pixel Camera Render Feature configurado no Renderer ativo: " +
                        string.Join(", ", foundIn.Where(p => activeRenderers.Any(a => a != null && AssetDatabase.GetAssetPath(a) == p))));
                }
                else if (foundIn.Count > 0)
                {
                    // O Render Feature existe em algum Renderer, mas NÃO está no Renderer ativo da pipeline.
                    report.Warning(
                        $"O Pixel Camera Render Feature existe no projeto (em {foundIn.Count} de {rendererAssets} Renderers), " +
                        "mas NÃO está no Renderer que está ATIVO na pipeline atual.\n" +
                        "Os efeitos NÃO vão aparecer em jogo!\n\n" +
                        "Use Tools > Pixel Camera > Corrigir Automaticamente para adicionar ao Renderer ativo, ou:\n" +
                        "  1. Abra Project Settings > Graphics e veja qual Renderer List é usado pelo seu URP Asset\n" +
                        "  2. Selecione o Renderer correto e adicione o Pixel Camera Render Feature nele.");
                }
                else
                {
                    // Nenhum Renderer tem o Feature — é o caso do aviso do usuário.
                    string rendererList = rendererAssets == 3
                        ? $"Foram encontrados {rendererAssets} Renderer Assets"
                        : $"Foram encontrados {rendererAssets} Renderer Asset(s)";

                    report.Warning(
                        $"{rendererList}, mas nenhum tem o \"Pixel Camera Render Feature\".\n" +
                        "\n" +
                        "👉 SOLUÇÃO RÁPIDA: vá em Tools > Pixel Camera > Corrigir Automaticamente (adiciona sozinho).\n" +
                        "\n" +
                        "Ou manualmente:\n" +
                        "  1. Selecione o seu Renderer Asset (geralmente chamado \"PC_Renderer\" ou similar)\n" +
                        "  2. Clique em Add Renderer Feature > Pixel Camera Render Feature\n" +
                        "  3. Salve o projeto (Ctrl+S).\n" +
                        "\n" +
                        $"Renderers encontrados sem o Feature: {string.Join(", ", missingList.Take(5))}" +
                        (missingList.Count > 5 ? $", ... (+{missingList.Count - 5} outros)" : ""));
                }
            }
            catch (Exception e)
            {
                // Nunca deixa o diagnóstico quebrar o editor.
                Debug.Log($"[PixelCamera] Checagem de Renderer Assets ignorada: {e.Message}");
            }
        }

        /// <summary>
        /// Fallback: verifica a propriedade m_RendererFeatures via SerializedObject
        /// para casos em que o tipo ScriptableRendererData não carregou por alguma razão.
        /// </summary>
        private static bool HasPixelCameraFeatureSerialized(ScriptableObject asset)
        {
            try
            {
                var serialized = new SerializedObject(asset);
                SerializedProperty features = serialized.FindProperty("m_RendererFeatures");
                if (features == null || !features.isArray)
                    return false;

                for (int i = 0; i < features.arraySize; i++)
                {
                    var objRef = features.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (objRef is PixelCameraRenderFeature)
                        return true;
                }
            }
            catch
            {
                // ignorado
            }
            return false;
        }

        // ------------------------------------------------------------- Report

        /// <summary>
        /// Resultado do diagnóstico, em texto pronto para Console/dialog.
        /// </summary>
        public class Report
        {
            private readonly List<string> m_Ok = new List<string>();
            private readonly List<string> m_Warnings = new List<string>();
            private readonly List<string> m_Errors = new List<string>();

            public int RendererTotalCount { get; set; }
            public int RendererConfiguredCount { get; set; }
            public int RendererMissingFeatureCount { get; set; }

            public IReadOnlyList<string> OkItems => m_Ok;
            public IReadOnlyList<string> Warnings => m_Warnings;
            public IReadOnlyList<string> Errors => m_Errors;

            public bool HasErrors => m_Errors.Count > 0;
            public bool HasWarnings => m_Warnings.Count > 0;

            public void Ok(string message) => m_Ok.Add(message);
            public void Warning(string message) => m_Warnings.Add(message);
            public void Error(string message) => m_Errors.Add(message);

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine("[PixelCamera] Diagnóstico do projeto");

                Append(sb, m_Errors, "  ✖ ");
                Append(sb, m_Warnings, "  ⚠ ");
                Append(sb, m_Ok, "  ✔ ");

                return sb.ToString().TrimEnd();
            }

            private static void Append(StringBuilder sb, List<string> items, string prefix)
            {
                foreach (string item in items)
                {
                    sb.AppendLine(prefix + item.Replace("\n", "\n    "));
                }
            }
        }
    }
}
