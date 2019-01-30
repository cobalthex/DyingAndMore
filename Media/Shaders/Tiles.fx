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
float2 TileUVScale; // tileSize / tilesImage.Size

float4 pmain(float4 position : SV_Position, float4 color : COLOR0, float2 rpos : TEXCOORD0) : SV_Target
{
    uint2 tsize;
    TilesLayout.GetDimensions(tsize.x, tsize.y);
        
    uint2 rel = uint2(rpos * tsize);
    float2 local = rpos % TileUVScale;
    
    uint4 tilecol = TilesLayout.Load(int3(rel, 0));
    uint tile = (tilecol.r << 24) + (tilecol.g << 16) + (tilecol.b << 8) + (tilecol.a << 0);
    if (tile == 0xffffffff)
        return float4(0.8, 0.2, 0.2, 1);
    return float4(0.5, tile / 98, 0, 1);

    float2 texcoord = uint2(
        ((uint)tile % TilesPerRow),
        ((uint)tile / TilesPerRow)
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