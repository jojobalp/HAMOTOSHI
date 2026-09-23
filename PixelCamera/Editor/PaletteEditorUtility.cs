using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelCamera.Editor
{
    /// <summary>
    /// Fluxo compartilhado de "escolher um arquivo de imagem e virar uma paleta
    /// utilizável" dentro do editor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Resolve dois problemas que existiam até aqui:
    /// </para>
    /// <list type="number">
    /// <item>
    /// O botão "Importar Paleta de Texture" do Render Feature chamava
    /// <c>AssetDatabase.LoadAssetAtPath</c> com o caminho <b>absoluto</b> devolvido por
    /// <c>EditorUtility.OpenFilePanel</c> (ex.: <c>C:\Users\...\foto.png</c>). Essa API só
    /// aceita caminhos relativos ao projeto (<c>Assets/...</c>), então devolvia sempre
    /// <c>null</c> e o usuário recebia "Não foi possível carregar a textura" mesmo com um
    /// arquivo válido.
    /// </item>
    /// <item>
    /// A extração de paleta (<see cref="PaletteUtility.ExtractPaletteFromTexture"/>) existia
    /// como API de runtime mas <b>não tinha nenhum botão</b> no editor, e exigir uma textura
    /// já importada com <i>Read/Write Enabled</i> tornava o fluxo manual (exportar PNG →
    /// importar no Unity → configurar Point/Clamp/mipmaps/Read-Write → arrastar).
    /// </item>
    /// </list>
    /// <para>
    /// Aqui a imagem é decodificada <b>direto do disco</b>, numa textura temporária em
    /// memória (sempre legível), então nem os settings de importação nem o formato da imagem
    /// de origem importam: o resultado é sempre gravado como um PNG <b>16×1</b> novo, com
    /// <c>Point</c>, <c>Clamp</c>, sem mipmaps, sem compressão e legível.
    /// </para>
    /// </remarks>
    public static class PaletteEditorUtility
    {
        /// <summary>Pasta (dentro de <c>Assets/</c>) onde as paletas importadas são gravadas.</summary>
        public const string PaletteFolder = "Assets/PixelCameraPalettes";

        /// <summary>Extensões aceitas nos dialogs de arquivo.</summary>
        /// <remarks>
        /// <c>Texture2D.LoadImage</c> decodifica apenas PNG e JPEG — BMP aparece em vários
        /// tutoriais de paleta retrô, mas não funciona.
        /// </remarks>
        private const string FileFilter = "png,jpg,jpeg";

        /// <summary>
        /// Abre um dialog de arquivo e transforma a imagem escolhida num asset de paleta
        /// <b>16×1</b> pronto para o campo <i>Paleta Custom</i> do Render Feature.
        /// </summary>
        /// <remarks>
        /// Se a imagem já for uma faixa de paleta (altura 1 e largura até 16), as cores são
        /// lidas diretamente e os slots restantes preenchidos por repetição. Qualquer outra
        /// imagem (foto, sprite, ilustração) passa pela extração das cores mais frequentes.
        /// </remarks>
        /// <param name="title">Título do dialog de arquivo.</param>
        /// <param name="forceExtract">
        /// <c>true</c> para sempre extrair as cores mais frequentes, mesmo de uma faixa 16×1.
        /// </param>
        /// <returns>A textura de paleta criada, ou <c>null</c> se o usuário cancelou ou houve falha.</returns>
        public static Texture2D ImportImageAsPalette(string title, bool forceExtract = false)
        {
            string sourcePath = EditorUtility.OpenFilePanel(title, Application.dataPath, FileFilter);
            if (string.IsNullOrEmpty(sourcePath)) return null;

            Color[] colors = PaletteColorsFromPath(sourcePath, forceExtract, out bool usedAsStrip);
            if (colors == null) return null;

            return SavePaletteAsset(colors, MakeAssetName(sourcePath), usedAsStrip);
        }

        /// <summary>
        /// Lê as 16 cores de um arquivo de imagem do disco (sem dialog).
        /// </summary>
        /// <remarks>
        /// A imagem é decodificada numa textura temporária em memória, que é sempre
        /// legível — então o fluxo independe dos settings de importação do arquivo de
        /// origem (não precisa de <i>Read/Write Enabled</i>).
        /// </remarks>
        /// <param name="usedAsStrip">
        /// <c>true</c> quando a imagem já era uma faixa de paleta e as cores foram usadas
        /// como estão, em vez de extraídas.
        /// </param>
        private static Color[] PaletteColorsFromPath(string sourcePath, bool forceExtract, out bool usedAsStrip)
        {
            Texture2D decoded = DecodeImage(sourcePath);
            if (decoded == null)
            {
                usedAsStrip = false;
                return null;
            }

            // Atribuição fora do try: mantém o fluxo do parâmetro out trivial de
            // verificar e garante a liberação da textura temporária.
            Color[] colors = PaletteColorsFromTexture(decoded, forceExtract, out usedAsStrip);
            Object.DestroyImmediate(decoded);
            return colors;
        }

        /// <summary>
        /// Preenche os 16 slots a partir de uma textura já decodificada.
        /// </summary>
        /// <remarks>
        /// Se a textura for uma faixa de paleta (altura 1, largura até 16), as cores são
        /// lidas diretamente e os slots restantes preenchidos por repetição — nunca em
        /// preto, porque o shader compara a cor do pixel contra todos os slots usados.
        /// Caso contrário, extrai as cores mais frequentes da imagem.
        /// </remarks>
        private static Color[] PaletteColorsFromTexture(Texture2D decoded, bool forceExtract, out bool usedAsStrip)
        {
            int slots = PaletteUtility.PaletteTextureWidth;
            var colors = new Color[slots];

            usedAsStrip = !forceExtract &&
                          decoded.height == 1 &&
                          decoded.width >= 1 &&
                          decoded.width <= slots;

            if (usedAsStrip)
            {
                for (int i = 0; i < slots; i++)
                {
                    Color c = decoded.GetPixel(i % decoded.width, 0);
                    c.a = 1f;
                    colors[i] = c;
                }
            }
            else
            {
                Texture2D extracted = PaletteUtility.ExtractPaletteFromTexture(decoded, slots);
                for (int i = 0; i < slots; i++)
                {
                    Color c = extracted.GetPixel(i, 0);
                    c.a = 1f;
                    colors[i] = c;
                }
                Object.DestroyImmediate(extracted);
            }

            return colors;
        }

        /// <summary>
        /// Devolve as 16 cores de uma imagem escolhida pelo usuário, sem gravar nenhum asset.
        /// </summary>
        /// <remarks>
        /// Usado pelos editores que precisam preencher um array de <see cref="Color"/>
        /// (Palette Editor e <see cref="PalettePresetAsset"/>) em vez de uma textura.
        /// </remarks>
        /// <param name="title">Título do dialog de arquivo.</param>
        /// <param name="forceExtract">
        /// <c>true</c> para sempre extrair as cores mais frequentes, mesmo de uma faixa 16×1.
        /// </param>
        /// <param name="sourceColorCount">
        /// Quantidade de cores distintas realmente presentes na imagem (1 a 16).
        /// </param>
        /// <returns>As 16 cores (slots excedentes preenchidos por repetição), ou <c>null</c> se cancelou/falhou.</returns>
        public static Color[] PickPaletteColors(string title, bool forceExtract, out int sourceColorCount)
        {
            sourceColorCount = 0;

            string sourcePath = EditorUtility.OpenFilePanel(title, Application.dataPath, FileFilter);
            if (string.IsNullOrEmpty(sourcePath)) return null;

            Texture2D decoded = DecodeImage(sourcePath);
            if (decoded == null) return null;

            try
            {
                Color[] colors = PaletteColorsFromTexture(decoded, forceExtract, out bool usedAsStrip);

                // Numa faixa de paleta as cores distintas são a largura da imagem;
                // numa imagem comum a extração devolve sempre os 16 slots.
                sourceColorCount = usedAsStrip
                    ? Mathf.Clamp(decoded.width, 1, PaletteUtility.PaletteTextureWidth)
                    : PaletteUtility.PaletteTextureWidth;

                return colors;
            }
            finally
            {
                Object.DestroyImmediate(decoded);
            }
        }

        /// <summary>
        /// Devolve as 16 cores de uma imagem escolhida pelo usuário, sem gravar nenhum asset.
        /// </summary>
        public static Color[] PickPaletteColorsFromFile(string title, bool forceExtract = false)
        {
            return PickPaletteColors(title, forceExtract, out _);
        }

        /// <summary>
        /// Grava uma lista de cores como PNG 16×1 dentro do projeto e configura o importer.
        /// </summary>
        /// <param name="colors">Cores da paleta (os slots excedentes são preenchidos por repetição).</param>
        /// <param name="assetName">Nome do arquivo, sem extensão.</param>
        /// <param name="usedAsStrip">Apenas para o texto do dialog de sucesso.</param>
        public static Texture2D SavePaletteAsset(IList<Color> colors, string assetName, bool usedAsStrip)
        {
            // Formato canônico: 16x1, RGBA32, Point, Clamp, slots preenchidos por repetição.
            Texture2D palette = PaletteUtility.CreatePaletteTexture(colors);

            string folder = EnsurePaletteFolder();
            string fileName = string.IsNullOrEmpty(assetName) ? "palette" : assetName;
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + fileName + ".png");

            byte[] pngData = palette.EncodeToPNG();
            Object.DestroyImmediate(palette);

            if (pngData == null || pngData.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Falha ao gravar a paleta",
                    "Não foi possível codificar a paleta como PNG.",
                    "OK");
                return null;
            }

            try
            {
                File.WriteAllBytes(assetPath, pngData);
            }
            catch (IOException e)
            {
                EditorUtility.DisplayDialog(
                    "Falha ao gravar a paleta",
                    "Não foi possível escrever em '" + assetPath + "'.\n\n" + e.Message,
                    "OK");
                return null;
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            ConfigurePaletteImporter(assetPath);

            var result = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

            Debug.Log($"[PixelCamera] Paleta criada ({PaletteUtility.PaletteTextureWidth}x1): {assetPath}");
            EditorUtility.DisplayDialog(
                "Paleta criada",
                $"Arquivo: {assetPath}\n\n" +
                (usedAsStrip
                    ? "A imagem já era uma faixa de paleta — as cores foram usadas como estão."
                    : "As 16 cores mais frequentes da imagem foram extraídas.") +
                "\n\nO asset já está configurado (16×1, Point, Clamp, sem mipmaps, sem " +
                "compressão, Read/Write Enabled) e pode ser arrastado direto para o campo " +
                "'Paleta Custom'.\n\n" +
                "Dica: foto com milhões de cores gera uma paleta 'média'. Para resultado " +
                "retrô fiel, use pixel art, ilustração flat ou sprite sheet — e prefira PNG " +
                "a JPEG.",
                "OK");

            EditorGUIUtility.PingObject(result);
            return result;
        }

        /// <summary>
        /// Decodifica um PNG/JPEG do disco numa textura temporária sempre legível.
        /// </summary>
        /// <remarks>
        /// <c>Texture2D.LoadImage</c> só decodifica PNG e JPEG. Devolve <c>null</c> (com dialog)
        /// se o arquivo não existir ou não puder ser decodificado — antes a falha passava
        /// silenciosamente e o usuário ficava com uma textura vazia.
        /// </remarks>
        public static Texture2D DecodeImage(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                EditorUtility.DisplayDialog(
                    "Arquivo não encontrado",
                    "O arquivo selecionado não existe:\n" + path,
                    "OK");
                return null;
            }

            byte[] data;
            try
            {
                data = File.ReadAllBytes(path);
            }
            catch (IOException e)
            {
                EditorUtility.DisplayDialog("Falha ao ler o arquivo", e.Message, "OK");
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(data))
            {
                Object.DestroyImmediate(texture);
                EditorUtility.DisplayDialog(
                    "Formato não suportado",
                    "Não foi possível decodificar:\n" + path +
                    "\n\nFormatos suportados: PNG e JPEG (BMP não é suportado pelo Unity " +
                    "nesta etapa).",
                    "OK");
                return null;
            }

            return texture;
        }

        /// <summary>
        /// Aplica os settings de importação que o shader de paleta exige.
        /// </summary>
        private static void ConfigurePaletteImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            // Local, e não campo estático: TextureImporterSettings é mutável e um
            // estado compartilhado entre chamadas vazaria configurações.
            var importerSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(importerSettings);

            importerSettings.filterMode = FilterMode.Point;
            importerSettings.wrapMode = TextureWrapMode.Clamp;
            importerSettings.mipmapEnabled = false;
            importerSettings.generateCubemap = TextureImporterGenerateCubemap.FullCubemap;
            importerSettings.readable = true;
            importerSettings.sRGBTexture = true;
            importerSettings.alphaSource = TextureImporterAlphaSource.None;
            importerSettings.npotScale = TextureImporterNPOTScale.None;
            importerSettings.textureCompression = TextureImporterCompression.Uncompressed;
            importerSettings.maxTextureSize = Mathf.Max(
                importerSettings.maxTextureSize, PaletteUtility.PaletteTextureWidth);

            importer.SetTextureSettings(importerSettings);

            // Sem overriding por plataforma: a paleta precisa ser idêntica em todas.
            importer.SaveAndReimport();
        }

        /// <summary>Cria a pasta de paletas se necessário e devolve o caminho relativo.</summary>
        public static string EnsurePaletteFolder()
        {
            if (!AssetDatabase.IsValidFolder(PaletteFolder))
            {
                string absolute = Path.Combine(Application.dataPath, "PixelCameraPalettes");
                if (!Directory.Exists(absolute))
                {
                    Directory.CreateDirectory(absolute);
                }

                AssetDatabase.Refresh();
            }

            return PaletteFolder;
        }

        /// <summary>
        /// Converte um caminho absoluto em caminho relativo ao projeto.
        /// </summary>
        /// <remarks>
        /// <c>AssetDatabase.LoadAssetAtPath</c> só aceita <c>Assets/...</c>; é exatamente por
        /// receber aqui o caminho absoluto do <c>OpenFilePanel</c> que a importação antiga
        /// falhava sempre.
        /// </remarks>
        public static string ToProjectRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return string.Empty;

            string dataPath = Application.dataPath;
            if (absolutePath.StartsWith(dataPath))
            {
                return "Assets" + absolutePath.Substring(dataPath.Length);
            }

            return FileUtil.GetProjectRelativePath(absolutePath);
        }

        /// <summary>Nome de asset a partir do nome do arquivo de origem (sem extensão).</summary>
        private static string MakeAssetName(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(name)) return "palette";

            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid, '_');
            }

            name = name.Trim();
            if (name.Length > 48)
            {
                name = name.Substring(0, 48);
            }

            return string.IsNullOrEmpty(name) ? "palette" : name;
        }
    }
}
