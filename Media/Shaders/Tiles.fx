float4x4 Transform;

struct Output
{
    float4 position : SV_POSITION;
    float4 color    : COLOR0;
    float2 texcoord : TEXCOORD0;
};

Output vmain(float4 position : POSITION, float4 color : COLOR0, float2 texcoord : TEXCOORD0)
{
    Output output;
    output.position = mul(position, Transform);
    output.color = color;
    output.texcoord = texcoord;

    return output;
}

//todo: overlay color (heuristic)

Texture2D TilesImage : register(t0);
Texture2D TilesLayout : register(t1);
SamplerState Sampler;

int TilesPerRow;
float2 TileSize;
float2 MapSize;

SamplerState LayoutSampler
{
    Filter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

float4 pmain(float4 position : SV_Position, float4 color : COLOR0, float2 rpos : TEXCOORD0) : SV_Target
{
    uint2 tsize;
    TilesLayout.GetDimensions(tsize.x, tsize.y);

	float4 tilec = TilesLayout.Sample(LayoutSampler, rpos * (MapSize / tsize));
	uint tile = ((uint)(tilec.a * 255) << 24) + ((uint)(tilec.b * 255) << 16) + ((uint)(tilec.g * 255) << 8) + ((uint)(tilec.r * 255) << 0);
	//uint4 tilec = TilesLayout.Load(uint3(rpos * MapSize, 0));
	//uint tile = (tilec.a << 24) + (tilec.b << 16) + (tilec.g << 8) + (tilec.r << 0);
    if (tile >= 0xffff)
        return float4(0.5, 0.3, 0.2, 1); // discard;

    float2 cell = float2(tile % TilesPerRow, tile / TilesPerRow) / (tsize / TileSize);
    TilesImage.GetDimensions(tsize.x, tsize.y);
    float2 local = (rpos % (1 / MapSize)) * MapSize;

    return color * TilesImage.Sample(Sampler, (cell + local));
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_4_0 vmain();
        PixelShader = compile ps_4_0 pmain();
    }
}