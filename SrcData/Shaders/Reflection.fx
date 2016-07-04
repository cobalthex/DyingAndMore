sampler2D Tex;
sampler2D Reflection;

struct PSOutput
{
    float4 color : COLOR0;
    float4 reflection : COLOR1;
};

PSOutput main(float4 pos : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0)
{
    PSOutput output;
    output.color = tex2D(Tex, uv) * color;
    output.reflection = tex2D(Reflection, uv);
    return output;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_5_0 main();
    }
}