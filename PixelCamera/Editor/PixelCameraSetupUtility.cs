using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Este arquivo agora usa tipos da URP — o asmdef do Editor já referencia
// Unity.RenderPipelines.Universal.Runtime, então tudo compila quando o URP está instalado.

namespace PixelCamera.Editor
{
    /// <summary>
    /// Utility para setup rápido do Pixel Camera.
    /// </summary>
    public static class PixelCameraSetupUtility
    {
        [MenuItem("Tools/Pixel Camera/Setup na Câmera Atual")]
        public static void SetupOnCurrentCamera()
        {
            // Object.FindObjectOfType ficou obsoleto no Unity 2023+/Unity 6 (aviso CS0618).
#if UNITY_2022_2_OR_NEWER
            var camera = Camera.current ?? Object.FindFirstObjectByType<Camera>();
#else
            var camera = Camera.current ?? Object.FindObjectOfType<Camera>();
#endif

            if (camera == null)
            {
                EditorUtility.DisplayDialog("Erro", "Nenhuma câmera encontrada na cena!", "OK");
                return;
            }

            // Garante que o Render Feature está configurado na pipeline (auto-fix)
            bool rendererOk = EnsurePixelCameraRenderFeatureExists();

            // Adiciona o componente PixelCameraAutoSetup (mais completo que só o Controller)
            var autoSetup = camera.GetComponent<PixelCameraAutoSetup>();
            if (autoSetup == null)
            {
                autoSetup = camera.gameObject.AddComponent<PixelCameraAutoSetup>();
            }

            // Adiciona o PixelCameraController também
            if (camera.GetComponent<PixelCameraController>() == null)
            {
                camera.gameObject.AddComponent<PixelCameraController>();
            }

            // Tenta preencher automaticamente o campo "Renderer Asset" com o Renderer ativo da pipeline
            AssignActiveRendererToAutoSetup(autoSetup);

            // Seleciona a câmera
            Selection.activeGameObject = camera.gameObject;

            Debug.Log($"[PixelCamera] Setup completo na câmera: {camera.gameObject.name}");

            string msg = "✅ Configuração aplicada!\n\n";
            if (rendererOk)
            {
                msg += "• PixelCameraRenderFeature configurado no Renderer ativo\n";
            }
            else
            {
                msg += "⚠️  Verifique o Console para instruções de configuração do Renderer\n";
            }
            msg += "• PixelCameraAutoSetup adicionado à câmera\n";
            msg += "• Aperte Play — o efeito é configurado automaticamente!\n\n";
            msg += "💡 Dica: ajuste resolução, paleta e CRT no Inspector do PixelCameraAutoSetup.";

            EditorUtility.DisplayDialog("Pixel Camera - Sucesso!", msg, "OK");
        }

        [MenuItem("Tools/Pixel Camera/Abrir Palette Editor")]
        public static void OpenPaletteEditor()
        {
            PaletteEditorWindow.ShowWindow();
        }

