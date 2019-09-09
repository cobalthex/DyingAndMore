float4x4 Transform;
float Thickness = 3;

struct PSInput
{
	float4 position : POSITION;
	float4 color    : COLOR0;
	float2 texcoord : TEXCOORD0;
    nointerpolation float radius : TEXCOORD1;
};

PSInput vmain(float4 position : POSITION, float4 color : COLOR0, float2 texcoord : TEXCOORD0)
{
	PSInput output;
	output.position = mul(float4(position.xy, 0, 1), Transform);
	output.color = color;
	output.texcoord = texcoord;
    output.radius = position.z * length(float3(Transform[2][0], Transform[2][1], Transform[2][2])); //[Z coordinate == radius] * diagonal size of screen (IIRC)

	return output;
}

float4 pmain(PSInput input) : SV_Target
{
    float2 polar = input.texcoord * 2 - 1;
    float dist = dot(polar, polar);

	//circle thickness uniformity is dependent on w/h ratio

    float circleDist = abs(1 - (Thickness / input.radius) - dist) * input.radius; //1 here represents the relative radius of the circle inside the bounds of this shader. theres some formula to make thickness match up here
	float alpha = saturate(Thickness - circleDist);
    return float4(input.color.rgb, input.color.a * alpha);
}

technique Technique1
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 vmain();
		PixelShader = compile ps_4_0 pmain();
	}
}