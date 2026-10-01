Shader "UQAT/Nebula"
{
    // Soft additive nebula clouds.
    // The noise texture is generated at runtime by SpaceBackground.cs; the per-cloud
    // tint and strength ride in the vertex colour (rgb = tint, a = strength).
    Properties
    {
        _MainTex ("Noise", 2D) = "black" {}
        _Intensity ("Intensity", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Background"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Nebula"

            Blend One One   // additive, like the stars: order-independent, feeds Bloom
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _Intensity;

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
                // The texture already contains its own radial falloff, so the quad
                // edges fade out and never show as a square.
                half n = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).r;

                half3 rgb = n * IN.color.rgb * IN.color.a * _Intensity;
                return half4(rgb, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
