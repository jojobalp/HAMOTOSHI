using UnityEditor;
using UnityEngine;

namespace PixelCamera.Editor
{
    /// <summary>
    /// Editor customizado para PalettePresetAsset
    /// </summary>
    [CustomEditor(typeof(PalettePresetAsset))]
    public class PalettePresetAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var nameProp = serializedObject.FindProperty("paletteName");
            var descProp = serializedObject.FindProperty("description");
            var colorsProp = serializedObject.FindProperty("colors");
            
            // Nome
            EditorGUILayout.LabelField("🎨 Palette Preset", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(nameProp, new GUIContent("Nome"));
            EditorGUILayout.PropertyField(descProp, new GUIContent("Descrição"));
            
            EditorGUILayout.Space(10);
            
            // Cores
            EditorGUILayout.LabelField("Cores", EditorStyles.boldLabel);
            
            int arraySize = Mathf.Min(colorsProp.arraySize, 16);
            int columns = 8;
            
            for (int row = 0; row < Mathf.CeilToInt(arraySize / (float)columns); row++)
            {
                EditorGUILayout.BeginHorizontal();
                
                for (int col = 0; col < columns; col++)
                {
                    int index = row * columns + col;
                    if (index >= arraySize) break;
                    
                    var element = colorsProp.GetArrayElementAtIndex(index);
                    EditorGUILayout.PropertyField(element, GUIContent.none, GUILayout.Width(40));
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(10);
            
            // Preview
            DrawPreview();
            
            EditorGUILayout.Space(10);
            
            // Botões
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("📷 Importar de Texture"))
            {
                ImportFromTexture();
            }
            
            if (GUILayout.Button("💾 Exportar como PNG"))
            {
                ExportAsPNG();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🎲 Gerar Aleatória"))
            {
                GenerateRandom();
            }
            
            if (GUILayout.Button("🌈 Gradiente"))
            {
                GenerateGradient();
            }
            
            EditorGUILayout.EndHorizontal();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawPreview()
        {
            var colorsProp = serializedObject.FindProperty("colors");
            int count = Mathf.Min(colorsProp.arraySize, 16);
            
            float previewWidth = Mathf.Min(EditorGUIUtility.currentViewWidth - 40, 460);
            float previewHeight = 50;
            Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
            
            float colorWidth = previewWidth / count;
            
            for (int i = 0; i < count; i++)
            {
                var element = colorsProp.GetArrayElementAtIndex(i);
                Color color = element.colorValue;
                
                Rect colorRect = new Rect(
                    previewRect.x + i * colorWidth,
                    previewRect.y,
                    colorWidth + 1,
                    previewHeight
                );
                EditorGUI.DrawRect(colorRect, color);
            }
        }
        
        /// <summary>
        /// Importa as cores de um PNG/JPEG para o preset.
        /// </summary>
        /// <remarks>
        /// Mantém o array com <b>16 slots</b> e preenche os excedentes repetindo
        /// as cores importadas. Antes o array era truncado para a largura da
        /// imagem, o que fazia <c>ToTexture()</c> gerar slots vazios (pretos) que
        /// corrompiam a busca de cor mais próxima no shader.
        /// </remarks>
        private void ImportFromTexture()
        {
            // LoadImage decodifica apenas PNG e JPEG.
            string path = EditorUtility.OpenFilePanel("Importar Textura", Application.dataPath, "png,jpg,jpeg");
            
            if (string.IsNullOrEmpty(path)) return;
            
            byte[] fileData = System.IO.File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (!texture.LoadImage(fileData))
            {
                EditorUtility.DisplayDialog(
                    "Formato não suportado",
                    "Não foi possível decodificar o arquivo.\n\nFormatos suportados: PNG e JPEG.",
                    "OK");
                DestroyImmediate(texture);
                return;
            }
            
            var colorsProp = serializedObject.FindProperty("colors");
            int count = Mathf.Clamp(texture.width, 1, PalettePresetAsset.MaxColors);
            colorsProp.arraySize = PalettePresetAsset.MaxColors;
            
            for (int i = 0; i < colorsProp.arraySize; i++)
            {
                // Repete ciclicamente quando a imagem tem menos cores que 16.
                Color c = texture.GetPixel(i % count, 0);
                c.a = 1.0f;
                colorsProp.GetArrayElementAtIndex(i).colorValue = c;
            }
            
            DestroyImmediate(texture);
            Debug.Log($"[PalettePreset] Textura importada com sucesso ({count} cores, 16 slots preenchidos)!");
        }
        
        /// <summary>
        /// Exporta o preset como PNG 16x1, no formato que o shader espera.
        /// </summary>
        private void ExportAsPNG()
        {
            var colorsProp = serializedObject.FindProperty("colors");
            int count = Mathf.Clamp(colorsProp.arraySize, 1, PalettePresetAsset.MaxColors);

            var colors = new System.Collections.Generic.List<Color>(count);
            for (int i = 0; i < count; i++)
            {
                colors.Add(colorsProp.GetArrayElementAtIndex(i).colorValue);
            }

            // 16x1, Point, Clamp e slots preenchidos por repetição — o arquivo
            // pode ser arrastado direto para o campo "Custom Palette".
            var texture = PaletteUtility.CreatePaletteTexture(colors);
            
            string path = EditorUtility.SaveFilePanel("Exportar PNG", Application.dataPath, "palette", "png");
            
            if (!string.IsNullOrEmpty(path))
            {
                byte[] pngData = texture.EncodeToPNG();
                System.IO.File.WriteAllBytes(path, pngData);
                Debug.Log($"[PalettePreset] Exportado ({PaletteUtility.PaletteTextureWidth}x1): {path}");
                AssetDatabase.Refresh();
            }
            
            DestroyImmediate(texture);
        }
        
        private void GenerateRandom()
        {
            var colorsProp = serializedObject.FindProperty("colors");
            // Preenche os 16 slots: deixar slots em preto corrompe a busca de cor
            // mais próxima no shader.
            colorsProp.arraySize = PalettePresetAsset.MaxColors;

            for (int i = 0; i < colorsProp.arraySize; i++)
            {
                colorsProp.GetArrayElementAtIndex(i).colorValue = Random.ColorHSV(0f, 1f, 0.3f, 1f, 0.3f, 1f);
            }
        }
        
        private void GenerateGradient()
        {
            var colorsProp = serializedObject.FindProperty("colors");
            colorsProp.arraySize = PalettePresetAsset.MaxColors;
            int count = colorsProp.arraySize;
            
            Color start = Random.ColorHSV(0f, 1f, 0.3f, 1f, 0.3f, 1f);
            Color end = Random.ColorHSV(0f, 1f, 0.3f, 1f, 0.3f, 1f);
            
            for (int i = 0; i < count; i++)
            {
                // Mathf.Max evita divisão por zero quando count == 1.
                float t = i / (float)Mathf.Max(1, count - 1);
                colorsProp.GetArrayElementAtIndex(i).colorValue = Color.Lerp(start, end, t);
            }
        }
    }
}
