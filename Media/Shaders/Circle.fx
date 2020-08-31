float4x4 Transform;

const static float TWO_PI = 6.28318530718;

struct PSInput
{
	float4 position						    : POSITION0;
	nointerpolation float2 radiusThickness  : TEXCOORD1;
	nointerpolation float2 dashLengthOffset : TEXCOORD2;
	float4 color						    : COLOR0;
	float2 texcoord					        : TEXCOORD0;
};

PSInput vmain(
	float2 position		    : POSITION0,
	float2 radiusThickness  : TEXCOORD1,
	float2 dashLengthOffset : TEXCOORD2,
	float4 color            : COLOR0,
	float2 texcoord         : TEXCOORD0
)
{
	PSInput output;
	output.position = mul(float4(position.xy, 0, 1), Transform);
	output.color = color;
	output.texcoord = texcoord;
	output.radiusThickness = radiusThickness;
    //output.radiusThickness.y *= length(float3(Transform[2][0], Transform[2][1], Transform[2][2])); //[Z coordinate == radius] * diagonal size of screen (IIRC)
    output.dashLengthOffset = dashLengthOffset;

	return output;
}

float4 pmain(PSInput input) : SV_Target
{
    float2 polar = input.texcoord * 2 - 1;
    float dist = dot(polar, polar);
	float theta = atan2(polar.y, polar.x);
	if (polar.y < 0) //this shouldnt be necessary
		theta += 3.141592653589793;

	float z = theta * input.radiusThickness.x;

	//circle thickness uniformity is dependent on w/h ratio

	//blurred edges when highly zoomed
    float circleDist = abs(input.radiusThickness.x * (1 - dist) - input.radiusThickness.y); //1 here represents the relative radius of the circle inside the bounds of this shader. theres some formula to make thickness match up here
	float alpha = saturate(input.radiusThickness.y - circleDist);

	if (input.dashLengthOffset.x > 0)
		alpha *= ((theta * input.radiusThickness.x + input.dashLengthOffset.y) % (input.dashLengthOffset.x * 2)) > input.dashLengthOffset.x;

    return float4(input.color.rgb, input.color.a * alpha);
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