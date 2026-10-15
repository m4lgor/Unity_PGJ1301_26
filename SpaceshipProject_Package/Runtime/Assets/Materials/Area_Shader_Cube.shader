Shader "Unlit/Area_Shader_Cube"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Falloff ("Falloff Distance", Range(0.05, 0.95)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float3 GetObjectScale()
            {  
                // unity_ObjectToWorld is column-major
                float3 scale;
                scale.x = length(unity_ObjectToWorld._m00_m10_m20); // X axis vector length
                scale.y = length(unity_ObjectToWorld._m01_m11_m21); // Y axis vector length
                scale.z = length(unity_ObjectToWorld._m02_m12_m22); // Z axis vector length
                return scale;
            }

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 localXY : TEXCOORD0;
            };

            float4 _Color;
            float _Falloff;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.localXY = v.vertex.xy;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float4 outColor = _Color;

                float3 scale = GetObjectScale();

                float distX = abs(i.localXY.x) * scale.x;
                distX *= 2.0f;

                float distY = abs(i.localXY.y) * scale.y;
                distY *= 2.0f;

                float alphaY = distY / scale.y;
                if(_Falloff > 0.0f)
                {
                    alphaY -= _Falloff;
                    alphaY = 1.0f / (1.0f - _Falloff) * alphaY;
                }

                float alphaX = distX / scale.x;
                if(_Falloff > 0.0f)
                {
                    alphaX -= _Falloff;
                    alphaX = 1.0f / (1.0f - _Falloff) * alphaX;
                }

                float alpha = max(alphaX, alphaY);
                alpha = clamp(alpha, 0.0f, 1.0f);

                return float4(outColor.rgb, alpha * outColor.a);
            }
            ENDHLSL
        }
    }
}
