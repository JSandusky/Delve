
Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
    AddressU = Clamp;
    AddressV = Clamp;
};


float3 randomNormal(float2 uv)
{
	float noiseX = (frac(sin(dot(uv, float2(15.8989f, 76.132f) * 1.0f)) * 46336.23745f));
	float noiseY = (frac(sin(dot(uv, float2(11.9899f, 62.223f) * 2.0f)) * 34748.34744f));
	float noiseZ = (frac(sin(dot(uv, float2(13.3238f, 63.122f) * 3.0f)) * 59998.47362f));
	return normalize(float3(noiseX, noiseY, noiseZ));
}

float3 normal_from_depth(float depth, float2 texcoords) 
{
  const float2 offset1 = float2(0.0,0.001);
  const float2 offset2 = float2(0.001,0.0);
  
  float depth1 = tex2D(SpriteTextureSampler, texcoords + offset1).r;
  float depth2 = tex2D(SpriteTextureSampler, texcoords + offset2).r;
  
  float3 p1 = float3(offset1, depth1 - depth);
  float3 p2 = float3(offset2, depth2 - depth);
  
  float3 normal = cross(p1, p2);
  normal.z = -normal.z;
  
  return normalize(normal);
}

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderInput
{
    float3 Position : POSITION;
    float4 Color : COLOR0;
    float2 UV : TEXCOORD0;
};

VertexShaderOutput VS(VertexShaderInput In)
{
    VertexShaderOutput output;
    
	output.Position = float4(In.Position, 1);
    output.Color = In.Color;
	output.TextureCoordinates = In.UV;
    
    return output;
}

float4 PS(VertexShaderOutput In) : SV_Target0
{   
  const float total_strength = 1.0;
  const float base = 0.2;
  
  const float area = 0.0075;
  const float falloff = 0.00001;
  
  const float radius = 0.02;
  
  const int samples = 16;
  float3 sample_sphere[16] = {
      float3( 0.5381, 0.1856,-0.4319), float3( 0.1379, 0.2486, 0.4430),
      float3( 0.3371, 0.5679,-0.0057), float3(-0.6999,-0.0451,-0.0019),
      float3( 0.0689,-0.1598,-0.8547), float3( 0.0560, 0.0069,-0.1843),
      float3(-0.0146, 0.1402, 0.0762), float3( 0.0100,-0.1924,-0.0344),
      float3(-0.3577,-0.5301,-0.4358), float3(-0.3169, 0.1063, 0.0158),
      float3( 0.0103,-0.5869, 0.0046), float3(-0.0897,-0.4940, 0.3287),
      float3( 0.7119,-0.0154,-0.0918), float3(-0.0533, 0.0596,-0.5411),
      float3( 0.0352,-0.0631, 0.5460), float3(-0.4776, 0.2847,-0.0271)
  };
  
  //float3 random = randomNormal(In.TextureCoordinates * 4);
  
  float depth = tex2D(SpriteTextureSampler, In.TextureCoordinates).r;
 
  float3 position = float3(In.TextureCoordinates, depth);
  //float3 normal = normal_from_depth(depth, In.TextureCoordinates);
  
  float radius_depth = radius/depth;
  float occlusion = 0.0;
  for(int i = 0; i < samples; i++) 
  {
  
    float3 ray = radius_depth * sample_sphere[i];//-reflect(sample_sphere[i], random);
    float3 hemi_ray = position + ray;//sign(dot(ray,normal)) * ray;
    
    float occ_depth = tex2D(SpriteTextureSampler, saturate(hemi_ray.xy)).r;
    float difference = depth - occ_depth;    
    occlusion += step(falloff, difference) * (1 - smoothstep(falloff, area, difference));
  }
  
  float ao = 1.0 - total_strength * occlusion * (1.0 / samples);
  return saturate(ao + base);
}

technique SSAO
{
    pass P0
    {
        PixelShader = compile ps_4_0 PS();
    }
}