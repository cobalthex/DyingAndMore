sampler2D Tex : register(S0);

float2 TexNormSize; // 1 / size
float2 FrameSize;

float Adjacent(float2 UV, float2 Distance)
{
	float4 nw = tex2D(Tex, UV + float2(-Distance.x, -Distance.y));
	float4 se = tex2D(Tex, UV + float2(Distance.x, Distance.y));
	float4 ne = tex2D(Tex, UV + float2(Distance.x, -Distance.y));
	float4 sw = tex2D(Tex, UV + float2(-Distance.x, Distance.y));

	return ne.a * se.a * nw.a * sw.a;
}


float offset[] = { 0.0, 1.3846153846, 3.2307692308 };
float weight[] = { 0.2270270270, 0.3162162162, 0.0702702703 };

float4 main(float4 pos : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : SV_Target
{
    /*float4 tex = tex2D(Tex, uv / 1024.0) * weight[0];

    for (int i = 1; i < 3; ++i)
    {
        tex += tex2D(Tex, (uv + float2(0.0, offset[i])) / 1024.0) * weight[i];
        tex += tex2D(Tex, (uv - float2(0.0, offset[i])) / 1024.0) * weight[i];
    }

    return tex;
*/
    //deprecated

    float4 px = tex2D(Tex, uv);
    if (px.a == 0)
        return px;

	float d1 = Adjacent(uv, TexNormSize);

    if (d1 < 1)
        return lerp(color, px, d1);
    return px;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_4_0 main();
    }
}