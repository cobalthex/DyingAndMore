Texture2D Tex : register(t0);
SamplerState Sampler;

float3x3 Filter;

float4 pmain(float4 position : SV_POSITION, float4 color : COLOR0, float2 texcoord : TEXCOORD0) : SV_Target
{
    uint2 tsize;
    Tex.GetDimensions(tsize.x, tsize.y);
    float2 inc = 1.0 / tsize;
    float2 off = texcoord - inc;

    float4 sum = 0;
    for (int y = 0; y < 3; ++y)
        for (int x = 0; x < 3; ++x)
            sum += Filter[y][x] * Tex.Sample(Sampler, float2(x, y) * inc + off);

	return color * (sum / 9);
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_4_0 pmain();
    }
}