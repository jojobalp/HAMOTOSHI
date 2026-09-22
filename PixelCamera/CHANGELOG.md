# 📋 CHANGELOG - Pixel Camera URP

## [Não publicado] - ajustes de documentação

### 📝 Documentação

- ✅ **Matriz de compatibilidade de pipeline explícita**: README raiz, `PixelCamera/README.md`,
  `QUICKSTART.md`, `FEATURES.md` e este arquivo agora afirmam claramente que o asset é
  **URP-only** — Built-in e HDRP **não** são suportados — com os motivos técnicos de cada caso.
  Antes a informação estava espalhada em frases de troubleshooting ("verifique se não está usando
  Built-in ou HDRP") e o `FEATURES.md` não mencionava o assunto.
- ✅ **Requisito de Input Manager documentado**: o `PixelCameraController` usa `UnityEngine.Input`,
  então projetos com *Active Input Handling* = **Input System Package (new)** exclusivo não têm
  F1–F4 nem scroll. Adicionado aos requisitos e ao troubleshooting.
- ✅ **Seção "Limitações conhecidas"** centralizada no `README.md`, com 11 itens verificados contra
  o código, incluindo:
  - presets de paleta com menos de 16 cores (GameBoy, CGA, Binary) têm slots pretos fantasma porque
    o `Frag` usa `paletteSize = 16` fixo;
  - `DitherType.FloydSteinberg` aparece no dropdown mas não está implementado;
  - `PaletteUtility.ExtractPaletteFromTexture` é amostragem aleatória, não extração de paleta;
  - UI em *Screen Space - Overlay* não é pixelada;
  - paletas geradas em runtime ficam com `Wrap Mode = Repeat`.
- ✅ **`FEATURES.md` reescrito como inventário real**: removida a alegação de "66 features / 100% /
  pronto para produção"; itens com ressalva marcados com ⚠️ e itens ausentes com ❌. Corrigidas
  descrições que não batiam com o código (extração de paleta "inteligente", "qualquer largura até 16
  pixels", suporte a BMP, gerador aleatório "HSV controlado").
- ✅ **Regra da textura de paleta documentada**: exatamente **16×1**, `Point`, `Clamp`, sem mipmaps,
  preenchendo os slots não usados com repetições de cor.
- ✅ **Menus `Tools > Pixel Camera` listados por completo** (7 itens reais, incluindo Diagnóstico e
  Corrigir Automaticamente, que faltavam no `FEATURES.md`).
- ✅ **Licença**: `README.md` não afirma mais "MIT" (não existe arquivo `LICENSE` no repositório) e
  passa a sinalizar que a licença precisa ser definida antes da publicação.

> Nenhuma alteração de código nesta entrada — apenas documentação.

---

## [1.0.3] - 2026-09-19

### 🐛 Correções

- ✅ **Falso negativo no diagnóstico**: "Foram encontrados 3 Renderer Asset(s), mas nenhum tem
  o Pixel Camera Render Feature" aparecia mesmo em projetos configurados. Causas:
  - O código só olhava dentro de `Assets/` (ignorava `Packages/`, onde URP Assets criados
    pelo template 2D/3D URP do Unity 6 costumam ficar).
  - O filtro era por nome de arquivo (procurava "Renderer" no nome) em vez de usar
    `t:ScriptableRendererData` — fragilidade que quebrava com nomes como `PC_RPAsset`,
    `URP-HighQuality` etc.
  - O `AssetDatabase.LoadAssetAtPath<ScriptableObject>` pegava só o asset raiz;
    sub-assets (onde o Render Feature é serializado quando você clica "Add Renderer Feature"
    no inspector novo do Unity 6/URP 17) podiam não ser vistos pelo SerializedProperty.
  - Agora usa busca por tipo real (`t:ScriptableRendererData`), inclui `Assets/` e
    `Packages/`, usa a API pública `ScriptableRendererData.rendererFeatures` e tem fallback
    via SerializedObject — mesma estratégia robusta do `PixelCameraAutoSetup`.
- ✅ **Diagnóstico diferencia Renderer ATIVO vs outros Renderers**: antes o aviso só dizia
  "nenhum tem o Feature", mesmo que ele existisse num Renderer que não estava em uso pela
  pipeline. Agora o aviso alerta que "o Feature existe, mas não está no Renderer que
  realmente está sendo renderizado" — causa comum de o efeito não aparecer em jogo.

### ✨ Novidades

- ✅ **Auto-correção em 1 clique**: novo menu **Tools > Pixel Camera > Corrigir
  Automaticamente (Adicionar Render Feature)** que adiciona o `PixelCameraRenderFeature`
  ao(s) Renderer(s) ativo(s) da pipeline sem duplicação e registra como sub-asset
  corretamente (`AssetDatabase.AddObjectToAsset`).
- ✅ **Botão "Corrigir Automaticamente"** no dialog do diagnóstico, além de botão para
  "Abrir Renderer Ativo" que pinga o Renderer em uso na janela Project.
- ✅ **Setup na Câmera Atual** agora:
  - Chama o auto-fix do Render Feature antes de qualquer coisa (não mais "configure
    manualmente depois");
  - Adiciona `PixelCameraAutoSetup` em vez de só o Controller, cobrindo pixel/paleta/
    dithering/CRT com valores padrão;
  - Preenche automaticamente o campo `Renderer Asset` com o Renderer ativo da pipeline;
  - Mostra um resumo claro do que foi configurado.

---

## [1.0.2] - 2026-09-18

### 🐛 Correções

- ✅ **`PixelCameraAutoSetup` nunca encontrava o Render Feature**
  (Console: `[PixelCamera AutoSetup] PixelCameraRenderFeature não encontrado!`, e o efeito nunca
  era aplicado):
  - A busca usava `Object.FindObjectsOfType<PixelCameraRenderFeature>()`. Um
    `ScriptableRendererFeature` é um **ScriptableObject gravado como sub-asset** do Renderer Asset,
    e o `FindObjectsOfType` só devolve objetos **da cena** — o array voltava sempre vazio.
  - O campo `rendererAsset` do Inspector era **ignorado** pelo código.
  - Em Unity 6 o `FindObjectsOfType` ainda é obsoleto (aviso CS0618).
  - Agora a resolução segue esta ordem: campo novo **Render Feature (direto)** →
    **Renderer Asset** do Inspector (via `ScriptableRendererData.rendererFeatures`, API pública da
    URP 12 à 17, sem reflection) → varredura dos Renderer Assets do projeto (Editor) →
    `Resources.FindObjectsOfTypeAll`. Se nada for achado, a mensagem de erro lista os passos exatos.
- ✅ **Aviso falso do `PixelCameraController`**: `"Nenhum RenderFeature configurado!"` aparecia
  sempre que o `PixelCameraAutoSetup` adicionava o controller em runtime (ele resolve a referência
  logo depois). O aviso agora é suprimido quando há um Auto Setup no mesmo GameObject.
- ✅ **CS0618 no Unity 6**: `Object.FindObjectOfType<Camera>()` em `PixelCameraSetupUtility`
  substituído por `FindFirstObjectByType` (com fallback para versões antigas).
- ✅ `PixelCameraAutoSetup` agora avisa quando o Render Feature está **inativo no Renderer Asset**
  (`ScriptableRendererFeature.isActive`) — nesse caso a URP nem chama `AddRenderPasses`.
- ✅ `PixelCameraAutoSetup` repete a busca algumas vezes (até ~2s) e passa a conexão do
  `PixelCameraController` para antes da aplicação das configurações, inclusive no caminho de
  preset rápido; `GraphicsSettings.currentRenderPipeline` ganhou fallback para Unity 2021
  (`renderPipelineAsset`).

### ✨ Novidades

- ✅ `PixelCameraAutoSetup.ActiveRenderFeature` (somente leitura) e `RendererAsset` (leitura/escrita)
  para configurar o componente via código.
- ✅ Campo opcional **"Render Feature (direto)"** no `PixelCameraAutoSetup`.

---

## [1.0.1] - 2026-09-18

### 🐛 Correções de compilação

- ✅ **CS0118** (`'Editor' is a namespace but is used like a type`): `PixelCameraRenderFeatureEditor`
  agora herda de `UnityEditor.Editor` (totalmente qualificado). Dentro do namespace
  `PixelCamera.Editor`, o identificador simples `Editor` resolvia para o namespace em vez da classe.
- ✅ **CS0234** (`'Universal' does not exist in the namespace 'UnityEngine.Rendering'`):
  - `PixelCamera.Editor.asmdef` agora referencia `Unity.RenderPipelines.Universal.Runtime`,
    `Unity.RenderPipelines.Core.Runtime` e `Unity.RenderPipelines.Core.Editor`
  - Removido o `using UnityEngine.Rendering.Universal` não utilizado de `PixelCameraSetupUtility.cs`
  - A causa raiz (URP ausente no projeto) é detectada e explicada automaticamente

### ✨ Novidades

- ✅ **`PixelCameraProjectDiagnostics`**: diagnóstico do projeto em
  **Tools > Pixel Camera > Diagnóstico do Projeto (URP)**. Roda após cada compilação e reporta no
  Console: URP instalado (e versão), pipeline asset atribuído em Graphics/Quality e presença do
  Render Feature nos Renderer Assets.
- ✅ Documentação de troubleshooting atualizada (README, QUICKSTART, GUIA-PASSO-A-PASSO e README raiz).

---

## [1.0.0] - 2026-09-18

### 🎉 Initial Release

#### Features Implementadas

**Pixelização**
- ✅ Resolução configurável em runtime (16x16 até 1280x720)
- ✅ Snap to Pixel Grid para alinhamento perfeito
- ✅ Filtro Point para pixels nítidos
- ✅ Aspect ratio automático

**Paletas de Cores**
- ✅ 7 presets inclusos:
  - GameBoy (4 tons)
  - NES (16 cores)
  - CGA (4 cores)
  - PICO-8 (16 cores)
  - GB Color (16 cores)
  - Grayscale (16 tons)
  - Binary (2 cores)
- ✅ Paleta custom via textura PNG
- ✅ Gerador de paleta aleatória
- ✅ Extrator de paleta de imagens
- ✅ ScriptableObject para presets custom

**Dithering**
- ✅ Bayer 2x2 (padrão grande, retrô)
- ✅ Bayer 4x4 (equilibrado)
- ✅ Bayer 8x8 (padrão fino, suave)
- ✅ Intensidade configurável
- ✅ Toggle independente

**Efeito CRT Completo**
- ✅ Scanlines (intensidade + espessura)
- ✅ Bloom/Glow (intensidade + raio)
- ✅ Curvatura da tela
- ✅ Vinheta
- ✅ Todos com controles individuais
- ✅ Toggle global

**Ferramentas de Editor**
- ✅ Inspector customizado com foldouts
- ✅ Palette Editor Window completo
- ✅ Preview em tempo real
- ✅ Import/Export de paletas PNG
- ✅ Presets rápidos (Retrô, GameBoy, CRT)
- ✅ Setup automático de câmera
- ✅ Gerador de textura preview

**Controller Runtime**
- ✅ Atalhos de teclado (F1-F4)
- ✅ Scroll para ajustar resolução
- ✅ API pública para código
- ✅ Toggle de todos os efeitos

**Arquitetura**
- ✅ URP RenderFeature nativo
- ✅ Shader HLSL otimizado
- ✅ Assembly definitions organizados
- ✅ Namespace dedicado (PixelCamera)
- ✅ Documentação completa

#### Documentação
- ✅ README.md com guia completo
- ✅ Quick Start Guide
- ✅ Exemplos de código
- ✅ Troubleshooting
- ✅ Presets recomendados
- ✅ CHANGELOG

#### Samples
- ✅ PixelCameraDemo com presets configuráveis
- ✅ Exemplos de uso em runtime

---

## Roadmap Futuro

### Próximas Versões

**[1.1.0] - Melhorias de Performance**
- [ ] Otimização de shaders para mobile
- [ ] GPU Instancing support
- [ ] Compute shaders para dithering complexo
- [ ] LOD automático baseado em resolução

**[1.2.0] - Novos Efeitos**
- [ ] Floyd-Steinberg dithering (error diffusion)
- [ ] CRT Phosphor mask (RGB subpixels)
- [ ] Chromatic aberration
- [ ] Noise/grain opcional
- [ ] Color bleeding entre pixels

**[1.3.0] - Ferramentas**
- [ ] Preview window com comparação side-by-side
- [ ] Timeline support para animação de efeitos
- [ ] Shader graph version (URP 14+)
- [ ] HDRP compatibility layer

**[2.0.0] - Refatoração**
- [ ] ScriptableObject para todas as configurações
- [ ] Sistema de camadas de efeitos
- [ ] Blend entre presets
- [ ] Preset browser no editor
- [ ] Undo/Redo support completo

---

## Notas Técnicas

### Requisitos
- Unity 2021.3 LTS ou superior
- Universal Render Pipeline 12.0+
- Shader Model 4.5+
- **Active Input Handling = Input Manager (old) ou Both** — o `PixelCameraController` usa
  `UnityEngine.Input`; em projeto *Input System Package (new)* exclusivo os atalhos F1–F4 e o
  scroll não funcionam
- **Render Graph habilitado** no Unity 6 (*Project Settings > Graphics > URP*)

### Compatibilidade de Render Pipeline

| Pipeline | Status |
| --- | --- |
| **URP** 12 → 17+ | ✅ Suportado |
| **Built-in** (legado) | ❌ **Não suportado** |
| **HDRP** | ❌ **Não suportado** |

Este asset **não é multi-pipeline**. Ele é implementado como `ScriptableRendererFeature` /
`ScriptableRenderPass` (mecanismo exclusivo da URP) e o shader inclui a ShaderLibrary da URP com
`Tags { "RenderPipeline" = "UniversalPipeline" }` e `Fallback Off`.

- **Built-in:** não tem Render Features — o mecanismo de pós-processamento dele é
  `MonoBehaviour.OnRenderImage` + `Graphics.Blit`, que alimenta `_MainTex` (este shader lê
  `_BlitTexture`). Sem o URP instalado o pacote nem compila (`CS0234`). Além disso a Unity anunciou
  a intenção de **remover** a Built-in, com disponibilidade garantida só até o **Unity 6.7 LTS**.
- **HDRP:** não executa Render Features da URP (o equivalente seria `CustomPass` +
  `CustomPassVolume`, implementação separada). Além disso o HDRP renderiza em **HDR** (FP16, com
  exposure), o que quebra a quantização/busca de paleta que assume cor em `0..1`; depende de
  acumulação **temporal** (TAA, SSAO, volumetria), que um downsample para grid fixo invalida; e
  **sempre** aplica um upscale filter no fim do frame (Catmull-Rom, CAS, FSR 1.0, TAA Upscale,
  DLSS), que suaviza a saída depois do efeito e destrói o alinhamento 1:1 dos pixels.

Motivos completos no [README raiz](../README.md#por-que-não-built-in--hdrp).

### Compatibilidade de plataforma
- ✅ Windows
- ✅ macOS
- ✅ Linux
- ✅ Android
- ✅ iOS
- ✅ WebGL (com limitações)
- ❌ Consoles (testar individualmente)

### Compatibilidade de versão da URP
| Unity | URP | Caminho de código |
| --- | --- | --- |
| Unity 6+ (6000.0+) | 17+ | Render Graph (`RecordRenderGraph`) |
| Unity 2022 / 2023 | 13–16 | `Execute` + `RTHandle` + `Blitter` |
| Unity 2021 ou anterior | ≤ 12 | Legado (`RenderTargetIdentifier`) |

### Performance
O efeito custa **dois blits fullscreen** (cena → baixa resolução → upscale *point*). Os números
abaixo são estimativas de referência e variam com GPU, resolução de saída e efeitos ativos:

- **1080p**: 320x180 ≈ 0.1ms no GPU
- **1440p**: 640x360 ≈ 0.2ms no GPU
- **4K**: 640x360 ≈ 0.3ms no GPU

O custo dominante é o **bloom do CRT** (25 amostras da textura por pixel quando ativo).

### Limitações Conhecidas

Detalhadas em [README.md → Limitações conhecidas](README.md#limitações-conhecidas).

1. **Paletas com menos de 16 cores** (GameBoy 4, CGA 4, Binary 2): `GetPresetPalette` cria sempre
   uma textura 16×1 e preenche só os slots reais; os demais ficam pretos. Como o `Frag` usa
   `int paletteSize = 16;` fixo, o preto fantasma vence a comparação de distância para cores
   escuras/médias — o **Binary** tende a ficar quase todo preto e o **GameBoy/CGA** perdem tons.
2. **Paleta máxima: 16 cores** (limitação do shader). `colorCount` aceita até 256, mas só afeta a
   pré-quantização.
3. **Floyd-Steinberg não implementado** — o valor existe no enum e aparece no dropdown, mas o shader
   só trata Bayer 2/4/8 (selecioná-lo equivale a dithering desligado).
4. **`ExtractPaletteFromTexture` é amostragem aleatória** — sem quantização/clustering, retorna N
   pixels aleatórios da imagem, não a paleta representativa.
5. **UI em Screen Space - Overlay não é pixelada** (é desenhada depois do pipeline da câmera).
6. **`PixelCameraController` usa Input Manager legado.**
7. **Paletas geradas em runtime usam `Wrap Mode = Repeat`** (não *Clamp*).
8. **CRT curvature pode causar clipping** (preto) nas bordas.
9. **Bloom é aproximação** por box 5×5 — não é physically-based.
10. **Dithering é modulação de brilho pós-quantização**, não dithering ordenado clássico com
    re-quantização para a paleta.
11. **Samples são scripts**, não uma cena `.unity` pré-montada.

---

## Créditos

Desenvolvido para Unity URP com foco em jogos pixel art e efeitos retrô.

Baseado em técnicas clássicas de:
- Bayer ordered dithering
- CGA/VGA palettes
- CRT emulation shaders
- Modern pixel art engines (PICO-8, Aseprite)
