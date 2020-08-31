float4x4 Transform;

Texture2D SDF;

SamplerState Sampler
{
    Filter = Linear;
    AddressU = Clamp;
    AddressU = Clamp;
};

struct Vertex
{
    float4 position         : POSITION0;
    float4 color            : COLOR0;
    float2 texcoord         : TEXCOORD0;
    float4 outlineColor     : COLOR1;
    float  outlineThickness : TEXCOORD1;
};

Vertex vmain(Vertex vertex)
{
    vertex.position = mul(vertex.position, Transform);
    return vertex;
}

float median(float3 rgb)
{
    return max(min(rgb.r, rgb.g), min(max(rgb.r, rgb.g), rgb.b));
}

float4 pmain(
    float4 position         : SV_POSITION, 
    float4 color            : COLOR0, 
    float2 texcoord         : TEXCOORD0,
    float4 outlineColor     : COLOR1,
    float  outlineThickness : TEXCOORD1
) : SV_Target
{
    float3 sample = SDF.Sample(Sampler, texcoord).rgb;

    float3 sz;
    SDF.GetDimensions(0, sz.x, sz.y, sz.z);

    float dx = ddx(texcoord.x) * sz.x;
    float dy = ddy(texcoord.y) * sz.y;
    
    float toPixels = /*8 * */rsqrt(dx * dx + dy * dy); //smoothing factor doesnt seem to affect quality (need more testing)
    float sigDist = median(sample);

    float w = fwidth(sigDist);
    
    float cmin = 0.5, cmax = 0.5;
    
    //hinting
    //if (dx > 3)
    //{
    //    cmin = 0.3;
    //    cmax = 0.7;
    //    outlineThickness /= 2;
    //}
    //    return float4(color.rgb, smoothstep(0.4 - w, 0.6 + w, sigDist));
    
    float centerToOutline = smoothstep(cmin - w, cmax + w, sigDist);
    
    //float outlineThickness = 0;
    if (outlineThickness == 0)
        return float4(color.rgb, centerToOutline * color.a);
        
    float centerEdge = cmin - outlineThickness / 2;
    
    float outlineToEdge = smoothstep(centerEdge - w, centerEdge + w, sigDist);
    
    float4 mix = lerp(outlineColor, color, centerToOutline);
    return float4(mix.rgb, outlineToEdge * mix.a);
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
