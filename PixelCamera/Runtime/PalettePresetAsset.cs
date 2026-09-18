using UnityEngine;

namespace PixelCamera
{
    /// <summary>
    /// ScriptableObject para armazenar presets de paleta
    /// </summary>
    [CreateAssetMenu(fileName = "NewPalettePreset", menuName = "Pixel Camera/Palette Preset")]
    public class PalettePresetAsset : ScriptableObject
    {
        public string paletteName = "Custom Palette";
        
        [Tooltip("Cores da paleta (máximo 16)")]
        [ColorUsage(false)]
        public Color[] colors = new Color[16];
        
        [Tooltip("Descrição da paleta")]
        [TextArea]
        public string description;
        
        /// <summary>
        /// Converte para Texture2D para uso no shader
        /// </summary>
        public Texture2D ToTexture()
        {
            int size = Mathf.Min(colors.Length, 16);
            var texture = new Texture2D(size, 1, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            
            for (int i = 0; i < size; i++)
            {
                texture.SetPixel(i, 0, colors[i]);
            }
            
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// Importa cores de uma Texture2D
        /// </summary>
        public void FromTexture(Texture2D source)
        {
            int size = Mathf.Min(source.width, 16);
            colors = new Color[size];
            
            for (int i = 0; i < size; i++)
            {
                colors[i] = source.GetPixel(i, 0);
            }
        }
        
        /// <summary>
        /// Cria preset GameBoy
        /// </summary>
        public static PalettePresetAsset CreateGameBoy()
        {
            var preset = CreateInstance<PalettePresetAsset>();
            preset.paletteName = "GameBoy";
            preset.description = "Paleta clássica do GameBoy original - 4 tons de verde";
            preset.colors = new Color[]
            {
                new Color(0.06f, 0.21f, 0.14f),
                new Color(0.18f, 0.38f, 0.18f),
                new Color(0.52f, 0.65f, 0.20f),
                new Color(0.85f, 0.89f, 0.47f),
            };
            return preset;
        }
        
        /// <summary>
        /// Cria preset PICO-8
        /// </summary>
        public static PalettePresetAsset CreatePICO8()
        {
            var preset = CreateInstance<PalettePresetAsset>();
            preset.paletteName = "PICO-8";
            preset.description = "Paleta de 16 cores do fantasy console PICO-8";
            preset.colors = new Color[]
            {
                new Color(0.0f, 0.0f, 0.0f),
                new Color(0.12f, 0.12f, 0.12f),
                new Color(0.5f, 0.5f, 0.5f),
                new Color(0.9f, 0.9f, 0.9f),
                new Color(1.0f, 0.0f, 0.2f),
                new Color(1.0f, 0.6f, 0.0f),
                new Color(1.0f, 0.9f, 0.0f),
                new Color(0.0f, 0.8f, 0.2f),
                new Color(0.2f, 0.5f, 1.0f),
                new Color(0.0f, 0.2f, 0.8f),
                new Color(0.6f, 0.2f, 0.8f),
                new Color(1.0f, 0.5f, 0.8f),
                new Color(0.8f, 0.6f, 0.4f),
                new Color(0.4f, 0.8f, 0.6f),
                new Color(0.6f, 0.4f, 0.2f),
                new Color(0.8f, 0.2f, 0.4f),
            };
            return preset;
        }
    }
}
