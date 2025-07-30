
#define SPECULAR
#define M_PI 3.141596
#define M_EPSILON 0.0001
#define ROUGHNESS_FLOOR 0.004
#define METALNESS_FLOOR 0.03
#define GAMMA 2.2

//---------------------------------------------------------------------------------------
//  Utility Functions    
//---------------------------------------------------------------------------------------

    float4 FromGamma(float4 val)
    {
        return pow(val, 2.2);
    }
    
    float3 FromGamma(float3 val)
    {
        return pow(val, 2.2);
    }
    
    float4 ToGamma(float4 val)
    {
        return pow(val, 1.0/2.2);
    }
    
    float3 ToGamma(float3 val)
    {
        return pow(val, 1.0/2.2);
    }
    
    float FromGamma(float val)
    {
        return pow(val, 2.2);
    }

    float3 GetMetalnessSpecular(float4 diffColor, float metalness)
    {
        return lerp(0.04, diffColor.rgb, clamp(metalness, 0, 1.0));
    }
    
    float4 GetMetalnessDiffuse(float4 diffColor, float metalness, float roughness)
    {
        return float4(max(diffColor.rgb - diffColor.rgb * metalness, 0.04), diffColor.a);
    }

//---------------------------------------------------------------------------------------
//  Surface Functions    
//---------------------------------------------------------------------------------------

    //Schlick Fresnel
    //specular  = the rgb specular color value of the pixel
    //VdotH     = the dot product of the camera view direction and the half vector 
    float3 SchlickFresnel(float3 specular, float VdotH)
    {
        return specular + (float3(1.0, 1.0, 1.0) - specular) * pow(1.0 - VdotH, 5.0);
    }
    
    float3 SchlickFresnelCustom(float3 specular, float LdotH)
    {
        float ior = 0.25;
        float airIor = 1.000277;
        float f0 = (ior - airIor) / (ior + airIor);
        const float max_ior = 2.5;
        f0 = clamp(f0 * f0, 0.0, (max_ior - airIor) / (max_ior + airIor));
        return specular * (f0   + (1 - f0) * pow(2, (-5.55473 * LdotH - 6.98316) * LdotH));
    }
    
    //Get Fresnel
    //specular  = the rgb specular color value of the pixel
    //VdotH     = the dot product of the camera view direction and the half vector 
    float3 Fresnel(float3 specular, float VdotH, float LdotH)
    {
        //return SchlickFresnelCustom(specular, LdotH);
        return SchlickFresnel(specular, VdotH);
    }

    // Smith GGX corrected Visibility
    // NdotL        = the dot product of the normal and direction to the light
    // NdotV        = the dot product of the normal and the camera view direction
    // roughness    = the roughness of the pixel
    float SmithGGXSchlickVisibility(float NdotL, float NdotV, float roughness)
    {
        float rough2 = roughness * roughness;
        float lambdaV = NdotL  * sqrt((-NdotV * rough2 + NdotV) * NdotV + rough2);   
        float lambdaL = NdotV  * sqrt((-NdotL * rough2 + NdotL) * NdotL + rough2);
    
        return 0.5 / (lambdaV + lambdaL);
    }
    
    float NeumannVisibility(float NdotV, float NdotL) 
    {
        return NdotL * NdotV / max(1e-7, max(NdotL, NdotV));
    }
    
    // Get Visibility
    // NdotL        = the dot product of the normal and direction to the light
    // NdotV        = the dot product of the normal and the camera view direction
    // roughness    = the roughness of the pixel
    float Visibility(float NdotL, float NdotV, float roughness)
    {
        return NeumannVisibility(NdotV, NdotL);
        //return SmithGGXSchlickVisibility(NdotL, NdotV, roughness);
    }
    
    // GGX Distribution
    // NdotH        = the dot product of the normal and the half vector
    // roughness    = the roughness of the pixel
    float GGXDistribution(float NdotH, float roughness)
    {
        float rough2 = roughness * roughness;
        float tmp =  (NdotH * rough2 - NdotH) * NdotH + 1;
        return rough2 / (tmp * tmp);
    }
    
    // Get Distribution
    // NdotH        = the dot product of the normal and the half vector
    // roughness    = the roughness of the pixel
    float Distribution(float NdotH, float roughness)
    {
        return GGXDistribution(NdotH, roughness);
    }

    // Custom Lambertian Diffuse
    // diffuseColor = the rgb color value of the pixel
    // roughness    = the roughness of the pixel
    // NdotV        = the normal dot with the camera view direction
    // NdotL        = the normal dot with the light direction
    // VdotH        = the camera view direction dot with the half vector
    float3 CustomLambertianDiffuse(float3 diffuseColor, float NdotV, float roughness)
    {
        return diffuseColor * (1.0 / M_PI) * pow(NdotV, 0.5 + 0.3 * roughness);
    }
    
    // Burley Diffuse
    // diffuseColor = the rgb color value of the pixel
    // roughness    = the roughness of the pixel
    // NdotV        = the normal dot with the camera view direction
    // NdotL        = the normal dot with the light direction
    // VdotH        = the camera view direction dot with the half vector
    float3 BurleyDiffuse(float3 diffuseColor, float roughness, float NdotV, float NdotL, float VdotH)
    {
        const float energyBias = lerp(0, 0.5, roughness);
        const float energyFactor = lerp(1.0, 1.0 / 1.51, roughness);
        const float fd90 = energyBias + 2.0 * VdotH * VdotH * roughness;
        const float f0 = 1.0;
        const float lightScatter = f0 + (fd90 - f0) * pow(1.0f - NdotL, 5.0f);
        const float viewScatter = f0 + (fd90 - f0) * pow(1.0f - NdotV, 5.0f);

        return diffuseColor * lightScatter * viewScatter * energyFactor;
    }
    
    //Get Diffuse
    // diffuseColor = the rgb color value of the pixel
    // roughness    = the roughness of the pixel
    // NdotV        = the normal dot with the camera view direction
    // NdotL        = the normal dot with the light direction
    // VdotH        = the camera view direction dot with the half vector
    float3 Diffuse(float3 diffuseColor, float roughness, float NdotV, float NdotL, float VdotH)
    {
        //return LambertianDiffuse(diffuseColor);
        //return CustomLambertianDiffuse(diffuseColor, NdotV, roughness);
        return BurleyDiffuse(diffuseColor, roughness, NdotV, NdotL, VdotH);
    }
    
    
