
//Returns a normal from a grid of heights
float3 ComputeHeightNormals(float hA, float hB, float hC, float hD, float hN, float heightScale)
{
    //To make it easier we offset the points such that n is "0" height
    float3 va = { 0, 1, (hA - hN)*heightScale };
    float3 vb = { 1, 0, (hB - hN)*heightScale };
    float3 vc = { 0, -1, (hC - hN)*heightScale };
    float3 vd = { -1, 0, (hD - hN)*heightScale };
    //cross products of each vector yields the normal of each tri - return the average normal of all 4 tris
    float3 average_n = ( cross(va, vb) + cross(vb, vc) + cross(vc, vd) + cross(vd, va) ) / -4;
    return normalize( average_n );
}

float3 SampleHeightNormal(sampler2D samp, float2 texCoord, float stepSize, float heightScale)
{
    float center = tex2D(samp, texCoord).r;
    float a = tex2D(samp, texCoord + float2(0, -stepSize)).r;
    float b = tex2D(samp, texCoord + float2(stepSize, 0)).r;
    float c = tex2D(samp, texCoord + float2(0, stepSize)).r;
    float d = tex2D(samp, texCoord + float2(-stepSize, 0)).r;
    return ComputeHeightNormals(a, b, c, d, center, heightScale);
}

float3x3 MatrixInverse(float3x3 inMatrix)
{  
   float det = dot(cross(inMatrix[0], inMatrix[1]), inMatrix[2]);
   float3x3 T = transpose(inMatrix);
   return float3x3(cross(T[1], T[2]),
               cross(T[2], T[0]),
               cross(T[0], T[1])) / det;
}


// http://www.thetenthplanet.de/archives/1180
float3x3 cotangent_frame(float3 N, float3 p, float2 uv)
{
    // get edge vectors of the pixel triangle
    float3 dp1 = ddx( p );
    float3 dp2 = ddy( p );
    float2 duv1 = ddx( uv );
    float2 duv2 = ddy( uv );
 
    // solve the linear system
    float3 dp2perp = cross( dp2, N );
    float3 dp1perp = cross( N, dp1 );
//    float3 T = dp2perp * duv1.x + dp1perp * duv2.x;
//    float3 B = dp2perp * duv1.y + dp1perp * duv2.y;
 
    float3x3 M = float3x3(dp1perp, dp2perp, cross(dp1perp, dp2perp));
    float3x3 inverseM = MatrixInverse(M);
    
    float3 T = mul(inverseM, float3(duv1.x, duv2.x, 0.0));
    float3 B = mul(inverseM, float3(duv1.y, duv2.y, 0.0));
   
    // construct tangent frame  
    float maxLength = max(length(T), length(B));
    T = T / maxLength;
    B = B / maxLength;
    return  float3x3(T, B, N);
 
    // construct a scale-invariant frame 
    //float invmax = rsqrt( max( dot(T,T), dot(B,B) ) );
    //return float3x3(T * invmax, B * invmax, normalize(cross(T, B)) );
}

float3 perturb_normal(float3 N, float3 V, sampler2D normalMap, float2 texcoord)
{
    // assume N, the interpolated vertex normal and 
    // V, the view vector (vertex to eye)
    float3 map = tex2D(normalMap, texcoord).xyz;
    map = map * 2.0 - 0.5;
    float3x3 TBN = cotangent_frame(N, -V, texcoord);
    return normalize(mul(TBN, map));
}