using UnityEditor;
using UnityEngine;

namespace PixelCamera.Editor
{
    /// <summary>
    /// Editor customizado para PixelCameraRenderFeature
    /// </summary>
    [CustomEditor(typeof(PixelCameraRenderFeature))]
    public class PixelCameraRenderFeatureEditor : Editor
    {
        private bool showPixelSettings = true;
        private bool showPaletteSettings = true;
        private bool showDitherSettings = false;
        private bool showCRTSettings = false;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("🎮 Pixel Camera", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Enabled toggle
            var enabledProp = serializedObject.FindProperty("enabled");
            EditorGUILayout.PropertyField(enabledProp, new GUIContent("Habilitado"));
            
            if (!enabledProp.boolValue)
            {
                EditorGUILayout.HelpBox("Render Feature está desabilitado. Habilite para aplicar efeitos.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Space();

            // Pixel Settings
            showPixelSettings = EditorGUILayout.Foldout(showPixelSettings, "📐 Pixelização", true, EditorStyles.foldoutHeader);
            if (showPixelSettings)
            {
                EditorGUI.indentLevel++;
                var pixelSettings = serializedObject.FindProperty("pixelSettings");
                EditorGUILayout.PropertyField(pixelSettings.FindPropertyRelative("pixelWidth"), new GUIContent("Largura (pixels)"));
                EditorGUILayout.PropertyField(pixelSettings.FindPropertyRelative("pixelHeight"), new GUIContent("Altura (pixels)"));
                EditorGUILayout.PropertyField(pixelSettings.FindPropertyRelative("snapToPixelGrid"), new GUIContent("Snap to Pixel Grid"));
                
                if (pixelSettings.FindPropertyRelative("pixelWidth").intValue == 0)
                {
                    EditorGUILayout.HelpBox("Largura 0 usa resolução nativa da câmera", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Palette Settings
            showPaletteSettings = EditorGUILayout.Foldout(showPaletteSettings, "🎨 Paleta de Cores", true, EditorStyles.foldoutHeader);
            if (showPaletteSettings)
            {
                EditorGUI.indentLevel++;
                var paletteSettings = serializedObject.FindProperty("paletteSettings");
                EditorGUILayout.PropertyField(paletteSettings.FindPropertyRelative("enablePalette"), new GUIContent("Habilitar Paleta"));
                
                if (paletteSettings.FindPropertyRelative("enablePalette").boolValue)
                {
                    EditorGUILayout.PropertyField(paletteSettings.FindPropertyRelative("preset"), new GUIContent("Preset"));
                    EditorGUILayout.PropertyField(paletteSettings.FindPropertyRelative("customPalette"), new GUIContent("Paleta Custom (Texture)"));
                    EditorGUILayout.PropertyField(paletteSettings.FindPropertyRelative("colorCount"), new GUIContent("Quantidade de Cores"));
                    EditorGUILayout.PropertyField(paletteSettings.FindPropertyRelative("colorQuantization"), new GUIContent("Quantização"));
                    
                    EditorGUILayout.Space();
                    
                    if (GUILayout.Button("🎲 Gerar Paleta Aleatória"))
                    {
                        GenerateRandomPalette();
                    }
                    
                    if (GUILayout.Button("📷 Importar Paleta de Texture"))
                    {
                        ImportPaletteFromTexture();
                    }
                    
                    EditorGUILayout.HelpBox(
                        "Presets disponíveis:\n" +
                        "• GameBoy (4 tons de verde)\n" +
                        "• NES (16 cores clássicas)\n" +
                        "• CGA (4 cores)\n" +
                        "• PICO-8 (16 cores)\n" +
                        "• GB Color (16 cores)\n" +
                        "• Grayscale (16 tons)\n" +
                        "• Binary (2 cores)",
                        MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Dither Settings
            showDitherSettings = EditorGUILayout.Foldout(showDitherSettings, "🔲 Dithering", true, EditorStyles.foldoutHeader);
            if (showDitherSettings)
            {
                EditorGUI.indentLevel++;
                var ditherSettings = serializedObject.FindProperty("ditherSettings");
                EditorGUILayout.PropertyField(ditherSettings.FindPropertyRelative("enableDithering"), new GUIContent("Habilitar Dithering"));
                
                if (ditherSettings.FindPropertyRelative("enableDithering").boolValue)
                {
                    EditorGUILayout.PropertyField(ditherSettings.FindPropertyRelative("ditherType"), new GUIContent("Tipo"));
                    EditorGUILayout.PropertyField(ditherSettings.FindPropertyRelative("intensity"), new GUIContent("Intensidade"));
                    
                    EditorGUILayout.HelpBox(
                        "Tipos de Dithering:\n" +
                        "• Bayer 2x2: Padrão simples, pixels grandes\n" +
                        "• Bayer 4x4: Equilibrado, padrão médio\n" +
                        "• Bayer 8x8: Suave, padrão fino\n" +
                        "• Floyd-Steinberg: Diffusion (requer múltiplos passes)",
                        MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // CRT Settings
            showCRTSettings = EditorGUILayout.Foldout(showCRTSettings, "📺 Efeito CRT", true, EditorStyles.foldoutHeader);
            if (showCRTSettings)
            {
                EditorGUI.indentLevel++;
                var crtSettings = serializedObject.FindProperty("crtSettings");
                EditorGUILayout.PropertyField(crtSettings.FindPropertyRelative("enableCRT"), new GUIContent("Habilitar CRT"));
                
                if (crtSettings.FindPropertyRelative("enableCRT").boolValue)
                {
                    EditorGUILayout.LabelField("Scanlines", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(crtSettings.FindPropertyRelative("scanlineIntensity"), new GUIContent("Intensidade"));
                    EditorGUILayout.PropertyField(crtSettings.FindPropertyRelative("scanlineThickness"), new GUIContent("Espessura"));
                    
                    EditorGUILayout.Space();
                    
                    EditorGUILayout.LabelField("Bloom/Glow", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(crtSettings.FindPropertyRelative("bloomIntensity"), new GUIContent("Intensidade"));
                    EditorGUILayout.PropertyField(crtSettings.FindPropertyRelative("bloomRadius"), new GUIContent("Raio"));
                    
                    EditorGUILayout.Space();
                    
                    EditorGUILayout.LabelField("Curvatura", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(crtSettings.FindPropertyRelative("curvatureIntensity"), new GUIContent("Intensidade"));
                    
                    EditorGUILayout.Space();
                    
                    EditorGUILayout.LabelField("Vinheta", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(crtSettings.FindPropertyRelative("vignetteIntensity"), new GUIContent("Intensidade"));
                    
                    EditorGUILayout.Space();
                    
                    if (GUILayout.Button("🔄 Resetar CRT"))
                    {
                        ResetCRTSettings();
                    }
                    
                    EditorGUILayout.HelpBox(
                        "Controle cada aspecto do efeito CRT separadamente:\n" +
                        "• Scanlines: Linhas horizontais clássicas\n" +
                        "• Bloom: Glow suave nas bordas\n" +
                        "• Curvatura: Distorção da tela\n" +
                        "• Vinheta: Escurecimento das bordas",
                        MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Render Pass Event
            EditorGUILayout.LabelField("Render Pass", EditorStyles.boldLabel);
            var renderPassEvent = serializedObject.FindProperty("renderPassEvent");
            EditorGUILayout.PropertyField(renderPassEvent, new GUIContent("Evento"));

            EditorGUILayout.Space();

            // Quick presets
            EditorGUILayout.LabelField("Presets Rápidos", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🎮 Retrô Completo"))
            {
                ApplyPreset_Retro();
            }
            if (GUILayout.Button("👾 GameBoy"))
            {
                ApplyPreset_GameBoy();
            }
            if (GUILayout.Button("📺 CRT Clássico"))
            {
                ApplyPreset_CRT();
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void GenerateRandomPalette()
        {
            var paletteSettings = serializedObject.FindProperty("paletteSettings");
            var colorCount = paletteSettings.FindPropertyRelative("colorCount").intValue;
            
            var palette = new Texture2D(16, 1, TextureFormat.RGBA32, false);
            palette.filterMode = FilterMode.Point;
            
            for (int i = 0; i < Mathf.Min(colorCount, 16); i++)
            {
                Color randomColor = new Color(
                    Random.value,
                    Random.value,
                    Random.value,
                    1.0f
                );
                palette.SetPixel(i, 0, randomColor);
            }
            
            palette.Apply();
            paletteSettings.FindPropertyRelative("customPalette").objectReferenceValue = palette;
            
            Debug.Log("[PixelCamera] Paleta aleatória gerada!");
        }

        private void ImportPaletteFromTexture()
        {
            string path = EditorUtility.OpenFilePanel("Selecionar Texture de Paleta", "", "png,jpg");
            
            if (string.IsNullOrEmpty(path)) return;
            
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            
            if (texture == null)
            {
                EditorUtility.DisplayDialog("Erro", "Não foi possível carregar a textura. Verifique se o arquivo é válido.", "OK");
                return;
            }
            
            var paletteSettings = serializedObject.FindProperty("paletteSettings");
            paletteSettings.FindPropertyRelative("customPalette").objectReferenceValue = texture;
            
            Debug.Log($"[PixelCamera] Paleta importada: {path}");
        }

        private void ResetCRTSettings()
        {
            var crtSettings = serializedObject.FindProperty("crtSettings");
            crtSettings.FindPropertyRelative("scanlineIntensity").floatValue = 0.3f;
            crtSettings.FindPropertyRelative("scanlineThickness").floatValue = 2.0f;
            crtSettings.FindPropertyRelative("bloomIntensity").floatValue = 0.2f;
            crtSettings.FindPropertyRelative("bloomRadius").floatValue = 3.0f;
            crtSettings.FindPropertyRelative("curvatureIntensity").floatValue = 0.1f;
            crtSettings.FindPropertyRelative("vignetteIntensity").floatValue = 0.3f;
        }

        private void ApplyPreset_Retro()
        {
            var pixelSettings = serializedObject.FindProperty("pixelSettings");
            pixelSettings.FindPropertyRelative("pixelWidth").intValue = 320;
            pixelSettings.FindPropertyRelative("pixelHeight").intValue = 180;
            pixelSettings.FindPropertyRelative("snapToPixelGrid").boolValue = true;
            
            var paletteSettings = serializedObject.FindProperty("paletteSettings");
            paletteSettings.FindPropertyRelative("enablePalette").boolValue = true;
            paletteSettings.FindPropertyRelative("preset").enumValueIndex = (int)PixelCameraRenderFeature.PalettePreset.PICO8;
            
            var ditherSettings = serializedObject.FindProperty("ditherSettings");
            ditherSettings.FindPropertyRelative("enableDithering").boolValue = true;
            ditherSettings.FindPropertyRelative("ditherType").enumValueIndex = (int)PixelCameraRenderFeature.DitherType.Bayer4x4;
            ditherSettings.FindPropertyRelative("intensity").floatValue = 0.3f;
            
            var crtSettings = serializedObject.FindProperty("crtSettings");
            crtSettings.FindPropertyRelative("enableCRT").boolValue = true;
            crtSettings.FindPropertyRelative("scanlineIntensity").floatValue = 0.2f;
            crtSettings.FindPropertyRelative("bloomIntensity").floatValue = 0.1f;
            crtSettings.FindPropertyRelative("curvatureIntensity").floatValue = 0.05f;
            crtSettings.FindPropertyRelative("vignetteIntensity").floatValue = 0.2f;
        }

        private void ApplyPreset_GameBoy()
        {
            var pixelSettings = serializedObject.FindProperty("pixelSettings");
            pixelSettings.FindPropertyRelative("pixelWidth").intValue = 160;
            pixelSettings.FindPropertyRelative("pixelHeight").intValue = 144;
            pixelSettings.FindPropertyRelative("snapToPixelGrid").boolValue = true;
            
            var paletteSettings = serializedObject.FindProperty("paletteSettings");
            paletteSettings.FindPropertyRelative("enablePalette").boolValue = true;
            paletteSettings.FindPropertyRelative("preset").enumValueIndex = (int)PixelCameraRenderFeature.PalettePreset.GameBoy;
            
            var ditherSettings = serializedObject.FindProperty("ditherSettings");
            ditherSettings.FindPropertyRelative("enableDithering").boolValue = true;
            ditherSettings.FindPropertyRelative("ditherType").enumValueIndex = (int)PixelCameraRenderFeature.DitherType.Bayer2x2;
            ditherSettings.FindPropertyRelative("intensity").floatValue = 0.5f;
            
            var crtSettings = serializedObject.FindProperty("crtSettings");
            crtSettings.FindPropertyRelative("enableCRT").boolValue = false;
        }

        private void ApplyPreset_CRT()
        {
            var pixelSettings = serializedObject.FindProperty("pixelSettings");
            pixelSettings.FindPropertyRelative("pixelWidth").intValue = 640;
            pixelSettings.FindPropertyRelative("pixelHeight").intValue = 360;
            pixelSettings.FindPropertyRelative("snapToPixelGrid").boolValue = true;
            
            var paletteSettings = serializedObject.FindProperty("paletteSettings");
            paletteSettings.FindPropertyRelative("enablePalette").boolValue = true;
            paletteSettings.FindPropertyRelative("preset").enumValueIndex = (int)PixelCameraRenderFeature.PalettePreset.NES;
            
            var ditherSettings = serializedObject.FindProperty("ditherSettings");
            ditherSettings.FindPropertyRelative("enableDithering").boolValue = false;
            
            var crtSettings = serializedObject.FindProperty("crtSettings");
            crtSettings.FindPropertyRelative("enableCRT").boolValue = true;
            crtSettings.FindPropertyRelative("scanlineIntensity").floatValue = 0.4f;
            crtSettings.FindPropertyRelative("scanlineThickness").floatValue = 3.0f;
            crtSettings.FindPropertyRelative("bloomIntensity").floatValue = 0.3f;
            crtSettings.FindPropertyRelative("bloomRadius").floatValue = 5.0f;
            crtSettings.FindPropertyRelative("curvatureIntensity").floatValue = 0.15f;
            crtSettings.FindPropertyRelative("vignetteIntensity").floatValue = 0.4f;
        }
    }
}