        [MenuItem("Tools/Pixel Camera/Gerar Textura Preview 320x180")]
        public static void GeneratePreviewTexture()
        {
            var texture = new Texture2D(320, 180, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            // Preencher com cores de teste
            for (int y = 0; y < 180; y++)
            {
                for (int x = 0; x < 320; x++)
                {
                    float u = (float)x / 320f;
                    float v = (float)y / 180f;

                    Color color;

                    // Checkerboard pattern
                    int checkX = x / 16;
                    int checkY = y / 16;
                    bool isCheck = (checkX + checkY) % 2 == 0;

                    if (isCheck)
                    {
                        color = new Color(u, v, 0.5f);
                    }
                    else
                    {
                        color = new Color(1f - u, 1f - v, u * v);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();

            // Salvar
            string path = "Assets/PixelCameraPreview.png";
            byte[] pngData = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, pngData);

            Object.DestroyImmediate(texture);
            AssetDatabase.Refresh();

            Debug.Log($"[PixelCamera] Preview gerado: {path}");
        }

        [MenuItem("Tools/Pixel Camera/Documentação")]
        public static void OpenDocumentation()
        {
            EditorUtility.DisplayDialog("Pixel Camera",
                "📖 Documentação completa em:\n" +
                "PixelCamera/README.md\n\n" +
                "🎮 Atalhos do Editor:\n" +
                "• Tools > Pixel Camera > Setup na Câmera Atual\n" +
                "• Tools > Pixel Camera > Corrigir Automaticamente (adiciona Render Feature)\n" +
                "• Tools > Pixel Camera > Diagnóstico do Projeto (URP)\n" +
                "• Tools > Pixel Camera > Palette Editor\n" +
                "• Tools > Pixel Camera > Gerar Preview\n\n" +
                "⌨️ Atalhos Runtime (com Controller):\n" +
                "• F1: Toggle Geral\n" +
                "• F2: Toggle CRT\n" +
                "• F3: Toggle Dithering\n" +
                "• F4: Toggle Paleta\n" +
                "• Scroll: Ajustar Resolução",
                "OK");
        }

        // ---------------------------------------------------------------- Auto-correção

        /// <summary>
        /// Garante que o Renderer ativo da pipeline tem o PixelCameraRenderFeature.
        /// Retorna true se já estava ok ou se foi adicionado com sucesso.
        /// </summary>
        private static bool EnsurePixelCameraRenderFeatureExists()
        {
            List<ScriptableRendererData> activeRenderers = GetActiveRenderers();
            bool anyMissing = false;

            foreach (var rd in activeRenderers)
            {
                if (rd == null) continue;
                bool has = false;
                if (rd.rendererFeatures != null)
                {
                    foreach (var f in rd.rendererFeatures)
                    {
                        if (f is PixelCameraRenderFeature) { has = true; break; }
                    }
                }
                if (!has) anyMissing = true;
            }

            if (!anyMissing && activeRenderers.Count > 0)
                return true;

            // Delega para o auto-fix do diagnóstico
            return PixelCameraProjectDiagnostics.AutoAddRenderFeatureToActiveRenderer();
        }

        private static List<ScriptableRendererData> GetActiveRenderers()
        {
            var list = new List<ScriptableRendererData>();
            CollectFromPipeline(GraphicsSettings.currentRenderPipeline, list);
            CollectFromPipeline(QualitySettings.renderPipeline, list);
            return list;
        }

        private static void CollectFromPipeline(RenderPipelineAsset pipeline, List<ScriptableRendererData> list)
        {
            if (pipeline == null) return;
            try
            {
                var so = new SerializedObject(pipeline);
                SerializedProperty prop = so.FindProperty("m_RendererDataList");
                if (prop == null || !prop.isArray)
                {
                    // Fallback URP mais antiga
                    SerializedProperty defaultProp = so.FindProperty("m_RendererData");
                    if (defaultProp?.objectReferenceValue is ScriptableRendererData rd && !list.Contains(rd))
                        list.Add(rd);
                    return;
                }
                for (int i = 0; i < prop.arraySize; i++)
                {
                    var elem = prop.GetArrayElementAtIndex(i);
                    if (elem?.objectReferenceValue is ScriptableRendererData rd && !list.Contains(rd))
                        list.Add(rd);
                }
            }
            catch
            {
                // ignorado
            }
        }

        private static void AssignActiveRendererToAutoSetup(PixelCameraAutoSetup autoSetup)
        {
            if (autoSetup == null) return;
            if (autoSetup.RendererAsset != null) return; // já tem um atribuído

            List<ScriptableRendererData> activeRenderers = GetActiveRenderers();
            if (activeRenderers.Count > 0 && activeRenderers[0] != null)
            {
                var so = new SerializedObject(autoSetup);
                SerializedProperty prop = so.FindProperty("rendererAsset");
                if (prop != null)
                {
                    prop.objectReferenceValue = activeRenderers[0];
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(autoSetup);
                    Debug.Log($"[PixelCamera] Renderer \"{activeRenderers[0].name}\" atribuído automaticamente ao PixelCameraAutoSetup.");
                }
            }
        }
    }
}
