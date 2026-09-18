using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PixelCamera
{
    /// <summary>
    /// Pixel Camera Render Feature para URP
    /// Aplica efeitos de pixelização, paleta limitada, dithering e CRT
    /// </summary>
    public class PixelCameraRenderFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class PixelSettings
        {
            [Header("Resolução")]
            [Tooltip("Largura em pixels (deixe 0 para usar resolução customizada)")]
            public int pixelWidth = 320;
            
            [Tooltip("Altura em pixels (deixe 0 para usar resolução customizada)")]
            public int pixelHeight = 180;
            
            [Tooltip("Snap para grid de pixels perfeito")]
            public bool snapToPixelGrid = true;
        }

        [System.Serializable]
        public class PaletteSettings
        {
            [Header("Paleta de Cores")]
            [Tooltip("Habilitar limitação de paleta")]
            public bool enablePalette = true;
            
            [Tooltip("Preset de paleta predefinido")]
            public PalettePreset preset = PalettePreset.PICO8;
            
            [Tooltip("Paleta customizada (sobrescreve preset se não for nulo)")]
            public Texture2D customPalette;
            
            [Tooltip("Quantidade de cores (usado quando paleta custom não é definida)")]
            [Range(2, 256)]
            public int colorCount = 16;
            
            [Tooltip("Quantização de cor (0 = sem quantização, 1 = máximo)")]
            [Range(0f, 1f)]
            public float colorQuantization = 0.5f;
        }

        [System.Serializable]
        public class DitherSettings
        {
            [Header("Dithering")]
            [Tooltip("Habilitar dithering")]
            public bool enableDithering = false;
            
            [Tooltip("Tipo de dithering")]
            public DitherType ditherType = DitherType.Bayer4x4;
            
            [Tooltip("Intensidade do dithering")]
            [Range(0f, 1f)]
            public float intensity = 0.5f;
        }

        [System.Serializable]
        public class CRTSettings
        {
            [Header("Efeito CRT")]
            [Tooltip("Habilitar efeito CRT")]
            public bool enableCRT = false;
            
            [Header("Scanlines")]
            [Tooltip("Intensidade das scanlines")]
            [Range(0f, 1f)]
            public float scanlineIntensity = 0.3f;
            
            [Tooltip("Espessura das scanlines")]
            [Range(1f, 4f)]
            public float scanlineThickness = 2f;
            
            [Header("Bloom/Glow")]
            [Tooltip("Intensidade do bloom/glow")]
            [Range(0f, 1f)]
            public float bloomIntensity = 0.2f;
            
            [Tooltip("Raio do bloom")]
            [Range(1f, 10f)]
            public float bloomRadius = 3f;
            
            [Header("Curvatura")]
            [Tooltip("Intensidade da curvatura da tela")]
            [Range(0f, 1f)]
            public float curvatureIntensity = 0.1f;
            
            [Header("Vinheta")]
            [Tooltip("Intensidade da vinheta")]
            [Range(0f, 1f)]
            public float vignetteIntensity = 0.3f;
        }

        public enum PalettePreset
        {
            GameBoy,
            NES,
            CGA,
            PICO8,
            GBColor,
            Grayscale,
            Binary
        }

        public enum DitherType
        {
            Bayer2x2,
            Bayer4x4,
            Bayer8x8,
            FloydSteinberg
        }

        [Header("Configurações Gerais")]
        public bool enabled = true;
        
        [Header("Pixelização")]
        public PixelSettings pixelSettings = new PixelSettings();
        
        [Header("Paleta")]
        public PaletteSettings paletteSettings = new PaletteSettings();
        
        [Header("Dithering")]
        public DitherSettings ditherSettings = new DitherSettings();
        
        [Header("CRT")]
        public CRTSettings crtSettings = new CRTSettings();

        [Header("Render Pass")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        private PixelCameraRenderPass m_RenderPass;

        public override void Create()
        {
            m_RenderPass = new PixelCameraRenderPass(this)
            {
                renderPassEvent = renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!enabled || m_RenderPass == null) return;

#if !UNITY_6000_0_OR_NEWER
            // No Unity 6 (URP 17+) não é preciso configurar um target manualmente:
            // o Render Graph obtém a cor ativa da câmera via UniversalResourceData.
#if UNITY_2022_1_OR_NEWER
            m_RenderPass.Setup(renderer.cameraColorTargetHandle);
#else
            m_RenderPass.Setup(renderer.cameraColorTarget);
#endif
#endif
            renderer.EnqueuePass(m_RenderPass);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_RenderPass?.Dispose();
            }
        }
    }
}
