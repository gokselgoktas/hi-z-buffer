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

Texture2D _Temporary;
SamplerState sampler_Temporary;

float4 _MainTex_TexelSize;

Varyings vertex(in Input input)
{
    Varyings output;

#ifndef HI_Z_BLIT_FALL_THROUGH
    output.vertex = UnityObjectToClipPos(input.vertex.xyz);
#else
    output.vertex = input.vertex;
#endif

    output.uv = input.uv;

#if UNITY_UV_STARTS_AT_TOP
    if (_MainTex_TexelSize.y < 0)
        output.uv.y = 1. - input.uv.y;
#endif

    return output;
}

float4 resolve(in Varyings input) : SV_Target
{
    float depth = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, input.uv).r;
    return 1. / (_ZBufferParams.x * depth + _ZBufferParams.y);
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

    return min(min(min(neighborhood.x, neighborhood.y), neighborhood.z), neighborhood.w);
}

float4 blit(in Varyings input) : SV_Target
{
    return _Temporary.Sample(sampler_Temporary, input.uv);
}

#endif
