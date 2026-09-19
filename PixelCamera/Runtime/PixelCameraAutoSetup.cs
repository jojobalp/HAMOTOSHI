using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PixelCamera
{
    /// <summary>
    /// Componente que configura automaticamente o Pixel Camera.
    /// Adicione à Main Camera e configure o Renderer no Inspector.
    ///
    /// INSTRUÇÕES:
    /// 1. Adicione este componente à Main Camera
    /// 2. No seu Renderer Asset (Universal Renderer Data), clique em
    ///    "Add Renderer Feature" > "Pixel Camera Render Feature"
    /// 3. Arraste esse Renderer Asset para o campo "Renderer Asset"
    ///    (opcional: arraste o sub-asset do Render Feature para "Render Feature (direto)")
    /// 4. Pressione Play - tudo será configurado automaticamente!
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Por que a busca antiga nunca funcionava:</b> um <see cref="PixelCameraRenderFeature"/> é um
    /// <c>ScriptableRendererFeature</c>, ou seja, um <c>ScriptableObject</c> gravado como <b>sub-asset</b>
    /// dentro do Renderer Asset. <c>Object.FindObjectsOfType&lt;T&gt;()</c> só devolve objetos que estão
    /// <i>na cena</i> (MonoBehaviours/Componentes de GameObjects ativos), então a chamada
    /// <c>FindObjectsOfType&lt;PixelCameraRenderFeature&gt;()</c> sempre retornava um array vazio e o
    /// componente terminava com "PixelCameraRenderFeature não encontrado!". Além disso, em Unity 6 o
    /// <c>FindObjectsOfType</c> está obsoleto (aviso CS0618).
    /// </para>
    /// <para>
    /// A busca agora usa a API pública da URP <c>ScriptableRendererData.rendererFeatures</c>
    /// (disponível da URP 12 até a 17, ou seja, Unity 2021.3 → Unity 6) sobre o Renderer Asset
    /// referenciado no Inspector, com alternativas para os casos em que ele não foi atribuído.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Pixel Camera/Pixel Camera Auto Setup")]
    public class PixelCameraAutoSetup : MonoBehaviour
    {
        private const string LogPrefix = "[PixelCamera AutoSetup] ";

        // Os Renderer Assets são carregados junto com o pipeline; em cenas pesadas isso pode
        // acontecer depois do primeiro frame, então a busca é repetida algumas vezes.
        private const int MaxSearchAttempts = 8;
        private const float SearchInterval = 0.25f;

        [Header("⚙️ Configuração")]
        [Tooltip("Opcional, e tem prioridade sobre o resto.\n" +
                 "No Project, expanda o seu Renderer Asset e arraste o sub-asset " +
                 "'Pixel Camera Render Feature' para cá.")]
        [SerializeField] private PixelCameraRenderFeature renderFeatureOverride;

        [Tooltip("Arraste aqui o Renderer Asset (Universal Renderer Data) que tem o " +
                 "'Pixel Camera Render Feature' adicionado")]
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

        private PixelCameraController m_Controller;
        private int m_SearchAttempts;

        /// <summary>
        /// Render Feature encontrado e configurado por este componente
        /// (nulo enquanto a busca não tiver sucesso).
        /// </summary>
        public PixelCameraRenderFeature ActiveRenderFeature { get; private set; }

        /// <summary>
        /// Renderer Asset usado na busca (útil para atribuir via código).
        /// </summary>
        public ScriptableRendererData RendererAsset
        {
            get => rendererAsset;
            set => rendererAsset = value;
        }

        private void Awake()
        {
            // [RequireComponent(typeof(Camera))] garante a câmera; quem precisa dela é o controller.

            // Adicionar controller automaticamente
            m_Controller = GetComponent<PixelCameraController>();
            if (m_Controller == null)
            {
                m_Controller = gameObject.AddComponent<PixelCameraController>();
            }
        }

        private void Start()
        {
            // Aguardar um pouco para garantir que o pipeline (e os Renderer Assets) foram carregados
            Invoke(nameof(TryApplySettings), 0.1f);
        }

        private void TryApplySettings()
        {
            PixelCameraRenderFeature renderFeature = FindPixelCameraRenderFeature();

            if (renderFeature == null)
            {
                m_SearchAttempts++;

                if (m_SearchAttempts < MaxSearchAttempts)
                {
                    Invoke(nameof(TryApplySettings), SearchInterval);
                    return;
                }

                LogFeatureNotFound();
                return;
            }

            ApplySettings(renderFeature);
        }

        private void ApplySettings(PixelCameraRenderFeature renderFeature)
        {
            ActiveRenderFeature = renderFeature;

            // Conectar ao controller ANTES de aplicar qualquer coisa: é isso que faz os
            // atalhos (F1-F4) e o scroll de resolução funcionarem em runtime.
            if (m_Controller != null)
            {
                m_Controller.RenderFeature = renderFeature;
            }

            // O checkbox ao lado do nome do Render Feature no Renderer Asset é outra coisa
            // (ScriptableRendererFeature.isActive): se estiver desligado, a URP nem chama
            // AddRenderPasses e o efeito não aparece.
            if (!renderFeature.isActive)
            {
                Debug.LogWarning(LogPrefix + $"O Render Feature \"{renderFeature.name}\" está inativo no " +
                    "Renderer Asset (caixa de seleção ao lado do nome). Ligue-o, senão a URP não executa o pass.");
            }

            // Aplicar preset rápido se selecionado
            if (quickPreset != QuickPreset.None)
            {
                ApplyQuickPreset(renderFeature, quickPreset);
                Debug.Log(LogPrefix + $"Preset '{quickPreset}' aplicado em \"{renderFeature.name}\".");
                return;
            }

            // Aplicar configurações manuais
            ApplyManualSettings(renderFeature);
            Debug.Log(LogPrefix + $"Configurações aplicadas em \"{renderFeature.name}\".");
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
        }

        // ---------------------------------------------------------------- Busca

        /// <summary>
        /// Localiza o <see cref="PixelCameraRenderFeature"/> em uso, do jeito mais confiável
        /// para o menos confiável. Nunca usa <c>Object.FindObjectsOfType</c>, que não enxerga
        /// ScriptableObjects (o Render Feature é um sub-asset do Renderer Asset).
        /// </summary>
        private PixelCameraRenderFeature FindPixelCameraRenderFeature()
        {
            // 1) Referência direta: a mais confiável (funciona em build e não depende de busca).
            if (renderFeatureOverride != null)
            {
                return renderFeatureOverride;
            }

            // 2) Renderer Asset indicado no Inspector, via API pública da URP.
            if (rendererAsset != null)
            {
                PixelCameraRenderFeature fromRenderer = FindInRendererData(rendererAsset);

                if (fromRenderer != null)
                {
                    return fromRenderer;
                }

                Debug.LogWarning(LogPrefix + $"O Renderer Asset \"{rendererAsset.name}\" não tem o " +
                    "\"Pixel Camera Render Feature\". Selecione o asset e use \"Add Renderer Feature\" > " +
                    "\"Pixel Camera Render Feature\".");
            }

            // 3) Editor: varre todos os Renderer Assets do projeto.
#if UNITY_EDITOR
            PixelCameraRenderFeature fromProject = FindInProjectRendererAssets();
            if (fromProject != null)
            {
                return fromProject;
            }
#endif

            // 4) Último recurso: qualquer instância já carregada em memória.
            //    (Resources.FindObjectsOfTypeAll enxerga assets/sub-assets; diferente de
            //     Object.FindObjectsOfType, que só enxerga objetos de cena.)
            PixelCameraRenderFeature[] loaded = Resources.FindObjectsOfTypeAll<PixelCameraRenderFeature>();
            if (loaded != null)
            {
                foreach (PixelCameraRenderFeature feature in loaded)
                {
                    if (feature != null)
                    {
                        return feature;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Procura o Render Feature dentro de um Renderer Asset específico.
        /// <c>ScriptableRendererData.rendererFeatures</c> é público da URP 12 (Unity 2021.3)
        /// até a URP 17 (Unity 6), então não é preciso reflection nem #if por versão.
        /// </summary>
        private static PixelCameraRenderFeature FindInRendererData(ScriptableRendererData rendererData)
        {
            if (rendererData == null)
            {
                return null;
            }

            List<ScriptableRendererFeature> features = rendererData.rendererFeatures;
            if (features == null)
            {
                return null;
            }

            foreach (ScriptableRendererFeature feature in features)
            {
                // Uma entrada pode estar nula (script removido / referência quebrada).
                if (feature is PixelCameraRenderFeature pixelFeature && pixelFeature != null)
                {
                    return pixelFeature;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Varre os Renderer Assets do projeto. Só existe no Editor: o Render Feature é um
        /// sub-asset, então precisa de <c>LoadAllAssetsAtPath</c> para ser encontrado.
        /// </summary>
        private static PixelCameraRenderFeature FindInProjectRendererAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:ScriptableRendererData");
            if (guids == null)
            {
                return null;
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                if (assets == null)
                {
                    continue;
                }

                foreach (Object asset in assets)
                {
                    if (asset is PixelCameraRenderFeature pixelFeature && pixelFeature != null)
                    {
                        return pixelFeature;
                    }
                }
            }

            return null;
        }
#endif

        private static void LogFeatureNotFound()
        {
            RenderPipelineAsset pipeline = GetActivePipelineAsset();

            if (pipeline == null)
            {
                Debug.LogError(LogPrefix + "Nenhum Render Pipeline Asset atribuído ao projeto. " +
                    "Vá em Edit > Project Settings > Graphics (e em Quality) e atribua um " +
                    "UniversalRenderPipelineAsset — sem URP o Pixel Camera não roda.");
                return;
            }

            Debug.LogError(LogPrefix + "PixelCameraRenderFeature não encontrado.\n" +
                "Como resolver (qualquer uma das opções):\n" +
                "  1. Selecione o seu Renderer Asset (Universal Renderer Data) no Project e clique em " +
                "\"Add Renderer Feature\" > \"Pixel Camera Render Feature\";\n" +
                "  2. Arraste esse Renderer Asset para o campo \"Renderer Asset\" deste componente;\n" +
                "  3. Ou arraste o sub-asset \"Pixel Camera Render Feature\" para o campo " +
                "\"Render Feature (direto)\".\n" +
                "Obs.: o Render Feature é um ScriptableObject (sub-asset do Renderer Asset), por isso " +
                "Object.FindObjectsOfType nunca o encontra.");
        }

        private static RenderPipelineAsset GetActivePipelineAsset()
        {
#if UNITY_2022_2_OR_NEWER
            RenderPipelineAsset qualityAsset = QualitySettings.renderPipeline;
            return qualityAsset != null ? qualityAsset : GraphicsSettings.currentRenderPipeline;
#else
            RenderPipelineAsset qualityAsset = QualitySettings.renderPipeline;
            return qualityAsset != null ? qualityAsset : GraphicsSettings.renderPipelineAsset;
#endif
        }
    }
}
