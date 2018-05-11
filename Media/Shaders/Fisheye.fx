Texture2D Tex;
SamplerState Sampler;

float Fov = 2.1; //in Radians

float4 pmain(float4 position : SV_POSITION, float4 color : COLOR0, float2 texcoord : TEXCOORD0) : SV_Target
{
	texcoord -= 0.5;
	float z = sqrt(1.0 - texcoord.x * texcoord.x - texcoord.y * texcoord.y);
	float a = 1.0 / (z * tan(Fov * 0.5));
	//a = (z * tan((3.14159 - Fov) * 0.5)) / 1.0; //reverse lens
	return Tex.Sample(Sampler, (texcoord * a) + 0.5);
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_4_0 pmain();
    }
}