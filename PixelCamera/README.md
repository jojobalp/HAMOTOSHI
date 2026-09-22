# 🎮 Pixel Camera (URP)

Sistema de câmera pixel art para **Unity URP**: pixelização, paletas limitadas, dithering Bayer e
efeito CRT — tudo num único Render Feature, com controles individuais e API em runtime.

> **Pacote:** `com.pixelcamera.unity` · **Versão:** 1.0.3 · **Unity:** 2021.3 LTS+ · **Pipeline:** URP 12+

---

## 🔌 Compatibilidade

**Este asset funciona apenas com a Universal Render Pipeline (URP).**

| Pipeline | Suporte |
| --- | --- |
| **URP** 12 → 17+ (Unity 2021.3 LTS a Unity 6.x) | ✅ **Suportado** |
| **Built-in** (pipeline legado) | ❌ Não suportado |
| **HDRP** (High Definition) | ❌ Não suportado |

Os motivos técnicos estão documentados no [README raiz](../README.md#por-que-não-built-in--hdrp).
Em resumo: o Built-in não tem `ScriptableRendererFeature` (o mecanismo de pós-processamento dele,
`OnRenderImage`/`Graphics.Blit`, não existe em SRPs), e o HDRP não executa Render Features da URP —
além de renderizar em HDR e aplicar upscale (FSR/CAS/DLSS/TAA) por cima do efeito, o que destrói o
alinhamento 1:1 dos pixels.

**Plataformas:** o efeito é um blit fullscreen e não usa API específica de plataforma. Windows,
macOS, Linux, Android, iOS e WebGL funcionam, respeitando o suporte que a própria URP tem em cada
uma. Consoles devem ser validados individualmente.

### Requisitos

| Requisito | Mínimo | Observação |
| --- | --- | --- |
| Unity | 2021.3 LTS | — |
| Universal RP | 12.0.0 | Resolvido automaticamente via Package Manager |
| Shader Model | 4.5+ | — |
| **Active Input Handling** | **Input Manager (old)** | Necessário para os atalhos F1–F4 e o scroll do `PixelCameraController`. Em projeto *Input System Package (new)* exclusivo, use `PixelCameraAutoSetup`/API própria em vez do Controller. |
| Render Graph (Unity 6) | Habilitado | *Project Settings > Graphics > URP* (padrão no Unity 6.1+) |

---

## ✨ Features

- **Pixelização** com resolução configurável em runtime (16×16 até 1280×720)
- **Paletas limitadas** com 7 presets: GameBoy, NES, CGA, PICO-8, GB Color, Grayscale, Binary
- **Paleta custom** via textura 16×1, gerador aleatório ou `PalettePresetAsset` (ScriptableObject)
- **Dithering** Bayer 2×2, 4×4 e 8×8 com intensidade configurável
- **Efeito CRT** com controles individuais: scanlines, bloom/glow, curvatura e vinheta
- **Snap to Pixel Grid** para alinhamento de pixels
- **Todos os efeitos toggáveis** individualmente (efeito desligado não é processado no shader)
- **Ferramentas de editor**: inspector customizado, Palette Editor window, presets rápidos,
  setup de câmera em 1 clique, diagnóstico de projeto e auto-correção do Render Feature

Veja o inventário completo (inclusive o que **não** está implementado) em
[`FEATURES.md`](FEATURES.md) e as limitações em [Limitações conhecidas](#limitações-conhecidas).

---

## 📦 Instalação

### Opção A — Package Manager (recomendado)

Adicione ao `Packages/manifest.json`:

```json
"com.pixelcamera.unity": "https://github.com/jojobalp/HAMOTOSHI.git?path=PixelCamera"
```

A dependência do URP é resolvida automaticamente.

### Opção B — Copiar para `Assets/`

Copie a pasta `PixelCamera` para dentro de `Assets/`. O conteúdo relevante:

```
Assets/
└── PixelCamera/
    ├── Runtime/
    │   ├── PixelCameraRenderFeature.cs   # ScriptableRendererFeature
    │   ├── PixelCameraRenderPass.cs      # ScriptableRenderPass (3 caminhos por versão de URP)
    │   ├── PixelCameraAutoSetup.cs       # Resolve o Render Feature e aplica as configs
    │   ├── PixelCameraController.cs      # Atalhos F1–F4 + scroll (Input Manager)
    │   ├── PalettePresetAsset.cs         # ScriptableObject de paleta
    │   ├── Shaders/PixelCameraShader.shader
    │   └── Utilities/PaletteUtility.cs
    └── Editor/
        ├── PixelCameraRenderFeatureEditor.cs
        ├── PaletteEditorWindow.cs
        ├── PalettePresetAssetEditor.cs
        ├── PixelCameraSetupUtility.cs
        └── PixelCameraProjectDiagnostics.cs
```

Neste modo o URP precisa ser instalado manualmente:

1. **Window > Package Manager > Unity Registry** → **Universal RP** → *Install*
2. **Edit > Project Settings > Graphics** → atribua um *UniversalRenderPipelineAsset*
3. **Edit > Project Settings > Quality** → atribua o mesmo asset

### Configurar o Renderer

#### 🚀 Automático (1 clique)

**Tools > Pixel Camera > Corrigir Automaticamente (Adicionar Render Feature)**

Localiza o Renderer **ativo** na pipeline e adiciona o `Pixel Camera Render Feature` sem duplicação.
Depois use **Tools > Pixel Camera > Setup na Câmera Atual**, que faz o resto (adiciona
`PixelCameraAutoSetup` na câmera, preenche o Renderer Asset e aplica valores padrão).

#### Manual

1. Abra o **Universal Render Pipeline Asset** (*Project Settings > Graphics*)
2. Clique em **Edit** no Renderer
3. **Add Renderer Feature** → **Pixel Camera Render Feature**
4. **Marque a caixa ao lado do nome** — se o Feature estiver inativo, a URP nem chama `AddRenderPasses`

> ⚠️ O Render Feature precisa estar no Renderer que a pipeline **realmente usa**. Um Feature
> adicionado num Renderer secundário (ex.: um Renderer 2D que não está em uso) não produz efeito
> nenhum. O diagnóstico avisa exatamente esse caso.

---

## 🎯 Uso

### Setup mínimo

1. Adicione o **Pixel Camera Render Feature** ao URP Renderer ativo
2. Defina a resolução (ex.: 320×180)
3. Escolha um preset de paleta (ex.: PICO-8)
4. Play

### Com o Controller em runtime

```csharp
var controller = camera.gameObject.AddComponent<PixelCameraController>();
controller.RenderFeature = pixelRenderFeature;
```

**Atalhos padrão** (reconfiguráveis no Inspector do componente):

| Tecla | Ação |
| --- | --- |
| `F1` | Liga/desliga todo o efeito |
| `F2` | Liga/desliga CRT |
| `F3` | Liga/desliga dithering |
| `F4` | Liga/desliga paleta |
| Scroll do mouse | Ajusta a largura da resolução (mantém o aspect ratio da câmera) |

### API pública

```csharp
pixelCamera.SetPixelResolution(640, 360);
pixelCamera.SetPalettePreset(PixelCameraRenderFeature.PalettePreset.GameBoy);
pixelCamera.SetCustomPalette(myPaletteTexture);
pixelCamera.GenerateRandomPalette(seed: 42);
pixelCamera.SetCRTEffect(true);
pixelCamera.SetDithering(true);
```

---

## 🎨 Paletas

### Presets inclusos

| Preset | Cores definidas |
| --- | --- |
| GameBoy | 4 tons de verde |
| NES | 16 cores |
| CGA | 4 cores |
| PICO-8 | 16 cores |
| GB Color | 16 cores |
| Grayscale | 16 tons de cinza |
| Binary | 2 cores (preto e branco) |

> ⚠️ **Os presets de menos de 16 cores têm um comportamento conhecido incorreto.** Leia
> [Limitações conhecidas → Paletas com menos de 16 cores](#1-paletas-com-menos-de-16-cor-gameboy-cga-binary).

### Como o shader lê a paleta (importante)

A paleta é uma **textura 16×1** (`RGBA32`, sem mipmaps) e o shader percorre **sempre os 16 slots**:

```hlsl
for (int i = 0; i < 16; i++) { ... }   // PALETTE_MAX_COLORS
```

Portanto, ao criar uma paleta custom:

- Use **exatamente 16 pixels de largura × 1 de altura**.
- `Filter Mode = Point`, `Wrap Mode = Clamp`, **mipmaps desligados**, sem compressão.
- Se a sua paleta tem menos de 16 cores, **preencha os slots restantes repetindo cores existentes**
  (não deixe em preto/transparente — slots vazios participam da busca de cor mais próxima).

### Fontes de paleta custom

| Método | Onde |
| --- | --- |
| Editor visual | **Tools > Pixel Camera > Palette Editor** (grid editável, gradiente, temas, export/import PNG) |
| ScriptableObject | **Assets > Create > Pixel Camera > Palette Preset** + `preset.ToTexture()` |
| Gerador aleatório | `PaletteUtility.CreateRandomPalette(16, seed: 42)` |
| Amostragem de imagem | `PaletteUtility.ExtractPaletteFromTexture(source, maxColors: 16)` — veja a limitação em [`FEATURES.md`](FEATURES.md) |

> Formatos de arquivo suportados para **carregar** paleta: **PNG e JPEG**
> (`Texture2D.LoadImage`). BMP e outros formatos **não** são suportados.

---

## 🔧 Configurações

### Pixelização

```csharp
pixelSettings.pixelWidth = 320;        // 0 = usa a resolução da tela
pixelSettings.pixelHeight = 180;       // 0 = usa a resolução da tela
pixelSettings.snapToPixelGrid = true;  // realinha a largura ao aspect ratio
```

### Paleta

```csharp
paletteSettings.enablePalette = true;
paletteSettings.preset = PixelCameraRenderFeature.PalettePreset.PICO8;
paletteSettings.customPalette = myTexture;  // se != null, sobrescreve o preset
paletteSettings.colorCount = 16;            // pré-quantização (2–256)
paletteSettings.colorQuantization = 0.5f;   // 0 = só paleta, 1 = só quantização
```

`colorCount` controla a **pré-quantização** (`floor(c*n+0.5)/n`) antes da busca na paleta. A busca
na paleta é sempre limitada a **16 cores** — veja as limitações.

### Dithering

```csharp
ditherSettings.enableDithering = true;
ditherSettings.ditherType = PixelCameraRenderFeature.DitherType.Bayer4x4;
ditherSettings.intensity = 0.5f;
```

Implementados: `Bayer2x2` (padrão grande, mais retrô), `Bayer4x4` (equilibrado), `Bayer8x8`
(fino/suave). **`FloydSteinberg` aparece no dropdown mas não está implementado** — selecioná-lo
equivale a dithering desligado.

O dithering é uma **modulação de brilho pós-quantização** (`color.rgb += (threshold-0.5)*intensity`),
não uma difusão de erro nem um re-snap para a paleta. É um efeito estilizado, não um dithering
ordenado "correto" no sentido clássico.

### CRT

```csharp
crtSettings.enableCRT = true;
crtSettings.scanlineIntensity = 0.3f;   // 0–1
crtSettings.scanlineThickness = 2f;     // 1–4
crtSettings.bloomIntensity = 0.2f;      // 0–1
crtSettings.bloomRadius = 3f;           // 1–10
crtSettings.curvatureIntensity = 0.1f;  // 0–1
crtSettings.vignetteIntensity = 0.3f;   // 0–1
```

As scanlines usam `_ScreenParams.y` da textura de **baixa resolução**, então a espessura percebida
muda conforme a resolução configurada. O bloom é uma aproximação por box de 5×5 amostras — não é
bloom physically-based.

---

## 🎮 Presets rápidos (Inspector)

- **🎮 Retrô Completo** — 320×180, PICO-8, dithering + CRT leve
- **👾 GameBoy** — 160×144, paleta GameBoy, dithering forte
- **📺 CRT Clássico** — 640×360, NES, CRT pesado

---

## 📊 Performance

- Um único efeito em **dois blits** (cena → baixa resolução → tela), com upscale em filtro *point*.
- O custo dominante é o **bloom** do CRT (25 amostras da textura por pixel quando ativo).
- Desligar um efeito remove o branch correspondente no shader.

### Dicas

1. Resoluções baixas (160×144, 320×180) dão o visual retrô **e** custam menos.
2. Desabilite CRT/bloom quando não estiver usando.
3. `Bayer4x4` é mais barato que `Bayer8x8`.
4. Reduza `bloomRadius` antes de desligar o bloom inteiro.

---

## ⚠️ Limitações conhecidas

Leia antes de comprar/usar. Nada aqui é bug de configuração — são limites reais da versão 1.0.3.

### 1. Paletas com menos de 16 cor (GameBoy, CGA, Binary)

`FindClosestPaletteColor` percorre sempre os 16 slots da textura, mas `GetPresetPalette` só preenche
os slots reais do preset — o restante fica `(0,0,0,0)`, ou seja, **preto**. Como o preto fantasma
participa da comparação de distância, ele "vence" para qualquer cor escura ou média.

Efeito prático: **Binary** tende a produzir uma imagem quase toda preta (não preto e branco),
e **GameBoy**/**CGA** perdem os tons escuros/médios. Os presets de 16 cores (NES, PICO-8, GB Color,
Grayscale) **não** são afetados.

*Workaround:* use um preset de 16 cores, ou crie uma paleta custom 16×1 preenchendo os slots
sobrantes com repetições das suas cores reais.

### 2. Máximo de 16 cores

Limite do lookup no shader (`PALETTE_MAX_COLORS 16`, textura de paleta 16×1). O `colorCount` do
Inspector aceita até 256, mas isso só afeta a pré-quantização — nunca haverá mais de 16 cores
distintas vindas da paleta.

### 3. `FloydSteinberg` não implementado

O valor existe no enum e aparece no dropdown, mas o shader só trata Bayer 2/4/8. Difusão de erro
exigiria múltiplos passes (está no roadmap).

### 4. `PaletteUtility.ExtractPaletteFromTexture` é amostragem aleatória

Apesar do nome, não há quantização nem clustering: o método sorteia até 1000 pixels da imagem e
guarda os primeiros `maxColors` distintos num `HashSet<Color>`. Como a comparação é em float
`RGBA`, quase todo pixel é "único", então o resultado são **N pixels aleatórios da imagem**, não a
paleta representativa dela. Os slots não preenchidos ficam pretos (limitação 1).
Não use isso esperando "extrair a paleta" de uma sprite — use o **Palette Editor**.

### 5. UI não é afetada

O efeito é aplicado na cor da câmera dentro do pipeline. **Canvas em *Screen Space - Overlay* não é
pixelado.** Para pixelar a UI junto, use *Screen Space - Camera* na mesma câmera (ou *World Space*).

### 6. `PixelCameraController` usa Input Manager legado

Se o projeto estiver com *Active Input Handling* = **Input System Package (new)** exclusivo, as
chamadas `Input.GetKeyDown`/`Input.mouseScrollDelta` não funcionam (e podem logar exceção).
Nesse caso controle o efeito pela API do `PixelCameraRenderFeature` ou pelo seu próprio input.

### 7. Texturas de paleta geradas em runtime usam Wrap = Repeat

`PaletteUtility` e `PalettePresetAsset.ToTexture()` criam `Texture2D` definindo apenas
`filterMode = Point`; o `wrapMode` fica no padrão (**Repeat**), não *Clamp*. Combinado com a
limitação 1, amostras fora do intervalo útil podem voltar ao início da textura.

### 8. Curvatura CRT recorta as bordas

Com `curvatureIntensity` alto, as UVs saem de `[0,1]` e o shader devolve preto — é intencional
(borda de tubo), mas pode cortar conteúdo importante. Compense com FOV/enquadramento.

### 9. Sem suporte a Built-in e HDRP

Veja a seção [Compatibilidade](#compatibilidade).

---

## 🐛 Troubleshooting

### Erros de compilação: CS0234 / CS0118

```
error CS0234: The type or namespace name 'Universal' does not exist in the namespace
              'UnityEngine.Rendering' (are you missing an assembly reference?)
error CS0118: 'Editor' is a namespace but is used like a type
```

**CS0234** — o projeto **não tem o URP instalado** (ou está em Built-in/HDRP sem URP). Todo o pacote
depende da URP (`ScriptableRendererFeature`, `ScriptableRenderPass`, `Blitter`…), então sem ela as
assemblies não compilam.

1. **Window > Package Manager > Unity Registry** → **Universal RP** → *Install*
2. **Project Settings > Graphics > Scriptable Render Pipeline Settings** → atribua um
   *UniversalRenderPipelineAsset* (*Assets > Create > Rendering > URP Asset (with Universal Renderer)*)
3. Repita em **Project Settings > Quality > Render Pipeline Asset**
4. Aguarde recompilar — o Console deve ficar limpo

> 💡 **Tools > Pixel Camera > Diagnóstico do Projeto (URP)** roda sozinho após cada compilação e
> diz exatamente o que falta (URP ausente, pipeline não atribuído, Render Feature não adicionado ou
> Feature presente num Renderer que não está em uso).

**CS0118** — colisão de nomes: dentro de `namespace PixelCamera.Editor`, o identificador `Editor`
resolve para o **namespace** (membros do namespace pai têm prioridade sobre tipos de `using`), não
para `UnityEditor.Editor`. Já corrigido no pacote: as classes de inspector herdam de
`UnityEditor.Editor` qualificado. Faça o mesmo se criar editores novos nesse namespace.

### Shader não encontrado

```
[PixelCamera] Shader 'Hidden/PixelCamera' não encontrado!
```

Verifique se `PixelCameraShader.shader` está em `Runtime/Shaders/` e faça **Assets > Reimport All**.

### `[PixelCamera AutoSetup] PixelCameraRenderFeature não encontrado!`

Corrigido na **1.0.2** — o Auto Setup usava `Object.FindObjectsOfType`, que não enxerga
ScriptableObjects gravados como sub-asset do Renderer Asset. A resolução agora segue:

1. campo **Render Feature (direto)** do componente, se preenchido;
2. campo **Renderer Asset** do componente;
3. no Editor, todos os Renderer Assets do projeto;
4. qualquer instância já carregada (`Resources.FindObjectsOfTypeAll`).

Se ainda aparecer, o Feature realmente não foi adicionado:

1. Selecione o **Renderer Asset** no Project
2. **Add Renderer Feature > Pixel Camera Render Feature**
3. **Marque a caixa** ao lado do nome (Feature inativo ⇒ a URP não chama o pass)
4. Arraste o Renderer Asset para o campo **Renderer Asset** do `Pixel Camera Auto Setup`
5. Ou use **Tools > Pixel Camera > Corrigir Automaticamente (Adicionar Render Feature)**

### Render Feature não aparece na lista

1. Confirme que o projeto usa **URP** (não Built-in nem HDRP)
2. Confirme que o pacote foi importado sem erros de compilação (Console limpo)
3. **Assets > Reimport All**

### O efeito não aparece no Game view

1. O Render Feature está no Renderer **ativo** da pipeline? (o diagnóstico diferencia isso)
2. A caixa ao lado do Feature no Renderer está **marcada**?
3. O campo `enabled` do Feature está ligado?
4. No **Unity 6**, o **Render Graph** está habilitado em *Project Settings > Graphics > URP*?
5. A câmera está renderizando com o Renderer certo (campo *Renderer* do Camera)?

### Pixels não alinhados / "tremendo"

1. Habilite **Snap to Pixel Grid**
2. Use resoluções que casem com o aspect ratio da tela (320×180 e 640×360 para 16:9)
3. Desative **Dynamic Resolution** e upscale filters da URP na câmera
4. Câmera **orthographic** ajuda em 2D

### Atalhos F1–F4 não respondem

O `PixelCameraController` usa o **Input Manager legado**. Veja a limitação 6.

---

## 📞 Suporte

Dúvidas, bugs e sugestões: abra uma **issue** no repositório
([jojobalp/HAMOTOSHI](https://github.com/jojobalp/HAMOTOSHI)).

## 📝 Licença

**A definir pelo autor antes da publicação.** Enquanto não houver um arquivo `LICENSE` na raiz do
repositório, o código está sob copyright reservado (todos os direitos reservados) — o que é
incompatível com distribuição em store. Escolha uma licença (MIT, Apache-2.0, ou a licença padrão da
Asset Store) e adicione o arquivo `LICENSE` antes de publicar.
