Texture2D Tex;
Texture2D Reflection;
SamplerState Sampler;

struct PSOutput
{
    float4 color : COLOR0;
    float4 reflection : COLOR1;
};

PSOutput pmain(float4 position : SV_POSITION, float4 color : COLOR0, float2 texcoord : TEXCOORD0)
{
    PSOutput output;
    output.color = Tex.Sample(Sampler, texcoord) * color;
    output.reflection = Reflection.Sample(Sampler, texcoord);
    output.reflection.y = color.a;
    return output;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_4_0 pmain();
    }
}