#ifndef TWO_CHANNEL_COLOR_DECODE_HLSL
#define TWO_CHANNEL_COLOR_DECODE_HLSL

static const half3 _LumWeights = half3(0.2126, 0.7152, 0.0722);

half3 Decode2ChannelColor(half2 data, half3 bc1, half3 bc2)
{
    half3 color = lerp(bc1, bc2, data.y);
    half colorLum = dot(color, _LumWeights);
    half targetLum = data.x * data.x;
    color *= targetLum * rcp(max(colorLum, 1e-6h));
    return color;
}

half3 Decode2ChannelColorToGamma(half2 data, half3 bc1, half3 bc2, half invGamma)
{
    half3 linColor = Decode2ChannelColor(data, bc1, bc2);
    return pow(max(linColor, (half3)0), invGamma);
}

half2 SampleEncodedRG(TEXTURE2D_PARAM(tex, samplerTex), half2 uv)
{
    return SAMPLE_TEXTURE2D(tex, samplerTex, uv).rg;
}

half2 SampleEncodedBA(TEXTURE2D_PARAM(tex, samplerTex), half2 uv)
{
    return SAMPLE_TEXTURE2D(tex, samplerTex, uv).ba;
}

#endif
