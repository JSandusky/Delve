using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DelveLib;

namespace Delve.Graphics
{
    public class SLATextureRenderer
    {
        Texture2D texture = null;

        public SLATextureRenderer(GraphicsDevice device, int resolution = 256)
        {
            texture = new Texture2D(device, resolution, resolution, true, SurfaceFormat.Color);
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(resolution, resolution);
            Vector3 camPos = Vector3.UnitZ * 4;
            Vector3 lightDir = -Vector3.UnitY;
            lightDir.Normalize();

            float aperture = 180.0f;
            float apertureHalf = 0.5f * aperture * (Mathf.PI / 180.0f);
            float maxFactor = Mathf.Sin(apertureHalf);

            int texRes = resolution;
            float roughness = 0.0f;
            for (int i = 0; i < texture.LevelCount; ++i)
            {
                Color[] pixelData = new Color[texRes * texRes];
                for (int y = 0; y < texRes; ++y)
                {
                    float fracY = Mathf.Denormalize(y / (float)texRes, -1.0f, 1.0f);
                    float tv = Mathf.Asin(fracY) / Mathf.PI + 0.5f;
                    for (int x = 0; x < texRes; ++x)
                    {
                        float fracX = Mathf.Denormalize(x / (float)texRes, -1.0f, 1.0f);
                        float tx = Mathf.Asin(fracX) / Mathf.PI + 0.5f;

                        float d = (new Vector2(fracX, fracY) * maxFactor).Length();
                        float z = Mathf.Sqrt(1.0f - d * d);
                        if (float.IsNaN(z))
                            z = 0.001f;
                        Vector3 norm = Vector3.Normalize(new Vector3(fracX, fracY, z));
                        Vector3 surfacePos = new Vector3(fracX, fracY, 0);

                        Vector3 value = GetBRDF(surfacePos, -lightDir, lightDir*2, Vector3.Normalize(camPos - surfacePos), norm, 1.0f, Vector3.One, new Vector3(1, 1, 1));
                        pixelData[x + (y * texRes)] = new Color(value.X, value.Y, value.Z, 1.0f);
                        //pixelData[x + (y * texRes)] = new Color(norm.X, norm.Y, norm.Z, 1.0f);
                        if (i == 0)
                        {
                            var col = new Color(norm.X * 0.5f + 0.5f, norm.Y * 0.5f + 0.5f, norm.Z * 0.5f + 0.5f, 1.0f);
                            bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(col.A, col.R, col.G, col.B));
                        }
                    }
                }

                texture.SetData<Color>(i, null, pixelData, 0, pixelData.Length);
                

                roughness += (1 / (texture.LevelCount - 1));
                texRes /= 2;
            }
            bmp.Save("TestBMP.png");
            using (var fs = new System.IO.FileStream("TestImg.png", System.IO.FileMode.Create))
                texture.SaveAsPng(fs, texRes, texRes);
        }

        static Vector3 SchlickFresnel(Vector3 specular, float VdotH)
        {
            return specular + ((Vector3.One - specular) * Mathf.Pow(1.0f - VdotH, 5.0f));
        }

        public static Vector3 Fresnel(Vector3 specular, float VdotH, float LdotH)
        {
            //return SchlickFresnelCustom(specular, LdotH);
            return SchlickFresnel(specular, VdotH);
        }

        static float NeumannVisibility(float NdotV, float NdotL)
        {
            return (float)(NdotL * NdotV / Math.Max(1e-7, Math.Max(NdotL, NdotV)));
        }

        public static float Visibility(float NdotL, float NdotV, float roughness)
        {
            return NeumannVisibility(NdotV, NdotL);
            //return SmithGGXSchlickVisibility(NdotL, NdotV, roughness);
        }

        static float GGXDistribution(float NdotH, float roughness)
        {
            float rough2 = roughness * roughness;
            float tmp = (NdotH * rough2 - NdotH) * NdotH + 1;
            return rough2 / (tmp * tmp);
        }

        public static float Distribution(float NdotH, float roughness)
        {
            return GGXDistribution(NdotH, roughness);
        }

        static Vector3 BurleyDiffuse(Vector3 diffuseColor, float roughness, float NdotV, float NdotL, float VdotH)
        {
            float energyBias = Mathf.Lerp(0, 0.5f, roughness);
            float energyFactor = Mathf.Lerp(1.0f, 1.0f / 1.51f, roughness);
            float fd90 = energyBias + 2.0f * VdotH * VdotH * roughness;
            float f0 = 1.0f;
            float lightScatter = f0 + (fd90 - f0) * Mathf.Pow(1.0f - NdotL, 5.0f);
            float viewScatter = f0 + (fd90 - f0) * Mathf.Pow(1.0f - NdotV, 5.0f);

            return diffuseColor * lightScatter * viewScatter * energyFactor;
        }

        public static Vector3 Diffuse(Vector3 diffuseColor, float roughness, float NdotV, float NdotL, float VdotH)
        {
            //return LambertianDiffuse(diffuseColor);
            //return CustomLambertianDiffuse(diffuseColor, NdotV, roughness);
            return BurleyDiffuse(diffuseColor, roughness, NdotV, NdotL, VdotH);
        }

        public static Vector3 GetBRDF(Vector3 worldPos, Vector3 lightDir, Vector3 lightVec, Vector3 toCamera, Vector3 normal, float roughness, Vector3 diffColor, Vector3 specColor)
        {
            Vector3 Hn = Vector3.Normalize(toCamera + lightDir);
            float vdh = Mathf.Clamp((Vector3.Dot(toCamera, Hn)), Mathf.EPSILON, 1.0f);
            float ndh = Mathf.Clamp((Vector3.Dot(normal, Hn)), Mathf.EPSILON, 1.0f);
            float ndl = Mathf.Clamp((Vector3.Dot(normal, lightVec)), Mathf.EPSILON, 1.0f);
            float ndv = Mathf.Clamp((Vector3.Dot(normal, toCamera)), Mathf.EPSILON, 1.0f);
            float ldh = Mathf.Clamp((Vector3.Dot(lightVec, Hn)), Mathf.EPSILON, 1.0f);

            Vector3 diffuseFactor = Diffuse(diffColor, roughness, ndv, ndl, vdh) * ndl;
            Vector3 specularFactor = Vector3.Zero;

            Vector3 fresnelTerm = Fresnel(specColor, vdh, ldh);
            float distTerm = Distribution(ndh, roughness);
            float visTerm = Visibility(ndl, ndv, roughness);
            specularFactor = (distTerm * visTerm * fresnelTerm * ndl / Mathf.PI);
            return diffuseFactor + specularFactor;
        }
    }
}
