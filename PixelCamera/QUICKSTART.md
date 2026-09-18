# 🚀 Quick Start Guide

## Setup em 5 minutos

### Passo 0: Pré-requisito (URP)
O pacote **exige** o Universal Render Pipeline. Sem ele você recebe erros como
`CS0234: The type or namespace name 'Universal' does not exist in the namespace 'UnityEngine.Rendering'`.

1. **Window > Package Manager > Unity Registry** → instale **Universal RP**
2. **Edit > Project Settings > Graphics** e **Quality** → atribua um *UniversalRenderPipelineAsset*

### Passo 1: Instalar
1. Copie a pasta `PixelCamera` para `Assets/` do seu projeto Unity
2. Aguarde a Unity compilar os scripts
3. Se aparecer algum erro no Console, rode **Tools > Pixel Camera > Diagnóstico do Projeto (URP)**

### Passo 2: Configurar URP Renderer
1. Vá em **Edit > Project Settings > Graphics**
2. Abra o **Universal Render Pipeline Asset**
3. Clique em **Edit** no campo **Renderer**
4. Clique em **Add Renderer Feature**
5. Selecione **Pixel Camera Render Feature**

### Passo 3: Configurar Efeitos
No Inspector do Renderer, configure o **Pixel Camera Render Feature**:

```
✅ Enabled: true

📐 Pixelização:
   - Largura: 320
   - Altura: 180
   - Snap to Grid: true

🎨 Paleta:
   - Enabled: true
   - Preset: PICO-8

🔲 Dithering:
   - Enabled: false (opcional)

📺 CRT:
   - Enabled: false (opcional)
```

### Passo 4: Adicionar Controller (Opcional)
1. Selecione a **Main Camera**
2. Adicione o componente **PixelCameraController**
3. Arraste o **Render Feature** para o campo

### Passo 5: Testar
- Pressione **Play**
- Use **F1-F4** para toggle efeitos
- Use **Scroll** para ajustar resolução

---

## Presets Recomendados

### 🎮 GameBoy Clássico
```
Resolução: 160x144
Paleta: GameBoy
Dithering: Bayer2x2 (0.5)
CRT: Off
```

### 👾 NES/Retro
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

---

## Paletas Custom

### Método 1: Editor Window
1. **Tools > Pixel Camera > Palette Editor**
2. Crie ou importe uma paleta
3. Clique em **Exportar PNG**
4. Importe o PNG no Unity
5. Arraste para o campo **Custom Palette**

### Método 2: ScriptableObject
1. **Assets > Create > Pixel Camera > Palette Preset**
2. Configure as cores
3. Use `preset.ToTexture()` para converter

### Método 3: Código
```csharp
var palette = PaletteUtility.CreateRandomPalette(16, seed: 42);
pixelCamera.SetCustomPalette(palette);
```

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
- Verifique se `PixelCameraShader.shader` está em `Assets/PixelCamera/Runtime/Shaders/`
- Recompile: **Assets > Reimport All**

### "Pixels não alinhados"
- Habilite **Snap to Pixel Grid**
- Use aspect ratio 16:9 (320x180, 640x360)
- Configure câmera para **Orthographic** se necessário

### "Performance baixa"
- Reduza resolução (160x144 é mais leve que 640x360)
- Desabilite efeitos não usados
- Use Bayer 4x4 ao invés de 8x8
- Reduza **Bloom Radius**

### "Render Feature não aparece"
- Confirme que está usando **URP** (não Built-in ou HDRP)
- Verifique versão do Unity (2021.3+)
- Reinicie a Unity

---

## Exemplos de Código

### Toggle em Runtime
```csharp
pixelCamera.SetCRTEffect(true);
pixelCamera.SetDithering(true);
pixelCamera.SetPixelResolution(320, 180);
```

### Mudar Paleta
```csharp
pixelCamera.SetPalettePreset(PixelCameraRenderFeature.PalettePreset.GameBoy);
```

### Gerar Paleta Aleatória
```csharp
pixelCamera.GenerateRandomPalette(seed: 42);
```

### Extrair Paleta de Imagem
```csharp
var palette = PaletteUtility.ExtractPaletteFromTexture(myTexture, maxColors: 16);
pixelCamera.SetCustomPalette(palette);
```

---

## Links Úteis

- **Documentação completa**: `PixelCamera/README.md`
- **Palette Editor**: `Tools > Pixel Camera > Palette Editor`
- **Setup rápido**: `Tools > Pixel Camera > Setup na Câmera Atual`

---

## Suporte

Para problemas ou dúvidas:
1. Verifique o **README.md** completo
2. Consulte os **Samples** na pasta `PixelCamera/Samples~/`
3. Abra uma issue no repositório
