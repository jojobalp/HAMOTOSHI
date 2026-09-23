# 🎯 GUIA VISUAL PASSO A PASSO - Pixel Camera na Unity

## 📋 Pré-requisitos

| Requisito | Valor |
| --- | --- |
| Unity | **2021.3 LTS** ou superior (funciona até Unity 6.x) |
| Render Pipeline | **Universal Render Pipeline (URP) 12+** — **obrigatório** |
| Shader Model | 4.5+ |
| Active Input Handling | **Input Manager (old)** ou **Both** (para os atalhos F1–F4 do Controller) |
| Render Graph (Unity 6) | Habilitado em *Project Settings > Graphics > URP* |

### ⛔ Este asset NÃO funciona em Built-in nem HDRP

O Pixel Camera é **exclusivo da URP**. Antes de seguir o tutorial, confira em
**Edit > Project Settings > Graphics > Scriptable Render Pipeline Settings**:

| O que está no campo | Pipeline | Funciona? |
| --- | --- | --- |
| *UniversalRenderPipelineAsset* | URP | ✅ Sim — siga o tutorial |
| **Vazio (None)** | Built-in | ❌ Não — erros de compilação `CS0234` |
| *HDRenderPipelineAsset* | HDRP | ❌ Não — o efeito nunca é aplicado |

