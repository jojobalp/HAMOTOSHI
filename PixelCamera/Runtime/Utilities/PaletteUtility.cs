using UnityEngine;

namespace PixelCamera
{
    /// <summary>
    /// Utility para criar e importar paletas de cores
    /// </summary>
    public static class PaletteUtility
    {
        /// <summary>
        /// Cria uma paleta aleatória com quantidade específica de cores
        /// </summary>
        public static Texture2D CreateRandomPalette(int colorCount, int seed = -1)
        {
            if (seed >= 0)
            {
                Random.InitState(seed);
            }
            
            var palette = new Texture2D(16, 1, TextureFormat.RGBA32, false);
            palette.filterMode = FilterMode.Point;
            
            for (int i = 0; i < Mathf.Min(colorCount, 16); i++)
            {
                Color color = new Color(
                    Random.value,
                    Random.value,
                    Random.value,
                    1.0f
                );
                palette.SetPixel(i, 0, color);
            }
            
            palette.Apply();
            return palette;
        }

        /// <summary>
        /// Cria uma paleta gradiente entre duas cores
        /// </summary>
        public static Texture2D CreateGradientPalette(Color colorA, Color colorB, int steps = 16)
        {
            var palette = new Texture2D(16, 1, TextureFormat.RGBA32, false);
            palette.filterMode = FilterMode.Point;
            
            for (int i = 0; i < Mathf.Min(steps, 16); i++)
            {
                float t = i / (float)(steps - 1);
                Color color = Color.Lerp(colorA, colorB, t);
                palette.SetPixel(i, 0, color);
            }
            
            palette.Apply();
            return palette;
        }

        /// <summary>
        /// Cria uma paleta de uma textura existente (extrai cores únicas)
        /// </summary>
        public static Texture2D ExtractPaletteFromTexture(Texture2D source, int maxColors = 16)
        {
            var palette = new Texture2D(16, 1, TextureFormat.RGBA32, false);
            palette.filterMode = FilterMode.Point;
            
            // Simplificado: amostra a textura em pontos específicos
            var uniqueColors = new System.Collections.Generic.HashSet<Color>();
            
            int sampleCount = 1000;
            for (int i = 0; i < sampleCount && uniqueColors.Count < maxColors; i++)
            {
                int x = Random.Range(0, source.width);
                int y = Random.Range(0, source.height);
                Color pixelColor = source.GetPixel(x, y);
                uniqueColors.Add(pixelColor);
            }
            
            int index = 0;
            foreach (var color in uniqueColors)
            {
                if (index >= 16) break;
                palette.SetPixel(index, 0, color);
                index++;
            }
            
            palette.Apply();
            return palette;
        }

        /// <summary>
        /// Salva uma paleta como PNG
        /// </summary>
        public static void SavePaletteAsPNG(Texture2D palette, string path)
        {
            byte[] pngData = palette.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, pngData);
            Debug.Log($"[PixelCamera] Paleta salva em: {path}");
        }

        /// <summary>
        /// Carrega uma paleta de um arquivo PNG
        /// </summary>
        public static Texture2D LoadPaletteFromPNG(string path)
        {
            byte[] fileData = System.IO.File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);
            texture.filterMode = FilterMode.Point;
            return texture;
        }
    }
}
