# 🎮 Pixel Camera URP

Sistema completo de câmera pixel para Unity com Universal Render Pipeline (URP).

## ✨ Features

- **Pixelização** com resolução configurável em runtime
- **Paletas Limitadas** com presets famosos (GameBoy, NES, CGA, PICO-8, etc.)
- **Paleta Custom** via textura ou gerada aleatoriamente
- **Dithering** com múltiplos algoritmos (Bayer 2x2, 4x4, 8x8)
- **Efeito CRT completo** com controles individuais:
  - Scanlines
  - Bloom/Glow
  - Curvatura da tela
  - Vinheta
- **Snap to Pixel Grid** para pixels perfeitamente alinhados
- **Todos os efeitos toggáveis** individualmente
- **Editor customizado** com presets rápidos

## 📦 Instalação

### 1. Copiar arquivos

Copie a pasta `PixelCamera` para o seu projeto Unity:

```
Assets/
└── PixelCamera/
    ├── Runtime/
    │   ├── PixelCameraRenderFeature.cs
    │   ├── PixelCameraRenderPass.cs
    │   ├── PixelCameraController.cs
    │   ├── Shaders/
    │   │   └── PixelCameraShader.shader
    │   └── Utilities/
    │       └── PaletteUtility.cs
    └── Editor/
        └── PixelCameraRenderFeatureEditor.cs
```

### 2. Configurar URP Renderer

1. Abra o **Universal Render Pipeline Asset** (Edit > Project Settings > Graphics)
2. Clique em **Edit** no Renderer
3. Clique em **Add Renderer Feature**
4. Selecione **Pixel Camera Render Feature**

### 3. Configurar Render Feature

No Inspector do Render Feature:

- ✅ **Habilitado**: Liga/desliga todos os efeitos
- 📐 **Pixelização**: Define resolução (ex: 320x180 para visual 8-bit)
- 🎨 **Paleta**: Escolha preset ou paleta custom
- 🔲 **Dithering**: Adiciona textura aos pixels
- 📺 **CRT**: Efeito de monitor antigo

## 🎯 Uso Básico

### Setup Rápido

1. Adicione o **PixelCameraRenderFeature** ao URP Renderer
2. Configure a resolução desejada (ex: 320x180)
3. Escolha um preset de paleta (ex: PICO-8)
4. Pronto!

### Com Controller Runtime

Adicione o componente **PixelCameraController** ao GameObject da câmera:

```csharp
// Adicionar ao GameObject da câmera
var controller = camera.gameObject.AddComponent<PixelCameraController>();
controller.renderFeature = pixelRenderFeature;
controller.enableRuntimeControls = true;
```

**Controles padrão:**
- `F1`: Toggle geral
- `F2`: Toggle CRT
- `F3`: Toggle Dithering
- `F4`: Toggle Paleta
- `Scroll do Mouse`: Ajustar resolução

### Via Script

```csharp
// Ajustar resolução
pixelCamera.SetPixelResolution(640, 360);

// Mudar paleta
pixelCamera.SetPalettePreset(PixelCameraRenderFeature.PalettePreset.GameBoy);

// Paleta custom
pixelCamera.SetCustomPalette(myPaletteTexture);

// Gerar paleta aleatória
pixelCamera.GenerateRandomPalette(seed: 42);

// Toggle efeitos
pixelCamera.SetCRTEffect(true);
pixelCamera.SetDithering(true);
```

## 🎨 Paletas

### Presets Incluídos

- **GameBoy**: 4 tons de verde clássico
- **NES**: 16 cores do Nintendo Entertainment System
- **CGA**: 4 cores do CGA antigo
- **PICO-8**: 16 cores do fantasy console
- **GB Color**: 16 cores do GameBoy Color
- **Grayscale**: 16 tons de cinza
- **Binary**: 2 cores (preto e branco)

### Paleta Custom

#### Via Texture
1. Crie uma textura 16x1 com as cores desejadas
2. Importe no Unity
3. Arraste para o campo **Custom Palette** no Inspector

#### Gerar Aleatória
1. No Inspector, clique em **🎲 Gerar Paleta Aleatória**
2. Ou via script:
```csharp
var palette = PaletteUtility.CreateRandomPalette(16, seed: 42);
```

#### Extrair de Imagem
```csharp
var palette = PaletteUtility.ExtractPaletteFromTexture(sourceTexture, maxColors: 16);
```

## 🔧 Configurações Avançadas

### Pixelização

```csharp
pixelSettings.pixelWidth = 320;        // Largura em pixels
pixelSettings.pixelHeight = 180;       // Altura em pixels
pixelSettings.snapToPixelGrid = true;  // Snap perfeito
```

### Paleta

