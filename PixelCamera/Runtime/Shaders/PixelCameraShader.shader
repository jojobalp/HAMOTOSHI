Shader "Hidden/PixelCamera"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    struct Attributes
    {
        float4 positionOS : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);
    TEXTURE2D(_CustomPalette);
    SAMPLER(sampler_CustomPalette);

    float4 _PixelResolution;      // x = width, y = height
    float _PaletteEnabled;
    float _ColorCount;
    float _ColorQuantization;
    float _DitherEnabled;
    float _DitherType;            // 0 = Bayer2x2, 1 = Bayer4x4, 2 = Bayer8x8, 3 = FloydSteinberg
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
        output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
        output.uv = input.uv;
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

        // Amostr textura
        float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

        // Aplicar paleta limitada
        if (_PaletteEnabled > 0.5)
        {
            // Primeiro quantizar
            float3 quantized = QuantizeColor(color.rgb, int(_ColorCount));
            
            // Depois encontrar cor mais próxima na paleta
            int paletteSize = 16; // Default para presets
            color.rgb = FindClosestPaletteColor(quantized, paletteSize);
            
            // Blend com quantização original
            color.rgb = lerp(color.rgb, quantized, _ColorQuantization);
        }

        // Aplicar dithering
        if (_DitherEnabled > 0.5)
        {
            float dither = 0.0;
            
            if (_DitherType < 0.5) // Bayer2x2
            {
                dither = ApplyBayerDither(uv, _DitherIntensity, 2);
            }
            else if (_DitherType < 1.5) // Bayer4x4
            {
                dither = ApplyBayerDither(uv, _DitherIntensity, 4);
            }
            else if (_DitherType < 2.5) // Bayer8x8
            {
                dither = ApplyBayerDither(uv, _DitherIntensity, 8);
            }
            // FloydSteinberg seria mais complexo e requer múltiplas passes
            // Por simplicidade, usamos apenas Bayer aqui
            
            color.rgb += dither;
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
                        bloom += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset).rgb;
                        samples += 1.0;
                    }
                }
                
                bloom /= samples;
                color.rgb += bloom * _BloomIntensity;
            }
        }

        return color;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            Name "PixelCameraPass"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
    
    Fallback Off
}
