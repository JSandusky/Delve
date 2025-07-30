using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DelveLib
{
    public class DecalMesh : IDisposable
    {
        public class Decal
        {
            public BoundingSphere bounds_;
            public DecalVertex[] vertices_;
        }

        public class DecalFace : List<DecalVertex> {
            public DecalFace Clone()
            {
                DecalFace r = new DecalFace();
                r.AddRange(this);
                return r;
            }
        }

        public List<DecalFace> Faces { get; set; }

        bool dirty_ = false;
        List<Decal> decals_ = new List<Decal>();
        DynamicVertexBuffer vertBuffer_;
        int vertCt_ = 0;

        /// <summary>
        /// Technically this could be VertexPositionNormalTexture
        /// Not being such keeps things flexible
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DecalVertex : IVertexType
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 TextureCoordinate;

            public static DecalVertex ClipEdge(DecalVertex v0, DecalVertex v1, float d0, float d1, bool skinned)
            {
                DecalVertex ret = new DecalVertex();
                float t = d0 / (d0 - d1);

                ret.Position = v0.Position + t * (v1.Position - v0.Position);
                ret.Normal = v0.Normal + t * (v1.Normal - v0.Normal);
                return ret;
            }

            public static VertexDeclaration DeclSpec = new VertexDeclaration(new []{
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(4*3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(4*3 + 4*3, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            });

            VertexDeclaration IVertexType.VertexDeclaration { get { return DeclSpec; } }
        }

        public void AddDecal(Vector3 pos, Vector3 decalNormal, float size, float depth, Vector2 topLeftUV, Vector2 bottomRightUV, float normalCutoff, bool skinned)
        {
            Matrix transformMat = Matrix.CreateFromQuaternion(XNAExt.FromStartEnd(Vector3.Up, decalNormal)) * Matrix.CreateTranslation(pos);
            BoundingFrustum frustum = new BoundingFrustum(transformMat * Matrix.CreateOrthographic(size, size, 0, depth));

            // ordered in most likely to clip fast
            Plane[] planes =
            {
                frustum.Near,
                frustum.Far,
                frustum.Left,
                frustum.Right,
                frustum.Top,
                frustum.Bottom
            };

            List<DecalFace> srcFaces = new List<DecalFace>();
            srcFaces.AddRange(Faces);
            if (srcFaces.Count == 0)
                return;

            for (int i = 0; i < 6; ++i)
            {
                for (int j = 0; j < srcFaces.Count; ++j)
                {
                    DecalFace newFace = new DecalFace();
                    ClipPolygon(newFace, srcFaces[j], planes[i], skinned);
                    if (newFace.Count == 0)
                    {
                        srcFaces.RemoveAt(j);
                        --j;
                    }
                    else
                        srcFaces[j] = newFace;
                }
            }

            if (srcFaces.Count == 0)
                return;

            List<DecalVertex> newVertices = new List<DecalVertex>();
            for (int i = 0; i < srcFaces.Count; ++i)
            {
                if (srcFaces[i].Count < 3)
                    continue;

                for (int j = 2; j < srcFaces[i].Count; ++j)
                {
                    newVertices.Add(srcFaces[i][0]);
                    newVertices.Add(srcFaces[i][j - 1]);
                    newVertices.Add(srcFaces[i][j]);
                }
            }

            if (newVertices.Count == 0)
                return;

            Matrix texMat = new Matrix();
            float extent = (1.0f / (size * 0.5f));
            texMat.M22 = extent;
            texMat.M11 = extent;
            texMat.M33 = 1 / depth;
            texMat.M44 = 1;

            CalculateUVs(newVertices, Matrix.Invert(frustum.Matrix), texMat, topLeftUV, bottomRightUV);

            decals_.Add(new DelveLib.DecalMesh.Decal { bounds_ = CalculateBounds(newVertices), vertices_ = newVertices.ToArray() });
            dirty_ = true;
        }

        public void Update(GraphicsDevice device, BoundingFrustum frustum)
        {
            if (!dirty_)
                return;

            vertCt_ = 0;
            dirty_ = false;
            if (decals_.Count > 0)
            {
                int ct = 0;
                bool[] passmask = new bool[decals_.Count];
                for (int i = 0; i < decals_.Count; ++i)
                {
                    if (frustum != null && frustum.Contains(decals_[i].bounds_) == ContainmentType.Disjoint)
                    {
                        passmask[i] = false;
                        continue;
                    }
                    passmask[i] = true;
                    ct += decals_[i].vertices_.Length;
                }
                if (ct == 0)
                    return;

                DecalVertex[] meshVertices = new DecalVertex[ct];
                int vidx = 0;
                for (int i = 0; i < decals_.Count; ++i)
                {
                    if (!passmask[i])
                        continue;
                    for (int v = 0; v < decals_[i].vertices_.Length; ++v)
                        meshVertices[vidx++] = decals_[i].vertices_[v];
                }

                if (vertBuffer_ == null)
                    vertBuffer_ = new DynamicVertexBuffer(device, DecalVertex.DeclSpec, ct, BufferUsage.WriteOnly);
                else
                    vertBuffer_.SetData<DecalVertex>(meshVertices, 0, ct, SetDataOptions.Discard);

                vertCt_ = meshVertices.Length;
            }
        }

        public void Draw(GraphicsDevice device)
        {
            Draw(device, null);
        }

        public virtual void Draw(GraphicsDevice device, BoundingFrustum frustum)
        {
            if (dirty_)
                Update(device, frustum);
            if (vertCt_ > 0 && decals_.Count > 0)
            {
                device.SetVertexBuffer(vertBuffer_);
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, vertCt_ / 3);
            }
        }

        public void Dispose()
        {
            if (vertBuffer_ != null)
            {
                vertBuffer_.Dispose();
                vertBuffer_ = null;
            }
        }

        void CalculateUVs(List<DecalVertex> verts, Matrix view, Matrix projection, Vector2 topLeftUV, Vector2 bottomRightUV)
        {
            Matrix viewProj = projection * view;
            for (int i = 0; i < verts.Count; ++i)
            {
                Vector3 projected = Vector3.Transform(verts[i].Position, viewProj);
                DecalVertex v = verts[i];
                v.TextureCoordinate.X = MathHelper.Lerp(topLeftUV.X, bottomRightUV.X, projected.X * 0.5f + 0.5f);
                v.TextureCoordinate.Y = MathHelper.Lerp(bottomRightUV.Y, topLeftUV.Y, projected.Y * 0.5f + 0.5f);
                verts[i] = v;
            }
        }

        BoundingSphere CalculateBounds(List<DecalVertex> verts)
        {
            Vector3[] pts = new Vector3[verts.Count];
            for (int i = 0; i < verts.Count; ++i) pts[i] = verts[i].Position;
            return BoundingSphere.CreateFromPoints(pts);
        }

        void ClipPolygon(List<DecalVertex> dest, List<DecalVertex> src, Plane plane, bool skinned)
        {
            int last = 0;
            float lastDistance = 0.0f;
            dest.Clear();

            if (src.Count == 0)
                return;

            for (int i = 0; i < src.Count; ++i)
            {
                float distance = plane.Distance(src[i].Position);
                if (distance >= 0.0f)
                {
                    if (lastDistance < 0.0f)
                        dest.Add(DecalVertex.ClipEdge(src[last], src[i], lastDistance, distance, skinned));
                    dest.Add(src[i]);
                }
                else
                {
                    if (lastDistance >= 0.0f && i != 0)
                        dest.Add(DecalVertex.ClipEdge(src[last], src[i], lastDistance, distance, skinned));
                }

                last = i;
                lastDistance = distance;
            }

            // Recheck the distances of the last and first vertices and add the final clipped vertex if applicable
            float dist = plane.Distance(src[0].Position);
            if ((lastDistance < 0.0f && dist >= 0.0f) || (lastDistance >= 0.0f && dist < 0.0f))
                dest.Add(DecalVertex.ClipEdge(src[last], src[0], lastDistance, dist, skinned));
        }

        void ClipPolygon(DecalFace dest, DecalFace src, Plane plane, bool skinned)
        {
            int last = 0;
            float lastDistance = 0.0f;

            if (src == null || src.Count == 0)
                return;

            for (int i = 0; i < src.Count; ++i)
            {
                float distance = plane.Distance(src[i].Position);
                if (distance >= 0.0f)
                {
                    if (lastDistance < 0.0f)
                        dest.Add(DecalVertex.ClipEdge(src[last], src[i], lastDistance, distance, skinned));
                    dest.Add(src[i]);
                }
                else
                {
                    if (lastDistance >= 0.0f && i != 0)
                        dest.Add(DecalVertex.ClipEdge(src[last], src[i], lastDistance, distance, skinned));
                }

                last = i;
                lastDistance = distance;
            }

            // Recheck the distances of the last and first vertices and add the final clipped vertex if applicable
            float dist = plane.Distance(src[0].Position);
            if ((lastDistance < 0.0f && dist >= 0.0f) || (lastDistance >= 0.0f && dist < 0.0f))
                dest.Add(DecalVertex.ClipEdge(src[last], src[0], lastDistance, dist, skinned));
        }
    }
}
