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
        
        private void ImportFromTexture()
        {
            string path = EditorUtility.OpenFilePanel("Importar Textura", Application.dataPath, "png,jpg");
            
            if (string.IsNullOrEmpty(path)) return;
            
            byte[] fileData = System.IO.File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);
            
            var colorsProp = serializedObject.FindProperty("colors");
            int count = Mathf.Min(texture.width, 16);
            colorsProp.arraySize = count;
            
            for (int i = 0; i < count; i++)
            {
                colorsProp.GetArrayElementAtIndex(i).colorValue = texture.GetPixel(i, 0);
            }
            
            DestroyImmediate(texture);
            Debug.Log("[PalettePreset] Textura importada com sucesso!");
        }
        
        private void ExportAsPNG()
        {
            var colorsProp = serializedObject.FindProperty("colors");
            int count = Mathf.Min(colorsProp.arraySize, 16);
            
            var texture = new Texture2D(count, 1, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            
            for (int i = 0; i < count; i++)
            {
                texture.SetPixel(i, 0, colorsProp.GetArrayElementAtIndex(i).colorValue);
            }
            texture.Apply();
            
            string path = EditorUtility.SaveFilePanel("Exportar PNG", Application.dataPath, "palette", "png");
            
            if (!string.IsNullOrEmpty(path))
            {
                byte[] pngData = texture.EncodeToPNG();
                System.IO.File.WriteAllBytes(path, pngData);
                Debug.Log($"[PalettePreset] Exportado: {path}");
            }
            
            DestroyImmediate(texture);
        }
        
        private void GenerateRandom()
        {
            var colorsProp = serializedObject.FindProperty("colors");
            int count = Mathf.Min(colorsProp.arraySize, 16);
            
            for (int i = 0; i < count; i++)
            {
                colorsProp.GetArrayElementAtIndex(i).colorValue = Random.ColorHSV(0f, 1f, 0.3f, 1f, 0.3f, 1f);
            }
        }
        
        private void GenerateGradient()
        {
            var colorsProp = serializedObject.FindProperty("colors");
            int count = Mathf.Min(colorsProp.arraySize, 16);
            
            Color start = Random.ColorHSV(0f, 1f, 0.3f, 1f, 0.3f, 1f);
            Color end = Random.ColorHSV(0f, 1f, 0.3f, 1f, 0.3f, 1f);
            
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                colorsProp.GetArrayElementAtIndex(i).colorValue = Color.Lerp(start, end, t);
            }
        }
    }
}
