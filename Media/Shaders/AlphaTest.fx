Texture2D Tex;
Texture2D Mask;
SamplerState Sampler;

float Cutoff;
float Range; //if 0, cutoff is all values below
//fade?

float4 pmain(float4 position : SV_POSITION, float4 color : COLOR0, float2 uv : TEXCOORD0) : SV_Target
{
    float4 mask = Mask.Sample(Sampler, uv);
    if ((Range == 0 && mask.x <= Cutoff) || //todo: replace with sdf
    	(Cutoff - Range <= mask.x && Cutoff + Range >= mask.x))
        return Tex.Sample(Sampler, uv) * color;
    return 0;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_4_0 pmain();
    }
}