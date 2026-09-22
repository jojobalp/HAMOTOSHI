# HAMOTOSHI

## PixelCamera

Efeito de câmera retrô para **Unity URP**: pixelização, paletas limitadas, dithering Bayer ordenado
e CRT.

> **Novidade na 1.1.0:** os presets de paleta com menos de 16 cores (GameBoy, CGA e **Binary**) e o
> dithering foram corrigidos — ambos tinham comportamento visual errado nas versões anteriores.
> Veja [`PixelCamera/CHANGELOG.md`](PixelCamera/CHANGELOG.md).

> **Pacote:** `com.pixelcamera.unity` · **Versão atual:** 1.1.0 · **Unity:** 2021.3 LTS ou superior

---

## 🔌 Compatibilidade de Render Pipeline

O Pixel Camera é um asset **exclusivo da Universal Render Pipeline (URP)**.

| Pipeline | Suporte | Observação |
| --- | --- | --- |
| **URP** (Universal Render Pipeline) | ✅ **Suportado** | Pipeline alvo do asset. URP 12 → 17+ (Unity 2021.3 LTS até Unity 6.x). |
| **Built-in** (pipeline legado) | ❌ **Não suportado** | Não compila sem o URP instalado (`CS0234`) e o shader não tem SubShader compatível. |
| **HDRP** (High Definition) | ❌ **Não suportado** | A URP não compartilha Render Features com o HDRP; o shader usa a ShaderLibrary da URP. |

