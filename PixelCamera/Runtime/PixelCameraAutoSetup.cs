using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PixelCamera
{
    /// <summary>
    /// Componente que configura automaticamente o Pixel Camera
    /// Adicione à Main Camera e configure o Renderer no Inspector
    /// 
    /// INSTRUÇÕES:
    /// 1. Adicione este componente à Main Camera
    /// 2. Arraste o PixelCameraRenderer para o campo "rendererAsset"
    /// 3. Pressione Play - tudo será configurado automaticamente!
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Pixel Camera/Pixel Camera Auto Setup")]
    public class PixelCameraAutoSetup : MonoBehaviour
    {
        [Header("⚙️ Configuração")]
        [Tooltip("Arraste aqui o PixelCameraRenderer (o Renderer Asset que tem o Render Feature)")]
        [SerializeField] private ScriptableRendererData rendererAsset;
        
        [Header("📐 Resolução Pixel")]
        [SerializeField] private int pixelWidth = 320;
        [SerializeField] private int pixelHeight = 180;
        
        [Header("🎨 Paleta")]
        [SerializeField] private bool enablePalette = true;
        [SerializeField] private PixelCameraRenderFeature.PalettePreset preset = PixelCameraRenderFeature.PalettePreset.PICO8;
        
        [Header("🔲 Dithering")]
        [SerializeField] private bool enableDithering = false;
        [SerializeField] private PixelCameraRenderFeature.DitherType ditherType = PixelCameraRenderFeature.DitherType.Bayer4x4;
        [SerializeField] private float ditherIntensity = 0.3f;
        
        [Header("📺 CRT")]
        [SerializeField] private bool enableCRT = false;
        [SerializeField] private float scanlineIntensity = 0.3f;
        [SerializeField] private float bloomIntensity = 0.2f;
        [SerializeField] private float curvatureIntensity = 0.1f;
        [SerializeField] private float vignetteIntensity = 0.3f;
        
        [Header("🎮 Preset Rápido")]
        [Tooltip("Aplicar preset ao invés das configurações acima")]
        [SerializeField] private QuickPreset quickPreset = QuickPreset.None;

        public enum QuickPreset
        {
            None,
            GameBoy,
            Retro,
            CRT_Classic,
            Modern_PixelArt
        }

        private Camera m_Camera;
        private PixelCameraController m_Controller;

        private void Awake()
        {
            m_Camera = GetComponent<Camera>();
            
            // Adicionar controller automaticamente
            m_Controller = GetComponent<PixelCameraController>();
            if (m_Controller == null)
            {
                m_Controller = gameObject.AddComponent<PixelCameraController>();
            }
        }

        private void Start()
        {
            // Aguardar um frame para garantir que o RenderFeature está carregado
            Invoke(nameof(ApplySettings), 0.1f);
        }

        private void ApplySettings()
        {
            // Encontrar o RenderFeature no renderer
            var renderFeature = FindPixelCameraRenderFeature();
            
            if (renderFeature == null)
            {
                Debug.LogError("[PixelCamera AutoSetup] PixelCameraRenderFeature não encontrado! " +
                    "Verifique se o Render Feature está adicionado ao Renderer.");
                return;
            }
            
            // Aplicar preset rápido se selecionado
            if (quickPreset != QuickPreset.None)
            {
                ApplyQuickPreset(renderFeature, quickPreset);
                Debug.Log($"[PixelCamera AutoSetup] Preset '{quickPreset}' aplicado!");
                return;
            }
            
            // Aplicar configurações manuais
            ApplyManualSettings(renderFeature);
            
            // Conectar ao controller
            m_Controller.renderFeature = renderFeature;
            
            Debug.Log("[PixelCamera AutoSetup] Configurações aplicadas com sucesso!");
        }

        private void ApplyManualSettings(PixelCameraRenderFeature feature)
        {
            feature.enabled = true;
            
            // Pixelização
            feature.pixelSettings.pixelWidth = pixelWidth;
            feature.pixelSettings.pixelHeight = pixelHeight;
            feature.pixelSettings.snapToPixelGrid = true;
            
            // Paleta
            feature.paletteSettings.enablePalette = enablePalette;
            feature.paletteSettings.preset = preset;
            
            // Dithering
            feature.ditherSettings.enableDithering = enableDithering;
            feature.ditherSettings.ditherType = ditherType;
            feature.ditherSettings.intensity = ditherIntensity;
            
            // CRT
            feature.crtSettings.enableCRT = enableCRT;
            feature.crtSettings.scanlineIntensity = scanlineIntensity;
            feature.crtSettings.bloomIntensity = bloomIntensity;
            feature.crtSettings.curvatureIntensity = curvatureIntensity;
            feature.crtSettings.vignetteIntensity = vignetteIntensity;
        }

        private void ApplyQuickPreset(PixelCameraRenderFeature feature, QuickPreset preset)
        {
            feature.enabled = true;
            
            switch (preset)
            {
                case QuickPreset.GameBoy:
                    feature.pixelSettings.pixelWidth = 160;
                    feature.pixelSettings.pixelHeight = 144;
                    feature.pixelSettings.snapToPixelGrid = true;
                    feature.paletteSettings.enablePalette = true;
                    feature.paletteSettings.preset = PixelCameraRenderFeature.PalettePreset.GameBoy;
                    feature.ditherSettings.enableDithering = true;
                    feature.ditherSettings.ditherType = PixelCameraRenderFeature.DitherType.Bayer2x2;
                    feature.ditherSettings.intensity = 0.5f;
                    feature.crtSettings.enableCRT = false;
                    break;
                    
                case QuickPreset.Retro:
                    feature.pixelSettings.pixelWidth = 320;
                    feature.pixelSettings.pixelHeight = 180;
                    feature.pixelSettings.snapToPixelGrid = true;
                    feature.paletteSettings.enablePalette = true;
                    feature.paletteSettings.preset = PixelCameraRenderFeature.PalettePreset.PICO8;
                    feature.ditherSettings.enableDithering = true;
                    feature.ditherSettings.ditherType = PixelCameraRenderFeature.DitherType.Bayer4x4;
                    feature.ditherSettings.intensity = 0.3f;
                    feature.crtSettings.enableCRT = true;
                    feature.crtSettings.scanlineIntensity = 0.2f;
                    feature.crtSettings.bloomIntensity = 0.1f;
                    feature.crtSettings.curvatureIntensity = 0.05f;
                    feature.crtSettings.vignetteIntensity = 0.2f;
                    break;
                    
                case QuickPreset.CRT_Classic:
                    feature.pixelSettings.pixelWidth = 640;
                    feature.pixelSettings.pixelHeight = 360;
                    feature.pixelSettings.snapToPixelGrid = true;
                    feature.paletteSettings.enablePalette = true;
                    feature.paletteSettings.preset = PixelCameraRenderFeature.PalettePreset.NES;
                    feature.ditherSettings.enableDithering = false;
                    feature.crtSettings.enableCRT = true;
                    feature.crtSettings.scanlineIntensity = 0.4f;
                    feature.crtSettings.scanlineThickness = 3f;
                    feature.crtSettings.bloomIntensity = 0.3f;
                    feature.crtSettings.bloomRadius = 5f;
                    feature.crtSettings.curvatureIntensity = 0.15f;
                    feature.crtSettings.vignetteIntensity = 0.4f;
                    break;
                    
                case QuickPreset.Modern_PixelArt:
                    feature.pixelSettings.pixelWidth = 640;
                    feature.pixelSettings.pixelHeight = 360;
                    feature.pixelSettings.snapToPixelGrid = true;
                    feature.paletteSettings.enablePalette = true;
                    feature.paletteSettings.preset = PixelCameraRenderFeature.PalettePreset.PICO8;
                    feature.ditherSettings.enableDithering = false;
                    feature.crtSettings.enableCRT = false;
                    break;
            }
            
            // Conectar ao controller
            m_Controller.renderFeature = feature;
        }

        private PixelCameraRenderFeature FindPixelCameraRenderFeature()
        {
            // Tentar encontrar via reflection no renderer ativo
            var renderPipelineAsset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (renderPipelineAsset == null)
            {
                Debug.LogError("[PixelCamera AutoSetup] Nenhum Render Pipeline configurado! " +
                    "Vá em Edit > Project Settings > Graphics e configure o Universal Render Pipeline.");
                return null;
            }
            
            // Procurar o render feature no renderer
            // Nota: Este é um approach simplificado. Em produção, você pode
            // querer armazenar uma referência direta ao RenderFeature.
            
            // Procurar em todos os objetos carregados
            var features = FindObjectsOfType<PixelCameraRenderFeature>();
            if (features.Length > 0)
            {
                return features[0];
            }
            
            // Se não encontrou, criar um novo
            Debug.LogWarning("[PixelCamera AutoSetup] RenderFeature não encontrado. " +
                "Certifique-se de adicionar o 'Pixel Camera Render Feature' ao URP Renderer.");
            return null;
        }
    }
}
