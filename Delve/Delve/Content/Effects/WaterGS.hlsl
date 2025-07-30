
cbuffer ShaderData : register(b0)
{
    matrix WorldViewProjection;
    float3 LightDirection;
    float3 LightColor;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
    float4 OrignalPos : TEXCOORD0;
	float4 Color : COLOR0;
};

[maxvertexcount(3)]
void MainGS(triangle in VertexShaderOutput vertexData[3], inout TriangleStream<VertexShaderOutput> triStream)
{
    float3 aToB = normalize(vertexData[1].OrignalPos.xyz - vertexData[0].OrignalPos.xyz);
    float3 aToC = normalize(vertexData[2].OrignalPos.xyz - vertexData[0].OrignalPos.xyz);
    float3 vertN = normalize(cross(aToB, aToC));
    
    float attenuation = abs(dot(-LightDirection, vertN));
    float3 lightValue = attenuation * LightColor;
    
    vertexData[0].Color.rgb = vertexData[0].Color.rgb * lightValue;
    vertexData[1].Color.rgb = vertexData[1].Color.rgb * lightValue;
    vertexData[2].Color.rgb = vertexData[2].Color.rgb * lightValue;
    
    triStream.Append(vertexData[0]);
    triStream.Append(vertexData[1]);
    triStream.Append(vertexData[2]);
}