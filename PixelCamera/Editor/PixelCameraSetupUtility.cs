using UnityEditor;
using UnityEngine;

// Obs.: nenhum tipo do URP é usado neste arquivo, então o "using UnityEngine.Rendering.Universal"
// foi removido de propósito — sem ele, a falta do pacote URP no projeto não gera o erro
// CS0234 aqui (as referências ao URP ficam restritas aos arquivos que realmente precisam).

namespace PixelCamera.Editor
{
    /// <summary>
    /// Utility para setup rápido do Pixel Camera
    /// </summary>
    public static class PixelCameraSetupUtility
    {
        [MenuItem("Tools/Pixel Camera/Setup na Câmera Atual")]
        public static void SetupOnCurrentCamera()
        {
            var camera = Camera.current ?? Object.FindObjectOfType<Camera>();
            
            if (camera == null)
            {
                EditorUtility.DisplayDialog("Erro", "Nenhuma câmera encontrada na cena!", "OK");
                return;
            }
            
            // Adicionar PixelCameraController
            if (camera.GetComponent<PixelCameraController>() == null)
            {
                camera.gameObject.AddComponent<PixelCameraController>();
            }
            
            // Selecionar a câmera
            Selection.activeGameObject = camera.gameObject;
            
            Debug.Log($"[PixelCamera] Setup completo na câmera: {camera.gameObject.name}");
            EditorUtility.DisplayDialog("Sucesso!", 
                "PixelCameraController adicionado à câmera!\n\n" +
                "Não esqueça de:\n" +
                "1. Configurar o URP Renderer com PixelCameraRenderFeature\n" +
                "2. Arrastar o RenderFeature para o campo 'Render Feature' do controller", 
                "OK");
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
                "• Tools > Pixel Camera > Setup na Câmera\n" +
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
    }
}
