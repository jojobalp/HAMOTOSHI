using System.Collections.Generic;
using UnityEngine;

namespace PixelCamera
{
    /// <summary>
    /// Utility para criar e importar paletas de cores.
    /// </summary>
    /// <remarks>
    /// Toda paleta produzida aqui segue o formato que o shader espera:
    /// <b>16x1 pixels, RGBA32, sem mipmaps, Filter Mode = Point e
    /// Wrap Mode = Clamp</b>, com os 16 slots preenchidos. Quando a paleta tem
    /// menos de 16 cores, os slots restantes são preenchidos <b>repetindo</b> as
    /// cores existentes — nunca ficam em preto/transparente, porque o shader
    /// compara a cor do pixel contra os slots da textura.
    /// </remarks>
    public static class PaletteUtility
    {
        /// <summary>Largura fixa da textura de paleta usada pelo shader.</summary>
        public const int PaletteTextureWidth = 16;

        private const string LogPrefix = "[PixelCamera] ";

        /// <summary>
        /// Monta uma textura de paleta 16x1 a partir de uma lista de cores,
        /// preenchendo os slots excedentes por repetição cíclica.
        /// </summary>
        public static Texture2D CreatePaletteTexture(IList<Color> colors)
        {
            var palette = new Texture2D(PaletteTextureWidth, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                // Clamp, e não o padrão Repeat: a amostragem nunca dá a volta.
                wrapMode = TextureWrapMode.Clamp,
                name = "PixelCameraPalette"
            };

            if (colors == null || colors.Count == 0)
            {
                // Sem cores informadas: devolve uma rampa de cinza neutra em vez
                // de uma textura preta (que tornaria a imagem inteira preta).
                for (int i = 0; i < PaletteTextureWidth; i++)
                {
                    float v = i / (float)(PaletteTextureWidth - 1);
                    palette.SetPixel(i, 0, new Color(v, v, v, 1.0f));
                }
            }
            else
            {
                for (int i = 0; i < PaletteTextureWidth; i++)
                {
                    Color c = colors[i % colors.Count];
                    c.a = 1.0f;
                    palette.SetPixel(i, 0, c);
                }
            }

            palette.Apply();
            return palette;
        }

        /// <summary>
        /// Cria uma paleta aleatória com quantidade específica de cores.
        /// </summary>
        /// <param name="colorCount">Número de cores distintas (1 a 16).</param>
        /// <param name="seed">
        /// Seed opcional. Usa um <see cref="System.Random"/> próprio, então
        /// <b>não</b> altera o estado global de <c>UnityEngine.Random</c>
        /// (antes chamava <c>Random.InitState</c>, o que mudava a sequência
        /// aleatória do jogo inteiro).
        /// </param>
        public static Texture2D CreateRandomPalette(int colorCount, int seed = -1)
        {
            colorCount = Mathf.Clamp(colorCount, 1, PaletteTextureWidth);

            // seed < 0 = aleatório de verdade; seed >= 0 = reproduzível.
            var rng = seed >= 0 ? new System.Random(seed) : new System.Random();

            var colors = new List<Color>(colorCount);
            for (int i = 0; i < colorCount; i++)
            {
                // HSV com saturação e valor altos: cores vibrantes, que é o que
                // se espera de uma paleta retrô. Sortear RGB uniforme (como era
                // antes) produz quase sempre tons lavados/acinzentados.
                float h = (float)rng.NextDouble();
                float s = 0.55f + 0.45f * (float)rng.NextDouble();
                float v = 0.55f + 0.45f * (float)rng.NextDouble();
                colors.Add(Color.HSVToRGB(h, s, v));
            }

            return CreatePaletteTexture(colors);
        }

        /// <summary>
        /// Cria uma paleta com gradiente entre duas cores.
        /// </summary>
        public static Texture2D CreateGradientPalette(Color colorA, Color colorB, int steps = 16)
        {
            steps = Mathf.Clamp(steps, 2, PaletteTextureWidth);

            var colors = new List<Color>(steps);
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                colors.Add(Color.Lerp(colorA, colorB, t));
            }

            return CreatePaletteTexture(colors);
        }

        /// <summary>
        /// Extrai uma paleta representativa de uma textura existente.
        /// </summary>
        /// <remarks>
        /// Amostra a imagem num grid determinístico, agrupa os pixels em buckets
        /// de 5 bits por canal e devolve as <paramref name="maxColors"/> cores
        /// mais frequentes (cada uma representada pela média do seu bucket).
        /// A versão anterior sorteava pixels e os guardava num
        /// <c>HashSet&lt;Color&gt;</c>: como a comparação era em float RGBA,
        /// praticamente todo pixel era "único", então o resultado eram N pixels
        /// aleatórios da imagem, não a paleta dela.
        /// </remarks>
        /// <param name="source">Textura de origem. Precisa ser legível (Read/Write Enabled).</param>
        /// <param name="maxColors">Número de cores distintas (1 a 16).</param>
        public static Texture2D ExtractPaletteFromTexture(Texture2D source, int maxColors = 16)
        {
            maxColors = Mathf.Clamp(maxColors, 1, PaletteTextureWidth);

            if (source == null)
            {
                Debug.LogWarning(LogPrefix + "ExtractPaletteFromTexture recebeu uma textura nula.");
                return CreatePaletteTexture(null);
            }

            List<Color> colors;
            try
            {
                colors = ExtractColors(source, maxColors);
            }
            catch (UnityException e)
            {
                // GetPixel lança UnityException quando a textura não é legível.
                Debug.LogError(LogPrefix +
                    "Não foi possível ler a textura '" + source.name + "'. Marque " +
                    "'Read/Write Enabled' nas configurações de importação dela.\n" + e.Message);
                return CreatePaletteTexture(null);
            }

            if (colors.Count == 0)
            {
                Debug.LogWarning(LogPrefix +
                    "Nenhuma cor pôde ser extraída de '" + source.name + "'.");
                return CreatePaletteTexture(null);
            }

            return CreatePaletteTexture(colors);
        }

