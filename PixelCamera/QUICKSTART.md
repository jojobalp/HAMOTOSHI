# 🚀 Quick Start Guide

## ⚠️ Antes de começar: o seu projeto é URP?

O Pixel Camera funciona **apenas com a Universal Render Pipeline (URP)**.

| Pipeline | Funciona? |
| --- | --- |
| URP (Unity 2021.3 LTS a Unity 6.x, URP 12+) | ✅ Sim |
| Built-in (pipeline legado) | ❌ Não |
| HDRP | ❌ Não |

Como conferir: **Edit > Project Settings > Graphics > Scriptable Render Pipeline Settings**.
Se o campo estiver **vazio** (Built-in) ou com um **HD Render Pipeline Asset** (HDRP), o asset não
vai funcionar. Motivos técnicos: [README raiz](../README.md#por-que-não-built-in--hdrp).

---

## Setup em 5 minutos

### Passo 0: Pré-requisitos

1. **Window > Package Manager > Unity Registry** → instale **Universal RP**
2. **Edit > Project Settings > Graphics** e **Quality** → atribua um *UniversalRenderPipelineAsset*
3. **Project Settings > Player > Active Input Handling** → **Input Manager (old)** ou **Both**
   (necessário para os atalhos F1–F4 e o scroll do `PixelCameraController`)
4. No **Unity 6**: confirme que o **Render Graph** está habilitado em
   *Project Settings > Graphics > URP*

Sem o URP você recebe erros como
`CS0234: The type or namespace name 'Universal' does not exist in the namespace 'UnityEngine.Rendering'`.

### Passo 1: Instalar

**Via Package Manager (recomendado)** — adicione ao `Packages/manifest.json`:

```json
"com.pixelcamera.unity": "https://github.com/jojobalp/HAMOTOSHI.git?path=PixelCamera"
```

**Ou** copie a pasta `PixelCamera` para dentro de `Assets/`.

Aguarde a Unity compilar. Se aparecer erro no Console, rode
**Tools > Pixel Camera > Diagnóstico do Projeto (URP)**.

### Passo 2: Configurar o URP Renderer

**Automático (1 clique):**

**Tools > Pixel Camera > Corrigir Automaticamente (Adicionar Render Feature)**

**Manual:**

1. **Edit > Project Settings > Graphics** → abra o **Universal Render Pipeline Asset**
2. Clique em **Edit** no campo **Renderer**
3. **Add Renderer Feature** → **Pixel Camera Render Feature**
4. **Marque a caixa ao lado do nome do Feature** — se ficar desmarcada, a URP nem chama o pass

> O Feature precisa estar no Renderer que a pipeline **usa de fato**. Adicionar num Renderer
> secundário não produz efeito.

### Passo 3: Configurar os efeitos

No Inspector do **Pixel Camera Render Feature**:

```
✅ Enabled: true

📐 Pixelização:
   - Largura: 320
   - Altura: 180
   - Snap to Grid: true

🎨 Paleta:
   - Enabled: true
   - Preset: PICO-8          ← qualquer um dos 7 presets funciona

🔲 Dithering:
   - Enabled: false (opcional)
   - Type: Bayer4x4          ← só existe Bayer 2x2/4x4/8x8

📺 CRT:
   - Enabled: false (opcional)
```

> Os presets de menos de 16 cores (**GameBoy**, **CGA**, **Binary**) funcionam corretamente a partir
> da **1.1.0** — o C# informa ao shader o número real de cores. Nas versões até 1.0.3 eles ficavam
> dominados por preto; se estiver atualizando, veja
> [README → Corrigido na 1.1.0](README.md#corrigido-na-110).

### Passo 4: (Opcional) Controller em runtime

1. Selecione a **Main Camera**
2. **Tools > Pixel Camera > Setup na Câmera Atual** — ou adicione o componente
   `PixelCameraAutoSetup`/`PixelCameraController` manualmente
3. Se adicionar o `PixelCameraController` à mão, arraste o **Render Feature** para o campo
   *Render Feature*

### Passo 5: Testar

- **Play**
- `F1` liga/desliga o efeito inteiro · `F2` CRT · `F3` dithering · `F4` paleta
- **Scroll do mouse** ajusta a resolução (mantém o aspect ratio da câmera)

---

## Presets recomendados

Valores testados com presets de **16 cores**.

### 👾 NES / Retrô
```
Resolução: 320x180
Paleta: PICO-8
Dithering: Bayer4x4 (0.3)
CRT: Scanlines (0.2) + Bloom (0.1)
```

### 📺 CRT Autêntico
```
Resolução: 640x360
Paleta: NES
Dithering: Off
CRT: Scanlines (0.4) + Bloom (0.3) + Curvature (0.15) + Vignette (0.4)
```

### 🎨 Pixel Art Moderno
```
Resolução: 640x360
Paleta: PICO-8
Dithering: Off
CRT: Off
```

### 🎮 GameBoy Clássico
```
Resolução: 160x144
Paleta: GameBoy (4 tons de verde)
Dithering: Bayer2x2 (0.5)
CRT: Off
```

### ⬛ Preto e branco (1-bit)
```
Resolução: 320x180
Paleta: Binary (2 cores)
Dithering: Bayer2x2 (0.6)   ← é o dithering que produz os meios-tons
CRT: Off
```

---

## Paletas custom

**Regra de ouro: a textura de paleta deve ter exatamente 16×1 pixels**, `Filter Mode = Point`,
`Wrap Mode = Clamp`, sem mipmaps e sem compressão. Se a sua paleta tem menos cores, **preencha os
slots restantes repetindo cores** (nunca deixe em preto) — uma paleta custom é sempre interpretada
como tendo 16 cores.

Todas as ferramentas do pacote (presets, `PaletteUtility`, `PalettePresetAsset.ToTexture()`, export
do Palette Editor) já geram a textura nesse formato, preenchendo os slots por repetição.

### Método 1: Palette Editor (recomendado)
1. **Tools > Pixel Camera > Palette Editor**
2. Edite o grid de cores (ou use gradiente/temas prontos)
3. **Exportar PNG**
4. Importe o PNG no Unity e ajuste os settings de importação (16×1, Point, Clamp, sem mipmaps)
5. Arraste para o campo **Custom Palette**

### Método 2: ScriptableObject
1. **Assets > Create > Pixel Camera > Palette Preset**
2. Configure as cores
3. Use `preset.ToTexture()` para converter

> Desde a 1.1.0 `ToTexture()` devolve sempre uma textura **16×1** com `Point`/`Clamp` e os slots
> preenchidos por repetição das cores do preset.

### Método 3: Código
```csharp
// Cores sorteadas em HSV (vibrantes), seed reproduzível, textura já em 16x1/Point/Clamp
var palette = PaletteUtility.CreateRandomPalette(16, seed: 42);
pixelCamera.SetCustomPalette(palette);

// Extrai as cores mais frequentes da imagem (requer Read/Write Enabled na origem)
var extracted = PaletteUtility.ExtractPaletteFromTexture(myTexture, maxColors: 16);
```

> `CreateRandomPalette` usa um `System.Random` próprio: **não** altera o estado global de
> `UnityEngine.Random` (a 1.0.3 chamava `Random.InitState`, o que mudava a sequência aleatória do
> jogo inteiro).

---

## Troubleshooting

### "CS0234: The type or namespace name 'Universal' does not exist"
- O **Universal RP não está instalado** no projeto
- **Window > Package Manager > Unity Registry > Universal RP > Install**
- Depois atribua o *UniversalRenderPipelineAsset* em **Project Settings > Graphics** e **Quality**
- Rode **Tools > Pixel Camera > Diagnóstico do Projeto (URP)** para confirmar

### "'Editor' is a namespace but is used like a type (CS0118)"
- Colisão entre o namespace `PixelCamera.Editor` e a classe `UnityEditor.Editor`
- Use a base totalmente qualificada: `public class MeuEditor : UnityEditor.Editor`

### "Shader não encontrado"
- Verifique se `PixelCameraShader.shader` está em `Runtime/Shaders/`
- Recompile: **Assets > Reimport All**

### "Render Feature não aparece"
- Confirme que está usando **URP** (não Built-in ou HDRP)
- Confirme que o Console está **sem erros de compilação** (com erro, o Inspector/Feature não aparece)
- Verifique a versão do Unity (2021.3+)
- Reinicie a Unity

### "O efeito não aparece no Game view"
- O Render Feature está no Renderer **ativo**? (o diagnóstico diz isso)
- A **caixa ao lado do Feature** está marcada?
- No **Unity 6**, o **Render Graph** está habilitado?
- A câmera está usando o Renderer certo (campo *Renderer* da Camera)?

### "[PixelCamera AutoSetup] PixelCameraRenderFeature não encontrado!"
- Corrigido na **1.0.2**: o Auto Setup usava `Object.FindObjectsOfType`, que não enxerga
  ScriptableObjects (o Render Feature é um sub-asset do Renderer Asset)
- Agora ele lê `ScriptableRendererData.rendererFeatures` (API pública da URP 12–17)
- Arraste o seu **Renderer Asset** para o campo **Renderer Asset** do componente —
  ou o próprio Render Feature para o campo **Render Feature (direto)**
- Confirme que o Render Feature foi adicionado ao Renderer e que a caixa ao lado do nome está marcada
- Atalho: **Tools > Pixel Camera > Corrigir Automaticamente (Adicionar Render Feature)**

### "Pixels não alinhados / tremendo"
- Habilite **Snap to Pixel Grid**
- Use resoluções que casem com o aspect ratio (320×180, 640×360 para 16:9)
- Desative **Dynamic Resolution** e upscale filters na câmera
- Em 2D, use câmera **Orthographic**

### "F1–F4 não respondem"
- O `PixelCameraController` usa o **Input Manager legado** (`UnityEngine.Input`)
- **Project Settings > Player > Active Input Handling** deve ser *Input Manager (old)* ou *Both*

### "Performance baixa"
- Reduza a resolução (160×144 é mais leve que 640×360)
- Desabilite efeitos não usados — o **bloom do CRT** é o mais caro (25 amostras por pixel)
- Use Bayer 4x4 em vez de 8x8
- Reduza **Bloom Radius**

### "A UI não fica pixelada"
- Esperado: **Canvas em Screen Space - Overlay** é desenhado depois do pipeline da câmera
- Use **Screen Space - Camera** (na mesma câmera) ou **World Space**

---

## Exemplos de código

```csharp
// Toggle em runtime
pixelCamera.SetCRTEffect(true);
pixelCamera.SetDithering(true);
pixelCamera.SetPixelResolution(320, 180);

// Mudar paleta (use um preset de 16 cores)
pixelCamera.SetPalettePreset(PixelCameraRenderFeature.PalettePreset.PICO8);

// Paleta aleatória
pixelCamera.GenerateRandomPalette(seed: 42);

// Extração de paleta (requer Read/Write Enabled na textura de origem)
var palette = PaletteUtility.ExtractPaletteFromTexture(myTexture, maxColors: 16);
pixelCamera.SetCustomPalette(palette);
```

---

## Links úteis

- **Documentação completa**: [`README.md`](README.md)
- **Limitações conhecidas**: [`README.md` → Limitações](README.md#limitações-conhecidas)
- **Inventário de features**: [`FEATURES.md`](FEATURES.md)
- **Tutorial visual**: [`GUIA-PASSO-A-PASSO.md`](GUIA-PASSO-A-PASSO.md)
- **Palette Editor**: `Tools > Pixel Camera > Palette Editor`
- **Setup rápido**: `Tools > Pixel Camera > Setup na Câmera Atual`
- **Diagnóstico**: `Tools > Pixel Camera > Diagnóstico do Projeto (URP)`

---

## Suporte

1. Rode **Tools > Pixel Camera > Diagnóstico do Projeto (URP)** e leia o Console
2. Consulte as [Limitações conhecidas](README.md#limitações-conhecidas) — pode ser um limite do
   asset, não um erro seu
3. Abra uma **issue** no repositório com a versão do Unity, a versão da URP e o log do diagnóstico
