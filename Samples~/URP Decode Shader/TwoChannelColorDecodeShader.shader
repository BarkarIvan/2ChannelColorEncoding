Shader "TwoChannelColor/Decode Unlit"
{
    Properties
    {
        _EncodedTex ("Encoded Texture (RG)", 2D) = "white" {}
        _BC1 ("Base Color 1 (Linear RGB)", Vector) = (1, 0, 0, 0)
        _BC2 ("Base Color 2 (Linear RGB)", Vector) = (0, 1, 0, 0)
        _DecodeGamma ("Decode Gamma", Float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Decode2Ch"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unitycgchat.2channelcolorencoding/Runtime/Shaders/TwoChannelColorDecode.hlsl"

            TEXTURE2D(_EncodedTex);
            SAMPLER(sampler_EncodedTex);

            CBUFFER_START(UnityPerMaterial)
                half3 _BC1;
                half3 _BC2;
                half _DecodeGamma;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half2 data = SAMPLE_TEXTURE2D(_EncodedTex, sampler_EncodedTex, input.uv).rg;
                half invGamma = rcp(_DecodeGamma);
                half3 color = Decode2ChannelColorToGamma(data, _BC1, _BC2, invGamma);
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
