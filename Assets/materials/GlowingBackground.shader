Shader "Custom/GlowingTreeSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Sprite Tint / Fade", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (0.25,0.8,1,1)
        _GlowIntensity ("Glow Intensity", Range(0,3)) = 0.45
        _GlowOpacity ("Glow Opacity", Range(0,1)) = 0.3
        _GlowRadius ("Glow Radius (pixels)", Range(1,12)) = 4
        _CoreBrightness ("Branch Brightness", Range(0.5,2)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _GlowColor;
                float _GlowIntensity;
                float _GlowOpacity;
                float _GlowRadius;
                float _CoreBrightness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half AlphaAt(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv)).a;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half spriteAlpha = tex.a;

                // Sample around the branch silhouette to create a soft-ish halo.
                float2 d = _MainTex_TexelSize.xy * _GlowRadius;
                half nearby = 0.0h;
                nearby = max(nearby, AlphaAt(IN.uv + float2( d.x, 0)));
                nearby = max(nearby, AlphaAt(IN.uv + float2(-d.x, 0)));
                nearby = max(nearby, AlphaAt(IN.uv + float2(0,  d.y)));
                nearby = max(nearby, AlphaAt(IN.uv + float2(0, -d.y)));
                nearby = max(nearby, AlphaAt(IN.uv + float2( d.x,  d.y)));
                nearby = max(nearby, AlphaAt(IN.uv + float2(-d.x,  d.y)));
                nearby = max(nearby, AlphaAt(IN.uv + float2( d.x, -d.y)));
                nearby = max(nearby, AlphaAt(IN.uv + float2(-d.x, -d.y)));

                half halo = saturate(nearby - spriteAlpha) * _GlowOpacity;
                half haloAlpha = halo * saturate(_GlowIntensity);
                half alpha = max(spriteAlpha, haloAlpha) * IN.color.a;

                half3 branch = tex.rgb * IN.color.rgb * _CoreBrightness;
                half3 glow = _GlowColor.rgb * _GlowIntensity;
                half3 color = lerp(glow, branch, spriteAlpha);

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
