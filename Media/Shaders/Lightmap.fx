float4x4 Transform;

float4 pmain(float4 position : SV_POSITION, float4 color : COLOR0, float2 texcoord : TEXCOORD0) : SV_Target
{
	return color;
}

#include "shadermodel.fxh"
technique Technique1
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL pmain();
    }
}