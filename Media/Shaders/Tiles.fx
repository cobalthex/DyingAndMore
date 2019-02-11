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
Texture2D<uint> TilesLayout : register(t1);

SamplerState Sampler;

int TilesPerRow;
float2 TileSize;
float2 MapSize;

SamplerState LayoutSampler
{
    Filter = Anisotropic;
    AddressU = Clamp;
    AddressV = Clamp;
};

float4 pmain(float4 position : SV_Position, float4 color : COLOR0, float2 rpos : TEXCOORD0) : SV_Target
{
    uint2 tsize;
    TilesLayout.GetDimensions(tsize.x, tsize.y);

    //surface format color (unorm)
    //float4 tilec = TilesLayout.Load(uint3(rpos * MapSize, 0));
    //uint tile = ((uint)(tilec.a * 255) << 24) + ((uint)(tilec.b * 255) << 16) + ((uint)(tilec.g * 255) << 8) + ((uint)(tilec.r * 255) << 0);

    uint tile = TilesLayout.Load(uint3(rpos * MapSize, 0));

    if (tile == 0xffffffff)
        discard;

    TilesImage.GetDimensions(tsize.x, tsize.y);
    float2 tdiv = tsize / TileSize;
    float2 cell = float2(tile % TilesPerRow, tile / TilesPerRow);
    float2 local = (rpos % (1 / MapSize)) * MapSize;

    return color * TilesImage.Sample(Sampler, (cell + local) / tdiv);
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_4_0 vmain();
        PixelShader = compile ps_4_0 pmain();
    }
}