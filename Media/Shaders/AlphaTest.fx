Texture2D Tex;
Texture2D Mask;
SamplerState Sampler;

float Cutoff;
float Range; //if 0, cutoff is all values below
//fade?

float4 pmain(float4 position : SV_POSITION, float4 color : COLOR0, float2 texcoord : TEXCOORD0) : SV_Target
{
    float4 mask = Mask.Sample(Sampler, texcoord);
    float4 col = 0;
     if ((Range == 0 && mask.r <= Cutoff) || //todo: replace with sdf
         (Cutoff - Range <= mask.r && Cutoff + Range >= mask.r))
    {
        col = Tex.Sample(Sampler, texcoord) * color;
        //col.a *= 1 - saturate((Cutoff - mask.r) / max(fwidth(mask.r), 0.0001));
    }
    return col;
}

#include "shadermodel.hlsli"
technique Technique1
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL pmain();
    }
}