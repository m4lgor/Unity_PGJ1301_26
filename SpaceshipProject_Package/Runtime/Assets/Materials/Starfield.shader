Shader "UQAT/Starfield"
{
    // Unlit additive stars.
    // Per-star data is baked into the mesh by SpaceBackground.cs:
    //   vertex colour rgb = star tint, a = brightness
    //   uv.x = twinkle phase, uv.y = twinkle speed
    Properties
    {
        _Intensity ("Intensity", Float) = 1.0
        _TwinkleAmount ("Twinkle Amount", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Background"          // drawn before the ship, so it can never cover it
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Starfield"

            Blend One One   // additive: glows over the dark clear colour and feeds Bloom
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
            };

            float _Intensity;
            float _TwinkleAmount;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // uv.x is a per-star phase, so neighbours never pulse in sync.
                float wave = 0.5 + 0.5 * sin(_Time.y * IN.uv.y + IN.uv.x);
                float twinkle = lerp(1.0 - _TwinkleAmount, 1.0, wave);

                half3 rgb = IN.color.rgb * IN.color.a * _Intensity * twinkle;
                return half4(rgb, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
