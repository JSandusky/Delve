
//128 -> 64 -> 32 -> 16 -> 4 -> 2 -> 1
    float GetMipFromRoughness(float roughness)
    {
        return ((roughness) * 7.0);// - pow(roughness, 7.0) * 1.5;
    }
    
    float3 EnvBRDFApprox (float3 specColor, float roughness, float ndv)
    {
        const float4 c0 = float4(-1, -0.0275, -0.572, 0.022 );
        const float4 c1 = float4(1, 0.0425, 1.0, -0.04 );
        float4 r = roughness * c0 + c1;
        float a004 = min( r.x * r.x, exp2( -9.28 * ndv ) ) * r.x + r.y;
        float2 AB = float2( -1.04, 1.04 ) * a004 + r.zw;
        return specColor * AB.x + AB.y;
    }
    
    float3 FixCubeLookup(float3 v) 
    {
        float M = max(max(abs(v.x), abs(v.y)), abs(v.z));
        float scale = (128 - 1) / 128;
        
        if (abs(v.x) != M) v.x += scale;
        if (abs(v.y) != M) v.y += scale;
        if (abs(v.z) != M) v.z += scale; 

        return v;
    }
    
    float2 GetIBLBrdf(in float ndv, in float roughness)
    {
        return (tex2D(IBLLUTSampler, float2(ndv, 1.0 - roughness))).xy;
    }
    
    /// Calculate IBL contributation
    ///     reflectVec: reflection vector for cube sampling
    ///     wsNormal: surface normal in word space
    ///     toCamera: normalized direction from surface point to camera
    ///     roughness: surface roughness
    ///     ambientOcclusion: ambient occlusion
    float3 ImageBasedLighting(in float3 reflectVec, in float3 wsNormal, in float3 toCamera, in float3 diffColor, in float3 specColor, in float roughness, inout float3 reflectionCubeColor)
    { 
        //reflectVec = GetSpecularDominantDir(wsNormal, reflectVec, roughness);
        const float ndv = abs(dot(-toCamera, wsNormal)) + 0.001;

        /// Test: Parallax correction, currently not working

        // float3 intersectMax = (cZoneMax - toCamera) / reflectVec;
        // float3 intersectMin = (cZoneMin - toCamera) / reflectVec;
        
        // float3 furthestPlane = max(intersectMax, intersectMin);
        
        // float planeDistance = min(min(furthestPlane.x, furthestPlane.y), furthestPlane.z);

        // // Get the intersection position
        // float3 intersectionPos = toCamera + reflectVec * planeDistance;
        // // Get corrected reflection
        // reflectVec = intersectionPos - ((cZoneMin + cZoneMax )/ 2);

        const float mipSelect = GetMipFromRoughness(roughness);
        float3 cube =  (texCUBElod(IBLSampler, float4(FixCubeLookup(reflectVec), mipSelect)).rgb);
        float3 cubeD = (texCUBElod(IBLSampler, float4(FixCubeLookup(wsNormal), 7.0)).rgb);
        
        const float3 Hn = normalize(toCamera + reflectVec);
        const float vdh = clamp((dot(toCamera, Hn)), M_EPSILON, 1.0);
        const float3 fresnelTerm = max(Fresnel(cube, vdh, vdh) * specColor, 0);
        
        const float2 brdf = GetIBLBrdf(ndv, roughness);
        const float3 environmentSpecular = (specColor * brdf.x + brdf.y) * cube;
        const float3 environmentDiffuse = cubeD * diffColor;
        
        return environmentDiffuse + environmentSpecular + fresnelTerm;
    }