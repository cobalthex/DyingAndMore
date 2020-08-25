float4x4 Transform;

const static float TWO_PI = 6.28318530718;

struct PSInput
{
	float4 position  : POSITION0;
	nointerpolation float radius     : POSITION1;
	nointerpolation float thickness  : POSITION2;
	nointerpolation float dashLength : POSITION3;
	nointerpolation float dashOffset : POSITION4;
	float4 color    : COLOR0;
	float2 texcoord : TEXCOORD0;
};

PSInput vmain(
	float2 position  : POSITION0,
	float radius	  : POSITION1,
	float thickness  : POSITION2,
	float dashLength : POSITION3,
	float dashOffset : POSITION4,
	float4 color     : COLOR0,
	float2 texcoord  : TEXCOORD0
)
{
	PSInput output;
	output.position = mul(float4(position.xy, 0, 1), Transform);
	output.color = color;
	output.texcoord = texcoord;
	output.thickness = thickness;
	output.dashLength = dashLength;
	output.dashOffset = dashOffset;
    output.radius = radius * length(float3(Transform[2][0], Transform[2][1], Transform[2][2])); //[Z coordinate == radius] * diagonal size of screen (IIRC)

	return output;
}

float4 pmain(PSInput input) : SV_Target
{
    float2 polar = input.texcoord * 2 - 1;
    float dist = dot(polar, polar);
	float theta = atan2(polar.y, polar.x);
	if (polar.y < 0) //this shouldnt be necessary
		theta += 3.141592653589793;

	float z = theta * input.radius;

	//circle thickness uniformity is dependent on w/h ratio

    float circleDist = abs(input.radius * (1 - dist) - input.thickness); //1 here represents the relative radius of the circle inside the bounds of this shader. theres some formula to make thickness match up here
	float alpha = saturate(input.thickness - circleDist);

	if (input.dashLength > 0)
		alpha *= ((theta * input.radius + input.dashOffset) % (input.dashLength * 2)) > input.dashLength;

    return float4(input.color.rgb, input.color.a * alpha);
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