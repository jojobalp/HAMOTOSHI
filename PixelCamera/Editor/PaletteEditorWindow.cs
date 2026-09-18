using UnityEditor;
using UnityEngine;

namespace PixelCamera.Editor
{
    /// <summary>
    /// Editor Window para criação e gestão de paletas
    /// </summary>
    public class PaletteEditorWindow : EditorWindow
    {
        private Texture2D previewTexture;
        private Color[] paletteColors = new Color[16];
        private int selectedColorIndex = 0;
        private int paletteSize = 16;
        private bool showGradientTool = false;
        private Color gradientStart = Color.black;
        private Color gradientEnd = Color.white;

        [MenuItem("Tools/Pixel Camera/Palette Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<PaletteEditorWindow>("Palette Editor");
            window.minSize = new Vector2(500, 600);
            window.InitPalette();
        }

        private void InitPalette()
        {
            for (int i = 0; i < 16; i++)
            {
                paletteColors[i] = new Color(
                    (float)i / 15f,
                    (float)i / 15f,
                    (float)i / 15f,
                    1f
                );
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🎨 Palette Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Configurações da paleta
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            paletteSize = EditorGUILayout.IntSlider("Tamanho da Paleta", paletteSize, 2, 16);
            if (EditorGUI.EndChangeCheck())
            {
                InitPalette();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Grid de cores
            EditorGUILayout.LabelField("Cores da Paleta", EditorStyles.boldLabel);
            DrawColorGrid();

            EditorGUILayout.Space(10);

            // Editor da cor selecionada
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Cor #{selectedColorIndex + 1}", GUILayout.Width(80));
            EditorGUI.BeginChangeCheck();
            paletteColors[selectedColorIndex] = EditorGUILayout.ColorField(paletteColors[selectedColorIndex], GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck())
            {
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Ferramenta de gradiente
            showGradientTool = EditorGUILayout.Foldout(showGradientTool, "🌈 Ferramenta de Gradiente", true);
            if (showGradientTool)
            {
                EditorGUI.indentLevel++;
                gradientStart = EditorGUILayout.ColorField("Cor Inicial", gradientStart);
                gradientEnd = EditorGUILayout.ColorField("Cor Final", gradientEnd);
                
                if (GUILayout.Button("Aplicar Gradiente"))
                {
                    for (int i = 0; i < paletteSize; i++)
                    {
                        float t = i / (float)(paletteSize - 1);
                        paletteColors[i] = Color.Lerp(gradientStart, gradientEnd, t);
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // Botões de ação
            EditorGUILayout.LabelField("Ações", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🎲 Aleatória", GUILayout.Height(30)))
            {
                for (int i = 0; i < paletteSize; i++)
                {
                    paletteColors[i] = Random.ColorHSV(0f, 1f, 0.3f, 1f, 0.3f, 1f);
                }
            }
            
            if (GUILayout.Button("⬛ Grayscale", GUILayout.Height(30)))
            {
                for (int i = 0; i < paletteSize; i++)
                {
                    float val = i / (float)(paletteSize - 1);
                    paletteColors[i] = new Color(val, val, val, 1f);
                }
            }
            
            if (GUILayout.Button("🔥 Quente", GUILayout.Height(30)))
            {
                gradientStart = new Color(0.1f, 0.0f, 0.05f);
                gradientEnd = new Color(1.0f, 0.9f, 0.2f);
                for (int i = 0; i < paletteSize; i++)
                {
                    float t = i / (float)(paletteSize - 1);
                    paletteColors[i] = Color.Lerp(gradientStart, gradientEnd, t);
                }
            }
            
            if (GUILayout.Button("🧊 Frio", GUILayout.Height(30)))
            {
                gradientStart = new Color(0.0f, 0.05f, 0.2f);
                gradientEnd = new Color(0.5f, 0.9f, 1.0f);
                for (int i = 0; i < paletteSize; i++)
                {
                    float t = i / (float)(paletteSize - 1);
                    paletteColors[i] = Color.Lerp(gradientStart, gradientEnd, t);
                }
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🌿 Natureza", GUILayout.Height(30)))
            {
                Color[] natureColors = {
                    new Color(0.1f, 0.3f, 0.1f),
                    new Color(0.2f, 0.5f, 0.2f),
                    new Color(0.4f, 0.7f, 0.3f),
                    new Color(0.6f, 0.8f, 0.4f),
                    new Color(0.8f, 0.9f, 0.6f),
                };
                FillFromColors(natureColors);
            }
            
            if (GUILayout.Button("🌅 Pôr-do-sol", GUILayout.Height(30)))
            {
                Color[] sunsetColors = {
                    new Color(0.1f, 0.0f, 0.2f),
                    new Color(0.5f, 0.0f, 0.3f),
                    new Color(0.8f, 0.2f, 0.2f),
                    new Color(1.0f, 0.5f, 0.1f),
                    new Color(1.0f, 0.9f, 0.4f),
                };
                FillFromColors(sunsetColors);
            }
            
            if (GUILayout.Button("🌌 Noturno", GUILayout.Height(30)))
            {
                Color[] nightColors = {
                    new Color(0.0f, 0.0f, 0.05f),
                    new Color(0.05f, 0.05f, 0.15f),
                    new Color(0.1f, 0.1f, 0.3f),
                    new Color(0.2f, 0.2f, 0.5f),
                    new Color(0.4f, 0.4f, 0.7f),
                };
                FillFromColors(nightColors);
            }
            
            if (GUILayout.Button("🍬 Pastel", GUILayout.Height(30)))
            {
                for (int i = 0; i < paletteSize; i++)
                {
                    float hue = (float)i / paletteSize;
                    paletteColors[i] = Color.HSVToRGB(hue, 0.3f, 0.95f);
                }
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(15);

            // Preview
            DrawPreview();

            EditorGUILayout.Space(10);

            // Botões de exportação
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("💾 Exportar PNG", GUILayout.Height(35)))
            {
                ExportPalette();
            }
            GUI.backgroundColor = Color.white;
            
            GUI.backgroundColor = new Color(0.3f, 0.6f, 1.0f);
            if (GUILayout.Button("📥 Importar PNG", GUILayout.Height(35)))
            {
                ImportPalette();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }

        private void FillFromColors(Color[] colors)
        {
            for (int i = 0; i < paletteSize; i++)
            {
                float t = i / (float)(paletteSize - 1) * (colors.Length - 1);
                int indexA = Mathf.Clamp(Mathf.FloorToInt(t), 0, colors.Length - 1);
                int indexB = Mathf.Clamp(indexA + 1, 0, colors.Length - 1);
                float localT = t - indexA;
                paletteColors[i] = Color.Lerp(colors[indexA], colors[indexB], localT);
            }
        }

        private void DrawColorGrid()
        {
            int columns = 8;
            float cellSize = 40;
            float spacing = 4;

            for (int row = 0; row < Mathf.CeilToInt(paletteSize / (float)columns); row++)
            {
                EditorGUILayout.BeginHorizontal();
                
                for (int col = 0; col < columns; col++)
                {
                    int index = row * columns + col;
                    if (index >= paletteSize)
                    {
                        GUILayout.Space(cellSize + spacing);
                        continue;
                    }

                    Rect rect = GUILayoutUtility.GetRect(cellSize, cellSize);
                    
                    // Background
                    EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));
                    
                    // Color
                    Rect colorRect = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);
                    EditorGUI.DrawRect(colorRect, paletteColors[index]);
                    
                    // Selection border
                    if (index == selectedColorIndex)
                    {
                        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2), Color.white);
                        EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - 2, rect.width, 2), Color.white);
                        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2, rect.height), Color.white);
                        EditorGUI.DrawRect(new Rect(rect.x + rect.width - 2, rect.y, 2, rect.height), Color.white);
                    }
                    
                    // Click
                    if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                    {
                        selectedColorIndex = index;
                        Event.current.Use();
                    }
                    
                    GUILayout.Space(spacing);
                }
                
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(spacing);
            }
        }

        private void DrawPreview()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            
            float previewWidth = Mathf.Min(position.width - 40, 460);
            float previewHeight = 60;
            Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
            
            float colorWidth = previewWidth / paletteSize;
            
            for (int i = 0; i < paletteSize; i++)
            {
                Rect colorRect = new Rect(
                    previewRect.x + i * colorWidth,
                    previewRect.y,
                    colorWidth + 1, // +1 para evitar gaps
                    previewHeight
                );
                EditorGUI.DrawRect(colorRect, paletteColors[i]);
            }
            
            // Borda
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y - 1, previewWidth, 1), Color.white);
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y + previewHeight, previewWidth, 1), Color.white);
            EditorGUI.DrawRect(new Rect(previewRect.x - 1, previewRect.y, 1, previewHeight), Color.white);
            EditorGUI.DrawRect(new Rect(previewRect.x + previewWidth, previewRect.y, 1, previewHeight), Color.white);
        }

        private void ExportPalette()
        {
            string path = EditorUtility.SaveFilePanel("Exportar Paleta", Application.dataPath, "palette", "png");
            
            if (string.IsNullOrEmpty(path)) return;
            
            var texture = new Texture2D(paletteSize, 1, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            
            for (int i = 0; i < paletteSize; i++)
            {
                texture.SetPixel(i, 0, paletteColors[i]);
            }
            texture.Apply();
            
            byte[] pngData = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, pngData);
            
            DestroyImmediate(texture);
            
            AssetDatabase.Refresh();
            Debug.Log($"[Palette Editor] Paleta exportada: {path}");
            
            EditorUtility.DisplayDialog("Sucesso!", $"Paleta salva em:\n{path}", "OK");
        }

        private void ImportPalette()
        {
            string path = EditorUtility.OpenFilePanel("Importar Paleta", Application.dataPath, "png,jpg,bmp");
            
            if (string.IsNullOrEmpty(path)) return;
            
            byte[] fileData = System.IO.File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);
            
            paletteSize = Mathf.Clamp(texture.width, 2, 16);
            
            for (int i = 0; i < paletteSize; i++)
            {
                paletteColors[i] = texture.GetPixel(i, 0);
            }
            
            DestroyImmediate(texture);
            selectedColorIndex = 0;
            
            Debug.Log($"[Palette Editor] Paleta importada: {path}");
            Repaint();
        }
    }
}