Motivos técnicos completos no [README raiz](../README.md#por-que-não-built-in--hdrp).

> 💡 Na dúvida, rode **Tools > Pixel Camera > Diagnóstico do Projeto (URP)** — ele diz no Console
> qual pipeline o projeto está usando e o que falta.

---

## 🚀 PASSO 1: Abrir o Projeto na Unity

### 1.1 - Abrir a Unity Hub
1. Abra a **Unity Hub**
2. Clique em **"Open"** (Abrir)
3. Navegue até a pasta do seu projeto (onde está a pasta `Assets/`)
4. Selecione a pasta e clique em **"Open Project"**
5. Aguarde a Unity compilar todos os scripts (pode levar 1-2 minutos na primeira vez)

### 1.2 - Verificar se o URP está configurado
Se seu projeto **NÃO** usa URP ainda, veja o **Passo 1 Extra** abaixo.

---

## 🔧 PASSO 1 EXTRA: Configurar URP (se necessário)

> ⚠️ **PULE ESTE PASSO** se seu projeto já usa URP!

### 1E.1 - Instalar o URP
1. Na Unity, vá em **Window > Package Manager**
2. No canto superior esquerdo, mude de **"Unity Registry"** para **"In Project"**
3. Se não aparecer o **Universal RP**, clique no **"+"** no canto superior esquerdo
4. Selecione **"Add package by name"**
5. Digite: `com.unity.render-pipelines.universal`
6. Clique em **"Add"**
7. Aguarde instalar

### 1E.2 - Configurar o Pipeline
1. Vá em **Edit > Project Settings > Graphics**
2. Role até **"Scriptable Render Pipeline Settings"**
3. Clique no campo e selecione um **UniversalRenderPipelineAsset**
   - Se não existir, clique em **"Create"** > **"Universal Render Pipeline Asset > Pipeline Asset (Forward)"**
   - Salve em `Assets/Settings/` com o nome `UniversalRP`
4. Arraste o asset criado para o campo **Scriptable Render Pipeline Settings**

### 1E.3 - Atualizar Quality Settings
1. Ainda em **Project Settings**, vá em **Quality**
2. Para cada nível de qualidade (Low, Medium, High, Ultra), role até **"Render Pipeline Asset"**
3. Selecione o mesmo **UniversalRenderPipelineAsset** que configurou acima

### 1E.4 - Verificar se funcionou
1. Crie um **Cube** (GameObject > 3D Object > Cube)
2. O material deve aparecer diferente (mais "plano", sem shaders legacy)
3. Se aparecer erro rosa, volte e revise os passos acima

---

## 📂 PASSO 2: Copiar os Arquivos do Pixel Camera

### 2.1 - Localizar a pasta PixelCamera
A pasta `PixelCamera/` deve estar na **raiz do seu projeto**, ao lado de `Assets/`

```
SeuProjeto/
├── Assets/
├── Packages/
├── PixelCamera/          ← ESTA PASTA
│   ├── Runtime/
│   ├── Editor/
│   └── ...
└── ...
```

### 2.2 - Copiar para dentro de Assets
1. Abra o **File Explorer** do seu sistema operacional
2. Navegue até a raiz do projeto
3. **Recorte** a pasta `PixelCamera/` completa (Ctrl+X)
4. Entre na pasta `Assets/`
5. **Cole** a pasta (Ctrl+V)
6. Volte para a Unity
7. **Aguarde** a compilação (barra azul no canto inferior)

### 2.3 - Verificar se compilou
Após compilar, você deve ver no **Project Window**:

```
Assets/
└── PixelCamera/
    ├── Runtime/
    │   ├── PixelCameraRenderFeature.cs
    │   ├── PixelCameraRenderPass.cs
    │   ├── PixelCameraController.cs
    │   ├── PalettePresetAsset.cs
    │   ├── Shaders/
    │   │   └── PixelCameraShader.shader
    │   └── Utilities/
    │       └── PaletteUtility.cs
    ├── Editor/
    │   ├── PixelCameraRenderFeatureEditor.cs
    │   ├── PaletteEditorWindow.cs
    │   ├── PalettePresetAssetEditor.cs
    │   └── PixelCameraSetupUtility.cs
    └── ...
```

> ⚠️ Se aparecerem **erros vermelhos** no Console, veja a seção **Solução de Problemas** no final.

---

## 🎨 PASSO 3: Criar o URP Renderer Asset

### 3.1 - Criar o Renderer
1. No **Project Window**, clique em **Assets** (raiz)
2. Clique com **botão direito** > **Create > Rendering > URP Renderer (Forward)**
3. Nomeie como: `PixelCameraRenderer`
4. Selecione o arquivo criado

### 3.2 - Configurar o Renderer
Com o `PixelCameraRenderer` selecionado, no **Inspector**:

1. Procure a seção **"Post-processing"**
2. Marque a caixa **"Post Processing"** ✅

### 3.3 - Vincular ao Pipeline Asset
1. Vá em **Edit > Project Settings > Graphics**
2. No campo **Scriptable Render Pipeline Settings**, selecione seu **UniversalRenderPipelineAsset**
3. No Inspector do Pipeline Asset, procure **"Renderer List"**
4. Clique no **"+"** para adicionar um novo renderer
5. Arraste o `PixelCameraRenderer` para o slot
6. Ou clique no círculo e selecione `PixelCameraRenderer`

---

## ⚡ PASSO 4: Adicionar o Pixel Camera Render Feature

> ⭐ **ESTE É O PASSO MAIS IMPORTANTE!**

### 4.1 - Selecionar o Renderer
1. No **Project Window**, selecione o `PixelCameraRenderer` que você criou
2. O **Inspector** vai mostrar as configurações dele

### 4.2 - Adicionar o Render Feature
1. No Inspector do Renderer, role até **"Renderer Features"**
2. Clique no botão **"Add Renderer Feature"**
3. Uma janela vai abrir listando todos os features disponíveis
4. Procure e clique em **"Pixel Camera Render Feature"**
5. O feature vai aparecer na lista

### 4.3 - Configurar o Render Feature
Clique na seta **▶** ao lado de "Pixel Camera Render Feature" para expandir. Você verá:

```
🎮 Pixel Camera
━━━━━━━━━━━━━━━━━━━━━━
☑ Habilitado

▼ 📐 Pixelização
   Largura (pixels): 320
   Altura (pixels): 180
   ☑ Snap to Pixel Grid

▼ 🎨 Paleta de Cores
   ☑ Habilitar Paleta
   Preset: PICO-8
   Quantidade de Cores: 16
   Quantização: 0.5

▼ 🔲 Dithering
   ☐ Habilitar Dithering

▼ 📺 Efeito CRT
   ☐ Habilitar CRT

Render Pass
Evento: BeforeRenderingPostProcessing

Presets Rápidos
[🎮 Retrô Completo] [👾 GameBoy] [📺 CRT Clássico]
```

### 4.4 - Configuração Inicial Recomendada
Configure assim para começar:

| Campo | Valor |
|-------|-------|
| **Habilitado** | ✅ Marcado |
| **Largura** | `320` |
| **Altura** | `180` |
| **Snap to Grid** | ✅ Marcado |
| **Habilitar Paleta** | ✅ Marcado |
| **Preset** | `PICO-8` |
| **Habilitar Dithering** | ❌ Desmarcado |
| **Habilitar CRT** | ❌ Desmarcado |

---

## 📷 PASSO 5: Configurar a Câmera

### 5.1 - Selecionar a Main Camera
1. Na **Hierarchy**, clique em **"Main Camera"**
2. No **Inspector**, verifique:

### 5.2 - Configurar a Câmera
No Inspector da Camera:

| Campo | Valor |
|-------|-------|
| **Projection** | `Orthographic` (para pixel art) ou `Perspective` (para 3D) |
| **Size** | `5` (orthographic) |
| **Clear Flags** | `Solid Color` |
| **Background** | Cor escura (ex: preto ou azul escuro) |
| **Culling Mask** | `Everything` |
| **Render Mode** | `Base` (se usar múltiplas câmeras) |

### 5.3 - (Opcional) Adicionar o Controller
1. Com a Main Camera selecionada
2. Clique em **"Add Component"**
3. Digite: `PixelCameraController`
4. Selecione o componente
5. Arraste o `PixelCameraRenderer` (que tem o feature) para o campo **"Render Feature"**

> 💡 O controller permite usar atalhos de teclado (F1-F4) e scroll para ajustar em runtime.

---

## 🎬 PASSO 6: Criar Cena de Teste

### 6.1 - Criar Objetos de Teste
Para ver o efeito funcionando, precisamos de algo na tela:

#### Opção A: Cena 3D Simples
1. **GameObject > 3D Object > Cube**
2. Posicione em `(0, 0, 0)`
3. Rotação: `(30, 45, 0)`
4. Crie um material colorido:
   - **Assets** > botão direito > **Create > Material**
   - Nome: `TestMaterial`
   - **Base Map**: cor vermelha ou azul
   - Arraste o material no Cube

#### Opção B: Sprite 2D
1. **GameObject > 2D Object > Sprite > Square**
2. Posicione em `(0, 0, 0)`
3. Crie um material colorido e aplique

#### Opção C: Skybox Colorido
1. **Window > Rendering > Lighting**
2. **Environment** > **Environment Lighting Source**: `Gradient`
3. Configure as cores do gradiente (ex: azul no topo, laranja embaixo)

### 6.2 - Adicionar Direção de Luz
1. **GameObject > Light > Directional Light**
2. Rotação: `(50, -30, 0)`
3. Intensidade: `1.0`

---

## ▶️ PASSO 7: Testar!

### 7.1 - Dar Play
1. Clique no botão **▶ Play** no topo da Unity
2. **O efeito pixelado deve aparecer!**

### 7.2 - Verificar se está funcionando
Você deve ver:
- ✅ A cena com **pixels visíveis** (quadrados grandes)
- ✅ As cores **limitadas à paleta** selecionada
- ✅ Tudo com aspecto **"retrô/8-bit"**

### 7.3 - Testar os Controles (se adicionou o Controller)
- **F1** - Liga/desliga tudo
- **F2** - Liga/desliga CRT
- **F3** - Liga/desliga Dithering
- **F4** - Liga/desliga Paleta
- **Scroll do Mouse** - Aumenta/diminui resolução

### 7.4 - Ajustar em Tempo Real
Com o jogo rodando (Play mode):
1. Selecione o `PixelCameraRenderer` no Project Window
2. Mude os valores no Inspector
3. Veja as mudanças **em tempo real** na Game View!

---

## 🎨 PASSO 8: Testar os Presets

### 8.1 - Preset GameBoy
No Inspector do Render Feature:
1. Largura: `160`
2. Altura: `144`
3. Preset: `GameBoy`
4. Habilitar Dithering: ✅
5. Tipo Dithering: `Bayer2x2`
6. Intensidade: `0.5`

### 8.2 - Preset Retrô
1. Clique no botão **"🎮 Retrô Completo"** no Inspector
2. Ou configure manualmente:
   - Largura: `320`, Altura: `180`
   - Preset: `PICO-8`
   - Dithering: ✅ (Bayer4x4, 0.3)
   - CRT: ✅ (Scanlines 0.2, Bloom 0.1)

### 8.3 - Preset CRT Clássico
1. Clique no botão **"📺 CRT Clássico"** no Inspector
2. Ou configure:
   - Largura: `640`, Altura: `360`
   - Preset: `NES`
   - CRT: ✅ (Scanlines 0.4, Bloom 0.3, Curvature 0.15, Vignette 0.4)

---

## 🎨 PASSO 9: Usar o Palette Editor

### 9.0 - O caminho mais rápido: paleta a partir de uma imagem
1. Menu: **Tools > Pixel Camera > Criar Paleta a partir de Imagem**
2. Escolha um **PNG ou JPEG** (pode ser baixado do Google — não precisa estar no projeto)
3. O Unity cria o asset **16×1** em `Assets/PixelCameraPalettes/`, já com `Point`, `Clamp`,
   sem mipmaps, sem compressão e `Read/Write Enabled`
4. Arraste para o campo **Paleta Custom** do Render Feature

> Usando o botão **🖼️ Extrair Paleta de Imagem** direto no Inspector do Render Feature, o passo 4
> é automático — o campo já é preenchido e *Habilitar Paleta* é ligado.

> **O que ele faz com a imagem:** se for uma faixa de paleta (altura 1, até 16 px de largura), as
> cores são usadas como estão. Qualquer outra imagem tem as **16 cores mais frequentes** extraídas.
> Foto gera paleta "média"; para resultado retrô fiel use **pixel art, ilustração flat ou sprite
> sheet**, de preferência em **PNG**.

### 9.1 - Abrir o Editor
1. Menu: **Tools > Pixel Camera > Palette Editor**
2. Uma janela vai abrir

### 9.2 - Criar uma Paleta
1. Clique nas cores do grid para selecionar
2. Use o color picker para mudar cada cor
3. Ou use os botões rápidos:
   - **🎲 Aleatória** - Gera cores aleatórias
   - **🌈 Ferramenta de Gradiente** - Cria gradiente entre 2 cores
   - **🔥 Quente** - Tons de vermelho/laranja
   - **🧊 Frio** - Tons de azul
   - **🌿 Natureza** - Tons de verde
   - **🌅 Pôr-do-sol** - Tons quentes
   - **🖼️ Extrair de Imagem** - As 16 cores mais frequentes de um PNG/JPEG

### 9.3 - Exportar a Paleta
1. Clique em **"💾 Exportar PNG"** (o arquivo já sai 16×1)
2. Escolha onde salvar
3. Importe o PNG na Unity
4. Arraste para o campo **Custom Palette** no Render Feature

> Prefere pular os passos 3 e 4? Use **9.0** — ele grava o asset dentro do projeto e já configura a
> importação para você.

---

## ⚠️ SOLUÇÃO DE PROBLEMAS

### ❌ Erros de compilação "CS0234 / CS0118"
**Problema:**
```
Assets\PixelCamera\Editor\PixelCameraSetupUtility.cs(3,29): error CS0234: The type or namespace
name 'Universal' does not exist in the namespace 'UnityEngine.Rendering'
Assets\PixelCamera\Editor\PixelCameraRenderFeatureEditor.cs(10,51): error CS0118: 'Editor' is a
namespace but is used like a type
```

**Causa:** o projeto **não tem o URP instalado** (o pacote inteiro depende dele). O CS0118 é uma
colisão de nomes entre o namespace `PixelCamera.Editor` e a classe `UnityEditor.Editor` — já está
corrigida no código atual (a herança usa `UnityEditor.Editor` qualificado).

**Solução:**
1. **Window > Package Manager**
2. Aba **Unity Registry** → procure **Universal RP** → **Install**
3. Aguarde terminar a importação/recompilação (o Console deve limpar)
4. **Edit > Project Settings > Graphics > Scriptable Render Pipeline Settings** → crie/atribua um
   **UniversalRenderPipelineAsset** (*Assets > Create > Rendering > URP Asset (with Universal Renderer)*)
5. **Edit > Project Settings > Quality > Render Pipeline Asset** → atribua o mesmo asset
6. Rode **Tools > Pixel Camera > Diagnóstico do Projeto (URP)** — ele mostra ✔ / ⚠ / ✖ do que falta

> Se mesmo com o URP instalado continuar o erro CS0234, apague as pastas `Library/` e `Temp/`
> do projeto (com a Unity fechada) e reabra para forçar a reimportação.

### ❌ "Shader não encontrado"
**Problema:** `[PixelCamera] Shader 'Hidden/PixelCamera' não encontrado!`

**Solução:**
1. Verifique se `PixelCameraShader.shader` está em `Assets/PixelCamera/Runtime/Shaders/`
2. Abra o arquivo do shader
3. Verifique se tem erros (vermelho no topo)
4. Tente: **Assets > Reimport All**
5. Reinicie a Unity

### ❌ "Render Feature não aparece na lista"
**Problema:** Não aparece "Pixel Camera Render Feature" ao clicar em "Add Renderer Feature"

**Solução:**
1. Confirme que o projeto usa **URP** (Built-in e HDRP não são suportados — veja os pré-requisitos)
2. Verifique se os scripts compilaram sem erros (Console limpo) — com erro de compilação o
   Render Feature **não** aparece na lista
3. Verifique se `PixelCameraRenderFeature.cs` tem a herança `ScriptableRendererFeature`
4. Tente: **Assets > Reimport All**
5. Feche e abra a Unity

### ❌ "Tela rosa/magenta"
**Problema:** Tudo aparece rosa na Game View

**Solução:**
1. Shader com erro. Verifique o Console
2. Abra `PixelCameraShader.shader` e procure erros
3. **Causa mais comum:** o projeto **não está em URP**. O shader tem
   `Tags { "RenderPipeline" = "UniversalPipeline" }` + `Fallback Off`, então no Built-in e no HDRP
   ele é descartado e a tela fica magenta
4. **Edit > Project Settings > Graphics** > Pipeline Asset configurado?

### ❌ "Não vejo nenhum efeito"
**Problema:** A cena aparece normal, sem pixelização

**Solução:**
1. Verifique se o Render Feature está **habilitado** ✅
2. **Marque a caixa ao lado do nome do Feature** no Renderer — se estiver desmarcada, a URP nem
   chama `AddRenderPasses`
3. Verifique se o Feature está no Renderer que a pipeline **usa de fato** (o diagnóstico
   diferencia "Feature existe" de "Feature no Renderer ativo")
4. Verifique se a Camera está usando o Renderer correto (campo **"Renderer"** da Camera)
5. Verifique se o **Render Pass Event** está em `BeforeRenderingPostProcessing`
6. No **Unity 6**: o **Render Graph** está habilitado em *Project Settings > Graphics > URP*?
7. Atalho: **Tools > Pixel Camera > Corrigir Automaticamente (Adicionar Render Feature)**

### ❌ "F1–F4 e o scroll não funcionam"
**Problema:** Os atalhos do `PixelCameraController` não respondem

**Causa:** o Controller usa o **Input Manager legado** (`UnityEngine.Input`).

**Solução:**
1. **Edit > Project Settings > Player > Active Input Handling**
2. Mude para **Input Manager (old)** ou **Both**
3. A Unity vai pedir para reiniciar — aceite
4. Alternativa: dispense o Controller e chame a API (`SetPixelResolution`, `SetPalettePreset`, …)
   a partir do seu próprio sistema de input

### ❌ "A imagem ficou quase toda preta / sem os tons do preset"
**Problema:** Ao usar os presets **GameBoy**, **CGA** ou **Binary**, a imagem perde os tons médios
e escuros (no Binary, quase tudo vira preto)

**Causa:** bug das versões **até 1.0.3**. `GetPresetPalette` criava uma textura 16×1 preenchendo só
os slots reais do preset (4, 4 e 2); os restantes ficavam `(0,0,0,0)` = preto, e o `Frag` usava
`int paletteSize = 16;` fixo — então o preto fantasma vencia a busca para qualquer cor escura/média.

**Corrigido na 1.1.0:** o C# envia o número real de cores da paleta (`_PaletteSize`) e o shader
compara apenas contra as cores que existem. Os 7 presets agora entregam o visual correto.

**Se você ainda vê o problema:**
1. Confirme que o pacote está na **1.1.0** (`PixelCamera/package.json` → `"version": "1.1.0"`)
2. **Assets > Reimport All** — o shader precisa ser recompilado
3. Se estiver usando uma **paleta custom** importada antes da correção, regenere-a: ela pode ter
   menos de 16 pixels de largura ou slots em preto

Detalhes em [README → Corrigido na 1.1.0](README.md#corrigido-na-110).

### ❌ "A interface (UI) não fica pixelada"
**Comportamento esperado:** **Canvas em Screen Space - Overlay** é desenhado depois do pipeline da
câmara, então não passa pelo efeito. Use **Screen Space - Camera** (apontando para a mesma câmera)
ou **World Space**.

### ❌ "Floyd-Steinberg sumiu do dropdown de dithering"
**Esperado a partir da 1.1.0.** O valor existia no enum, mas o shader nunca o tratou — selecioná-lo
deixava o dithering **silenciosamente desligado**. Ele foi removido para não enganar ninguém.

Se o seu Render Feature tinha Floyd-Steinberg selecionado, o Inspector mostra um aviso amarelo e
normaliza o valor para **Bayer 4×4**. Difusão de erro real está no roadmap 1.3.0 (exige múltiplos
passes sequenciais).
5. Se estiver usando o **Pixel Camera Auto Setup**: arraste o Renderer Asset para o campo
   **"Renderer Asset"** do componente. Até a versão 1.0.1 ele buscava o Render Feature com
   `Object.FindObjectsOfType`, que não encontra ScriptableObjects (o Render Feature é um
   sub-asset do Renderer Asset) e sempre falhava com
   *"[PixelCamera AutoSetup] PixelCameraRenderFeature não encontrado!"* — corrigido na 1.0.2,
   que lê `ScriptableRendererData.rendererFeatures`

### ❌ "Pixels não são quadrados perfeitos"
**Problema:** Pixels aparecem distorcidos/retangulares

**Solução:**
1. Marque **"Snap to Pixel Grid"** ✅
2. Use resolução com aspect ratio **16:9** (320x180, 640x360)
3. Na Camera, use **Orthographic** mode
4. Ajuste o tamanho da camera para múltiplo da resolução

### ❌ "Performance muito baixa"
**Problema:** FPS caindo muito

**Solução:**
1. Reduza a resolução pixelada (use 160x144 ou 320x180)
2. Desabilite efeitos que não usa
3. Use **Bayer4x4** ao invés de **Bayer8x8**
4. Reduza **Bloom Radius** (é o mais pesado)

### ❌ "Paleta não funciona"
**Problema:** As cores não mudam para as da paleta

**Solução:**
1. Marque **"Habilitar Paleta"** ✅
2. Verifique se o **Preset** está correto
3. Se usar Custom Palette, verifique se a textura foi atribuída
4. A textura deve ter as cores na **primeira linha** (linha y=0) e **exatamente 16×1**

---

### ❌ "Não foi possível carregar a textura" ao importar uma imagem
**Problema:** você clica em importar paleta, escolhe um PNG/JPEG válido e recebe erro.

**Causa:** bug das versões **até 1.1.0**. O botão passava o caminho **absoluto** do disco
(`C:\Users\...\foto.png`) para `AssetDatabase.LoadAssetAtPath`, que só aceita caminho relativo ao
projeto (`Assets/...`) — devolvia sempre `null`, para qualquer arquivo.

**Solução:** atualize para a **1.1.1**. O botão agora decodifica a imagem do disco e grava um PNG
16×1 em `Assets/PixelCameraPalettes/`, já configurado e atribuído ao campo.

---

### ⚠️ "warning CS0618: FindFirstObjectByType is obsolete"
**Problema:** aviso no Console apontando `PixelCameraSetupUtility.cs`.

**Causa:** `FindFirstObjectByType` ficou obsoleto no Unity 6 por depender da ordenação de
*instance ID*.

**Importante:** é **apenas um aviso** — não impede compilação nem tem relação com falha de
importação de paleta. Corrigido na **1.1.1** (troca por `FindAnyObjectByType`).

---

### ❌ "Formato não suportado" ao importar paleta
**Problema:** o arquivo escolhido não decodifica.

**Causa:** `Texture2D.LoadImage` decodifica **apenas PNG e JPEG**. BMP, TGA, PSD, WebP e os formatos
clássicos de paleta (`.pal`, `.gpl`, `.act`, `.hex`, JSON, CSV) **não** são suportados.

**Solução:** converta para PNG (ou JPEG). Até a 1.0.3 o filtro do dialog aceitava BMP e a falha era
silenciosa — você recebia uma textura vazia sem nenhum erro.

---

## 📱 Checklist Final

Antes de dizer "não funciona", verifique:

- [ ] Unity 2021.3+ instalada
- [ ] URP configurado no Project Settings > Graphics
- [ ] Pasta `PixelCamera` dentro de `Assets/`
- [ ] Scripts compilaram sem erros (Console limpo)
- [ ] `PixelCameraRenderer` criado
- [ ] Render Feature adicionado ao Renderer
- [ ] Render Feature está **habilitado** ✅
- [ ] Câmera usando o Renderer correto
- [ ] Resolução configurada (ex: 320x180)
- [ ] Objeto na cena para ver o efeito
- [ ] Deu Play e viu o resultado

Se **tudo** acima está correto e não funciona, me mande:
1. Screenshot do Inspector do Render Feature
2. Screenshot do Console (com erros, se houver)
3. Screenshot da Game View

---

## 🎯 Resumo Visual

```
┌─────────────────────────────────────────────┐
│              FLUXO DE CONFIGURAÇÃO           │
├─────────────────────────────────────────────┤
│                                             │
│  1. URP Pipeline Asset ──────────────┐      │
│       │                              │      │
│       ▼                              │      │
│  2. URP Renderer ──────────┐         │      │
│       │                    │         │      │
│       ▼                    ▼         │      │
│  3. Render Feature    Quality       │      │
│     (Pixel Camera)     Settings     │      │
│       │                              │      │
│       ▼                              │      │
│  4. Main Camera ─────────────────────┘      │
│     (usa o Renderer)                        │
│                                             │
│  5. Adicionar objetos na cena               │
│                                             │
│  6. Play ▶ → Veja o efeito!                 │
│                                             │
└─────────────────────────────────────────────┘
```
