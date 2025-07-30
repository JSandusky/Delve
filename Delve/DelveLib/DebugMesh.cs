using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DelveLib
{
    public class DebugMesh : IDisposable
    {
        public const int MAX_VERTS = 12000;
        public const int MAX_INDICES = 4000;

        #region Fields

        bool ownsEffect = false;
        BasicEffect mainEffect_;
        public BasicEffect BasicEffect { get; set; }
        DynamicVertexBuffer vertexBuffer;
        DynamicIndexBuffer indexBuffer;

        ushort[] Indices = new ushort[MAX_INDICES];
        VertexPositionColorTexture[] Vertices = new VertexPositionColorTexture[MAX_VERTS];
        int IndexCount;
        int VertexCount;

        #endregion

        #region Initialization

        public DebugMesh(GraphicsDevice device, BasicEffect effect = null)
        {
            vertexBuffer = new DynamicVertexBuffer(device, typeof(VertexPositionColorTexture), MAX_VERTS, BufferUsage.WriteOnly);
            indexBuffer = new DynamicIndexBuffer(device, typeof(ushort), MAX_INDICES, BufferUsage.WriteOnly);

            if (effect == null)
            {
                ownsEffect = true;
                BasicEffect = new BasicEffect(device); //(device, null);
                BasicEffect.LightingEnabled = false;
                BasicEffect.VertexColorEnabled = true;
                BasicEffect.TextureEnabled = false;
                BasicEffect.World = Matrix.CreateScale(-1, 1, 1);
                mainEffect_ = BasicEffect;
            }
            else
            {
                BasicEffect = effect;
            }
        }

        #endregion

        ~DebugMesh()
        {
            Dispose(false);
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (vertexBuffer != null)
                    vertexBuffer.Dispose();
                vertexBuffer = null;

                if (indexBuffer != null)
                    indexBuffer.Dispose();
                indexBuffer = null;

                if (ownsEffect)
                {
                    if (mainEffect_ != null)
                        mainEffect_.Dispose();
                    mainEffect_ = null;
                    //if (BasicEffect != null)
                    //    BasicEffect.Dispose();
                    //BasicEffect = null;
                }
            }
        }

        #endregion

        #region Draw Basics

        /// <summary>
        /// Starts debug drawing by setting the required render states and camera information
        /// </summary>
        CullMode oldCullMode_;
        public void Begin(Matrix view, Matrix projection)
        {
            BasicEffect.World = Matrix.Identity;
            BasicEffect.View = view;
            BasicEffect.Projection = projection;
            BasicEffect.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            //oldCullMode_ = basicEffect.GraphicsDevice.RasterizerState.CullMode;
            //basicEffect.GraphicsDevice.RasterizerState.CullMode = CullMode.None;
            VertexCount = 0;
            IndexCount = 0;
        }

        public void Begin(BasicEffect effect, Matrix view, Matrix projection)
        {
            BasicEffect = effect;
            Begin(view, projection);
        }

        /// <summary>
        /// Ends debug drawing and restores standard render states
        /// </summary>
        public void End()
        {
            FlushDrawing();
            BasicEffect = mainEffect_;
            //basicEffect.GraphicsDevice.RasterizerState.CullMode = oldCullMode_;
        }

        private void FlushDrawing()
        {
            if (IndexCount > 0)
            {
                vertexBuffer.SetData(Vertices, 0, VertexCount, SetDataOptions.Discard);
                indexBuffer.SetData(Indices, 0, IndexCount, SetDataOptions.Discard);

                GraphicsDevice device = BasicEffect.GraphicsDevice;
                device.SetVertexBuffer(vertexBuffer);
                device.Indices = indexBuffer;

                foreach (EffectPass pass in BasicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexCount / 3);
                }

                device.SetVertexBuffer(null);
                device.Indices = null;
            }
            IndexCount = 0;
            VertexCount = 0;
        }

        // Check if there's enough space to draw an object with the given vertex/index counts.
        // If necessary, call FlushDrawing() to make room.
        private bool Reserve(int numVerts, int numIndices)
        {
            if (numVerts > MAX_VERTS || numIndices > MAX_INDICES)
            {
                // Whatever it is, we can't draw it
                return false;
            }
            if (VertexCount + numVerts > MAX_VERTS || IndexCount + numIndices >= MAX_INDICES)
            {
                // We can draw it, but we need to make room first
                FlushDrawing();
            }
            return true;
        }

        #endregion

        #region DrawShapes

        public void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            if (Reserve(3, 3))
            {
                Indices[IndexCount++] = (ushort)VertexCount;
                Indices[IndexCount++] = (ushort)(VertexCount + 1);
                Indices[IndexCount++] = (ushort)(VertexCount + 2);
                Vertices[VertexCount++] = new VertexPositionColorTexture(a, color, Vector2.Zero);
                Vertices[VertexCount++] = new VertexPositionColorTexture(b, color, Vector2.Zero);
                Vertices[VertexCount++] = new VertexPositionColorTexture(c, color, Vector2.Zero);
            }
        }

        public void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color colorA, Color colorB, Color colorC)
        {
            if (Reserve(3, 3))
            {
                Indices[IndexCount++] = (ushort)VertexCount;
                Indices[IndexCount++] = (ushort)(VertexCount + 1);
                Indices[IndexCount++] = (ushort)(VertexCount + 2);
                Vertices[VertexCount++] = new VertexPositionColorTexture(a, colorA, Vector2.Zero);
                Vertices[VertexCount++] = new VertexPositionColorTexture(b, colorB, Vector2.Zero);
                Vertices[VertexCount++] = new VertexPositionColorTexture(c, colorC, Vector2.Zero);
            }
        }

        public void DrawQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        {
            if (Reserve(6,6))
            {
                Indices[IndexCount++] = (ushort)VertexCount;
                Indices[IndexCount++] = (ushort)(VertexCount + 1);
                Indices[IndexCount++] = (ushort)(VertexCount + 2);
                Vertices[VertexCount++] = new VertexPositionColorTexture(a, color, Vector2.Zero);
                Vertices[VertexCount++] = new VertexPositionColorTexture(b, color, Vector2.Zero);
                Vertices[VertexCount++] = new VertexPositionColorTexture(c, color, Vector2.Zero);

                Indices[IndexCount++] = (ushort)VertexCount;
                Indices[IndexCount++] = (ushort)(VertexCount + 1);
                Indices[IndexCount++] = (ushort)(VertexCount + 2);
                Vertices[VertexCount++] = new VertexPositionColorTexture(b, color, Vector2.Zero);
                Vertices[VertexCount++] = new VertexPositionColorTexture(c, color, Vector2.Zero);
                Vertices[VertexCount++] = new VertexPositionColorTexture(a, color, Vector2.Zero);
            }
        }

        public void DrawTexturedQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
            Vector2 ta, Vector2 tb, Vector2 tc, Vector2 td)
        {
            if (Reserve(6, 6))
            {
                Indices[IndexCount++] = (ushort)VertexCount;
                Indices[IndexCount++] = (ushort)(VertexCount + 1);
                Indices[IndexCount++] = (ushort)(VertexCount + 2);
                Vertices[VertexCount++] = new VertexPositionColorTexture(a, Color.White, ta);
                Vertices[VertexCount++] = new VertexPositionColorTexture(b, Color.White, tb);
                Vertices[VertexCount++] = new VertexPositionColorTexture(c, Color.White, tc);

                Indices[IndexCount++] = (ushort)VertexCount;
                Indices[IndexCount++] = (ushort)(VertexCount + 1);
                Indices[IndexCount++] = (ushort)(VertexCount + 2);
                Vertices[VertexCount++] = new VertexPositionColorTexture(c, Color.White, tc);
                Vertices[VertexCount++] = new VertexPositionColorTexture(d, Color.White, td);
                Vertices[VertexCount++] = new VertexPositionColorTexture(a, Color.White, ta);
            }
        }

        public delegate Vector3 ShapeFunction(Vector2 coord);

        public static Vector3 SphereFunction(Vector2 coord)
        {
            float phi = (float)(coord.X * Math.PI);
            float theta = (float)(coord.Y * 2 * Math.PI);
            return new Vector3(Mathf.Cos(theta) * Mathf.Sin(phi),
                Mathf.Sin(theta) * Mathf.Sin(phi),
                Mathf.Cos(phi));
        }

        public static Vector3 CylinderFunction(Vector2 coord)
        {
            float theta = (float)(coord.Y * 2 * Math.PI);
            return new Vector3(Mathf.Sin(theta),
                Mathf.Cos(theta),
                coord.X);
        }

        public static Vector3 TrefoilFunction(Vector2 coord)
        {
            float minor = 2.0f; // userData

            float a = 0.5f;
            float b = 0.3f;
            float c = 0.5f;
            float d = minor * 0.1f;
            float u = (1 - coord.X) * 4 * (float)Math.PI;
            float v = coord.Y * 2 * (float)Math.PI;
            float r = a + b * Mathf.Cos(1.5f * u);
            float x = r * Mathf.Cos(u);
            float y = r * Mathf.Sin(u);
            float z = c * Mathf.Sin(1.5f * u);
            Vector3 q = new Vector3();
            q.X =
                -1.5f * b * Mathf.Sin(1.5f * u) * Mathf.Cos(u) - (a + b * Mathf.Cos(1.5f * u)) * Mathf.Sin(u);
            q.Y =
                -1.5f * b * Mathf.Sin(1.5f * u) * Mathf.Sin(u) + (a + b * Mathf.Cos(1.5f * u)) * Mathf.Cos(u);
            q.Z = 1.5f * c * Mathf.Cos(1.5f * u);

            q.Normalize();
            Vector3 qvn = new Vector3(q.Y, -q.X, 0);
            qvn.Normalize();

            Vector3 ww = Vector3.Cross(q, qvn);
            return new Vector3(x + d * (qvn.X * Mathf.Cos(v) + ww.X * Mathf.Sin(v)),
            y + d * (qvn.Y * Mathf.Cos(v) + ww.Y * Mathf.Sin(v)),
            z + d * ww.Z * Mathf.Sin(v));
        }

        public static Vector3 KleinBottle(Vector2 coord)
        {
            float u = coord.X * (float)Math.PI;
            float v = coord.Y * 2 * (float)Math.PI;
            u = u * 2;

            Vector3 xyz = new Vector3();
            if (u < (float)Math.PI)
            {
                xyz.X = 3 * Mathf.Cos(u) * (1 + Mathf.Sin(u)) + (2 * (1 - Mathf.Cos(u) / 2)) *
                    Mathf.Cos(u) * Mathf.Cos(v);
                xyz.Z = -8 * Mathf.Sin(u) - 2 * (1 - Mathf.Cos(u) / 2) * Mathf.Sin(u) * Mathf.Cos(v);
            }
            else
            {
                xyz.X = 3 * Mathf.Cos(u) * (1 + Mathf.Sin(u)) + (2 * (1 - Mathf.Cos(u) / 2)) *
                    Mathf.Cos(v + (float)Math.PI);
                xyz.Z = -8 * Mathf.Sin(u);
            }
            xyz.Y = -2 * (1 - Mathf.Cos(u) / 2) * Mathf.Sin(v);
            return xyz;
        }

        public static Vector3 Torus(Vector2 coord)
        {
            float major = 1;
            float minor = 0.5f; // userdata
            float theta = coord.X * 2 * (float)Math.PI;
            float phi = coord.Y * 2 * (float)Math.PI;
            float beta = major + minor * Mathf.Cos(phi);
            return new Vector3(Mathf.Cos(theta) * beta,
                        Mathf.Sin(theta) * beta,
                        Mathf.Sin(phi) * minor);
        }

        public void DrawTrefoil(Color color)
        {
            DrawParametric(TrefoilFunction, color);
        }

        public void DrawKleinBottle(Color color)
        {
            DrawParametric(KleinBottle, color);
        }

        public void DrawTorus(Color color)
        {
            DrawParametric(Torus, color);
        }

        public void DrawSphere(Color color)
        {
            DrawParametric(SphereFunction, color);
        }

        public void DrawSphere(Matrix transform, Color color)
        {
            DrawParametric(SphereFunction, transform, color);
        }

        public void DrawSphere(BoundingSphere sphere, Color color)
        {
            DrawParametric(SphereFunction, Matrix.CreateScale(sphere.Radius) * Matrix.CreateTranslation(sphere.Center), color, 16, 16);
        }

        public void DrawCylinder(Color color)
        {
            DrawParametric(CylinderFunction, color);
        }

        public void DrawCylinder(Matrix transform, Color color)
        {
            DrawParametric(CylinderFunction, transform, color);
        }

        public void DrawParametric(ShapeFunction fn, Color color, int slices = 32, int stacks = 32)
        {
            Vector3[] pts = new Vector3[(slices + 1) * (stacks + 1)];

            int ptIndex = 0;
            Vector2 coord = new Vector2();
            for (int stack = 0; stack < stacks + 1; stack++)
            {
                coord.X = (float)stack / stacks;
                for (int slice = 0; slice < slices + 1; slice++)
                {
                    coord.Y = (float)slice / slices;
                    pts[ptIndex++] = fn(coord);
                }
            }

            int nTriangles = 2 * slices * stacks;
            int[] indices = new int[3 * nTriangles];
            int indIndex = 0;
            int v = 0;
            for (int stack = 0; stack < stacks; stack++)
            {
                for (int slice = 0; slice < slices; slice++)
                {
                    int next = slice + 1;
                    indices[indIndex++] = v + slice + slices + 1;
                    indices[indIndex++] = v + next;
                    indices[indIndex++] = v + slice;
                    indices[indIndex++] = v + slice + slices + 1;
                    indices[indIndex++] = v + next + slices + 1;
                    indices[indIndex++] = v + next;
                }
                v += slices + 1;
            }

            for (int i = 0; i < indices.Length; i += 3)
                DrawTriangle(pts[indices[i]], pts[indices[i + 1]], pts[indices[i + 2]], color);
        }

        public void DrawParametric(ShapeFunction fn, Matrix transform, Color color, int slices = 16, int stacks = 8)
        {
            Vector3[] pts = new Vector3[(slices + 1) * (stacks + 1)];

            int ptIndex = 0;
            Vector2 coord = new Vector2();
            for (int stack = 0; stack < stacks + 1; stack++)
            {
                coord.X = (float)stack / stacks;
                for (int slice = 0; slice < slices + 1; slice++)
                {
                    coord.Y = (float)slice / slices;
                    pts[ptIndex++] = Vector3.Transform(fn(coord), transform);
                }
            }

            int nTriangles = 2 * slices * stacks;
            int[] indices = new int[3 * nTriangles];
            int indIndex = 0;
            int v = 0;
            for (int stack = 0; stack < stacks; stack++)
            {
                for (int slice = 0; slice < slices; slice++)
                {
                    int next = slice + 1;
                    indices[indIndex++] = v + slice + slices + 1;
                    indices[indIndex++] = v + next;
                    indices[indIndex++] = v + slice;
                    indices[indIndex++] = v + slice + slices + 1;
                    indices[indIndex++] = v + next + slices + 1;
                    indices[indIndex++] = v + next;
                }
                v += slices + 1;
            }

            for (int i = 0; i < indices.Length; i += 3)
                DrawTriangle(pts[indices[i]], pts[indices[i + 1]], pts[indices[i + 2]], color);
        }

        #endregion

        #region Basic Shapes

        void DrawDisk(float radius, int slices, Color color)
        {
            int nPoints = slices + 1;
            Vector3[] points = new Vector3[nPoints];
            int ptIndex = 0;
            for (int i = 0; i < slices; ++i)
            {
                float theta = i * (float)Math.PI * 2 / slices;
                points[ptIndex++] = new Vector3(radius * Mathf.Cos(theta), radius * Mathf.Sin(theta),  0);
            }

            int ntriangles = slices;
            int[] indices = new int[3 * ntriangles];
            int tri = 0;
            for (int i = 0; i < slices; i++) {
                indices[tri++] = 0;
                indices[tri++] = 1 + i;
                indices[tri++] = 1 + (i + 1) % slices;
            }

            for (int i = 0; i < indices.Length; i += 3)
                DrawTriangle(points[indices[i]], points[indices[i + 1]], points[indices[i + 2]], color);
        }

        public void DrawIcosahedron(Color color)
        {
            Vector3[] verts = {
                new Vector3(0.000f,  0.000f,  1.000f),
                new Vector3(0.894f,  0.000f,  0.447f),
                new Vector3(0.276f,  0.851f,  0.447f),
                new Vector3(-0.724f,  0.526f,  0.447f),
                new Vector3(-0.724f, -0.526f,  0.447f),
                new Vector3(0.276f, -0.851f,  0.447f),
                new Vector3(0.724f,  0.526f, -0.447f),
                new Vector3(-0.276f,  0.851f, -0.447f),
                new Vector3(-0.894f,  0.000f, -0.447f),
                new Vector3(-0.276f, -0.851f, -0.447f),
                new Vector3(0.724f, -0.526f, -0.447f),
                new Vector3(0.000f,  0.000f, -1.000f)
            };

            int[] faces = {
                0,1,2,
                0,2,3,
                0,3,4,
                0,4,5,
                0,5,1,
                7,6,11,
                8,7,11,
                9,8,11,
                10,9,11,
                6,10,11,
                6,2,1,
                7,3,2,
                8,4,3,
                9,5,4,
                10,1,5,
                6,7,2,
                7,8,3,
                8,9,4,
                9,10,5,
                10,6,1
            };

            for (int i = 0; i < faces.Length; i += 3)
                DrawTriangle(verts[faces[i]], verts[faces[i + 1]], verts[faces[i + 2]], color);
        }
            #endregion
        }
}
