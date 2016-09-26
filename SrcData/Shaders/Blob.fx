sampler2D Tex;
sampler2D Mask; //the reflection mask that dictates how the reflection is to be drawn (distortion and reflectivity)
sampler2D Reflection; //the texture to reflect

float Reflectivity = 0.25;
float Depth = 0.03;

float4 main(float4 pos : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : SV_Target
{
    float4 px = tex2D(Tex, uv);
    float4 mask = tex2D(Mask, uv);
    mask.xyz = (mask.xyz * 2) - 1;

    float4 refl = tex2D(Reflection, uv + mask.xy * Depth);
    refl.a *= mask.a * Reflectivity;

    if (px.a > 0.5) //todo: replace with sdf
    {
        px.rgb = (px.rgb * (1 - refl.a)) + (refl.rgb * refl.a);
        return px * color; 
    }
    return 0;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_4_0 main();
    }
}