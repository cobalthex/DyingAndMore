Texture2D Tex;
Texture2D Mask;
SamplerState Sampler;
float Cutoff;

float4 pmain(float4 position : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : SV_Target
{
    float4 mask = Mask.Sample(Sampler, uv);
    if (mask.x <= Cutoff) //todo: replace with sdf
        return Tex.Sample(Sampler, uv) * color;
    return 0;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_4_0 pmain();
    }
}