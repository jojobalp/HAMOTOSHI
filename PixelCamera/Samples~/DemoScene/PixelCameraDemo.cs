using UnityEngine;

namespace PixelCamera
{
    /// <summary>
    /// Exemplo de uso da Pixel Camera em runtime
    /// </summary>
    public class PixelCameraDemo : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private PixelCameraController pixelCamera;
        [SerializeField] private PixelCameraRenderFeature renderFeature;
        
        [Header("Configurações Demo")]
        [SerializeField] private bool cyclePalettes = false;
        [SerializeField] private float cycleInterval = 3f;
        [SerializeField] private bool autoAnimate = true;
        
        private float cycleTimer;
        private int currentPaletteIndex;
        
        private void Start()
        {
            if (pixelCamera == null)
            {
                pixelCamera = GetComponent<PixelCameraController>();
            }
            
            if (renderFeature == null && pixelCamera != null)
            {
                // Tentar obter via reflection ou campo público
            }
        }
        
        private void Update()
        {
            // Ciclar paletas
            if (cyclePalettes && renderFeature != null)
            {
                cycleTimer += Time.deltaTime;
                if (cycleTimer >= cycleInterval)
                {
                    cycleTimer = 0f;
                    currentPaletteIndex = (currentPaletteIndex + 1) % 7;
                    
                    renderFeature.paletteSettings.preset = (PixelCameraRenderFeature.PalettePreset)currentPaletteIndex;
                    Debug.Log($"[Demo] Palette changed to: {renderFeature.paletteSettings.preset}");
                }
            }
            
            // Animação automática
            if (autoAnimate && Input.GetKeyDown(KeyCode.Alpha1))
            {
                DemoGameBoy();
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                DemoRetro();
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                DemoCRT();
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                DemoModern();
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                DemoRandom();
            }
        }
        
        /// <summary>
        /// Preset GameBoy completo
        /// </summary>
        public void DemoGameBoy()
        {
            if (renderFeature == null) return;
            
            renderFeature.pixelSettings.pixelWidth = 160;
            renderFeature.pixelSettings.pixelHeight = 144;
            renderFeature.paletteSettings.enablePalette = true;
            renderFeature.paletteSettings.preset = PixelCameraRenderFeature.PalettePreset.GameBoy;
            renderFeature.ditherSettings.enableDithering = true;
            renderFeature.ditherSettings.ditherType = PixelCameraRenderFeature.DitherType.Bayer2x2;
            renderFeature.ditherSettings.intensity = 0.5f;
            renderFeature.crtSettings.enableCRT = false;
            
            Debug.Log("[Demo] GameBoy mode!");
        }
        
        /// <summary>
        /// Preset retrô completo
        /// </summary>
        public void DemoRetro()
        {
            if (renderFeature == null) return;
            
            renderFeature.pixelSettings.pixelWidth = 320;
            renderFeature.pixelSettings.pixelHeight = 180;
            renderFeature.paletteSettings.enablePalette = true;
            renderFeature.paletteSettings.preset = PixelCameraRenderFeature.PalettePreset.PICO8;
            renderFeature.ditherSettings.enableDithering = true;
            renderFeature.ditherSettings.ditherType = PixelCameraRenderFeature.DitherType.Bayer4x4;
            renderFeature.ditherSettings.intensity = 0.3f;
            renderFeature.crtSettings.enableCRT = true;
            renderFeature.crtSettings.scanlineIntensity = 0.2f;
            renderFeature.crtSettings.bloomIntensity = 0.1f;
            renderFeature.crtSettings.curvatureIntensity = 0.05f;
            
            Debug.Log("[Demo] Retro mode!");
        }
        
        /// <summary>
        /// Preset CRT clássico
        /// </summary>
        public void DemoCRT()
        {
            if (renderFeature == null) return;
            
            renderFeature.pixelSettings.pixelWidth = 640;
            renderFeature.pixelSettings.pixelHeight = 360;
            renderFeature.paletteSettings.enablePalette = true;
            renderFeature.paletteSettings.preset = PixelCameraRenderFeature.PalettePreset.NES;
            renderFeature.ditherSettings.enableDithering = false;
            renderFeature.crtSettings.enableCRT = true;
            renderFeature.crtSettings.scanlineIntensity = 0.4f;
            renderFeature.crtSettings.scanlineThickness = 3f;
            renderFeature.crtSettings.bloomIntensity = 0.3f;
            renderFeature.crtSettings.bloomRadius = 5f;
            renderFeature.crtSettings.curvatureIntensity = 0.15f;
            renderFeature.crtSettings.vignetteIntensity = 0.4f;
            
            Debug.Log("[Demo] CRT mode!");
        }
        
        /// <summary>
        /// Preset moderno (pixel art atual)
        /// </summary>
        public void DemoModern()
        {
            if (renderFeature == null) return;
            
            renderFeature.pixelSettings.pixelWidth = 640;
            renderFeature.pixelSettings.pixelHeight = 360;
            renderFeature.paletteSettings.enablePalette = true;
            renderFeature.paletteSettings.preset = PixelCameraRenderFeature.PalettePreset.PICO8;
            renderFeature.ditherSettings.enableDithering = false;
            renderFeature.crtSettings.enableCRT = false;
            
            Debug.Log("[Demo] Modern pixel art mode!");
        }
        
        /// <summary>
        /// Gera paleta aleatória e aplica
        /// </summary>
        public void DemoRandom()
        {
            if (pixelCamera == null) return;
            
            pixelCamera.GenerateRandomPalette(Random.Range(0, 99999));
            
            renderFeature.pixelSettings.pixelWidth = 320;
            renderFeature.pixelSettings.pixelHeight = 180;
            renderFeature.ditherSettings.enableDithering = true;
            renderFeature.ditherSettings.intensity = 0.4f;
            renderFeature.crtSettings.enableCRT = false;
            
            Debug.Log("[Demo] Random palette!");
        }
    }
}
