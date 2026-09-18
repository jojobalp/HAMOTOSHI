using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

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
    /// <remarks>
    /// Nada aqui referencia tipos do URP diretamente (só <c>UnityEngine.Rendering</c>, que é do
    /// core da Unity), então o diagnóstico continua útil mesmo quando o URP não está instalado.
    /// </remarks>
    public static class PixelCameraProjectDiagnostics
    {
        private const string UrpPackageName = "com.unity.render-pipelines.universal";
        private const string MenuPath = "Tools/Pixel Camera/Diagnóstico do Projeto (URP)";

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
            EditorUtility.DisplayDialog("Pixel Camera - Diagnóstico", report.ToString(), "OK");
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
                        if (tail[i] == '"')
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

        private static void CheckRenderFeatureInProject(Report report)
        {
            try
            {
                string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");

                if (guids == null || guids.Length == 0)
                {
                    return;
                }

                int rendererAssets = 0;
                var foundIn = new List<string>();

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets"))
                    {
                        continue;
                    }

                    // Filtro barato por nome para não carregar todos os ScriptableObjects do projeto.
                    string fileName = Path.GetFileNameWithoutExtension(path);

                    if (fileName.IndexOf("Renderer", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                    if (asset == null)
                    {
                        continue;
                    }

                    var serialized = new SerializedObject(asset);
                    SerializedProperty features = serialized.FindProperty("m_RendererFeatures");

                    // Só Renderer Assets do URP possuem "m_RendererFeatures".
                    if (features == null || !features.isArray)
                    {
                        continue;
                    }

                    rendererAssets++;

                    for (int i = 0; i < features.arraySize; i++)
                    {
                        if (features.GetArrayElementAtIndex(i).objectReferenceValue is PixelCameraRenderFeature)
                        {
                            foundIn.Add(path);
                            break;
                        }
                    }
                }

                if (rendererAssets == 0)
                {
                    report.Warning(
                        "Nenhum Renderer Asset do URP encontrado em Assets/. Crie um em " +
                        "Assets > Create > Rendering > URP Asset (with Universal Renderer) para poder " +
                        "adicionar o \"Pixel Camera Render Feature\".");
                    return;
                }

                if (foundIn.Count == 0)
                {
                    report.Warning(
                        $"Foram encontrados {rendererAssets} Renderer Asset(s), mas nenhum tem o " +
                        "\"Pixel Camera Render Feature\".\n" +
                        "Selecione o seu Renderer > Add Renderer Feature > Pixel Camera Render Feature.");
                }
                else
                {
                    report.Ok("Pixel Camera Render Feature configurado em: " + string.Join(", ", foundIn));
                }
            }
            catch (Exception e)
            {
                // Nunca deixa o diagnóstico quebrar o editor.
                Debug.Log($"[PixelCamera] Checagem de Renderer Assets ignorada: {e.Message}");
            }
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
