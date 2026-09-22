using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace PixelCamera
{
    /// <summary>
    /// Render Pass que executa o shader de pixelização
    ///
    /// Compatível com várias versões da URP:
    /// - Unity 6+ (URP 17+): usa o Render Graph (RecordRenderGraph). A partir da URP 17.1
    ///   o método Execute(ScriptableRenderContext, ref RenderingData) foi removido da API
    ///   pública, o que causava o erro CS0115 ("no suitable method found to override").
    /// - Unity 2022/2023 (URP 13-16): usa Execute com RTHandle + Blitter.
    /// - Unity 2021 ou mais antigo (URP 12-): caminho legado com RenderTargetIdentifier.
    /// </summary>
    public class PixelCameraRenderPass : ScriptableRenderPass
    {
        private const string k_RenderTag = "Pixel Camera";

        private PixelCameraRenderFeature m_Feature;
        private Material m_Material;
        private int m_PixelWidth;
        private int m_PixelHeight;

        // Shader property IDs
        private static readonly int PixelResolution = Shader.PropertyToID("_PixelResolution");
        private static readonly int PaletteEnabled = Shader.PropertyToID("_PaletteEnabled");
        private static readonly int CustomPalette = Shader.PropertyToID("_CustomPalette");
        private static readonly int ColorCount = Shader.PropertyToID("_ColorCount");
        private static readonly int ColorQuantization = Shader.PropertyToID("_ColorQuantization");
        private static readonly int PaletteSize = Shader.PropertyToID("_PaletteSize");
        private static readonly int DitherEnabled = Shader.PropertyToID("_DitherEnabled");
        private static readonly int DitherType = Shader.PropertyToID("_DitherType");
        private static readonly int DitherIntensity = Shader.PropertyToID("_DitherIntensity");
        private static readonly int CRTEnabled = Shader.PropertyToID("_CRTEnabled");
        private static readonly int ScanlineIntensity = Shader.PropertyToID("_ScanlineIntensity");
        private static readonly int ScanlineThickness = Shader.PropertyToID("_ScanlineThickness");
        private static readonly int BloomIntensity = Shader.PropertyToID("_BloomIntensity");
        private static readonly int BloomRadius = Shader.PropertyToID("_BloomRadius");
        private static readonly int CurvatureIntensity = Shader.PropertyToID("_CurvatureIntensity");
        private static readonly int VignetteIntensity = Shader.PropertyToID("_VignetteIntensity");
        private static readonly int PixelCameraTime = Shader.PropertyToID("_PixelCameraTime");
        private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");

        // Cache de paletas (evita criar uma textura nova a cada frame)
        private readonly Dictionary<PixelCameraRenderFeature.PalettePreset, Texture2D> m_PresetPalettes =
            new Dictionary<PixelCameraRenderFeature.PalettePreset, Texture2D>();

        // Nº de cores REAL de cada preset cacheado. O shader precisa disso para
        // não comparar contra os slots não usados da textura 16x1.
        private readonly Dictionary<PixelCameraRenderFeature.PalettePreset, int> m_PresetPaletteSizes =
            new Dictionary<PixelCameraRenderFeature.PalettePreset, int>();

        // Uma textura de paleta custom é sempre interpretada como 16 slots.
        private const int k_CustomPaletteSize = 16;

#if UNITY_6000_0_OR_NEWER
        // Dados passados para as render functions do Render Graph
        private class PixelPassData
        {
            public TextureHandle source;
            public Material material;
            public int shaderPass;
        }
#elif UNITY_2022_1_OR_NEWER
        private RTHandle m_CameraColorTargetHandle;
        private RTHandle m_LowResHandle;
#else
        private RenderTargetIdentifier m_CameraColorTarget;
        private RenderTexture m_LowResTexture;
#endif

        public PixelCameraRenderPass(PixelCameraRenderFeature feature)
        {
            m_Feature = feature;

            // Carregar material do shader
            var shader = Shader.Find("Hidden/PixelCamera");
            if (shader == null)
            {
                Debug.LogError("[PixelCamera] Shader 'Hidden/PixelCamera' não encontrado!");
                return;
            }

            m_Material = new Material(shader);
            m_Material.hideFlags = HideFlags.HideAndDontSave;

#if UNITY_2021_1_OR_NEWER
            // A partir da URP 11, os blits (Blitter) desenham um triângulo fullscreen
            // procedural em vez de usar uma malha, então o shader precisa da variante
            // _USE_DRAW_PROCEDURAL.
            m_Material.EnableKeyword("_USE_DRAW_PROCEDURAL");
#endif

#if UNITY_2022_1_OR_NEWER
            // O efeito precisa amostrar a cor ativa da câmera, então garante que a URP
            // renderize através de uma textura intermediária (não direto no backbuffer).
            requiresIntermediateTexture = true;
#endif
        }

#if !UNITY_6000_0_OR_NEWER
#if UNITY_2022_1_OR_NEWER
        public void Setup(RTHandle cameraColorTarget)
        {
            m_CameraColorTargetHandle = cameraColorTarget;
        }
#else
        public void Setup(RenderTargetIdentifier cameraColorTarget)
        {
            m_CameraColorTarget = cameraColorTarget;
        }
#endif
#endif

        // URP versions that use the RenderGraph path no longer expose OnCameraSetup
        // as an overridable method. Keep the setup in a regular helper and call it
        // from the render entry point so the feature works with both compatibility
        // and RenderGraph renderer implementations.
        private void ComputePixelResolution(int screenWidth, int screenHeight)
        {
            // Calcular resolução de pixel
            m_PixelWidth = m_Feature.pixelSettings.pixelWidth > 0
                ? m_Feature.pixelSettings.pixelWidth
                : screenWidth;
            m_PixelHeight = m_Feature.pixelSettings.pixelHeight > 0
                ? m_Feature.pixelSettings.pixelHeight
                : screenHeight;

            // Snap to pixel grid
            if (m_Feature.pixelSettings.snapToPixelGrid && screenHeight > 0)
            {
                var aspect = (float)screenWidth / screenHeight;
                m_PixelWidth = Mathf.RoundToInt(m_PixelWidth / aspect) * Mathf.RoundToInt(aspect);
                m_PixelHeight = Mathf.RoundToInt(m_PixelHeight);
            }
        }

        private void UpdateMaterial()
        {
            // Configurar parâmetros do shader
            m_Material.SetVector(PixelResolution, new Vector4(m_PixelWidth, m_PixelHeight, 0, 0));
            m_Material.SetFloat(PaletteEnabled, m_Feature.paletteSettings.enablePalette ? 1.0f : 0.0f);

            // Paleta
            int paletteSize;
            if (m_Feature.paletteSettings.customPalette != null)
            {
                m_Material.SetTexture(CustomPalette, m_Feature.paletteSettings.customPalette);
                paletteSize = k_CustomPaletteSize;
            }
            else
            {
                // Usar preset de paleta
                var presetPalette = GetPresetPalette(m_Feature.paletteSettings.preset, out paletteSize);
                m_Material.SetTexture(CustomPalette, presetPalette);
            }

            // Informa ao shader quantas cores a paleta ativa realmente tem.
            // Sem isso ele assumia 16 e os slots vazios (pretos) da textura
            // dominavam os tons escuros/médios — o preset Binary, por exemplo,
            // ficava quase todo preto em vez de preto e branco.
            m_Material.SetFloat(PaletteSize, Mathf.Clamp(paletteSize, 1, 16));

            m_Material.SetFloat(ColorCount, m_Feature.paletteSettings.colorCount);
            m_Material.SetFloat(ColorQuantization, m_Feature.paletteSettings.colorQuantization);

            // Dithering
            m_Material.SetFloat(DitherEnabled, m_Feature.ditherSettings.enableDithering ? 1.0f : 0.0f);
            m_Material.SetFloat(DitherType, (int)m_Feature.ditherSettings.ditherType);
            m_Material.SetFloat(DitherIntensity, m_Feature.ditherSettings.intensity);

            // CRT
            m_Material.SetFloat(CRTEnabled, m_Feature.crtSettings.enableCRT ? 1.0f : 0.0f);
            m_Material.SetFloat(ScanlineIntensity, m_Feature.crtSettings.scanlineIntensity);
            m_Material.SetFloat(ScanlineThickness, m_Feature.crtSettings.scanlineThickness);
            m_Material.SetFloat(BloomIntensity, m_Feature.crtSettings.bloomIntensity);
            m_Material.SetFloat(BloomRadius, m_Feature.crtSettings.bloomRadius);
            m_Material.SetFloat(CurvatureIntensity, m_Feature.crtSettings.curvatureIntensity);
            m_Material.SetFloat(VignetteIntensity, m_Feature.crtSettings.vignetteIntensity);

            // Time para animações
            m_Material.SetFloat(PixelCameraTime, Time.time);
        }

#if UNITY_6000_0_OR_NEWER
        /// <summary>
        /// Unity 6 (URP 17+): o método Execute(ScriptableRenderContext, ref RenderingData)
        /// foi removido da API pública (erro CS0115). Toda a renderização customizada
        /// agora passa pelo Render Graph, através do RecordRenderGraph.
        /// </summary>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null || m_Feature == null || !m_Feature.enabled)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle source = resourceData.activeColorTexture;
            if (!source.IsValid())
                return;

            TextureDesc sourceDesc = renderGraph.GetTextureDesc(source);
            ComputePixelResolution(sourceDesc.width, sourceDesc.height);
            UpdateMaterial();

            // 1) Textura de baixa resolução (filtro point, essencial para o look pixelado)
            TextureDesc lowResDesc = renderGraph.GetTextureDesc(source);
            lowResDesc.name = "_PixelCameraLowRes";
            lowResDesc.width = m_PixelWidth;
            lowResDesc.height = m_PixelHeight;
            lowResDesc.depthBufferBits = 0;
            lowResDesc.msaaSamples = MSAASamples.None;
            lowResDesc.filterMode = FilterMode.Point;
            lowResDesc.wrapMode = TextureWrapMode.Clamp;
            lowResDesc.clearBuffer = false;
            TextureHandle lowRes = renderGraph.CreateTexture(lowResDesc);

            // 2) Cena -> baixa resolução (pixelização + paleta + dithering + CRT)
            using (var builder = renderGraph.AddRasterRenderPass<PixelPassData>("Pixel Camera Effect", out var effectData))
            {
                effectData.source = source;
                effectData.material = m_Material;
                effectData.shaderPass = 0;

                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(lowRes, 0, AccessFlags.WriteAll);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PixelPassData data, RasterGraphContext ctx) =>
                    Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1f, 1f, 0f, 0f), data.material, data.shaderPass));
            }

            // 3) Baixa resolução -> resolução da câmera (upscale com filtro point)
            TextureHandle result = renderGraph.CreateTexture(source, "_PixelCameraResult", false);

            using (var builder = renderGraph.AddRasterRenderPass<PixelPassData>("Pixel Camera Upscale", out var upscaleData))
            {
                upscaleData.source = lowRes;
                upscaleData.material = m_Material;
                upscaleData.shaderPass = 1;

                builder.UseTexture(lowRes, AccessFlags.Read);
                builder.SetRenderAttachment(result, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PixelPassData data, RasterGraphContext ctx) =>
                    Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1f, 1f, 0f, 0f), data.material, data.shaderPass));
            }

            // A partir daqui a cor da câmera é a versão pixelada
            resourceData.cameraColor = result;
        }
