# 📋 CHANGELOG - Pixel Camera

## [1.1.1] - 2026-09-23

Versão de **correção da importação de paleta** + o fluxo "escolher uma imagem e virar preset".
Nenhum breaking change na API pública; `PalettePresetAsset.FromTexture` mudou de comportamento
(para o que a documentação já descrevia).

### ✨ Novidades

- ✅ **Extração de paleta a partir de qualquer imagem, com 1 clique.** `ExtractPaletteFromTexture`
  existia desde a 1.1.0 como API de runtime, mas **nenhum botão no editor a usava** — o único jeito
  era escrever código, e ainda exigia uma textura já importada com *Read/Write Enabled*. Agora há
  três entradas para o mesmo fluxo:
  - **Tools > Pixel Camera > Criar Paleta a partir de Imagem**
  - **🖼️ Extrair Paleta de Imagem** no Inspector do Render Feature (já atribui ao campo
    *Paleta Custom* e liga *Habilitar Paleta*)
  - **🖼️ Extrair de Imagem** no Palette Editor e no Inspector do `PalettePresetAsset`
- ✅ **`PaletteEditorUtility`** (novo, `PixelCamera/Editor`): fluxo compartilhado de
  "arquivo de imagem → paleta utilizável". Decodifica a imagem **direto do disco** numa textura
  temporária em memória (sempre legível), então **não depende dos settings de importação do arquivo
  de origem**. Grava o resultado como PNG **16×1** em `Assets/PixelCameraPalettes/` e configura o
  importer automaticamente (`Point`, `Clamp`, sem mipmaps, sem compressão, `Read/Write Enabled`,
  `sRGB`, `npotScale = None`).
- ✅ **Detecção automática de faixa de paleta.** Se a imagem tem altura 1 e largura até 16, as cores
  são usadas **como estão** (slots restantes preenchidos por repetição). Qualquer outra imagem
  (foto, sprite, ilustração) passa pela extração das 16 cores mais frequentes. O dialog informa qual
  dos dois caminhos foi usado.

### 🐛 Correções

- ✅ **"Importar Paleta de Texture" do Render Feature falhava sempre** (o erro relatado ao enviar
  uma imagem). `EditorUtility.OpenFilePanel` devolve caminho **absoluto** de disco
  (`C:\Users\...\foto.png`), e o código o passava direto para `AssetDatabase.LoadAssetAtPath`, que
  **só aceita caminho relativo ao projeto** (`Assets/...`). O retorno era sempre `null`, então qualquer
  arquivo válido produzia *"Não foi possível carregar a textura. Verifique se o arquivo é válido."*
  Agora a imagem é decodificada do disco e gravada como asset 16×1 dentro do projeto.
  O filtro também era `"png,jpg"` (sem `jpeg`), inconsistente com os outros dois dialogs — unificado
  em `png,jpg,jpeg`.
- ✅ **`PaletteEditorWindow.ImportPalette` não reamostrava.** Fazia
  `paletteSize = Clamp(texture.width, 2, 16)` e lia apenas a linha 0: importar qualquer imagem que
  não fosse uma faixa 16×1 (ex.: 256×256) devolvia os primeiros 16 pixels do topo, não a paleta.
  `PaletteUtility.LoadPaletteFromPNG` reamostrava corretamente, mas a janela duplicava a lógica em
  vez de usá-la. Agora as duas passam pelo mesmo fluxo.
- ✅ **`PalettePresetAsset.FromTexture` ainda truncava o array.** A correção da 1.1.0 (preencher os
  16 slots por repetição) tinha sido aplicada só ao `PalettePresetAssetEditor.ImportFromTexture`; a
  API pública de runtime continuava fazendo `colors = new Color[source.width]`, deixando slots
  vazios que corrompem a busca de cor mais próxima no shader. Agora devolve sempre 16 slots.
- ✅ **CS0618 no Unity 6:** `Object.FindFirstObjectByType<Camera>()` (obsoleto por depender da
  ordenação de *instance ID*) substituído por `Object.FindAnyObjectByType<Camera>()` em
  `PixelCameraSetupUtility`. Ambos existem desde o Unity 2022.2, então a guarda
  `UNITY_2022_2_OR_NEWER` continua válida. Era apenas um aviso — **não** era a causa da falha de
  importação.

### ⚠️ Notas de atualização (1.1.0 → 1.1.1)

- **`PalettePresetAsset.FromTexture` mudou de comportamento**: o array `colors` passa a ter sempre
  **16 elementos** (antes tinha a largura da textura de origem). Código que dependia do tamanho
  antigo precisa se ajustar.
