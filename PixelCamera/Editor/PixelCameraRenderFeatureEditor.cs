using UnityEditor;
using UnityEngine;

namespace PixelCamera.Editor
{
    /// <summary>
    /// Editor customizado para PixelCameraRenderFeature
    /// </summary>
    /// <remarks>
    /// A base é escrita como <c>UnityEditor.Editor</c> (totalmente qualificada) de propósito:
    /// como estamos dentro do namespace <c>PixelCamera.Editor</c>, o identificador simples
    /// <c>Editor</c> é resolvido primeiro para o próprio namespace (membros do namespace
    /// enclosing têm prioridade sobre tipos importados por <c>using</c>), o que gera o erro
    /// CS0118 "'Editor' is a namespace but is used like a type".
    /// </remarks>
    [CustomEditor(typeof(PixelCameraRenderFeature))]
    public class PixelCameraRenderFeatureEditor : UnityEditor.Editor
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
                    DrawDitherTypeField(ditherSettings.FindPropertyRelative("ditherType"));
                    EditorGUILayout.PropertyField(ditherSettings.FindPropertyRelative("intensity"), new GUIContent("Intensidade"));
                    
                    EditorGUILayout.HelpBox(
                        "Tipos de Dithering (Bayer ordenado):\n" +
                        "• Bayer 2x2: Padrão simples, pixels grandes\n" +
                        "• Bayer 4x4: Equilibrado, padrão médio\n" +
                        "• Bayer 8x8: Suave, padrão fino\n\n" +
                        "O threshold é aplicado ANTES da quantização/lookup da paleta, então o\n" +
                        "dithering alterna entre cores reais da paleta.\n\n" +
                        "Floyd-Steinberg (difusão de erro) não está implementado — requer\n" +
                        "múltiplos passes (roadmap 1.2.0).",
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

        /// <summary>
        /// Desenha o campo de tipo de dithering como um popup explícito.
        /// </summary>
        /// <remarks>
        /// <c>EditorGUILayout.PropertyField</c> num enum mostra <b>todos</b> os
        /// nomes da <c>Enum</c> subjacente. Como <c>FloydSteinberg</c> foi
        /// removido do <c>DitherType</c>, um asset antigo que tinha esse valor
        /// selecionado guardaria o inteiro órfão 3 e o popup exibiria
        /// "FloydSteinberg" mesmo sem ele existir — além de não aplicar dithering
        /// nenhum. Aqui o valor é validado e normalizado para Bayer 4x4.
        /// </remarks>
        private static void DrawDitherTypeField(SerializedProperty ditherTypeProp)
        {
            var names = System.Enum.GetNames(typeof(PixelCameraRenderFeature.DitherType));
            int current = ditherTypeProp.enumValueFlag;

            if (current < 0 || current >= names.Length)
            {
                EditorGUILayout.HelpBox(
                    "Este asset tinha um tipo de dithering que não existe mais nesta versão " +
                    "(provavelmente Floyd-Steinberg, que nunca foi implementado). " +
                    "O valor foi normalizado para Bayer 4x4.",
                    MessageType.Warning);

                ditherTypeProp.enumValueFlag = (int)PixelCameraRenderFeature.DitherType.Bayer4x4;
                current = ditherTypeProp.enumValueFlag;
            }

            int selected = EditorGUILayout.Popup(new GUIContent("Tipo"), current, names);
            if (selected != current)
            {
                ditherTypeProp.enumValueFlag = selected;
            }
        }

        /// <summary>
        /// Gera uma paleta aleatória e a salva como asset no projeto.
        /// </summary>
        /// <remarks>
        /// Antes isto criava um <c>Texture2D</c> apenas em memória e o atribuía a
        /// um campo serializado. Uma textura sem existência em disco não
        /// sobrevive a domain reload / salvar a cena, então a referência se
        /// perdia (o Inspector ficava com "None"). Agora a paleta é gravada como
        /// asset via <c>AssetDatabase.CreateAsset</c>.
        /// </remarks>
        private void GenerateRandomPalette()
        {
            var paletteSettings = serializedObject.FindProperty("paletteSettings");
            int colorCount = Mathf.Clamp(paletteSettings.FindPropertyRelative("colorCount").intValue, 1, 16);

            // Passa pelo PaletteUtility para manter o formato que o shader
            // espera (16x1, Point, Clamp, 16 slots preenchidos, cores em HSV).
            var palette = PaletteUtility.CreateRandomPalette(colorCount);

            string folder = EditorUtility.SaveFolderPanel(
                "Onde salvar a paleta aleatória", Application.dataPath, "");
            if (string.IsNullOrEmpty(folder))
            {
                DestroyImmediate(palette);
                return;
            }

            if (!folder.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog(
                    "Fora do projeto",
                    "Escolha uma pasta dentro de 'Assets/' para que a paleta possa ser salva como asset do Unity.",
                    "OK");
                DestroyImmediate(palette);
                return;
            }

            // Converte o caminho absoluto em caminho relativo a Assets/
            string relativeFolder = "Assets" + folder.Substring(Application.dataPath.Length);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(relativeFolder + "/RandomPalette.asset");

            AssetDatabase.CreateAsset(palette, assetPath);
            AssetDatabase.SaveAssets();

            paletteSettings.FindPropertyRelative("customPalette").objectReferenceValue = palette;

            Debug.Log("[PixelCamera] Paleta aleatória gerada em: " + assetPath);
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
            paletteSettings.FindPropertyRelative("preset").enumValueFlag = (int)PixelCameraRenderFeature.PalettePreset.PICO8;
            
            var ditherSettings = serializedObject.FindProperty("ditherSettings");
            ditherSettings.FindPropertyRelative("enableDithering").boolValue = true;
            ditherSettings.FindPropertyRelative("ditherType").enumValueFlag = (int)PixelCameraRenderFeature.DitherType.Bayer4x4;
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
            paletteSettings.FindPropertyRelative("preset").enumValueFlag = (int)PixelCameraRenderFeature.PalettePreset.GameBoy;
            
            var ditherSettings = serializedObject.FindProperty("ditherSettings");
            ditherSettings.FindPropertyRelative("enableDithering").boolValue = true;
            ditherSettings.FindPropertyRelative("ditherType").enumValueFlag = (int)PixelCameraRenderFeature.DitherType.Bayer2x2;
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
            paletteSettings.FindPropertyRelative("preset").enumValueFlag = (int)PixelCameraRenderFeature.PalettePreset.NES;
            
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
