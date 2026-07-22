Shader "ARPG/SimpleDissolve"
{
    // 死亡溶解：优先采样 Noise；无贴图时用程序化噪声，保证 Demo 开箱即用。
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _NoiseMap ("Noise Map", 2D) = "white" {}
        _UseProceduralNoise ("Use Procedural Noise", Float) = 1
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _EdgeColor ("Edge Color", Color) = (1,0.45,0.1,1)
        _Cutoff ("Dissolve Amount", Range(0,1)) = 0
        _EdgeWidth ("Edge Width", Range(0,0.2)) = 0.06
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Dissolve"
            Tags { "LightMode"="UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);
            float4 _BaseColor;
            float4 _EdgeColor;
            float4 _BaseMap_ST;
            float _Cutoff;
            float _EdgeWidth;
            float _UseProceduralNoise;

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

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                half noise = _UseProceduralNoise > 0.5
                    ? Hash21(i.uv * 12.0)
                    : SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, i.uv).r;

                clip(noise - _Cutoff);
                half edge = step(noise, _Cutoff + _EdgeWidth);
                return lerp(baseCol, _EdgeColor, edge);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
