Texture2D Tex : register(S0);
SamplerState Sampler;

float2 TexNormSize; // 1 / size
float2 FrameSize;

//switch to convolution filter?

float Adjacent(float2 texcoord, float2 distance)
{
	float4 nw = Tex.Sample(Sampler, texcoord + float2(-distance.x, -distance.y));
	float4 se = Tex.Sample(Sampler, texcoord + float2(distance.x, distance.y));
	float4 ne = Tex.Sample(Sampler, texcoord + float2(distance.x, -distance.y));
	float4 sw = Tex.Sample(Sampler, texcoord + float2(-distance.x, distance.y));

	return ne.a * se.a * nw.a * sw.a;
}

float offset[] = { 0.0, 1.3846153846, 3.2307692308 };
float weight[] = { 0.2270270270, 0.3162162162, 0.0702702703 };

float4 pmain(float4 position : SV_POSITION, float4 color : COLOR0, float2 texcoord : TEXCOORD0) : SV_Target
{
    /*float4 tex = tex2D(Tex, texcoord / 1024.0) * weight[0];

    for (int i = 1; i < 3; ++i)
    {
        tex += Tex.Sample(Sampler, (texcoord + float2(0.0, offset[i])) / 1024.0) * weight[i];
        tex += Tex.Sample(Sampler, (texcoord - float2(0.0, offset[i])) / 1024.0) * weight[i];
    }

    return tex;
*/
    //deprecated

    float4 px = Tex.Sample(Sampler, texcoord);
    if (px.a == 0)
        return px;

	float d1 = Adjacent(texcoord, TexNormSize);

    if (d1 < 1)
        return lerp(color, px, d1);
    return px;
}

#include "shadermodel.hlsli"
technique Technique1
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL pmain();
    }
}