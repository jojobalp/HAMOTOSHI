# HAMOTOSHI

## PixelCamera

Efeito de câmera retrô para URP (pixelização, paletas, dithering e CRT).

### ⚠️ Requisito obrigatório: Universal Render Pipeline

O pacote usa `ScriptableRendererFeature` / `ScriptableRenderPass`, portanto **o projeto precisa ter
o URP instalado**. Sem ele a Unity mostra erros como:

```
error CS0234: The type or namespace name 'Universal' does not exist in the namespace
              'UnityEngine.Rendering' (are you missing an assembly reference?)
```

Instale em **Window > Package Manager > Unity Registry > Universal RP > Install** e atribua um
*UniversalRenderPipelineAsset* em **Project Settings > Graphics** e **Project Settings > Quality**.

Existe um verificador embutido em **Tools > Pixel Camera > Diagnóstico do Projeto (URP)** (ele também
roda sozinho após cada compilação e explica no Console o que está faltando).

### Instalação

- **Como pacote (recomendado):** adicione em `Packages/manifest.json`
  `"com.pixelcamera.unity": "https://github.com/jojobalp/HAMOTOSHI.git?path=PixelCamera"` —
  a dependência do URP é resolvida automaticamente.
- **Copiando para o projeto:** coloque a pasta `PixelCamera` dentro de `Assets/` e instale o URP
  manualmente (passo acima).

### Compatibilidade (URP)

| Versão do Unity | URP | Caminho usado |
| --- | --- | --- |
| Unity 6+ (6000.0+) | 17+ | Render Graph (`RecordRenderGraph`) |
| Unity 2022 / 2023 | 13–16 | `Execute` com `RTHandle` + `Blitter` |
| Unity 2021 ou mais antigo | ≤ 12 | Legado com `RenderTargetIdentifier` |

> A partir da URP 17.1, o método `ScriptableRenderPass.Execute(ScriptableRenderContext, ref RenderingData)`
> foi removido da API pública (erro CS0115). No Unity 6 o efeito é implementado via
> `RecordRenderGraph`, que é a única forma suportada quando o Render Graph está ativo.

No Unity 6, certifique-se de que o **Render Graph está habilitado** em
*Edit > Project Settings > Graphics > URP* (padrão no Unity 6.1+).

### Erros conhecidos de compilação

| Erro | Causa | Solução |
| --- | --- | --- |
| `CS0234 ... 'Universal' does not exist` | URP não instalado (ou `.asmdef` sem referência) | Instalar **Universal RP**; os `.asmdef` já referenciam `Unity.RenderPipelines.Universal.Runtime` |
| `CS0118 'Editor' is a namespace but is used like a type` | Dentro de `namespace PixelCamera.Editor`, `Editor` resolve para o namespace, não para `UnityEditor.Editor` | Herdar de `UnityEditor.Editor` (qualificado) — já aplicado em `PixelCameraRenderFeatureEditor` |
| `CS0115 ... no suitable method found to override` | URP 17+ removeu `Execute` público | Usar Unity 6 com `RecordRenderGraph` (já implementado) |
| `[PixelCamera AutoSetup] PixelCameraRenderFeature não encontrado!` | O Auto Setup usava `Object.FindObjectsOfType`, que não enxerga ScriptableObjects (o Render Feature é sub-asset do Renderer Asset) | Corrigido na **1.0.2**: usa a API pública `ScriptableRendererData.rendererFeatures` + campo opcional de referência direta |

Documentação completa em [`PixelCamera/README.md`](PixelCamera/README.md) e
[`PixelCamera/GUIA-PASSO-A-PASSO.md`](PixelCamera/GUIA-PASSO-A-PASSO.md).