#else
#if UNITY_2022_1_OR_NEWER
        /// <summary>
        /// Unity 2022/2023 (URP 13-16): Execute ainda existe, mas a URP trabalha
        /// com RTHandles e o Blitter (não existem mais Blits com RenderTargetIdentifier).
        /// </summary>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null || m_Feature == null || !m_Feature.enabled) return;

            var cameraData = renderingData.cameraData;
            var screenDesc = cameraData.cameraTargetDescriptor;
            ComputePixelResolution(screenDesc.width, screenDesc.height);

            // (Re)alocar a textura de baixa resolução
            if (m_LowResHandle == null || m_LowResHandle.rt == null ||
                m_LowResHandle.rt.width != m_PixelWidth || m_LowResHandle.rt.height != m_PixelHeight)
            {
                m_LowResHandle?.Release();

                var lowResDesc = screenDesc;
                lowResDesc.width = m_PixelWidth;
                lowResDesc.height = m_PixelHeight;
                lowResDesc.depthBufferBits = 0;
                lowResDesc.msaaSamples = 1;
                lowResDesc.useMipMap = false;

                m_LowResHandle = RTHandles.Alloc(
                    lowResDesc,
                    FilterMode.Point,
                    TextureWrapMode.Clamp,
                    name: "_PixelCameraLowRes");
            }

            UpdateMaterial();

            CommandBuffer cmd = CommandBufferPool.Get(k_RenderTag);
            var rasterCmd = CommandBufferHelpers.GetRasterCommandBuffer(cmd);

            // Renderizar para textura de baixa resolução
            Blitter.BlitCameraTexture(rasterCmd, m_CameraColorTargetHandle, m_LowResHandle, m_Material, 0);

            // Blit de volta para a câmera com filtro point (pixelado)
            Blitter.BlitCameraTexture(rasterCmd, m_LowResHandle, m_CameraColorTargetHandle, bilinear: false);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#else
        /// <summary>
        /// Caminho legado (URP 12 ou mais antigo).
        /// </summary>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null || !m_Feature.enabled) return;

            CommandBuffer cmd = CommandBufferPool.Get(k_RenderTag);
            SetupCamera(cmd, ref renderingData);
            UpdateMaterial();

            // O Blit legado configura "_MainTex"; o shader atual lê "_BlitTexture"
            // (o nome usado pelo Blitter), então definimos a global manualmente.
            cmd.SetGlobalTexture(BlitTextureId, m_CameraColorTarget);

            // Renderizar para textura de baixa resolução
            Blit(cmd, m_CameraColorTarget, m_LowResTexture, m_Material);

            // Blit de volta para a câmera com filtro point (pixelado)
            cmd.SetGlobalTexture(BlitTextureId, m_LowResTexture);
            Blit(cmd, m_LowResTexture, m_CameraColorTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void SetupCamera(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            var screenDesc = cameraData.cameraTargetDescriptor;

            ComputePixelResolution(screenDesc.width, screenDesc.height);

            // Criar render texture de baixa resolução
            if (m_LowResTexture == null || m_LowResTexture.width != m_PixelWidth || m_LowResTexture.height != m_PixelHeight)
            {
                if (m_LowResTexture != null)
                {
                    m_LowResTexture.Release();
                    Object.Destroy(m_LowResTexture);
                }

                m_LowResTexture = new RenderTexture(m_PixelWidth, m_PixelHeight, 0, RenderTextureFormat.ARGB32)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                m_LowResTexture.Create();
            }
        }
#endif
#endif

        public void Dispose()
        {
#if UNITY_2022_1_OR_NEWER && !UNITY_6000_0_OR_NEWER
            m_LowResHandle?.Release();
            m_LowResHandle = null;
#elif !UNITY_6000_0_OR_NEWER
            if (m_LowResTexture != null)
            {
                m_LowResTexture.Release();
                Object.Destroy(m_LowResTexture);
                m_LowResTexture = null;
            }
#endif

            foreach (var palette in m_PresetPalettes.Values)
            {
                if (palette != null)
                {
                    Object.Destroy(palette);
                }
            }
            m_PresetPalettes.Clear();

            if (m_Material != null)
            {
                Object.Destroy(m_Material);
            }
        }

        /// <summary>
        /// Retorna a textura de paleta (16x1, cacheada) do preset e, em
        /// <paramref name="paletteSize"/>, o número de cores que o preset define
        /// de fato. O shader usa esse número para não comparar contra os slots
        /// não preenchidos da textura.
        /// </summary>
        private Texture2D GetPresetPalette(PixelCameraRenderFeature.PalettePreset preset, out int paletteSize)
        {
            // Reutilizar a textura de paleta (criar uma nova a cada frame causaria leak de memória)
            if (m_PresetPalettes.TryGetValue(preset, out var cached) && cached != null)
            {
                paletteSize = m_PresetPaletteSizes.TryGetValue(preset, out var cachedSize) ? cachedSize : 16;
                return cached;
            }

            // Criar paletas programaticamente
            var palette = new Texture2D(16, 1, TextureFormat.RGBA32, false);
            palette.filterMode = FilterMode.Point;
            // Clamp (e não o padrão Repeat) para a amostragem nunca dar a volta
            // na textura.
            palette.wrapMode = TextureWrapMode.Clamp;

            Color[] colors;

            switch (preset)
            {
                case PixelCameraRenderFeature.PalettePreset.GameBoy:
                    colors = new Color[]
                    {
                        new Color(0.06f, 0.21f, 0.14f), // Preto esverdeado
                        new Color(0.18f, 0.38f, 0.18f),
                        new Color(0.52f, 0.65f, 0.20f),
                        new Color(0.85f, 0.89f, 0.47f), // Branco esverdeado
                    };
                    break;

                case PixelCameraRenderFeature.PalettePreset.NES:
                    colors = new Color[]
                    {
                        new Color(0.0f, 0.0f, 0.0f),
                        new Color(1.0f, 1.0f, 1.0f),
                        new Color(0.75f, 0.75f, 0.75f),
                        new Color(0.5f, 0.5f, 0.5f),
                        new Color(1.0f, 0.0f, 0.0f),
                        new Color(0.0f, 1.0f, 0.0f),
                        new Color(0.0f, 0.0f, 1.0f),
                        new Color(1.0f, 1.0f, 0.0f),
                        new Color(1.0f, 0.0f, 1.0f),
                        new Color(0.0f, 1.0f, 1.0f),
                        new Color(1.0f, 0.5f, 0.0f),
                        new Color(0.5f, 0.0f, 1.0f),
                        new Color(0.0f, 0.5f, 1.0f),
                        new Color(1.0f, 0.0f, 0.5f),
                        new Color(0.5f, 1.0f, 0.0f),
                        new Color(0.0f, 1.0f, 0.5f),
                    };
                    break;

                case PixelCameraRenderFeature.PalettePreset.CGA:
                    colors = new Color[]
                    {
                        new Color(0.0f, 0.0f, 0.0f),
                        new Color(0.33f, 1.0f, 1.0f),
                        new Color(1.0f, 0.33f, 1.0f),
                        new Color(1.0f, 1.0f, 1.0f),
                    };
                    break;

                case PixelCameraRenderFeature.PalettePreset.PICO8:
                    colors = new Color[]
                    {
                        new Color(0.0f, 0.0f, 0.0f),       // 0: Preto
                        new Color(0.12f, 0.12f, 0.12f),    // 1: Cinza escuro
                        new Color(0.5f, 0.5f, 0.5f),       // 2: Cinza
                        new Color(0.9f, 0.9f, 0.9f),       // 3: Branco
                        new Color(1.0f, 0.0f, 0.2f),       // 4: Vermelho
                        new Color(1.0f, 0.6f, 0.0f),       // 5: Laranja
                        new Color(1.0f, 0.9f, 0.0f),       // 6: Amarelo
                        new Color(0.0f, 0.8f, 0.2f),       // 7: Verde
                        new Color(0.2f, 0.5f, 1.0f),       // 8: Azul claro
                        new Color(0.0f, 0.2f, 0.8f),       // 9: Azul escuro
                        new Color(0.6f, 0.2f, 0.8f),       // 10: Roxo
                        new Color(1.0f, 0.5f, 0.8f),       // 11: Rosa
                        new Color(0.8f, 0.6f, 0.4f),       // 12: Marrom
                        new Color(0.4f, 0.8f, 0.6f),       // 13: Verde claro
                        new Color(0.6f, 0.4f, 0.2f),       // 14: Marrom escuro
                        new Color(0.8f, 0.2f, 0.4f),       // 15: Vermelho escuro
                    };
                    break;

                case PixelCameraRenderFeature.PalettePreset.GBColor:
                    colors = new Color[]
                    {
                        new Color(0.0f, 0.0f, 0.0f),
                        new Color(0.25f, 0.25f, 0.25f),
                        new Color(0.5f, 0.5f, 0.5f),
                        new Color(0.75f, 0.75f, 0.75f),
                        new Color(1.0f, 1.0f, 1.0f),
                        new Color(1.0f, 0.0f, 0.0f),
                        new Color(0.0f, 1.0f, 0.0f),
                        new Color(0.0f, 0.0f, 1.0f),
                        new Color(1.0f, 1.0f, 0.0f),
                        new Color(1.0f, 0.0f, 1.0f),
                        new Color(0.0f, 1.0f, 1.0f),
                        new Color(1.0f, 0.5f, 0.0f),
                        new Color(0.5f, 0.0f, 1.0f),
                        new Color(0.0f, 0.5f, 1.0f),
                        new Color(0.5f, 1.0f, 0.0f),
                        new Color(1.0f, 0.0f, 0.5f),
                    };
                    break;

                case PixelCameraRenderFeature.PalettePreset.Grayscale:
                    colors = new Color[16];
                    for (int i = 0; i < 16; i++)
                    {
                        float val = i / 15.0f;
                        colors[i] = new Color(val, val, val, 1.0f);
                    }
                    break;

                case PixelCameraRenderFeature.PalettePreset.Binary:
                default:
                    colors = new Color[]
                    {
                        new Color(0.0f, 0.0f, 0.0f),
                        new Color(1.0f, 1.0f, 1.0f),
                    };
                    break;
            }

            // Aplicar cores à textura
            for (int i = 0; i < colors.Length && i < 16; i++)
            {
                palette.SetPixel(i, 0, colors[i]);
            }

            palette.Apply();

            paletteSize = Mathf.Clamp(colors.Length, 1, 16);
            m_PresetPalettes[preset] = palette;
            m_PresetPaletteSizes[preset] = paletteSize;
            return palette;
        }
    }
}