Se o seu projeto usa Built-in ou HDRP, **não compre/instale este asset** — ele não vai funcionar e a
Unity vai reportar erros de compilação. A seção [Por que não Built-in / HDRP?](#por-que-não-built-in--hdrp)
explica os motivos técnicos.

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

### ✅ Requisitos do projeto

| Requisito | Mínimo | Observação |
| --- | --- | --- |
| Unity | **2021.3 LTS** | Testado também em Unity 2022.3, Unity 2023.x e Unity 6 (6000.x). |
| Universal RP | **12.0.0** | Resolvido automaticamente ao instalar via Package Manager. |
| Shader Model | 4.5+ | — |
| **Active Input Handling** | **Input Manager (old)** | O `PixelCameraController` usa `UnityEngine.Input` (Input Manager legado). Se o projeto estiver em *Input System Package (new)* **exclusivo**, os atalhos F1–F4 e o scroll não funcionam. Use *Both* ou *Input Manager (old)*. |

---

## 📦 Instalação

- **Como pacote (recomendado):** adicione em `Packages/manifest.json`
  `"com.pixelcamera.unity": "https://github.com/jojobalp/HAMOTOSHI.git?path=PixelCamera"` —
  a dependência do URP é resolvida automaticamente.
- **Copiando para o projeto:** coloque a pasta `PixelCamera` dentro de `Assets/` e instale o URP
  manualmente (passo acima).

---

## 🧭 Compatibilidade entre versões da URP

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

---

## 🔍 Por que não Built-in / HDRP?

Decisão técnica documentada de propósito — não é uma limitação acidental.

**Built-in (pipeline legado)**

- O ponto de injeção de pós-processamento no Built-in é `MonoBehaviour.OnRenderImage` +
  `Graphics.Blit`. Esse mecanismo **não existe** em Scriptable Render Pipelines e não pode ser
  usado em conjunto com o `ScriptableRendererFeature` que este asset implementa.
- O `Graphics.Blit` alimenta a textura `_MainTex`; o shader deste asset lê `_BlitTexture`
  (convenção do `Blitter`/Render Graph da URP).
- O shader declara `Tags { "RenderPipeline" = "UniversalPipeline" }` e `Fallback Off`, ou seja:
  no Built-in o SubShader é descartado e **não há fallback** (tela magenta).
- A Unity anunciou publicamente a **intenção de remover** a Built-in Render Pipeline, com
  disponibilidade garantida apenas até o **Unity 6.7 LTS** (suporte até ~2028/2029). Construir
  sobre ela hoje significa investir numa fundação com prazo de validade.

**HDRP (High Definition Render Pipeline)**

- O HDRP **não executa** `ScriptableRendererFeature` da URP — o mecanismo equivalente seria
  `CustomPass` + `CustomPassVolume`, uma implementação inteiramente separada.
- O HDRP renderiza em **HDR** (buffer FP16, valores acima de `1.0`, com exposure aplicada). A
  quantização e a busca de cor mais próxima da paleta assumem cor em `0..1`: em HDR todo valor
  acima de 1 colapsa para a cor mais clara da paleta, destruindo highlights.
- O HDRP depende de **acumulação temporal** (TAA, SSAO/SSGI, volumetria, motion blur). Um
  downsample para um grid de baixa resolução faz o grid "dançar" em relação à geometria
  (shimmer) e invalida o histórico temporal desses efeitos.
- O HDRP **sempre** aplica um upscale filter no fim do frame (Catmull-Rom, CAS, FSR 1.0,
  TAA Upscale ou DLSS). Isso suaviza/afia a saída **depois** do efeito, destruindo o alinhamento
  1:1 dos pixels — que é justamente o objetivo do asset. Com *TAA Upscale* ativo, o TAA passa a
  ser o único método de AA e nenhum outro pós-processamento funciona como esperado.
- Na prática, pixel art "honesto" no HDRP exigiria renderizar nativamente em baixa resolução com
  TAA, dynamic resolution e upscale filter **desligados** — ou seja, o asset passaria a exigir que
  o usuário desative exatamente as features pelas quais ele escolheu o HDRP.

---

## 🐛 Erros conhecidos de compilação

| Erro | Causa | Solução |
| --- | --- | --- |
| `CS0234 ... 'Universal' does not exist` | URP não instalado (ou `.asmdef` sem referência) | Instalar **Universal RP**; os `.asmdef` já referenciam `Unity.RenderPipelines.Universal.Runtime` |
| `CS0118 'Editor' is a namespace but is used like a type` | Dentro de `namespace PixelCamera.Editor`, `Editor` resolve para o namespace, não para `UnityEditor.Editor` | Herdar de `UnityEditor.Editor` (qualificado) — já aplicado em `PixelCameraRenderFeatureEditor` |
| `CS0115 ... no suitable method found to override` | URP 17+ removeu `Execute` público | Usar Unity 6 com `RecordRenderGraph` (já implementado) |
| `[PixelCamera AutoSetup] PixelCameraRenderFeature não encontrado!` | O Auto Setup usava `Object.FindObjectsOfType`, que não enxerga ScriptableObjects (o Render Feature é sub-asset do Renderer Asset) | Corrigido na **1.0.2**: usa a API pública `ScriptableRendererData.rendererFeatures` + campo opcional de referência direta |

---

## 📚 Documentação

| Arquivo | Conteúdo |
| --- | --- |
| [`PixelCamera/README.md`](PixelCamera/README.md) | Guia completo: features, API, paletas, limitações conhecidas |
| [`PixelCamera/QUICKSTART.md`](PixelCamera/QUICKSTART.md) | Setup rápido e troubleshooting |
| [`PixelCamera/GUIA-PASSO-A-PASSO.md`](PixelCamera/GUIA-PASSO-A-PASSO.md) | Tutorial visual detalhado, do projeto vazio ao efeito rodando |
| [`PixelCamera/FEATURES.md`](PixelCamera/FEATURES.md) | Inventário real de features (o que está e o que não está implementado) |
| [`PixelCamera/CHANGELOG.md`](PixelCamera/CHANGELOG.md) | Histórico de versões, requisitos, limitações e roadmap |

## 📝 Licença

**MIT License** — veja [`LICENSE`](LICENSE). Use livremente em projetos pessoais e comerciais.
