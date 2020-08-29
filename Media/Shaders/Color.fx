float4x4 Transform;
float4 ColorMultiplier;

struct Output
{
    float4 position : POSITION;
    float4 color    : COLOR0;
};

Output vmain(float4 position : POSITION, float4 color : COLOR0)
{
    Output output;
    output.position = mul(position, Transform);
    output.color = color;

    return output;
}

float4 pmain(float4 position : SV_POSITION, float4 color : COLOR0) : SV_Target
{
    return color * ColorMultiplier;
}

#include "shadermodel.fxh"
technique Technique1
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL vmain();
        PixelShader = compile PS_SHADERMODEL pmain();
    }
}