- Paletas importadas/extraídas são gravadas em **`Assets/PixelCameraPalettes/`**. A pasta é criada
  automaticamente; pode ser movida ou renomeada depois (o asset continua válido).
- **Formatos de arquivo continuam PNG e JPEG** — `Texture2D.LoadImage` não decodifica BMP, e não há
  leitor para `.pal`, `.gpl`, `.act`, `.hex`, JSON ou CSV.

---

## [1.1.0] - 2026-09-22

Versão de **correção de comportamento visual** + documentação honesta, preparada para a primeira
publicação. A API pública não mudou (exceto pela remoção de um membro de enum que nunca funcionou),
mas o resultado visual de qualquer paleta com menos de 16 cores e de qualquer uso de dithering muda
— para o que a documentação sempre descreveu.

### 🐛 Correções

- ✅ **Presets com menos de 16 cores ficavam dominados por preto** (o bug mais visível do pacote):
  - `GetPresetPalette` sempre criou uma textura **16×1**, mas preenchia apenas os slots reais do
    preset — GameBoy (4), CGA (4) e Binary (2). Os demais ficavam `(0,0,0,0)` = preto.
  - O `Frag` usava `int paletteSize = 16;` **fixo**, então esses slots pretos competiam na busca de
    cor mais próxima e venciam para qualquer cor escura ou média.
  - Resultado prático: **Binary** produzia uma imagem quase toda preta em vez de preto e branco;
    **GameBoy** e **CGA** perdiam os tons escuros/médios.
  - Agora o C# envia o número **real** de cores (`_PaletteSize`) e o shader faz
    `clamp(int(_PaletteSize), 1, 16)`. Presets de 16 cores ficam visualmente idênticos.
  - `FindClosestPaletteColor` ganhou guarda para `paletteSize <= 1` (a divisão `i / (size-1)` era
    por zero nesse caso).

- ✅ **Dithering não era dithering ordenado de verdade**: o threshold de Bayer era somado **depois**
  do lookup da paleta (`color.rgb += dither`), o que apenas deslocava o brilho de uma cor já
  escolhida e gerava tons que não existem na paleta. Agora é somado **antes** da quantização e do
  lookup, escalado pelo número de níveis (`offset / colorCount`) — o comportamento clássico do Bayer
  ordered dithering, alternando entre as duas cores vizinhas da paleta. O padrão usa as UVs
  **originais**, não as distorcidas pela curvatura CRT, então fica estável e alinhado à tela.
  Com a paleta desligada, o dithering continua valendo (aplicado direto na cor).

- ✅ **`DitherType.FloydSteinberg` removido do enum.** O valor existia e aparecia no dropdown do
  Inspector, mas o shader só tratava Bayer 2/4/8 — selecioná-lo deixava o dithering
  silenciosamente desligado, sem nenhum aviso. O Inspector agora valida o valor serializado e,
  se encontrar o inteiro órfão `3` de um asset antigo, avisa e normaliza para **Bayer 4×4**.

- ✅ **`PaletteUtility.ExtractPaletteFromTexture` era amostragem aleatória, não extração de paleta:**
  sorteava até 1000 pixels e guardava os primeiros `maxColors` distintos num `HashSet<Color>`. Como
  a comparação era em float `RGBA`, quase todo pixel era "único" — o resultado eram N pixels
  aleatórios da imagem. Reescrito como extração real: amostragem em **grid determinístico**
  (até ~4096 pontos), buckets de **5 bits por canal**, seleção das cores **mais frequentes** (cada
  uma pela média do seu bucket) e ordenação estável. Também passou a tratar textura não legível com
  erro claro pedindo *Read/Write Enabled*.

- ✅ **`PaletteUtility.CreateRandomPalette` gerava cores lavadas e corrompia o RNG do jogo:**
  - Sorteava `Random.value` por canal **RGB** (a doc dizia "HSV controlado para cores vibrantes") —
    agora sorteia em **HSV** com saturação e valor altos.
  - Chamava `Random.InitState(seed)`, que altera o estado **global** de `UnityEngine.Random` e muda
    a sequência aleatória do jogo inteiro — agora usa um `System.Random` próprio.

- ✅ **Todas as paletas geradas pelo pacote saíam com `Wrap Mode = Repeat`** (padrão do `Texture2D`),
  não *Clamp*. Corrigido em `PaletteUtility`, `PalettePresetAsset.ToTexture()` e nos presets do
  `PixelCameraRenderPass`.

