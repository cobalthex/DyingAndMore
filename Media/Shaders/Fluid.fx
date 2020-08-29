Texture2D Tex;
Texture2D Mask; //the reflection mask that dictates how the reflection is to be drawn (distortion and reflectivity)
Texture2D Reflection; //the texture to reflect
SamplerState Sampler;

float Reflectivity = 0.25;
float Depth = 0.03;

float4 pmain(float4 position : SV_POSITION, float4 color : COLOR0, float2 texcoord : TEXCOORD0) : SV_Target
{
    float4 px = Tex.Sample(Sampler, texcoord);
    float4 mask = Mask.Sample(Sampler, texcoord);
    mask.x = (mask.x * 2) - 1;

    float4 refl = Reflection.Sample(Sampler, texcoord + mask.xy * Depth);
    refl.a *= mask.a * Reflectivity;

    if (px.a > 0.5) //todo: replace with sdf
    {
        px.rgb = (px.rgb * (1 - refl.a)) + (refl.rgb * refl.a);
        return px * float4(color.rgb, mask.y);
    }
    return 0;
}

#include "shadermodel.fxh"
technique Technique1
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL pmain();
    }
}