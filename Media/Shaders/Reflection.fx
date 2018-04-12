Texture2D Tex;
Texture2D Reflection;
SamplerState Sampler;

struct PSOutput
{
    float4 color : COLOR0;
    float4 reflection : COLOR1;
};

PSOutput pmain(float4 position : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0)
{
    PSOutput output;
    output.color = Tex.Sample(Sampler, uv) * color;
    output.reflection = Reflection.Sample(Sampler, uv);
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