- ✅ **`PalettePresetAsset.ToTexture()` gerava textura com largura = número de cores** (não 16), o que
  não bate com o que o shader amostra. Agora devolve sempre **16×1** com os slots preenchidos por
  repetição cíclica das cores reais.

- ✅ **"Gerar Paleta Aleatória" do Inspector criava um `Texture2D` só em memória** e o atribuía a um
  campo serializado. Textura sem existência em disco não sobrevive a domain reload / salvar a cena,
  então a referência se perdia (o campo voltava para *None*). Agora grava como asset via
  `AssetDatabase.CreateAsset`, com escolha de pasta e validação de que está dentro de `Assets/`.

- ✅ **Export do Palette Editor e do `PalettePresetAssetEditor` gerava PNG com largura variável**,
  inutilizável no campo *Custom Palette* (que espera 16 slots). Ambos agora exportam **16×1**,
  prontos para arrastar, e o dialog explica os settings de importação (Point, Clamp, Read/Write,
  sem compressão).

- ✅ **Importação de paleta aceitava BMP** nos dialogs (`"png,jpg,bmp"`), mas `Texture2D.LoadImage` só
  decodifica **PNG e JPEG** — o usuário escolhia um BMP e recebia uma textura vazia, sem erro.
  Filtro corrigido para `png,jpg,jpeg` e a falha de decodificação agora é verificada e reportada.

- ✅ **`GenerateGradient` do `PalettePresetAssetEditor` dividia por zero** quando o array tinha 1 cor
  (`i / (float)(count - 1)`).

- ✅ **CS0618 no Unity 6:** `SerializedProperty.enumValueIndex` (obsoleto) substituído por
  `enumValueFlag` em `PixelCameraRenderFeatureEditor`.

### ✨ Novidades

- ✅ **`PaletteUtility.CreatePaletteTexture(IList<Color>)`** — helper público que monta a textura no
  formato canônico que o shader espera (16×1, RGBA32, Point, Clamp, slots preenchidos por
  repetição). Todas as ferramentas do pacote passaram a usá-lo, então não há mais como gerar uma
  paleta fora do formato.
- ✅ **`PaletteUtility.PaletteTextureWidth`** e **`PalettePresetAsset.MaxColors`** — constantes
  públicas para o limite de 16 cores, em vez de número mágico espalhado pelo código.
- ✅ **Arquivo [`LICENSE`](../LICENSE) (MIT)** na raiz do repositório. Até então não havia licença
  alguma, o que significa *todos os direitos reservados* — incompatível com distribuição em store.

### 📝 Documentação

