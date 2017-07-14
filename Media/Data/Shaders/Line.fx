float4x4 Transform;

struct Output
{
    float4 position : POSITION;
    float4 color    : COLOR0;
};

Output vmain(float4 Position : POSITION, float4 Color : COLOR0)
{
    Output output;
    output.position = mul(Position, Transform);
    output.color = Color;

    return output;
}

float4 pmain(float4 Position : SV_POSITION, float4 Color : COLOR0) : SV_Target
{
	return Color;
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_4_0 vmain();
        PixelShader = compile ps_4_0 pmain();
    }
}