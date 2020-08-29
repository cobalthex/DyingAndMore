float4x4 Transform;

struct Output
{
    float4 position : SV_POSITION;
    float2 texcoord : TEXCOORD0;
};

Output vmain(float4 position : POSITION, float2 texcoord : TEXCOORD0)
{
    Output output;
    output.position = mul(position, Transform);
    output.texcoord = texcoord;

    return output;
}

Texture2D TilesImage : register(t0);
Texture2D<uint> TilesLayout : register(t1);
Texture2D<float> TileSDF : register(t2);
float2 TilesImageSize;

SamplerState Sampler;

int TilesPerRow;
float2 TileSize;
float2 MapSize;
float AlphaThreshold = 0.5;

SamplerState LayoutSampler
{
    Filter = Anisotropic;
    AddressU = Clamp;
    AddressV = Clamp;
};

float TileValueAt(uint tile, uint2 offset, float2 texcoord, float2 pixelFraction)
{
    float2 cell = float2(tile % TilesPerRow, tile / TilesPerRow) + offset;
    float2 local = (texcoord % (1 / MapSize)) * MapSize;

    return TilesImage.Sample(Sampler, (cell + local) / pixelFraction).a;
}

int4 pmain(float4 position : SV_Position, float2 texcoord : TEXCOORD0) : SV_Target
{
    //surface format color (unorm)
    //float4 tilec = TilesLayout.Load(uint3(texcoord * MapSize, 0));
    //uint tile = ((uint)(tilec.a * 255) << 24) + ((uint)(tilec.b * 255) << 16) + ((uint)(tilec.g * 255) << 8) + ((uint)(tilec.r * 255) << 0);

    return 0;

 //    uint tile = TilesLayout.Load(uint3(texcoord * MapSize, 0));

	// if (tile == 0xffffffff)
	// 	return 0;

 //    float2 pixelFraction = TilesImageSize / TileSize;

 //    float color = TileValueAt(tile, 0, texcoord, pixelFraction);

 //    return color;
}

#include "shadermodel.fxh"
technique Technique1
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL vmain();
        PixelShader = compile PS_SHADERMODEL pmain();
    }
}