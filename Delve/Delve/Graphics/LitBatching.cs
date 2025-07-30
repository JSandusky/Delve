using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Delve.Graphics
{
    public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
    {
        public int Compare(TKey x, TKey y)
        {
            int result = x.CompareTo(y);
            if (result == 0)
                return 1;   // Handle equality as beeing greater
            else
                return result;
        }
    }

    public enum LightShape
    {
        Point,
        Spot,
        Directional,
    }

    public class Light
    {
        public LightShape shape_ = LightShape.Point;
        public Matrix transform_;
        public Vector4 params_;
        public Vector4 color_;
        public bool isNegative_;
        public bool castShadows_;
        public BoundingSphere bounds_;
        public float shadowDepthBias_;
        public float shadowSlopeBias_;
        public uint lightMask_ = 0xFFFFFFFF;

        public float Radius { get { return params_.X; } set { params_.X = value; } }

        internal LitBatching.ShadowMap shadowMap_;
    }

    public class LitBatching
    {
        internal class ShadowMap
        {
            internal RenderTarget2D target_;
            internal RenderTargetCube cubeTarget_;
            internal int width_, height_;
            internal bool taken_;
        }
        Effect lightEffect_;
        Effect shadowEffect_;
        Effect alphaShadowEffect_;

        GraphicsDevice device_;
        List<ShadowMap> shadowMapTargets_ = new List<ShadowMap>();
        SortedList<ulong, KeyValuePair<Light, MeshDraw>> litDraws_ = new SortedList<ulong, KeyValuePair<Light, MeshDraw>>(new DuplicateKeyComparer<ulong>());
        SortedList<Light, SortedList<ulong, MeshDraw>> shadowDraws_ = new SortedList<Light, SortedList<ulong, MeshDraw>>();
        SortedSet<Light> activeLights_ = new SortedSet<Light>();
        List<Light> lights_ = new List<Light>();

        public int MaxShadowMaps { get; set; } = 8;

        public LitBatching(GraphicsDevice device, Effect lightEffect, Effect shadowEffect, Effect alphaShadowEffect)
        {
            device_ = device;
            lightEffect_ = lightEffect;
            shadowEffect_ = shadowEffect;
            alphaShadowEffect_ = alphaShadowEffect;
        }

        public void AddToLightList(MeshDraw batch, BoundingSphere sphere, bool shadowed)
        {
            for (int i = 0; i < lights_.Count; ++i)
            {
                // verify we can light this object and that it is in our bounds.
                if ((lights_[i].lightMask_ & batch.lightMask) > 0 && lights_[i].bounds_.Intersects(sphere))
                {
                    var record = new KeyValuePair<Light, MeshDraw>(lights_[i], batch);
                    litDraws_.Add(batch.sortCode, record);
                    if (shadowed && lights_[i].castShadows_)
                    {
                        if (shadowDraws_.ContainsKey(lights_[i]))
                            shadowDraws_[lights_[i]].Add(batch.sortCode, batch);
                        else
                        {
                            SortedList<ulong, MeshDraw> set = new SortedList<ulong, MeshDraw>(new DuplicateKeyComparer<ulong>());
                            set.Add(batch.sortCode, batch);
                            shadowDraws_.Add(lights_[i], set);
                        }
                    }
                    activeLights_.Add(lights_[i]);
                }
            }
        }

        public void RenderShadowMaps()
        {
            foreach (var item in shadowDraws_)
            {
                Light light = item.Key;
                var draws = item.Value;
                if (light.shape_ == LightShape.Spot)
                {
                    var shadowMap = GetShadowMap(512, 512, false);
                    if (shadowMap != null)
                    {
                        device_.SetRenderTarget(shadowMap.target_);
                        foreach (var draw in draws)
                        {
                            shadowEffect_.Parameters["Transform"].SetValue(draw.Value.transform);
                            shadowEffect_.CurrentTechnique.Passes[0].Apply();
                            device_.SetVertexBuffer(draw.Value.verts);
                            device_.Indices = draw.Value.indices;
                            device_.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, draw.Value.indices.IndexCount / 3);
                        }
                    }
                }
                else if (light.shape_ == LightShape.Directional)
                {
                    var shadowMap = GetShadowMap(1024, 1024, false);
                    if (shadowMap != null)
                    {
                        device_.SetRenderTarget(shadowMap.target_);
                        foreach (var draw in draws)
                        {
                            shadowEffect_.Parameters["Transform"].SetValue(draw.Value.transform);
                            shadowEffect_.CurrentTechnique.Passes[0].Apply();
                            device_.SetVertexBuffer(draw.Value.verts);
                            device_.Indices = draw.Value.indices;
                            device_.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, draw.Value.indices.IndexCount / 3);
                        }
                    }
                }
                else if (light.shape_ == LightShape.Point)
                {
                    var shadowMap = GetShadowMap(0, 0, true);
                    if (shadowMap != null)
                    {
                        for (int i = 0; i < 6; ++i)
                        {
                            device_.SetRenderTargets(new RenderTargetBinding(shadowMap.cubeTarget_, (CubeMapFace)i));
                            foreach (var draw in draws)
                            {
                                shadowEffect_.Parameters["Transform"].SetValue(draw.Value.transform);
                                shadowEffect_.CurrentTechnique.Passes[0].Apply();
                                device_.SetVertexBuffer(draw.Value.verts);
                                device_.Indices = draw.Value.indices;
                                device_.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, draw.Value.indices.IndexCount / 3);
                            }
                        }
                    }
                }
            }
        }

        public void RenderForwardLights()
        {
            foreach (var litItem in litDraws_)
            {
                Light light = litItem.Value.Key;
                MeshDraw draw = litItem.Value.Value;
            }
        }

        public void RenderDeferredLights()
        {

        }

        ShadowMap GetShadowMap(int w, int h, bool cube)
        {
            for (int i = 0; i < shadowMapTargets_.Count; ++i)
            {
                if (shadowMapTargets_[i].taken_)
                    continue;
                if (shadowMapTargets_[i].width_ == w && shadowMapTargets_[i].height_ == h)
                {
                    if (cube && shadowMapTargets_[i].cubeTarget_ == null)
                        continue;
                    shadowMapTargets_[i].taken_ = true;
                    return shadowMapTargets_[i];
                }
            }

            if (MaxShadowMaps <= shadowMapTargets_.Count)
                return null;

            if (cube)
            {
                ShadowMap sm = new ShadowMap()
                {
                    cubeTarget_ = new RenderTargetCube(device_, w, false, SurfaceFormat.Single, DepthFormat.Depth24),
                    width_ = w,
                    height_ = h,
                    taken_ = true
                };
                shadowMapTargets_.Add(sm);
                return sm;
            }
            else
            {
                ShadowMap sm = new ShadowMap()
                {
                    target_ = new RenderTarget2D(device_, w, h, false, SurfaceFormat.Single, DepthFormat.Depth24),
                    width_ = w,
                    height_ = h,
                    taken_ = true
                };
                shadowMapTargets_.Add(sm);
                return sm;
            }
        }
    }
}