        /// <summary>
        /// Devolve as cores mais frequentes da textura, por contagem em buckets
        /// quantizados. Determinístico (não depende de Random).
        /// </summary>
        private static List<Color> ExtractColors(Texture2D source, int maxColors)
        {
            int w = source.width;
            int h = source.height;
            if (w <= 0 || h <= 0) return new List<Color>();

            // Grid de amostragem: no máximo ~4096 pontos, sempre determinístico.
            const int maxSamplesPerAxis = 64;
            int stepX = Mathf.Max(1, Mathf.CeilToInt(w / (float)maxSamplesPerAxis));
            int stepY = Mathf.Max(1, Mathf.CeilToInt(h / (float)maxSamplesPerAxis));

            // Bucket = 5 bits por canal (32 níveis), suficiente para agrupar
            // variações de compressão sem colapsar cores distintas.
            const int shift = 3;
            var counts = new Dictionary<int, int>();
            var sums = new Dictionary<int, Vector3>();

            for (int y = 0; y < h; y += stepY)
            {
                for (int x = 0; x < w; x += stepX)
                {
                    Color c = source.GetPixel(x, y);
                    // Ignora pixels totalmente transparentes.
                    if (c.a < 0.01f) continue;

                    int key = ((int)(c.r * 255f) >> shift << 10)
                            | ((int)(c.g * 255f) >> shift << 5)
                            | ((int)(c.b * 255f) >> shift);

                    counts[key] = counts.TryGetValue(key, out int n) ? n + 1 : 1;
                    sums[key] = sums.TryGetValue(key, out Vector3 acc)
                        ? acc + new Vector3(c.r, c.g, c.b)
                        : new Vector3(c.r, c.g, c.b);
                }
            }

            if (counts.Count == 0) return new List<Color>();

            // Ordena por frequência (desempate pela chave, para ser estável).
            var keys = new List<int>(counts.Keys);
            keys.Sort((a, b) =>
            {
                int cmp = counts[b].CompareTo(counts[a]);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });

            var result = new List<Color>(Mathf.Min(maxColors, keys.Count));
            for (int i = 0; i < keys.Count && result.Count < maxColors; i++)
            {
                int key = keys[i];
                Vector3 acc = sums[key] / counts[key];
                result.Add(new Color(acc.x, acc.y, acc.z, 1.0f));
            }

            return result;
        }

        /// <summary>
        /// Salva uma paleta como PNG.
        /// </summary>
        public static void SavePaletteAsPNG(Texture2D palette, string path)
        {
            if (palette == null)
            {
                Debug.LogError(LogPrefix + "SavePaletteAsPNG recebeu uma textura nula.");
                return;
            }

            byte[] pngData;
            try
            {
                pngData = palette.EncodeToPNG();
            }
            catch (UnityException e)
            {
                Debug.LogError(LogPrefix +
                    "Não foi possível codificar a paleta como PNG (a textura precisa ser legível).\n" +
                    e.Message);
                return;
            }

            System.IO.File.WriteAllBytes(path, pngData);
            Debug.Log(LogPrefix + "Paleta salva em: " + path);
        }

        /// <summary>
        /// Carrega uma paleta de um arquivo PNG ou JPEG.
        /// </summary>
        /// <remarks>
        /// Usa <c>Texture2D.LoadImage</c>, que decodifica <b>apenas PNG e
        /// JPEG</b> — BMP e outros formatos não são suportados. A imagem é
        /// reamostrada para o layout 16x1 que o shader espera.
        /// </remarks>
        public static Texture2D LoadPaletteFromPNG(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                Debug.LogError(LogPrefix + "Arquivo de paleta não encontrado: " + path);
                return CreatePaletteTexture(null);
            }

            byte[] fileData = System.IO.File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (!texture.LoadImage(fileData))
            {
                Debug.LogError(LogPrefix +
                    "Não foi possível decodificar '" + path + "'. Formatos suportados: PNG e JPEG.");
                Object.Destroy(texture);
                return CreatePaletteTexture(null);
            }

            // Reamostra a primeira linha para os 16 slots esperados pelo shader.
            var colors = new List<Color>(PaletteTextureWidth);
            for (int i = 0; i < PaletteTextureWidth; i++)
            {
                int x = Mathf.Min(i, texture.width - 1);
                colors.Add(texture.GetPixel(x, 0));
            }

            Object.Destroy(texture);
            return CreatePaletteTexture(colors);
        }
    }
}
