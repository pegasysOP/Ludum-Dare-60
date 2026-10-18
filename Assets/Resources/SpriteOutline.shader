// draws a line around a sprite with a small gap between them, sized in world units so it scales with the screen
Shader "Liquid Assets/Sprite Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [PerRendererData] _Color ("Colour", Color) = (1, 1, 1, 1)
        [PerRendererData] _Thickness ("Thickness (world units)", Float) = 0.02
        [PerRendererData] _Gap ("Gap (world units)", Float) = 0.02
        // set by SpriteOutline: where the sprite sits in its texture, and how much uv one world unit covers
        [PerRendererData] _SpriteRect ("Sprite Rect", Vector) = (0, 0, 1, 1)
        [PerRendererData] _UVPerUnit ("UV Per Unit", Vector) = (1, 1, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Thickness;
                float _Gap;
                float4 _SpriteRect;
                float4 _UVPerUnit;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            // anything outside the sprite's rect counts as empty
            float Alpha(float2 uv)
            {
                float2 local = (uv - _SpriteRect.xy) / _SpriteRect.zw;
                float inside = step(0, local.x) * step(local.x, 1) * step(0, local.y) * step(local.y, 1);
                return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).a * inside;
            }

            // most solid the sprite gets on a circle of the given world radius
            float Ring(float2 uv, float radius)
            {
                float most = 0;
                [unroll]
                for (int i = 0; i < 12; i++)
                {
                    float angle = i * (6.2831853 / 12);
                    most = max(most, Alpha(uv + float2(cos(angle), sin(angle)) * _UVPerUnit.xy * radius));
                }
                return most;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // solid within the gap means too close, solid within gap plus thickness means on the line
                float tooClose = Alpha(input.uv);
                // inside the sprite or right against it can never be on the line, so skip the rest
                if (tooClose > 0.99)
                    return 0;
                tooClose = max(tooClose, Ring(input.uv, _Gap * 0.5));
                tooClose = max(tooClose, Ring(input.uv, _Gap));
                if (tooClose > 0.99)
                    return 0;
                float onLine = max(tooClose, Ring(input.uv, _Gap + _Thickness * 0.5));
                onLine = max(onLine, Ring(input.uv, _Gap + _Thickness));

                return half4(_Color.rgb, _Color.a * onLine * (1 - tooClose));
            }
            ENDHLSL
        }
    }
}
