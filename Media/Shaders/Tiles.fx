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
float2 TileUVScale; // tileSize / tilesImage.Size

float4 pmain(float4 position : SV_Position, float4 color : COLOR0, float2 rpos : TEXCOORD0) : SV_Target
{
    uint2 tsize;
    TilesLayout.GetDimensions(tsize.x, tsize.y);

    uint2 rel = uint2(rpos * tsize);
    float2 local = rpos % TileUVScale;

    int tile = (int)TilesLayout.Load(int3(rel, 0));
    if (tile >= 0xffff)
        discard;

    float2 texcoord = uint2(
        ((float)tile % TilesPerRow),
        ((float)tile / TilesPerRow)
        ) * TileUVScale;

    return color * TilesImage.Sample(Sampler, texcoord);
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_4_0 vmain();
        PixelShader = compile ps_4_0 pmain();
    }
}