```csharp
paletteSettings.enablePalette = true;
paletteSettings.preset = PixelCameraRenderFeature.PalettePreset.PICO8;
paletteSettings.customPalette = myTexture;  // Sobrescreve preset
paletteSettings.colorCount = 16;
paletteSettings.colorQuantization = 0.5f;
```

### Dithering

```csharp
ditherSettings.enableDithering = true;
ditherSettings.ditherType = PixelCameraRenderFeature.DitherType.Bayer4x4;
ditherSettings.intensity = 0.5f;
```

**Tipos:**
- `Bayer2x2`: Padrão grande, retrô
- `Bayer4x4`: Equilibrado
- `Bayer8x8`: Padrão fino, suave

### CRT

```csharp
crtSettings.enableCRT = true;

// Scanlines
crtSettings.scanlineIntensity = 0.3f;
crtSettings.scanlineThickness = 2f;

// Bloom/Glow
crtSettings.bloomIntensity = 0.2f;
crtSettings.bloomRadius = 3f;

// Curvatura
crtSettings.curvatureIntensity = 0.1f;

// Vinheta
crtSettings.vignetteIntensity = 0.3f;
```

## 🎮 Presets Rápidos (Editor)

No Inspector do Render Feature:

- **🎮 Retrô Completo**: 320x180, PICO-8, dithering + CRT leve
- **👾 GameBoy**: 160x144, GameBoy palette, dithering forte
- **📺 CRT Clássico**: 640x360, NES, CRT pesado

## 📊 Performance

- **Render Pass único** otimizado para URP
- **Snap to Pixel Grid** garante alinhamento perfeito
- **Texture filtering Point** para pixels nítidos
- Todos os efeitos são **toggleáveis** para melhor performance

### Dicas de Performance

1. Use resoluções baixas (160x144, 320x180) para melhor visual retrô
2. Desabilite efeitos que não está usando
3. Bayer 4x4 é mais performático que 8x8
4. Bloom é o efeito mais pesado - use com moderação

## 🐛 Troubleshooting

### Erros de compilação: CS0234 / CS0118

```
error CS0234: The type or namespace name 'Universal' does not exist in the namespace
              'UnityEngine.Rendering' (are you missing an assembly reference?)
error CS0118: 'Editor' is a namespace but is used like a type
```

**Causa do CS0234:** o projeto **não tem o Universal Render Pipeline instalado**.
Todo o pacote depende da URP (`ScriptableRendererFeature`, `ScriptableRenderPass`, `Blitter`…),
então sem ela as assemblies não compilam.

**Solução:**
1. **Window > Package Manager > Unity Registry**
2. Procure **Universal RP** e clique em **Install**
3. **Edit > Project Settings > Graphics > Scriptable Render Pipeline Settings** → atribua um
   *UniversalRenderPipelineAsset* (crie em *Assets > Create > Rendering > URP Asset (with Universal Renderer)*)
4. Faça o mesmo em **Project Settings > Quality > Render Pipeline Asset**
5. Aguarde a recompilação — o Console deve ficar limpo

> 💡 Existe um diagnóstico automático: **Tools > Pixel Camera > Diagnóstico do Projeto (URP)**.
> Ele também roda sozinho depois de cada compilação e diz no Console exatamente o que falta
> (URP ausente, pipeline não atribuído ou Render Feature não adicionado).

**Causa do CS0118:** colisão de nomes. Dentro do namespace `PixelCamera.Editor`, o identificador
simples `Editor` resolve para o **namespace** `PixelCamera.Editor` (membros do namespace "pai" têm
prioridade sobre os tipos importados pelo `using UnityEditor;`), e não para a classe
`UnityEditor.Editor`. **Já corrigido no pacote:** as classes de inspector herdam de
`UnityEditor.Editor` (totalmente qualificado). Se você escrever novos editores dentro de
`PixelCamera.Editor`, faça o mesmo — ou use outro nome de namespace.

### Shader não encontrado
```
[PixelCamera] Shader 'Hidden/PixelCamera' não encontrado!
```
**Solução:** Verifique se o arquivo `PixelCameraShader.shader` está em `Assets/PixelCamera/Runtime/Shaders/`

### Render Feature não aparece
**Solução:** 
1. Verifique se está usando URP (não Built-in ou HDRP)
2. Recompile shaders: Assets > Reimport All

### Pixels não alinhados
**Solução:**
1. Habilite **Snap to Pixel Grid**
2. Use resoluções com aspect ratio 16:9 (320x180, 640x360)
3. Configure a câmera para orthographic se necessário

## 📝 Licença

MIT License - Use livremente em projetos pessoais e comerciais.

## 🤝 Contribuindo

Sugestões e melhorias são bem-vindas! Abra uma issue ou pull request.

## 📞 Suporte

Para dúvidas ou problemas, abra uma issue no repositório.
