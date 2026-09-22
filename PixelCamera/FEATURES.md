# 📊 PIXEL CAMERA - INVENTÁRIO DE FEATURES

Inventário **real** do que existe no pacote `com.pixelcamera.unity` 1.0.3.
Itens marcados com ⚠️ funcionam, mas têm ressalva documentada. Itens em ❌ **não** estão
implementados (estão no roadmap do [`CHANGELOG.md`](CHANGELOG.md)).

## 🔌 Compatibilidade de Pipeline

| Pipeline | Status |
| --- | --- |
| **URP** 12 → 17+ (Unity 2021.3 LTS a Unity 6.x) | ✅ Suportado |
| **Built-in** (pipeline legado) | ❌ Não suportado |
| **HDRP** (High Definition) | ❌ Não suportado |

O asset é implementado como `ScriptableRendererFeature` / `ScriptableRenderPass` (mecanismo exclusivo
da URP) e o shader inclui a ShaderLibrary da URP com `Tags { "RenderPipeline" = "UniversalPipeline" }`
e `Fallback Off`. Motivos técnicos completos no [README raiz](../README.md#por-que-não-built-in--hdrp).

**Requisitos adicionais**

| Requisito | Valor |
| --- | --- |
| Unity | 2021.3 LTS+ |
| Universal RP | 12.0.0+ |
| Shader Model | 4.5+ |
| Active Input Handling | **Input Manager (old)** ou *Both* — o `PixelCameraController` usa `UnityEngine.Input` |
| Render Graph (Unity 6) | Habilitado em *Project Settings > Graphics > URP* |

## ✅ Features Implementadas

### 🎯 Core Features
- [x] **Pixelização com resolução customizável**
  - Resolução configurável em runtime via inspector
  - Range: 16x16 até 1280x720
  - Snap to pixel grid opcional
  - Aspect ratio automático
  
- [x] **URP RenderFeature nativo**
  - Integração perfeita com Universal Render Pipeline
  - Render pass único otimizado
  - Suporte a múltiplas câmeras
  
- [x] **Todos os efeitos toggáveis**
  - Cada efeito pode ser ligado/desligado independentemente
  - Sem custo de performance para efeitos desabilitados

### 🎨 Sistema de Paletas

#### Presets Incluídos (7)
- [x] **GameBoy** - 4 tons de verde clássico
- [x] **NES** - 16 cores do Nintendo Entertainment System
- [x] **CGA** - 4 cores do CGA antigo ⚠️
- [x] **PICO-8** - 16 cores do fantasy console
- [x] **GB Color** - 16 cores do GameBoy Color
- [x] **Grayscale** - 16 tons de cinza
- [x] **Binary** - 2 cores (preto e branco) ⚠️

> ⚠️ **Presets com menos de 16 cores (GameBoy, CGA, Binary):** `GetPresetPalette` cria sempre uma
> textura **16×1**, mas preenche apenas os slots reais do preset. Os slots restantes ficam
> `(0,0,0,0)` = preto, e o shader compara a cor do pixel contra **os 16 slots**
> (`int paletteSize = 16;` fixo no `Frag`). O preto fantasma acaba vencendo a comparação para
> qualquer cor escura/média: na prática o **Binary** tende a ficar quase todo preto e o
> **GameBoy**/**CGA** perdem os tons escuros e médios. Os presets de 16 cores (NES, PICO-8,
> GB Color, Grayscale) não são afetados. Workaround: paleta custom 16×1 com as cores repetidas.
> Detalhes em [README → Limitações conhecidas](README.md#1-paletas-com-menos-de-16-cor-gameboy-cga-binary).

#### Paleta Custom
- [x] **Importar via textura PNG**
  - Carregamento por arquivo: **PNG e JPEG** (`Texture2D.LoadImage`). **BMP não é suportado.**
  - A textura de paleta deve ter **exatamente 16×1 pixels** — o shader itera os 16 slots
  - `Filter Mode = Point`, `Wrap Mode = Clamp`, sem mipmaps, sem compressão
  - Preview no editor
  
- [x] **Gerar aleatória** ⚠️
  - Seed opcional para reprodutibilidade
  - ⚠️ As cores são sorteadas em **RGB uniforme** (`Random.value` por canal), não em HSV —
    o resultado tende a cores dessaturadas, não "vibrantes"
  - ⚠️ `Random.InitState(seed)` altera o estado **global** do `UnityEngine.Random`
  - ⚠️ Slots não preenchidos ficam pretos (mesma causa do aviso dos presets acima)
  - Botão no editor + API em código
  
- [x] **Extrair de imagem existente** ⚠️
  - Configuração de max colors
  - ⚠️ **Não é extração de paleta real**: o método sorteia até 1000 pixels da imagem e guarda os
    primeiros `maxColors` distintos num `HashSet<Color>`. Sem quantização e sem clustering, e como a
    comparação é em float `RGBA` quase todo pixel é "único" — o resultado são **N pixels aleatórios
    da imagem**, não a paleta representativa dela. Use o **Palette Editor** para trabalho real.

- [x] **ScriptableObject para presets** ⚠️
  - Criação via menu Assets
  - Inspector customizado
  - Conversão para Texture2D
  - Import/Export de PNG
  - ⚠️ `ToTexture()` gera a textura com **largura = número de cores** (não 16) e não define
    `wrapMode` (fica *Repeat*, não *Clamp*)

- [x] **Palette Editor Window**
  - Interface visual completa
  - Grid de cores editável
  - Ferramenta de gradiente
  - Presets de tema (Natureza, Pôr-do-sol, Noturno, Pastel)
  - Preview em tempo real
  - Export/Import PNG

### 🔲 Sistema de Dithering

#### Algoritmos implementados (Bayer ordenado)
- [x] **Bayer 2x2**
  - Matriz 2x2
  - Padrão grande, muito retrô
  - Ideal para paletas pequenas
  
- [x] **Bayer 4x4**
  - Matriz 4x4
  - Equilibrado entre detalhe e padrão
  - Mais usado em jogos pixel art
  
- [x] **Bayer 8x8**
  - Matriz 8x8
  - Padrão fino e suave
  - Melhor para paletas maiores

- [x] **Intensidade configurável**
  - Slider de 0 a 1
  - Ajuste fino do efeito
  - Preview em tempo real

#### Não implementado
- ❌ **Floyd-Steinberg (difusão de erro)**
  - O valor `DitherType.FloydSteinberg` **existe no enum e aparece no dropdown do Inspector**, mas o
    shader só trata Bayer 2/4/8 (`if (_DitherType < 0.5 / < 1.5 / < 2.5)`). Selecioná-lo resulta em
    **dithering desligado**, sem aviso.
  - Difusão de erro exige múltiplos passes sequenciais — está no roadmap 1.2.0.

> **Como o dithering é aplicado:** o shader soma um offset de brilho ao cor já quantizada
> (`color.rgb += (threshold - 0.5) * intensity`) calculado pela matriz de Bayer na resolução da
> textura de baixa resolução. Ou seja: é uma **modulação de brilho estilizada**, não um dithering
> ordenado clássico que re-quantiza para a paleta depois do threshold. O efeito visual é retrô e
> coerente, mas não é o algoritmo "de livro".

### 📺 Efeito CRT Completo

#### Controles Individuais
- [x] **Scanlines**
  - Intensidade: 0-1
  - Espessura: 1-4
  - Linhas horizontais clássicas
  - ⚠️ Calculadas sobre `_ScreenParams.y` da textura de **baixa resolução**, então a espessura
    percebida muda conforme a resolução configurada
  
- [x] **Bloom/Glow**
  - Intensidade: 0-1
  - Raio: 1-10
  - ⚠️ Aproximação por **box de 5×5 amostras** da textura de origem (25 samples por pixel) —
    não é bloom physically-based, e é o efeito **mais caro** do pacote
  
- [x] **Curvatura**
  - Intensidade: 0-1
  - Distorção da tela
  - Efeito de monitor de tubo
  - ⚠️ UVs fora de `[0,1]` devolvem **preto** (borda de tubo intencional) — com intensidade alta
    isso recorta conteúdo nas bordas
  
- [x] **Vinheta**
  - Intensidade: 0-1
  - Escurecimento das bordas
  - Foco no centro da tela

#### Características
- [x] Todos os parâmetros configuráveis separadamente
- [x] Toggle global do CRT
- [x] Combinação de efeitos
- [x] Presets rápidos no editor

### 🎮 Controller Runtime

#### Atalhos de Teclado
- [x] **F1** - Toggle geral
- [x] **F2** - Toggle CRT
- [x] **F3** - Toggle Dithering
- [x] **F4** - Toggle Paleta
- [x] **Scroll do Mouse** - Ajustar resolução (mantém o aspect ratio da câmera)

> ⚠️ **Requer o Input Manager legado.** O `PixelCameraController` usa `UnityEngine.Input`
> (`Input.GetKeyDown`, `Input.mouseScrollDelta`). Com *Active Input Handling* =
> **Input System Package (new)** exclusivo, os atalhos não respondem. Configure para
> *Input Manager (old)* ou *Both*, ou controle o efeito pela API.

#### API Pública
- [x] `SetPixelResolution(int width, int height)`
- [x] `SetPalettePreset(PalettePreset preset)`
- [x] `SetCustomPalette(Texture2D palette)`
- [x] `GenerateRandomPalette(int seed)`
- [x] `SetCRTEffect(bool enabled)`
- [x] `SetDithering(bool enabled)`
- [x] `PixelCameraController.RenderFeature { get; set; }`
- [x] `PixelCameraAutoSetup.ActiveRenderFeature { get; }` / `RendererAsset { get; set; }`

> Todos os setters saem em silêncio (sem erro) se `RenderFeature` estiver nulo.

#### Configurações
- [x] Teclas customizáveis
- [x] Scroll sensitivity ajustável
- [x] Min/max resolution configurável
- [x] Enable/disable controles

### 🛠️ Ferramentas de Editor

#### Menu Tools (itens reais)
- [x] **Tools > Pixel Camera > Setup na Câmera Atual** - auto-fix do Feature + componente na câmera
- [x] **Tools > Pixel Camera > Palette Editor** - abre o editor de paletas
- [x] **Tools > Pixel Camera > Abrir Palette Editor** - atalho duplicado para o mesmo editor
- [x] **Tools > Pixel Camera > Gerar Textura Preview 320x180** - cria textura de teste
- [x] **Tools > Pixel Camera > Documentação** - abre o guia
- [x] **Tools > Pixel Camera > Diagnóstico do Projeto (URP)** - checa URP, pipeline asset e Feature
- [x] **Tools > Pixel Camera > Corrigir Automaticamente (Adicionar Render Feature)** - auto-fix

#### Inspector Customizado
- [x] Foldouts organizados por seção
- [x] Ícones visuais (emojis)
- [x] Tooltips explicativos
- [x] Help boxes com informações
- [x] Botões de preset rápido

#### Presets Rápidos
- [x] **Retrô Completo** - 320x180, PICO-8, dithering + CRT leve
- [x] **GameBoy** - 160x144, GameBoy, dithering forte
- [x] **CRT Clássico** - 640x360, NES, CRT pesado

### 📚 Documentação

#### Arquivos de Documentação
- [x] **README.md** - guia completo, requisitos, API e limitações conhecidas
- [x] **QUICKSTART.md** - setup rápido e troubleshooting
- [x] **GUIA-PASSO-A-PASSO.md** - tutorial visual do projeto vazio ao efeito rodando
- [x] **CHANGELOG.md** - histórico de versões, requisitos, limitações e roadmap
- [x] **FEATURES.md** - este arquivo (inventário real)
- [x] **../README.md** - README raiz com a matriz de compatibilidade de pipeline

#### Conteúdo da Documentação
- [x] Instalação passo a passo (UPM e cópia para `Assets/`)
- [x] Configuração do URP Renderer (automática e manual)
- [x] Uso básico e avançado
- [x] Exemplos de código
- [x] Presets recomendados
- [x] Troubleshooting
- [x] Performance tips
- [x] **Limitações conhecidas**
- [ ] API reference gerada (hoje a API está documentada inline no README)

### 🎬 Samples

Incluídos em `Samples~/DemoScene` (instaláveis pelo Package Manager, aba *Samples*):

- [x] **PixelCameraDemo.cs** - script de exemplo com 5 modos acionáveis por teclas `1`–`5`:
      GameBoy, Retrô, CRT, Moderno e Random
- [x] **PixelCameraTestScene.cs** - gerador de cena de teste

> ⚠️ São **scripts** de exemplo, não uma cena `.unity` pré-montada com materiais e volumes
> configurados. O modo *GameBoy* do demo usa o preset afetado pela limitação de paletas
> com menos de 16 cores.
> O `PixelCameraDemo` também usa `Input.GetKeyDown` (Input Manager legado).

### 📦 Estrutura do Projeto

```
PixelCamera/
├── Runtime/
│   ├── PixelCameraRenderFeature.cs       # ScriptableRendererFeature (settings serializadas)
│   ├── PixelCameraRenderPass.cs          # ScriptableRenderPass (RenderGraph / RTHandle / legado)
│   ├── PixelCameraAutoSetup.cs           # Resolve o Render Feature e aplica as configurações
│   ├── PixelCameraController.cs          # Atalhos F1-F4 + scroll (Input Manager legado)
│   ├── PalettePresetAsset.cs             # ScriptableObject de paleta
│   ├── Shaders/
│   │   └── PixelCameraShader.shader      # Shader HLSL (pass 0 = efeito, pass 1 = upscale point)
│   ├── Utilities/
│   │   └── PaletteUtility.cs             # Gerar/carregar/salvar paletas
│   └── PixelCamera.Runtime.asmdef
├── Editor/
│   ├── PixelCameraRenderFeatureEditor.cs # Inspector customizado
│   ├── PaletteEditorWindow.cs            # Palette Editor window
│   ├── PalettePresetAssetEditor.cs       # Inspector do ScriptableObject de paleta
│   ├── PixelCameraSetupUtility.cs        # Itens do menu Tools
│   ├── PixelCameraProjectDiagnostics.cs  # Diagnóstico + auto-correção do Render Feature
│   └── PixelCamera.Editor.asmdef
├── Samples~/
│   └── DemoScene/
│       ├── PixelCameraDemo.cs            # 5 modos de demo (teclas 1-5)
│       └── PixelCameraTestScene.cs       # Gerador de cena de teste
├── package.json                          # Manifesto UPM
├── README.md                             # Documentação principal
├── QUICKSTART.md                         # Quick start guide
├── GUIA-PASSO-A-PASSO.md                 # Tutorial visual
├── CHANGELOG.md                          # Changelog + roadmap
└── FEATURES.md                           # Este arquivo
```

## 🎯 Resumo

| Categoria | Status |
| --- | --- |
| **Pixelização** | ✅ Completo |
| **Paletas** (presets de 16 cores) | ✅ Completo |
| **Paletas** (presets de 2–4 cores) | ⚠️ Funcional com desvio visual conhecido |
| **Extração de paleta de imagem** | ⚠️ Amostragem aleatória, não extração real |
| **Dithering** (Bayer 2x2 / 4x4 / 8x8) | ✅ Completo |
| **Dithering** (Floyd-Steinberg) | ❌ Não implementado (enum exposto no Inspector) |
| **CRT** (scanlines, bloom, curvatura, vinheta) | ✅ Completo (bloom é aproximação 5×5) |
| **Controller Runtime** | ✅ Completo — requer Input Manager legado |
| **Ferramentas de Editor** | ✅ Completo (7 itens de menu + inspector + Palette Editor) |
| **Documentação** | ✅ Completo |
| **Samples** | ⚠️ Scripts de exemplo; sem cena `.unity` pré-montada |
| **Pipeline: URP** | ✅ Suportado (12 → 17+) |
| **Pipeline: Built-in** | ❌ Não suportado |
| **Pipeline: HDRP** | ❌ Não suportado |

## ⚠️ Limitações que você precisa conhecer antes de publicar/usar

Lista completa e detalhada em [README.md → Limitações conhecidas](README.md#limitações-conhecidas).
Resumo:

1. Paletas com menos de 16 cores têm slots pretos fantasma (afeta GameBoy, CGA, Binary).
2. Máximo de 16 cores (limite do shader; `colorCount` até 256 só afeta a pré-quantização).
3. `FloydSteinberg` aparece no dropdown mas não faz nada.
4. `ExtractPaletteFromTexture` é amostragem aleatória, não extração de paleta.
5. UI em *Screen Space - Overlay* não é pixelada.
6. `PixelCameraController` depende do Input Manager legado.
7. Paletas geradas em runtime usam `Wrap Mode = Repeat` (não *Clamp*).
8. Curvatura CRT alta recorta as bordas em preto.
9. Sem suporte a Built-in e HDRP.

## 🚀 Destaques

### ✅ Pontos fortes reais
- Efeito completo em **dois blits** (cena → baixa resolução → upscale *point*)
- **Três caminhos de implementação** cobrindo URP 12 → 17+ (RenderGraph no Unity 6), o que é a
  parte mais difícil de manter num asset de pós-processamento URP
- **Diagnóstico de projeto embutido** que roda após cada compilação e aponta a causa exata
  (URP ausente, pipeline não atribuído, Feature faltando, Feature no Renderer errado)
- **Auto-correção em 1 clique** do Render Feature, com registro correto como sub-asset
- Cache das texturas de paleta por preset (evita alocar por frame)
- Assembly definitions com namespace dedicado e referências explícitas
- Ferramentas de editor de verdade: inspector customizado, Palette Editor com export/import PNG
- Documentação extensa, incluindo limitações

### 🎨 Qualidade de código
- Comentários XML nos tipos públicos
- `Debug.LogError`/`LogWarning` com passos de correção, não só a mensagem
- Guards de null nos setters da API pública
- `HideFlags.HideAndDontSave` no material interno
- `Dispose(bool)` liberando os RTHandles no caminho URP 13–16

### 📖 Documentação
- README com requisitos, instalação, API e limitações
- Quick start com troubleshooting
- Tutorial visual passo a passo
- Matriz de compatibilidade de pipeline no README raiz

## 🎉 Conclusão

**Pixel Camera** é um efeito de pixel art **completo e funcional para URP**, cobrindo da URP 12
(Unity 2021.3 LTS) à URP 17+ (Unity 6, Render Graph).

Ele entrega o que promete no eixo principal — pixelização com upscale *point*, paletas de 16 cores,
dithering Bayer e CRT com controles individuais — e vem com ferramental de editor acima da média
para um asset desse porte (diagnóstico automático e auto-fix do Render Feature).

O que ele **não** é:

- ❌ Não é multi-pipeline. É **URP apenas** — Built-in e HDRP não são suportados, por decisão
  técnica documentada.
- ❌ Não é um sistema de paleta ilimitado. O teto é **16 cores**.
- ❌ Não tem difusão de erro (Floyd-Steinberg) nesta versão.
- ⚠️ Os presets de 2 e 4 cores precisam de ajuste (limitação 1) para entregar o visual anunciado.

