float4 FogVector;
float3 FogColor;
float4x4 World;
float4x4 WorldViewProj;
float3 EyePosition;
float3x3 WorldInverseTranspose;
float3 LightDirection;
float3 LightDiffuseColor;
float3 LightSpecularColor;
float4 DiffuseColor;
float3 EmissiveColor;
float3 SpecularColor;
float SpecularPower;
float Time;
float WaveFactor;
float WaveLength;

// Vertex shader output structures.

struct VSOutput
{
    float4 Position : Position;
    float4 Color : COLOR;
    float4 PositionPS : SV_Position;
};

struct CommonVSOutput
{
    float4 Pos_ps;
    float4 Diffuse;
    float3 Specular;
};

struct LightColorPair
{
    float3 Diffuse;
    float3 Specular;
};

struct ColorPair
{
    float4 Diffuse;
    float3 Specular;
};

struct VSInput
{
    float4 Position : POSITION;
    float4 Color : COLOR;
};

struct GSInput
{
    float4 Position0 : POSITION0;
    float4 Color0 : COLOR0;

    float4 Position1 : POSITION1;
    float4 Color1 : COLOR1;

    float4 Position2 : POSITION2;
    float4 Color2 : COLOR2;
};

struct GSOutputPair
{
    float4 PositionPS : SV_Position;
    ColorPair Pair : Pair;
};

struct GSOutput
{
    float4 PositionPS0 : SV_Position0;
    ColorPair Pair0 : Pair0;

    float4 PositionPS1 : SV_Position1;
    ColorPair Pair1 : Pair1;

    float4 PositionPS2 : SV_Position2;
    ColorPair Pair2 : Pair2;

};

struct PSInput
{
    float4 PositionPS : SV_Position;
    ColorPair Pair : Pair;
};

LightColorPair ComputeLights(float3 eyeVector, float3 worldNormal, uniform int numLights)
{
    // float3x3 lightDirections = 0;
    // float3x3 lightDiffuse = 0;
    // float3x3 lightSpecular = 0;
    // float3x3 halfVectors = 0;
    
    //     lightDirections[0] = float3x3(LightDirection,     DirLight1Direction,     DirLight2Direction)[0];
    //     lightDiffuse[0]    = float3x3(DirLight1DiffuseColor,  DirLight1DiffuseColor,  DirLight2DiffuseColor)[0];
    //     lightSpecular[0]   = float3x3(DirLight0SpecularColor, DirLight1SpecularColor, DirLight2SpecularColor)[0];
        
    //     halfVectors[0] = normalize(eyeVector - lightDirections[0]);

    float3 lightDirection = LightDirection;
    float3 lightDiffuse = LightDiffuseColor;
    float3 lightSpecular = LightSpecularColor;
    float3 halfVectors = 0;
        
    halfVectors = normalize(eyeVector - lightDirection);

    float3 dotL = mul(-lightDirection, worldNormal);
    float3 dotH = mul(halfVectors, worldNormal);
    
    float3 zeroL = step(float3(0, 0, 0), dotL);

    float3 diffuse = zeroL * dotL;
    float3 specular = pow(max(dotH, 0) * zeroL, SpecularPower);

    LightColorPair result;
    
    result.Diffuse = mul(diffuse, lightDiffuse) * DiffuseColor.rgb + EmissiveColor;
    result.Specular = mul(specular, lightSpecular) * SpecularColor;

    return result;
}

float ComputeFogFactor(float4 position)
{
    return saturate(dot(position, FogVector));
}

void AddSpecular(inout float4 color, float3 specular)
{
    color.rgb += specular * color.a;
}

CommonVSOutput ComputeCommonVSOutputWithLighting(float4 position, float3 normal, uniform int numLights)
{
    CommonVSOutput vout;
    
    float4 pos_ws = mul(position, World);
    float3 eyeVector = normalize(EyePosition - pos_ws.xyz);
    float3 worldNormal = normalize(mul(normal, WorldInverseTranspose));

    LightColorPair lightResult = ComputeLights(eyeVector, worldNormal, numLights);
    
    vout.Pos_ps = mul(position, WorldViewProj);
    vout.Diffuse = float4(lightResult.Diffuse, DiffuseColor.a);
    vout.Specular = lightResult.Specular;
    
    return vout;
}

float generateOffset(float x, float z, float val1, float val2)
{
    float radiansX = ((((x + z * x * val1) % WaveLength) / WaveLength) + Time * ((x * 0.8f + z) % 1.5f)) * 6.283185307179586476925286766559f;
    float radiansZ = (((val2 * (z * x + x * z) % WaveLength) / WaveLength) + Time * 2.0f * (x % 2.0f)) * 6.283185307179586476925286766559f;
    return WaveFactor * 50 * (float) (sin(radiansZ) + cos(radiansX));
}

 
float3 applyDistortion(float3 vertex)
{
    float xDistortion = generateOffset(vertex.x, vertex.z, 0.2f, 0.1f);
    float yDistortion = generateOffset(vertex.x, vertex.z, 0.8f, 0.95f);
    float zDistortion = generateOffset(vertex.x, vertex.z, 0.15f, 0.2f);
    return vertex + float3(xDistortion * 1.5f, yDistortion * 1.5f, zDistortion * 1.5f);
}

[maxvertexcount(3)]
void GeometryShader_(triangle VSInput input[3], inout TriangleStream<GSOutputPair> outstream)
{
    float3 v1 = input[1].Position.xyz - input[0].Position.xyz;
    float3 v2 = input[2].Position.xyz - input[0].Position.xyz;
    float3 normal = cross(v1, v2);

    normalize(normal);

    GSOutputPair output;

    CommonVSOutput cout = ComputeCommonVSOutputWithLighting(input[0].Position, normal, 1);
    output.PositionPS = cout.Pos_ps;
    output.Pair.Diffuse = cout.Diffuse;
    output.Pair.Specular = cout.Specular;
    
    output.Pair.Diffuse *= input[0].Color;

    outstream.Append(output);

    cout = ComputeCommonVSOutputWithLighting(input[1].Position, normal, 1);
    output.PositionPS = cout.Pos_ps;
    output.Pair.Diffuse = cout.Diffuse;
    output.Pair.Specular = cout.Specular;
    
    output.Pair.Diffuse *= input[1].Color;

    outstream.Append(output);

    cout = ComputeCommonVSOutputWithLighting(input[2].Position, normal, 1);
    output.PositionPS = cout.Pos_ps;
    output.Pair.Diffuse = cout.Diffuse;
    output.Pair.Specular = cout.Specular;
    
    output.Pair.Diffuse *= input[2].Color;

    outstream.Append(output);
}

VSOutput VertexShader_(VSInput input)
{
    VSOutput r;
    r.Position = float4(applyDistortion(input.Position.xyz), input.Position.w);
    r.Color = input.Color;
    r.PositionPS = mul(input.Position, World);
    return r;
}

float4 PixelShader_(PSInput pin) : SV_Target
{
    float4 color = pin.Pair.Diffuse;

    AddSpecular(color, pin.Pair.Specular.rgb);
    
    return color;
}
