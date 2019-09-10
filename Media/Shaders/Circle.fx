float4x4 Transform;
float Thickness = 3;

float SegmentLength = 0;// 15.7079633;
float SegmentOffset = 0;

const static float TWO_PI = 6.28318530718;

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
	float theta = atan2(polar.y, polar.x);
	if (polar.y < 0) //this shouldnt be necessary
		theta += 3.14159;

	float z = theta * input.radius;

	//circle thickness uniformity is dependent on w/h ratio

    float circleDist = abs(input.radius * (1 - dist) - Thickness); //1 here represents the relative radius of the circle inside the bounds of this shader. theres some formula to make thickness match up here
	float alpha = saturate(Thickness - circleDist);

	if (SegmentLength > 0)
		alpha *= ((theta * input.radius + SegmentOffset) % (SegmentLength * 2)) > SegmentLength;

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