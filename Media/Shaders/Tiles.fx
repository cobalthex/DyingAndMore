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
#if OPENGL
Texture2D<float> TilesLayout : register(t1);
#else
Texture2D<uint> TilesLayout : register(t1);
#endif
Texture2D TileSDF : register(t2);
float2 TilesImageSize;

SamplerState Sampler;

int TilesPerRow; //in tiles
float2 TileSize; //in pixels
float2 MapSize; //in tiles
float2 SDFScale; //fraction

SamplerState LayoutSampler
{
    Filter = Anisotropic;
    AddressU = Clamp;
    AddressV = Clamp;
};

float4 pmain(float4 position : SV_Position, float4 color : COLOR0, float2 rpos : TEXCOORD0) : SV_Target
{
#if OPENGL
    //sm3.0 is shit
    float4 tilec = TilesLayout.Sample(LayoutSampler, rpos);
    int tile = ((int)(tilec.b * 255) * 65536) + ((int)(tilec.g * 255) * 255) + ((int)(tilec.r * 255));
    // tile += (int)(tilec.a * 255) * 16777216);

    if (tile >= (int)0xffffff) //hack
        discard;
#else
    uint tile = TilesLayout.Load(uint3(rpos * MapSize, 0));
    if (tile == 0xffffffff)
        discard; //not sure why htis returns this value
#endif

    float2 tdiv = TilesImageSize / TileSize;
    float2 cell = float2(tile % TilesPerRow, tile / TilesPerRow);
    float2 local = (rpos % (1 / MapSize)) * MapSize;

    float4 ti = TilesImage.Sample(Sampler, (cell + local) / tdiv);

    //draw border around map
    //float sdf = TileSDF.Sample(LayoutSampler, rpos * SDFScale).a;
    //float cutoff = 1 / 255.0;
    //if (sdf < cutoff)
    //float alpha = smoothstep(sdf - cutoff, sdf + cutoff, sdf * 2);

    return color * ti;
}

#include "shadermodel.hlsli"
technique Technique1
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL vmain();
        PixelShader = compile PS_SHADERMODEL pmain();
    }
}