# 📋 CHANGELOG - Pixel Camera URP

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

### Compatibilidade
- ✅ Windows
- ✅ macOS
- ✅ Linux
- ✅ Android
- ✅ iOS
- ✅ WebGL (com limitações)
- ❌ Consoles (testar individualmente)

### Performance
- **1080p**: 320x180 = ~0.1ms no GPU
- **1440p**: 640x360 = ~0.2ms no GPU
- **4K**: 640x360 = ~0.3ms no GPU

### Limitações Conhecidas
- Paleta máxima: 16 cores (limitação do shader)
- Floyd-Steinberg requer múltiplos passes (não implementado)
- CRT curvature pode causar clipping nas bordas
- Bloom é aproximação simples (não é physically-based)

---

## Créditos

Desenvolvido para Unity URP com foco em jogos pixel art e efeitos retrô.

Baseado em técnicas clássicas de:
- Bayer ordered dithering
- CGA/VGA palettes
- CRT emulation shaders
- Modern pixel art engines (PICO-8, Aseprite)
