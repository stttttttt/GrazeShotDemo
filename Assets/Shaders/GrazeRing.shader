Shader "ShotGame/GrazeRing"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (0.35, 1, 0.72, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Thickness ("Thickness", Range(0.005, 0.15)) = 0.018
        _Softness ("Softness", Range(0.001, 0.08)) = 0.006
        _Brightness ("Brightness", Range(0, 5)) = 1
        _ArcAmount ("Arc Amount", Range(0, 1)) = 0
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0
        _Rotation ("Rotation", Float) = 0
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
            Name "GrazeRing"
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

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Alpha;
                float _Thickness;
                float _Softness;
                float _Brightness;
                float _ArcAmount;
                float _NoiseStrength;
                float _Rotation;
            CBUFFER_END

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
                float2 centered = input.uv - 0.5;
                float angle = atan2(centered.y, centered.x);
                float angularNoise = sin(angle * 13.0 + _Rotation * 2.7) * 0.5 +
                                     sin(angle * 23.0 - _Rotation * 1.9) * 0.25;
                float radius = 0.455 + angularNoise * _NoiseStrength * 0.008;
                float distanceToRing = abs(length(centered) - radius);
                float ring = 1.0 - smoothstep(_Thickness, _Thickness + _Softness, distanceToRing);

                float arcWave = 0.5 + 0.5 * cos(angle * 8.0 + _Rotation * 6.2831853);
                float arcMask = smoothstep(0.22, 0.72, arcWave);
                ring *= lerp(1.0, arcMask, saturate(_ArcAmount));

                float alpha = saturate(ring * _Alpha * _BaseColor.a * input.color.a);
                half3 color = _BaseColor.rgb * input.color.rgb * _Brightness * alpha;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
