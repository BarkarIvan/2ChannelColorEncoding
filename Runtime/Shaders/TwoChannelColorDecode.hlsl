#ifndef TWO_CHANNEL_COLOR_DECODE_HLSL
#define TWO_CHANNEL_COLOR_DECODE_HLSL

float3 Decode2ChannelColor(float2 data, float3 bc1, float3 bc2)
{
    float3 color = lerp(bc1, bc2, data.y);
    float colorLum = dot(color, float3(0.2126, 0.7152, 0.0722));
    float targetLum = data.x * data.x;
    color *= targetLum / max(colorLum, 1e-6);
    return color;
}

float3 Decode2ChannelColorToGamma(float2 data, float3 bc1, float3 bc2, float invGamma)
{
    float3 linColor = Decode2ChannelColor(data, bc1, bc2);
    return pow(max(linColor, (float3)0), invGamma);
}

float3 Decode2ChannelColorExact(float2 data, float3 bc1, float3 bc2, float invGamma)
{
    float3 linColor = Decode2ChannelColor(data, bc1, bc2);
    return pow(max(linColor, (float3)0), invGamma);
}

float2 SampleEncodedRG(TEXTURE2D_PARAM(tex, samplerTex), float2 uv)
{
    return SAMPLE_TEXTURE2D(tex, samplerTex, uv).rg;
}

float2 SampleEncodedBA(TEXTURE2D_PARAM(tex, samplerTex), float2 uv)
{
    return SAMPLE_TEXTURE2D(tex, samplerTex, uv).ba;
}

#endif