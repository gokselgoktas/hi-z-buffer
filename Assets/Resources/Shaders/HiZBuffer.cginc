#ifndef __HI_Z__
#define __HI_Z__

#include "UnityCG.cginc"

struct Input
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
};

Texture2D _MainTex;
SamplerState sampler_MainTex;

Texture2D _CameraDepthTexture;
SamplerState sampler_CameraDepthTexture;

float4 _MainTex_TexelSize;

Varyings vertex(in Input input)
{
    Varyings output;

    output.vertex = UnityObjectToClipPos(input.vertex.xyz);
    output.uv = input.uv;

#if UNITY_UV_STARTS_AT_TOP
    if (_MainTex_TexelSize.y < 0)
        output.uv.y = 1. - input.uv.y;
#endif

    return output;
}

float4 resolve(in Varyings input) : SV_Target
{
    return _CameraDepthTexture.Sample(sampler_CameraDepthTexture, input.uv).r;
}

float4 reduce(in Varyings input) : SV_Target
{
#if SHADER_API_METAL
    int2 xy = (int2) (input.uv * (_MainTex_TexelSize.zw - 1.));

    float4 neighborhood = float4(
        _MainTex.mips[0][xy].r, _MainTex.mips[0][xy + int2(1, 0)].r,
        _MainTex.mips[0][xy + int2(0, 1)].r, _MainTex.mips[0][xy + 1].r);
#else
    float4 neighborhood = _MainTex.Gather(sampler_MainTex, input.uv);
#endif

    return max(max(max(neighborhood.x, neighborhood.y), neighborhood.z), neighborhood.w);
}

#endif
