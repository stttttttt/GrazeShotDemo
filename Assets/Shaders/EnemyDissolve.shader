Shader "ShotGame/EnemyDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Dissolve ("Dissolve", Range(-0.1, 1.1)) = -0.08
        [HDR] _EdgeColor ("Edge Color", Color) = (1,0.35,0.05,1)
        _EdgeWidth ("Edge Width", Range(0.001,0.3)) = 0.08
        _NoiseScale ("Noise Scale", Float) = 28
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "EnemyDissolve"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _EdgeColor;
                float _Dissolve;
                float _EdgeWidth;
                float _NoiseScale;
            CBUFFER_END

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 textureColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 spriteColor = textureColor * input.color * _Color;
                clip(spriteColor.a - 0.001);

                float2 noiseCell = floor(input.uv * max(1.0, _NoiseScale));
                float noise = saturate(Hash21(noiseCell) * 0.82 + input.uv.y * 0.18);
                float distanceToEdge = noise - _Dissolve;
                clip(distanceToEdge);

                float edge = 1.0 - smoothstep(0.0, max(0.001, _EdgeWidth), distanceToEdge);
                half3 color = lerp(spriteColor.rgb, _EdgeColor.rgb, edge * _EdgeColor.a);
                color *= spriteColor.a;
                return half4(color, spriteColor.a);
            }
            ENDHLSL
        }
    }
}
