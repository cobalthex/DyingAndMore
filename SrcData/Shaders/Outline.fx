sampler2D Tex : register(S0);

float2 TexNormSize; // 1 / size
float2 FrameSize;

float Adjacent(float2 UV, float2 Distance)
{
	float4 n = tex2D(Tex, UV + float2(0, -Distance.y));
	float4 s = tex2D(Tex, UV + float2(0, Distance.y));
	float4 e = tex2D(Tex, UV + float2(Distance.x, 0));
	float4 w = tex2D(Tex, UV + float2(-Distance.x, 0));

	return n.a * s.a * e.a * w.a;
}

float4 main(float4 pos : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : SV_Target
{
    float4 px = tex2D(Tex, uv);
    if (px.a == 0)
        return px;

	float d1 = Adjacent(uv, TexNormSize);

    if (d1 < 0.1)
        return color;
    return px;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_4_0 main();
    }
}