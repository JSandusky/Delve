#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

matrix WorldViewProjection;
matrix InverseWorldView;
matrix Transform;

Texture2D MatCapTex;
sampler2D MatCapSampler = sampler_state
{
    Texture = <MatCapTex>;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Normal : NORMAL0;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float3 Normal : TEXCOORD0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(mul(input.Position, Transform), WorldViewProjection);
	output.Normal = normalize(mul(input.Normal, (float3x3)Transform));

	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    // "normal" is the unmodified input normal
    float2 matCapCoord = ((mul((float3x3)InverseWorldView, input.Normal) + 1) * 0.5).xy;
    matCapCoord.y = 1 - matCapCoord.y; // Depending on our environment we need to invert the y coordinate
    matCapCoord.x = 1 - matCapCoord.x; // flip handedness
    float4 color = tex2D(MatCapSampler, matCapCoord);
	return color;
}

technique Main
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};