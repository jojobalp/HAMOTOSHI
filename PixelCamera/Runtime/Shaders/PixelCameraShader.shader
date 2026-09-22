Shader "Hidden/PixelCamera"
{
    Properties
    {
        _CustomPalette ("Paleta de cores", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    struct Attributes
    {
    #if defined(_USE_DRAW_PROCEDURAL)
        // O Blitter/RenderGraph da URP desenha um triângulo fullscreen procedural
        // (sem malha), então recebemos apenas o ID do vértice.
        uint vertexID : SV_VertexID;
    #else
        float4 positionOS : POSITION;
        float2 uv : TEXCOORD0;
    #endif
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    // O Blitter da URP expõe a textura de origem como "_BlitTexture"
    // (o "_MainTex" só era configurado pelo caminho legado bem antigo).
    TEXTURE2D_X(_BlitTexture);
    SAMPLER(sampler_BlitTexture);
    TEXTURE2D(_CustomPalette);
    SAMPLER(sampler_CustomPalette);

    float4 _PixelResolution;      // x = width, y = height
    float _PaletteEnabled;
    float _ColorCount;
    float _ColorQuantization;
    // Nº de cores REAL da paleta ativa (preenchido pelo C#). Presets de 4 cores
    // (GameBoy/CGA) enviam 4, Binary envia 2, presets de 16 cores e paletas
    // custom enviam 16. Sem isso os slots não usados da textura 16x1 (que ficam
    // em preto) participavam da busca de cor mais próxima e "sugavam" todos os
    // tons escuros/médios da imagem.
    float _PaletteSize;
    float _DitherEnabled;
    float _DitherType;            // 0 = Bayer2x2, 1 = Bayer4x4, 2 = Bayer8x8
    float _DitherIntensity;
    float _CRTEnabled;
    float _ScanlineIntensity;
    float _ScanlineThickness;
    float _BloomIntensity;
    float _BloomRadius;
    float _CurvatureIntensity;
    float _VignetteIntensity;
    // NOTA: não declarar "_Time" aqui! O URP já declara float4 _Time em
    // UnityInput.hlsl (incluído por Core.hlsl), o que gera o erro
    // "redefinition of '_Time'". Use um nome próprio para o tempo.
    float _PixelCameraTime;

    // Matrizes de Bayer para dithering
    static const float Bayer2x2[4] = {
        0.0, 2.0,
        3.0, 1.0
    };

    static const float Bayer4x4[16] = {
         0.0,  8.0,  2.0, 10.0,
        12.0,  4.0, 14.0,  6.0,
         3.0, 11.0,  1.0,  9.0,
        15.0,  7.0, 13.0,  5.0
    };

    static const float Bayer8x8[64] = {
         0.0, 32.0,  8.0, 40.0,  2.0, 34.0, 10.0, 42.0,
        48.0, 16.0, 56.0, 24.0, 50.0, 18.0, 58.0, 26.0,
        12.0, 44.0,  4.0, 36.0, 14.0, 46.0,  6.0, 38.0,
        60.0, 28.0, 52.0, 20.0, 62.0, 30.0, 54.0, 22.0,
         3.0, 35.0, 11.0, 43.0,  1.0, 33.0,  9.0, 41.0,
        51.0, 19.0, 59.0, 27.0, 49.0, 17.0, 57.0, 25.0,
        15.0, 47.0,  7.0, 39.0, 13.0, 45.0,  5.0, 37.0,
        63.0, 31.0, 55.0, 23.0, 61.0, 29.0, 53.0, 21.0
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
    #if defined(_USE_DRAW_PROCEDURAL)
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
    #else
        output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
        output.uv = input.uv;
    #endif
        return output;
    }

    // Aplicar curvatura CRT
    float2 ApplyCurvature(float2 uv, float intensity)
    {
        float2 centered = uv * 2.0 - 1.0;
        float2 r2 = centered * centered;
        centered *= 1.0 + intensity * (r2.x + r2.y);
        return centered * 0.5 + 0.5;
    }

    // Aplicar vinheta
    float ApplyVignette(float2 uv, float intensity)
    {
        float2 centered = uv * 2.0 - 1.0;
        float dist = length(centered);
        return 1.0 - smoothstep(0.5, 1.5, dist) * intensity;
    }

    // Aplicar scanlines
    float ApplyScanlines(float2 uv, float intensity, float thickness)
    {
        float scanline = sin(uv.y * _ScreenParams.y * thickness * 3.14159) * 0.5 + 0.5;
        return 1.0 - scanline * intensity;
    }

    // Aplicar dithering Bayer
    float ApplyBayerDither(float2 uv, float intensity, int size)
    {
        int2 pixel = int2(uv * _ScreenParams.xy) % size;
        float threshold = 0.0;

        if (size == 2)
        {
            threshold = Bayer2x2[pixel.y * 2 + pixel.x] / 4.0;
        }
        else if (size == 4)
        {
            threshold = Bayer4x4[pixel.y * 4 + pixel.x] / 16.0;
        }
        else if (size == 8)
        {
            threshold = Bayer8x8[pixel.y * 8 + pixel.x] / 64.0;
        }

        return (threshold - 0.5) * intensity;
    }

    // Quantizar cor para paleta
    float3 QuantizeColor(float3 color, int colorCount)
    {
        float levels = colorCount;
        return floor(color * levels + 0.5) / levels;
    }

    // Máximo de cores suportado (mesmo limite da textura de paleta 16x1)
    #define PALETTE_MAX_COLORS 16

    // Encontrar cor mais próxima na paleta
    float3 FindClosestPaletteColor(float3 color, int paletteSize)
    {
        float minDist = 1000.0;
        float3 closestColor = color;

        // Com um único slot não há intervalo para amostrar (a divisão abaixo
        // seria por zero), então devolve direto a cor desse slot.
        if (paletteSize <= 1)
        {
            return SAMPLE_TEXTURE2D(_CustomPalette, sampler_CustomPalette, float2(0.5 / float(PALETTE_MAX_COLORS), 0.5)).rgb;
        }

        // O limite do loop deve ser constante para o compilador conseguir
        // desenrolar (necessário no d3d11 / feature level 9.3 usado pelo URP).
        for (int i = 0; i < PALETTE_MAX_COLORS; i++)
        {
            if (i >= paletteSize) break;

            float3 paletteColor = SAMPLE_TEXTURE2D(_CustomPalette, sampler_CustomPalette, float2(float(i) / float(paletteSize - 1), 0.5)).rgb;
            float dist = distance(color, paletteColor);
            
            if (dist < minDist)
            {
                minDist = dist;
                closestColor = paletteColor;
            }
        }

        return closestColor;
    }

    // Offset do dithering ordenado (Bayer), em (-0.5..+0.5) * intensidade.
    // Recebe as UVs ORIGINAIS (não as distorcidas pela curvatura CRT) para o
    // padrão ficar estável e alinhado à tela.
    float GetDitherOffset(float2 uv)
    {
        if (_DitherType < 0.5)      // Bayer2x2
        {
            return ApplyBayerDither(uv, _DitherIntensity, 2);
        }
        else if (_DitherType < 1.5) // Bayer4x4
        {
            return ApplyBayerDither(uv, _DitherIntensity, 4);
        }

        return ApplyBayerDither(uv, _DitherIntensity, 8); // Bayer8x8
    }

    float4 Frag(Varyings input) : SV_Target
    {
        float2 uv = input.uv;

        // Aplicar curvatura CRT
        if (_CRTEnabled > 0.5)
        {
            uv = ApplyCurvature(uv, _CurvatureIntensity);
            
            // Verificar se está fora dos limites
            if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
            {
                return float4(0.0, 0.0, 0.0, 1.0);
            }
        }

        // Amostrar textura
        float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);

        // Dithering ordenado: o threshold é somado ANTES da quantização e da
        // busca na paleta, não depois. É isso que faz o dithering alternar entre
        // as duas cores vizinhas da paleta (comportamento clássico do Bayer
        // ordered dithering). Somar depois - como era antes - apenas deslocava o
        // brilho de uma cor já escolhida, gerando tons que não existem na paleta.
        // A escala pelo número de níveis mantém a intensidade com peso
        // perceptível parecido em qualquer configuração.
        float3 ditheredColor = color.rgb;
        if (_DitherEnabled > 0.5)
        {
            float levels = max(2.0, _ColorCount);
            ditheredColor = saturate(color.rgb + GetDitherOffset(input.uv) / levels);
        }

        // Aplicar paleta limitada
        if (_PaletteEnabled > 0.5)
        {
            // Primeiro quantizar
            float3 quantized = QuantizeColor(ditheredColor, int(_ColorCount));

            // Depois encontrar a cor mais próxima na paleta, usando o número REAL
            // de cores da paleta ativa (_PaletteSize vem do C#). Antes era fixo
            // em 16, o que fazia os slots não usados da textura 16x1 - que ficam
            // em preto - competirem na busca e dominarem os tons escuros/médios.
            int paletteSize = clamp(int(_PaletteSize), 1, PALETTE_MAX_COLORS);
            float3 paletteColor = FindClosestPaletteColor(quantized, paletteSize);

            // Blend com a cor quantizada (o dithering aparece como alternância
            // entre a cor da paleta e a quantizada)
            color.rgb = lerp(paletteColor, quantized, _ColorQuantization);
        }
        else
        {
            // Paleta desligada: o dithering continua valendo, aplicado na cor.
            color.rgb = ditheredColor;
        }

        // Aplicar efeitos CRT
        if (_CRTEnabled > 0.5)
        {
            // Scanlines
            float scanline = ApplyScanlines(uv, _ScanlineIntensity, _ScanlineThickness);
            color.rgb *= scanline;

            // Vinheta
            float vignette = ApplyVignette(uv, _VignetteIntensity);
            color.rgb *= vignette;

            // Bloom/Glow (aproximação simples)
            if (_BloomIntensity > 0.0)
            {
                float3 bloom = float3(0.0, 0.0, 0.0);
                float samples = 0.0;
                
                for (int i = -2; i <= 2; i++)
                {
                    for (int j = -2; j <= 2; j++)
                    {
                        float2 offset = float2(i, j) * _BloomRadius / _ScreenParams.xy;
                        bloom += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + offset).rgb;
                        samples += 1.0;
                    }
                }
                
                bloom /= samples;
                color.rgb += bloom * _BloomIntensity;
            }
        }

        return color;
    }

    // Sampler explícito com filtro point: usado no upscale da textura de baixa
    // resolução para manter os pixels "quadradões" (sem interpolação bilinear).
    SamplerState PixelCameraPointClampSampler
    {
        Filter = MIN_MAG_MIP_POINT;
        AddressU = Clamp;
        AddressV = Clamp;
    };

    // Pass de cópia simples com filtro point (upscale da textura de baixa resolução)
    float4 FragUpscale(Varyings input) : SV_Target
    {
        return SAMPLE_TEXTURE2D_X(_BlitTexture, PixelCameraPointClampSampler, input.uv);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        
        // Pass 0: pixelização + paleta + dithering + CRT (cena -> baixa resolução)
        Pass
        {
            Name "PixelCameraPass"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
            ENDHLSL
        }

        // Pass 1: upscale com filtro point (baixa resolução -> resolução da câmera)
        Pass
        {
            Name "PixelCameraUpscalePass"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragUpscale
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
            ENDHLSL
        }
    }
    
    Fallback Off
}
