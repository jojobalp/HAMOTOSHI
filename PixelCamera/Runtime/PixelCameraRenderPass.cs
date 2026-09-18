using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PixelCamera
{
    /// <summary>
    /// Render Pass que executa o shader de pixelização
    /// </summary>
    public class PixelCameraRenderPass : ScriptableRenderPass
    {
        private const string k_RenderTag = "Pixel Camera";
        
        private PixelCameraRenderFeature m_Feature;
        private RenderTargetIdentifier m_CameraColorTarget;
        private Material m_Material;
        private RenderTexture m_LowResTexture;
        private int m_PixelWidth;
        private int m_PixelHeight;

        // Shader property IDs
        private static readonly int PixelResolution = Shader.PropertyToID("_PixelResolution");
        private static readonly int PaletteEnabled = Shader.PropertyToID("_PaletteEnabled");
        private static readonly int CustomPalette = Shader.PropertyToID("_CustomPalette");
        private static readonly int ColorCount = Shader.PropertyToID("_ColorCount");
        private static readonly int ColorQuantization = Shader.PropertyToID("_ColorQuantization");
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
        private static readonly int Time = Shader.PropertyToID("_Time");

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
        }

        public void Setup(RenderTargetIdentifier cameraColorTarget)
        {
            m_CameraColorTarget = cameraColorTarget;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            var screenDesc = cameraData.cameraTargetDescriptor;
            
            // Calcular resolução de pixel
            m_PixelWidth = m_Feature.pixelSettings.pixelWidth > 0 
                ? m_Feature.pixelSettings.pixelWidth 
                : screenDesc.width;
            m_PixelHeight = m_Feature.pixelSettings.pixelHeight > 0 
                ? m_Feature.pixelSettings.pixelHeight 
                : screenDesc.height;

            // Snap to pixel grid
            if (m_Feature.pixelSettings.snapToPixelGrid)
            {
                var aspect = (float)screenDesc.width / screenDesc.height;
                m_PixelWidth = Mathf.RoundToInt(m_PixelWidth / aspect) * Mathf.RoundToInt(aspect);
                m_PixelHeight = Mathf.RoundToInt(m_PixelHeight);
            }

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

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null || !m_Feature.enabled) return;

            CommandBuffer cmd = CommandBufferPool.Get(k_RenderTag);

            // Configurar parâmetros do shader
            m_Material.SetVector(PixelResolution, new Vector4(m_PixelWidth, m_PixelHeight, 0, 0));
            m_Material.SetFloat(PaletteEnabled, m_Feature.paletteSettings.enablePalette ? 1.0f : 0.0f);
            
            // Paleta
            if (m_Feature.paletteSettings.customPalette != null)
            {
                m_Material.SetTexture(CustomPalette, m_Feature.paletteSettings.customPalette);
            }
            else
            {
                // Usar preset de paleta
                var presetPalette = GetPresetPalette(m_Feature.paletteSettings.preset);
                m_Material.SetTexture(CustomPalette, presetPalette);
            }
            
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
            m_Material.SetFloat(Time, Time.time);

            // Renderizar para textura de baixa resolução
            Blit(cmd, m_CameraColorTarget, m_LowResTexture, m_Material);
            
            // Blit de volta para a câmera com filtro point (pixelado)
            cmd.SetGlobalTexture("_MainTex", m_LowResTexture);
            Blit(cmd, m_LowResTexture, m_CameraColorTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            if (m_LowResTexture != null)
            {
                m_LowResTexture.Release();
                Object.Destroy(m_LowResTexture);
            }
            
            if (m_Material != null)
            {
                Object.Destroy(m_Material);
            }
        }

        private Texture2D GetPresetPalette(PixelCameraRenderFeature.PalettePreset preset)
        {
            // Criar paletas programaticamente
            var palette = new Texture2D(16, 1, TextureFormat.RGBA32, false);
            palette.filterMode = FilterMode.Point;
            
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
            return palette;
        }
    }
}
