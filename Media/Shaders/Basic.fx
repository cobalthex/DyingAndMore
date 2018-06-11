Texture2D Tex;
SamplerState Sampler;

float4x4 Transform;

struct Output
{
    float4 position : POSITION;
    float4 color    : COLOR0;
    float3 texcoord : TEXCOORD0; //texcoord z is fake depth (inverted) -- used for trails
};

Output vmain(float4 position : POSITION, float4 color : COLOR0, float3 texcoord : TEXCOORD0)
{
    Output output;
    output.position = mul(position, Transform);
    output.texcoord = texcoord;
    output.color = color;

    return output;
}

float4 pmain(float4 position : SV_POSITION, float4 color : COLOR0, float3 texcoord : TEXCOORD0) : SV_Target
{
	return color * Tex.Sample(Sampler, texcoord.xy / (1 - texcoord.z));
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_4_0 vmain();
        PixelShader = compile ps_4_0 pmain();
    }
}