//---------------------------------------------------------------------------------------
//  Light Functions    
//---------------------------------------------------------------------------------------

    float3 GetSpecularDominantDir(float3 normal, float3 reflection, float roughness)
    {
        const float smoothness = 1.0 - roughness;
        const float lerpFactor = smoothness * (sqrt(smoothness) + roughness);
        return lerp(normal, reflection, lerpFactor);
    }
    
    //Return the PBR BRDF value
	// lightDir  = the vector to the light
	// lightVec  = normalised lightDir
	// toCamera  = vector to the camera
	// normal    = surface normal of the pixel
	// roughness = roughness of the pixel
	// diffColor = the rgb color of the pixel
	// specColor = the rgb specular color of the pixel
	float3 GetBRDF(float3 worldPos, float3 lightDir, float3 lightVec, float3 toCamera, float3 normal, float roughness, float3 diffColor, float3 specColor)
	{

        const float3 Hn = normalize(toCamera + lightDir);
        const float vdh = clamp((dot(toCamera, Hn)), M_EPSILON, 1.0);
        const float ndh = clamp((dot(normal, Hn)), M_EPSILON, 1.0);
        float ndl = clamp((dot(normal, lightVec)), M_EPSILON, 1.0);
        const float ndv = clamp((dot(normal, toCamera)), M_EPSILON, 1.0);
        const float ldh = clamp((dot(lightVec, Hn)), M_EPSILON, 1.0);

        const float3 diffuseFactor = Diffuse(diffColor, roughness, ndv, ndl, vdh)  * ndl;
        float3 specularFactor = 0;

        #ifdef SPECULAR
            //if(cLightRad > 0.0)
            //{
            //    if(cLightLength > 0.0)
            //    {
            //        return TubeLight(worldPos, lightVec, normal, toCamera, roughness, specColor, diffColor, ndl);
            //        
            //    }
            //    else
            //    {
            //        return SphereLight(worldPos, lightVec, normal, toCamera, roughness, specColor, diffColor, ndl);
            //    }
            //}
            //else
            {
                const float3 fresnelTerm = Fresnel(specColor, vdh, ldh);
                const float distTerm = Distribution(ndh, roughness);
                const float visTerm = Visibility(ndl, ndv, roughness);
                specularFactor = distTerm * visTerm * fresnelTerm * ndl/ M_PI;
                return diffuseFactor + specularFactor;// * specColor;
            }
        #else
            return diffuseFactor + specularFactor;
        #endif
	}