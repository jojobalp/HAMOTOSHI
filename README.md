# HAMOTOSHI

## PixelCamera

Efeito de câmera retrô para URP (pixelização, paletas, dithering e CRT).

### Compatibilidade (URP)

| Versão do Unity | URP | Caminho usado |
| --- | --- | --- |
| Unity 6+ (6000.0+) | 17+ | Render Graph (`RecordRenderGraph`) |
| Unity 2022 / 2023 | 13–16 | `Execute` com `RTHandle` + `Blitter` |
| Unity 2021 ou mais antigo | ≤ 12 | Legado com `RenderTargetIdentifier` |

> A partir da URP 17.1, o método `ScriptableRenderPass.Execute(ScriptableRenderContext, ref RenderingData)`
> foi removido da API pública (erro CS0115). No Unity 6 o efeito é implementado via
> `RecordRenderGraph`, que é a única forma suportada quando o Render Graph está ativo.

No Unity 6, certifique-se de que o **Render Graph está habilitado** em
*Edit > Project Settings > Graphics > URP* (padrão no Unity 6.1+).
