using UnityEngine;

namespace PixelCamera
{
    /// <summary>
    /// Componente para controlar a Pixel Camera em runtime
    /// Adicione ao GameObject com a Camera
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PixelCameraController : MonoBehaviour
    {
        [Header("Referências")]
        [Tooltip("PixelCameraRenderFeature configurado")]
        [SerializeField] private PixelCameraRenderFeature renderFeature;

        /// <summary>
        /// Render feature configurado (preenchido pelo PixelCameraAutoSetup ou via inspector).
        /// </summary>
        public PixelCameraRenderFeature RenderFeature
        {
            get => renderFeature;
            set => renderFeature = value;
        }

        [Header("Controles Runtime")]
        [Tooltip("Permite ajustar resolução em runtime")]
        [SerializeField] private bool enableRuntimeControls = true;
        
        [SerializeField] private KeyCode toggleEnabledKey = KeyCode.F1;
        [SerializeField] private KeyCode toggleCRTKey = KeyCode.F2;
        [SerializeField] private KeyCode toggleDitherKey = KeyCode.F3;
        [SerializeField] private KeyCode togglePaletteKey = KeyCode.F4;

        [Header("Resolução Dinâmica")]
        [Tooltip("Ajustar resolução com scroll do mouse")]
        [SerializeField] private bool enableScrollResolution = true;
        [SerializeField] private int minResolution = 160;
        [SerializeField] private int maxResolution = 640;
        [SerializeField] private float scrollSensitivity = 10f;

        private Camera m_Camera;

        private void Awake()
        {
            m_Camera = GetComponent<Camera>();

            // O PixelCameraAutoSetup adiciona este componente em runtime e só resolve o
            // RenderFeature alguns instantes depois (ele é um sub-asset do Renderer Asset).
            // Avisar nesse caso seria um falso alarme.
            if (renderFeature == null && GetComponent<PixelCameraAutoSetup>() == null)
            {
                Debug.LogWarning("[PixelCamera] Nenhum RenderFeature configurado! Configure no Universal Render Pipeline Asset " +
                    "ou adicione o componente 'Pixel Camera Auto Setup' à câmera.");
            }
        }

        private void Update()
        {
            if (!enableRuntimeControls || renderFeature == null) return;

            // Toggle enabled
            if (Input.GetKeyDown(toggleEnabledKey))
            {
                renderFeature.enabled = !renderFeature.enabled;
                Debug.Log($"[PixelCamera] Enabled: {renderFeature.enabled}");
            }

            // Toggle CRT
            if (Input.GetKeyDown(toggleCRTKey))
            {
                renderFeature.crtSettings.enableCRT = !renderFeature.crtSettings.enableCRT;
                Debug.Log($"[PixelCamera] CRT: {renderFeature.crtSettings.enableCRT}");
            }

            // Toggle Dither
            if (Input.GetKeyDown(toggleDitherKey))
            {
                renderFeature.ditherSettings.enableDithering = !renderFeature.ditherSettings.enableDithering;
                Debug.Log($"[PixelCamera] Dithering: {renderFeature.ditherSettings.enableDithering}");
            }

            // Toggle Palette
            if (Input.GetKeyDown(togglePaletteKey))
            {
                renderFeature.paletteSettings.enablePalette = !renderFeature.paletteSettings.enablePalette;
                Debug.Log($"[PixelCamera] Palette: {renderFeature.paletteSettings.enablePalette}");
            }

            // Scroll para ajustar resolução
            if (enableScrollResolution)
            {
                float scroll = Input.mouseScrollDelta.y;
                if (scroll != 0)
                {
                    int currentWidth = renderFeature.pixelSettings.pixelWidth;
                    int newWidth = Mathf.Clamp(
                        currentWidth + (int)(scroll * scrollSensitivity),
                        minResolution,
                        maxResolution
                    );
                    
                    renderFeature.pixelSettings.pixelWidth = newWidth;
                    
                    // Manter aspect ratio
                    float aspect = (float)m_Camera.pixelWidth / m_Camera.pixelHeight;
                    renderFeature.pixelSettings.pixelHeight = Mathf.RoundToInt(newWidth / aspect);
                    
                    Debug.Log($"[PixelCamera] Resolution: {newWidth}x{renderFeature.pixelSettings.pixelHeight}");
                }
            }
        }

        /// <summary>
        /// Ajusta a resolução de pixel em runtime
        /// </summary>
        public void SetPixelResolution(int width, int height)
        {
            if (renderFeature == null) return;
            
            renderFeature.pixelSettings.pixelWidth = width;
            renderFeature.pixelSettings.pixelHeight = height;
        }

        /// <summary>
        /// Define qual preset de paleta usar
        /// </summary>
        public void SetPalettePreset(PixelCameraRenderFeature.PalettePreset preset)
        {
            if (renderFeature == null) return;
            
            renderFeature.paletteSettings.preset = preset;
        }

        /// <summary>
        /// Define uma paleta customizada
        /// </summary>
        public void SetCustomPalette(Texture2D palette)
        {
            if (renderFeature == null) return;
            
            renderFeature.paletteSettings.customPalette = palette;
        }

        /// <summary>
        /// Gera uma paleta aleatória e aplica
        /// </summary>
        public void GenerateRandomPalette(int seed = -1)
        {
            if (renderFeature == null) return;
            
            var palette = PaletteUtility.CreateRandomPalette(16, seed);
            renderFeature.paletteSettings.customPalette = palette;
        }

        /// <summary>
        /// Habilita/desabilita efeito CRT
        /// </summary>
        public void SetCRTEffect(bool enabled)
        {
            if (renderFeature == null) return;
            
            renderFeature.crtSettings.enableCRT = enabled;
        }

        /// <summary>
        /// Habilita/desabilita dithering
        /// </summary>
        public void SetDithering(bool enabled)
        {
            if (renderFeature == null) return;
            
            renderFeature.ditherSettings.enableDithering = enabled;
        }
    }
}
