# 📊 PIXEL CAMERA - INVENTÁRIO DE FEATURES

Inventário **real** do que existe no pacote `com.pixelcamera.unity` 1.1.1.
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
- [x] **GameBoy** - 4 tons de verde clássico (correto a partir da 1.1.0)
- [x] **NES** - 16 cores do Nintendo Entertainment System
- [x] **CGA** - 4 cores do CGA antigo (correto a partir da 1.1.0)
- [x] **PICO-8** - 16 cores do fantasy console
- [x] **GB Color** - 16 cores do GameBoy Color
- [x] **Grayscale** - 16 tons de cinza
- [x] **Binary** - 2 cores (preto e branco) (correto a partir da 1.1.0)

> **Corrigido na 1.1.0:** até a 1.0.3 os presets com menos de 16 cores ficavam dominados por preto —
> `GetPresetPalette` criava uma textura 16×1 preenchendo só os slots reais, e o `Frag` usava
> `int paletteSize = 16;` fixo, então os slots vazios (pretos) competiam na busca de cor mais
> próxima. Agora o C# envia o número real de cores via `_PaletteSize` e o shader compara apenas
> contra as cores que existem. Veja
> [README → Corrigido na 1.1.0](README.md#corrigido-na-110).

#### Paleta Custom
- [x] **Importar via arquivo de imagem** (1.1.1: funcionando de verdade)
  - Carregamento por arquivo: **PNG e JPEG** (`Texture2D.LoadImage`). **BMP não é suportado**, e
    também não há leitor para `.pal`, `.gpl`, `.act`, `.hex`, JSON ou CSV
  - A imagem é decodificada **direto do disco**, numa textura temporária em memória — então **não
    depende dos settings de importação do arquivo de origem**
  - Se for uma **faixa de paleta** (altura 1, largura até 16), as cores são usadas como estão;
    qualquer outra imagem passa pela extração das cores mais frequentes
  - O resultado é gravado como PNG **16×1** em `Assets/PixelCameraPalettes/` com `Point`, `Clamp`,
    sem mipmaps, sem compressão, `Read/Write Enabled` e `npotScale = None` — **configurado
    automaticamente**, sem passos manuais
  - Botões no Inspector do Render Feature, no Palette Editor, no Inspector do `PalettePresetAsset` e
    em **Tools > Pixel Camera > Criar Paleta a partir de Imagem**
  - Preview no editor
  
- [x] **Gerar aleatória**
  - Seed opcional para reprodutibilidade
  - Cores sorteadas em **HSV** com saturação e valor altos (tons vibrantes, como se espera de uma
    paleta retrô)
  - Usa um `System.Random` próprio: **não** altera o estado global de `UnityEngine.Random`
  - Gera a textura já no formato 16×1 / Point / Clamp, com os slots preenchidos por repetição
  - Botão no editor (grava como **asset em disco**, a referência sobrevive a domain reload) + API
  
- [x] **Extrair de imagem existente** (1.1.1: agora tem botão no editor)
  - Configuração de max colors
  - Extração **real** a partir da 1.1.0: amostragem em grid determinístico, agrupamento em buckets
    de 5 bits por canal e seleção das cores **mais frequentes** (cada uma pela média do bucket)
  - Pela **API de runtime** (`PaletteUtility.ExtractPaletteFromTexture`) requer *Read/Write Enabled*
    na textura de origem (falha com mensagem clara se não estiver)
  - Pelos **botões do editor** esse requisito não existe: a imagem é decodificada do disco em
    memória, sempre legível
  - ⚠️ Foto com milhões de cores gera uma paleta "média". Para resultado retrô fiel, use pixel art,
    ilustração flat ou sprite sheet — e prefira PNG a JPEG (artefato de compressão espalha as cores)

- [x] **ScriptableObject para presets**
  - Criação via menu Assets
  - Inspector customizado
  - Conversão para Texture2D (`ToTexture()` devolve sempre 16×1, Point, Clamp, slots preenchidos)
  - Import/Export de PNG
  - `FromTexture()` mantém os **16 slots** preenchidos por repetição (1.1.1)

- [x] **Palette Editor Window**
  - Interface visual completa
  - Grid de cores editável
  - Ferramenta de gradiente
  - Presets de tema (Natureza, Pôr-do-sol, Noturno, Pastel)
  - Preview em tempo real
  - Export/Import PNG + **Extrair de Imagem**

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
- ❌ **Floyd-Steinberg (difusão de erro)** — requer múltiplos passes sequenciais; roadmap 1.3.0.
  - Até a 1.0.3 o valor **existia no enum e aparecia no dropdown**, mas o shader nunca o tratou:
    selecioná-lo deixava o dithering silenciosamente desligado. Foi **removido na 1.1.0**; assets
    antigos com o inteiro órfão 3 são detectados no Inspector e normalizados para Bayer 4×4.

> **Como o dithering é aplicado (a partir da 1.1.0):** é dithering **ordenado de verdade**. O
> threshold de Bayer é somado à cor **antes** da quantização e do lookup da paleta, escalado pelo
> número de níveis (`offset / colorCount`), então o resultado alterna entre as duas cores vizinhas
> que existem na paleta. O padrão usa as UVs **originais** (não as distorcidas pela curvatura CRT),
> ficando estável e alinhado à tela.
>
> Até a 1.0.3 o offset era somado **depois** do lookup (`color.rgb += dither`), o que apenas
> deslocava o brilho de uma cor já escolhida e gerava tons que não pertencem à paleta.

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
- [x] **Tools > Pixel Camera > Criar Paleta a partir de Imagem** - PNG/JPEG do disco → asset 16×1
      configurado (novo na 1.1.1)
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
| **Paletas** (todos os 7 presets, 2 a 16 cores) | ✅ Completo |
| **Extração de paleta de imagem** | ✅ Completo (frequência em buckets quantizados) |
| **Dithering** (Bayer 2x2 / 4x4 / 8x8, ordenado) | ✅ Completo |
| **Dithering** (Floyd-Steinberg) | ❌ Não implementado (removido do enum na 1.1.0) |
| **CRT** (scanlines, bloom, curvatura, vinheta) | ✅ Completo (bloom é aproximação 5×5) |
| **Controller Runtime** | ✅ Completo — requer Input Manager legado |
| **Ferramentas de Editor** | ✅ Completo (7 itens de menu + inspector + Palette Editor) |
| **Documentação** | ✅ Completo |
| **Licença** | ✅ MIT (`LICENSE` na raiz do repositório) |
| **Samples** | ⚠️ Scripts de exemplo; sem cena `.unity` pré-montada |
| **Pipeline: URP** | ✅ Suportado (12 → 17+) |
| **Pipeline: Built-in** | ❌ Não suportado |
| **Pipeline: HDRP** | ❌ Não suportado |

## ⚠️ Limitações que você precisa conhecer antes de publicar/usar

Lista completa e detalhada em [README.md → Limitações conhecidas](README.md#limitações-conhecidas).
Resumo (versão 1.1.1):

1. Máximo de **16 cores** (`colorCount` até 256 só afeta a pré-quantização).
2. **Floyd-Steinberg** não existe (só Bayer ordenado); difusão de erro é roadmap 1.3.0.
3. A textura de paleta custom precisa ter **exatamente 16×1**.
4. UI em *Screen Space - Overlay* **não** é pixelada.
5. `PixelCameraController` depende do **Input Manager legado**.
6. Curvatura CRT alta recorta as bordas em preto.
7. Bloom do CRT é box 5×5 (25 amostras/pixel) — o efeito mais caro.
8. Scanlines usam a resolução da textura de baixa resolução.
9. Sem suporte a **Built-in e HDRP**.

### Corrigido na 1.1.1 (não são mais limitações)

- "Importar Paleta de Texture" do Render Feature falhava **sempre** (caminho absoluto passado a
  `AssetDatabase.LoadAssetAtPath`) → imagem decodificada do disco e gravada como asset 16×1.
- Extração de paleta não tinha **nenhum botão** no editor (só API de código) → 4 entradas no editor.
- `PaletteEditorWindow.ImportPalette` lia a linha 0 sem reamostrar → fluxo compartilhado com detecção
  de faixa de paleta.
- `PalettePresetAsset.FromTexture` truncava o array de cores → sempre 16 slots.
- Configurar `Point`/`Clamp`/mipmaps/`Read-Write` manualmente após importar → `TextureImporter`
  configurado automaticamente.
- `CS0618` de `FindFirstObjectByType` no Unity 6 → `FindAnyObjectByType`.

### Corrigido na 1.1.0 (não são mais limitações)

- Presets de 2–4 cores dominados por preto → o shader recebe o nº real de cores.
- Dithering somado depois do lookup → agora é ordenado, aplicado antes da quantização.
- `FloydSteinberg` no dropdown sem efeito → removido, com migração automática no Inspector.
- `ExtractPaletteFromTexture` aleatória → extração por frequência real.
- `CreateRandomPalette` em RGB + `Random.InitState` global → HSV + `System.Random` próprio.
- Paletas em runtime com `Wrap = Repeat` → `Clamp`.
- `ToTexture()` com largura variável → sempre 16×1.
- "Gerar Paleta Aleatória" criava textura só em memória → grava como asset.
- Export do Palette Editor com largura variável → sempre 16×1.
- Importação aceitava BMP (não decodificável) → filtro PNG/JPEG + checagem de falha.
- Sem arquivo `LICENSE` → **MIT** adicionado.

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
- ⚠️ Os *Samples* são scripts, não uma cena `.unity` pronta para abrir e dar Play.