- ✅ **Matriz de compatibilidade de pipeline explícita**: README raiz, `PixelCamera/README.md`,
  `QUICKSTART.md`, `FEATURES.md`, `GUIA-PASSO-A-PASSO.md` e este arquivo agora afirmam claramente
  que o asset é **URP-only** — Built-in e HDRP **não** são suportados — com os motivos técnicos de
  cada caso. Antes a informação estava espalhada em frases de troubleshooting ("verifique se não
  está usando Built-in ou HDRP") e o `FEATURES.md` não mencionava o assunto.
- ✅ **Requisito de Input Manager documentado**: o `PixelCameraController` usa `UnityEngine.Input`,
  então projetos com *Active Input Handling* = **Input System Package (new)** exclusivo não têm
  F1–F4 nem scroll. Adicionado aos requisitos e ao troubleshooting.
- ✅ **Seção "Limitações conhecidas"** no `README.md`, reduzida ao que ainda é limite real da 1.1.0,
  mais a seção **"Corrigido na 1.1.0"** com o antes/depois de cada bug.
- ✅ **`FEATURES.md` reescrito como inventário real**: removida a alegação de "66 features / 100% /
  pronto para produção"; itens com ressalva marcados com ⚠️ e ausentes com ❌. Corrigidas descrições
  que não batiam com o código ("Suporta PNG, JPG, BMP", "Qualquer largura até 16 pixels",
  "Amostragem inteligente", "HSV controlado").
- ✅ **Regra da textura de paleta documentada**: exatamente **16×1**, `Point`, `Clamp`, sem mipmaps,
  slots preenchidos por repetição.
- ✅ **Menus `Tools > Pixel Camera` listados por completo** (7 itens reais; faltavam Diagnóstico do
  Projeto e Corrigir Automaticamente).
- ✅ **Samples descritos como scripts** (não há cena `.unity` pré-montada).
- ✅ **`package.json`**: `displayName` "Pixel Camera URP" → "Pixel Camera (URP)"; `description` deixa
  de dizer "Sistema completo" e declara o requisito de URP e a ausência de suporte a Built-in/HDRP;
  `keywords` += `urp`, `renderfeature`; `samples` "Demo Scene / Cena de exemplo com configuração
  completa" → "Demo Scripts" com a descrição real do conteúdo.

### ⚠️ Notas de atualização (1.0.3 → 1.1.0)

- **`DitherType.FloydSteinberg` foi removido.** Se o seu código referencia esse membro, ele não
  compila mais — troque por `Bayer2x2`/`Bayer4x4`/`Bayer8x8`. Assets serializados com o valor são
  normalizados automaticamente pelo Inspector.
- **`PaletteUtility.ExtractPaletteFromTexture` mudou de comportamento** (agora é determinístico e
  retorna as cores mais frequentes, não pixels aleatórios) e **requer *Read/Write Enabled*** na
  textura de origem.
- **`PalettePresetAsset.ToTexture()` retorna sempre 16×1** (antes a largura era o número de cores).
- **`PaletteUtility.LoadPaletteFromPNG` reamostra para 16×1** e devolve uma paleta de cinzas com
  aviso se o arquivo não puder ser decodificado (antes devolvia a textura crua do `LoadImage`).
- **O visual muda** para quem usava presets de 2–4 cores ou dithering: ambos passam a entregar o
  resultado que a documentação sempre descreveu.
- Rode **Assets > Reimport All** após atualizar, para o shader ser recompilado.

---

## [Não publicado] - ajustes de documentação

> Entradas criadas durante a auditoria e **incorporadas à 1.1.0** acima.

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

**[1.2.0] - Melhorias de Performance**
- [ ] Otimização de shaders para mobile
- [ ] GPU Instancing support
- [ ] Compute shaders para dithering complexo
- [ ] LOD automático baseado em resolução

**[1.3.0] - Novos Efeitos**
- [ ] Floyd-Steinberg dithering (error diffusion)
- [ ] CRT Phosphor mask (RGB subpixels)
- [ ] Chromatic aberration
- [ ] Noise/grain opcional
- [ ] Color bleeding entre pixels

**[1.4.0] - Ferramentas**
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

### Limitações Conhecidas (estado atual, 1.1.0)

Detalhadas em [README.md → Limitações conhecidas](README.md#limitações-conhecidas).

1. **Paleta máxima: 16 cores** (limitação do shader). `colorCount` aceita até 256, mas só afeta a
   pré-quantização — nunca haverá mais de 16 cores distintas vindas da paleta.
2. **Floyd-Steinberg (difusão de erro) não existe** — só Bayer ordenado 2×2/4×4/8×8. Roadmap 1.3.0.
3. **A textura de paleta custom precisa ter exatamente 16×1** (Point, Clamp, sem mipmaps).
4. **UI em Screen Space - Overlay não é pixelada** (é desenhada depois do pipeline da câmera).
5. **`PixelCameraController` usa Input Manager legado** — em projeto *Input System Package (new)*
   exclusivo, F1–F4 e o scroll não funcionam.
6. **CRT curvature pode causar clipping** (preto) nas bordas.
7. **Bloom é aproximação** por box 5×5 (25 amostras/pixel) — não é physically-based, e é o efeito
   mais caro do pacote.
8. **Scanlines usam `_ScreenParams.y` da textura de baixa resolução**, então a espessura percebida
   muda conforme a resolução de pixelização.
9. **Sem suporte a Built-in e HDRP** (decisão técnica documentada).
10. **Samples são scripts**, não uma cena `.unity` pré-montada.

> Itens que eram limitação até a 1.0.3 e foram **corrigidos na 1.1.0**: presets de 2–4 cores
> dominados por preto, dithering aplicado depois do lookup da paleta, `FloydSteinberg` no dropdown
> sem efeito, `ExtractPaletteFromTexture` aleatória, `CreateRandomPalette` em RGB com
> `Random.InitState` global, paletas com `Wrap = Repeat`, `ToTexture()` com largura variável e
> ausência de arquivo `LICENSE`.

---

## Créditos

Desenvolvido para Unity URP com foco em jogos pixel art e efeitos retrô.

Baseado em técnicas clássicas de:
- Bayer ordered dithering
- CGA/VGA palettes
- CRT emulation shaders
- Modern pixel art engines (PICO-8, Aseprite)
