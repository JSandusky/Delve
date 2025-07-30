#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 offsets[] = {
        float2(-1,0),
        float2(1, 0),
        float2(0, -1),
        float2(0, 1),
        float2(-0.5,-0.5),
        float2(0.5,0.5),
        float2(-0.5,0.5),
        float2(0.5,-0.5)
    };

	float4 a = tex2D(SpriteTextureSampler,input.TextureCoordinates);
    //for (int i = 0; i < 8; ++i)
    //{
    //    float4 c = tex2D(SpriteTextureSampler,input.TextureCoordinates + offsets[i]*0.001);
    //    a = min(a, c);
    //}
    
    for (int i = 0; i < 8; ++i)
    {
        float4 c = tex2D(SpriteTextureSampler,input.TextureCoordinates + offsets[i]*0.0025);
        a = min(a, c);
    }
    
    return a;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};