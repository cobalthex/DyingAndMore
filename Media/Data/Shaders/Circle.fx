float4x4 Transform;

struct Output
{
	float4 position : POSITION;
	float4 color    : COLOR0;
	float2 texcoord : TEXCOORD0;
};

Output vmain(float4 position : POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0)
{
	Output output;
	output.position = mul(position, Transform);
	output.color = color;
	output.texcoord = uv;

	return output;
}

float4 pmain(float2 uv : TEXCOORD0, float4 color : COLOR0) : SV_Target
{
	uv = uv * 2 - 1;
	return float4(dot(uv, uv) / 10, 1, 1, 1);
	return (dot(uv, uv) <= 1 /*&& dot(uv, uv) >= 0.5*/) ? color : 0;
}

technique Technique1
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 vmain();
		PixelShader = compile ps_4_0 pmain();
	}
}