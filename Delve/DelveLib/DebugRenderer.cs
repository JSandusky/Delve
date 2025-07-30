using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DelveLib
{
    public enum DebugDrawDepth
    {
        Pass,   // Only when Z passes (depth-test less)
        Always, // Always
        Fail    // Only when Z fails (depth-test greater)
    }

    public class DebugDrawData
    {
        public List<VertexPositionColor>[] lines_ = new List<VertexPositionColor>[] {
            new List<VertexPositionColor>(),
            new List<VertexPositionColor>(),
            new List<VertexPositionColor>()
        };
        public List<VertexPositionColor>[] tris_ = new List<VertexPositionColor>[] {
            new List<VertexPositionColor>(),
            new List<VertexPositionColor>(),
            new List<VertexPositionColor>()
        };

        public void SetAlpha(float a)
        {
            for (int i = 0; i < 3; ++i)
            {
                for (int v = 0; v < lines_[v].Count; ++v)
                    lines_[i] = lines_[i].ConvertAll(d => new VertexPositionColor(d.Position, new Color(d.Color, a)));
                for (int v = 0; v < tris_[v].Count; ++v)
                    tris_[i] = tris_[i].ConvertAll(d => new VertexPositionColor(d.Position, new Color(d.Color, a)));
            }
        }
    }

    /// <summary>
    /// Records diagnostic (or not) primitives of lines and triangles to be rendered.
    /// The lines/triangles may be drawn with one of 3 combinations of depth modes:
    ///     Test = will be depth-tested and write to depth
    ///     None = will ignore depth
    ///     Fail = depth-tested for GreaterEqual so that it will only draw where Z-Fails
    /// </summary>
    /// <example>
    ///     DebugRenderer debugRenderer_ = new DebugRenderer();
    ///     debugRenderer.SetEffects(new BasicEffect() { LightingEnabled = false, VertexColorEnabled = true, TextureEnabled = false });
    ///     debugRenderer.Begin();
    ///         ... use it from where-ever ...
    ///     debugRenderer.Render(graphicsDevice, myCamera.WorldViewProjection);
    /// </example>
    public class DebugRenderer : IDisposable
    {
        DebugDrawData data_ = new DebugDrawData();

        public void Begin()
        {
            for (int i = 0; i < 3; ++i)
            {
                data_.lines_[i].Clear();
                data_.tris_[i].Clear();
            }
        }

        #region Primary Draw Primitive Functions

        public void DrawLine(Vector3 v0, Vector3 v1, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            data_.lines_[(int)depthTest].Add(new VertexPositionColor(v0, color));
            data_.lines_[(int)depthTest].Add(new VertexPositionColor(v1, color));
        }

        public void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            data_.tris_[(int)depthTest].Add(new VertexPositionColor(a, color));
            data_.tris_[(int)depthTest].Add(new VertexPositionColor(b, color));
            data_.tris_[(int)depthTest].Add(new VertexPositionColor(c, color));
        }

        public void DrawQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            DrawTriangle(a, b, c, color, depthTest);
            DrawTriangle(b, c, a, color, depthTest);
        }

        #endregion

        // By keeping them seperate per depth test type it's assured the driver won't do anything crazy
        // All data is uploaded first, then the draw calls are sent
        DynamicVertexBuffer[] lineVertexBuffers_ = new DynamicVertexBuffer[] { null, null, null };
        DynamicVertexBuffer[] triVertexBuffers_ = new DynamicVertexBuffer[] { null, null, null };
        Effect[] effects_ = new Effect[] { null, null, null };
        static DepthStencilState[] depthStates_ = new DepthStencilState[] {
            DepthStencilState.Default,
            DepthStencilState.None,
            new DepthStencilState {
                Name = "DepthStencilState.Greater",
                DepthBufferFunction = CompareFunction.GreaterEqual,
                DepthBufferEnable = true,
                DepthBufferWriteEnable = false
            } };
        static int[] drawSequence_ = { 2, 0, 1 };

        public void SetEffect(Effect depthTestEffect, Effect noDepthEffect = null, Effect lessDepthEffect = null)
        {
            effects_[0] = depthTestEffect;
            effects_[1] = noDepthEffect ?? depthTestEffect;
            effects_[2] = lessDepthEffect?? depthTestEffect;
        }
        
        public void Render(GraphicsDevice device, Matrix worldViewProjection)
        {
            // Upload all data, update in standard order
            for (int i = 0; i < 3; ++i)
            {
                if (data_.lines_[i].Count > 0)
                {
                    if (lineVertexBuffers_[i] == null)
                        lineVertexBuffers_[i] = new DynamicVertexBuffer(device, VertexPositionColor.VertexDeclaration, 4096, BufferUsage.WriteOnly);
                    if (lineVertexBuffers_[i].VertexCount < data_.lines_[i].Count)
                    {
                        lineVertexBuffers_[i].Dispose();
                        lineVertexBuffers_[i] = new DynamicVertexBuffer(device, VertexPositionColor.VertexDeclaration, data_.lines_[i].Count, BufferUsage.WriteOnly);
                    }
                    lineVertexBuffers_[i].SetData(data_.lines_[i].ToArray(), 0, data_.lines_[i].Count, SetDataOptions.Discard);
                }

                if (data_.tris_[i].Count > 0)
                {
                    if (triVertexBuffers_[i] == null)
                        triVertexBuffers_[i] = new DynamicVertexBuffer(device, VertexPositionColor.VertexDeclaration, 4096, BufferUsage.WriteOnly);
                    if (triVertexBuffers_[i].VertexCount < data_.tris_[i].Count)
                    {
                        triVertexBuffers_[i].Dispose();
                        triVertexBuffers_[i] = new DynamicVertexBuffer(device, VertexPositionColor.VertexDeclaration, data_.tris_[i].Count, BufferUsage.WriteOnly);
                    }
                    triVertexBuffers_[i].SetData(data_.tris_[i].ToArray(), 0, data_.tris_[i].Count, SetDataOptions.Discard);
                }
            }

            var originalState = device.DepthStencilState;
            // Draw as Less-Depth -> No-Depth -> Depth-Test
            for (int p = 0; p < 3; ++p)
            {
                int i = drawSequence_[p];
                effects_[i].Parameters["WorldViewProj"].SetValue(worldViewProjection);
                if (data_.lines_[i].Count > 0 || data_.tris_[i].Count > 0)
                {
                    effects_[i].CurrentTechnique.Passes[0].Apply();
                    device.DepthStencilState = depthStates_[i];

                    if (data_.tris_[i].Count > 0)
                    {
                        device.SetVertexBuffer(triVertexBuffers_[i]);
                        device.DrawPrimitives(PrimitiveType.TriangleList, 0, data_.tris_[i].Count / 3);
                    }

                    if (data_.lines_[i].Count > 0)
                    {
                        device.SetVertexBuffer(lineVertexBuffers_[i]);
                        device.DrawPrimitives(PrimitiveType.LineList, 0, data_.lines_[i].Count / 2);
                    }
                }
            }
            device.DepthStencilState = originalState;
        }

        public void Dispose()
        {
            for (int i = 0; i < 3; ++i)
            {
                lineVertexBuffers_[i]?.Dispose();
                triVertexBuffers_[i]?.Dispose();
            }
        }

        #region Line Drawing Functions


        public void DrawWireTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            DrawLine(v0, v1, color, depthTest);
            DrawLine(v0, v2, color, depthTest);
            DrawLine(v1, v2, color, depthTest);
        }

        /// <summary>
        /// Renders a 2D grid
        /// </summary>
        /// <param name="xAxis">Vector direction for the local X-axis direction of the grid</param>
        /// <param name="yAxis">Vector direction for the local Y-axis of the grid</param>
        /// <param name="origin">3D starting anchor point for the grid</param>
        /// <param name="iXDivisions">Number of divisions in the local X-axis direction</param>
        /// <param name="iYDivisions">Number of divisions in the local Y-axis direction</param>
        /// <param name="color">Color of the grid lines</param>
        public void DrawWireGrid(Vector3 xAxis, Vector3 yAxis, Vector3 origin, int iXDivisions, int iYDivisions, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            Vector3 pos, step;

            pos = origin;
            step = xAxis / iXDivisions;
            for (int i = 0; i <= iXDivisions; i++)
            {
                DrawLine(pos, pos + yAxis, color, depthTest);
                pos += step;
            }

            pos = origin;
            step = yAxis / iYDivisions;
            for (int i = 0; i <= iYDivisions; i++)
            {
                DrawLine(pos, pos + xAxis, color, depthTest);
                pos += step;
            }
        }

        static Vector3 PointOnSphere(Vector3 center, float radius, float theta, float phi)
        {
            return new Vector3(
                (float)(center.X + radius * Math.Sin((float)theta) * Math.Sin((float)phi)),
                (float)(center.Y + radius * Math.Cos((float)phi)),
                (float)(center.Z + radius * Math.Cos((float)theta) * Math.Sin((float)phi))
            );
        }

        public void DrawWireSphere(Vector3 center, float radius, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            for (float j = 0; j < 180; j += 45)
            {
                for (float i = 0; i < 360; i += 45)
                {
                    Vector3 p1 = PointOnSphere(center, radius, MathHelper.ToRadians(i), MathHelper.ToRadians(j));
                    Vector3 p2 = PointOnSphere(center, radius, MathHelper.ToRadians(i + 45), MathHelper.ToRadians(j));
                    Vector3 p3 = PointOnSphere(center, radius, MathHelper.ToRadians(i), MathHelper.ToRadians(j + 45));
                    Vector3 p4 = PointOnSphere(center, radius, MathHelper.ToRadians(i + 45), MathHelper.ToRadians(j + 45));

                    DrawLine(p1, p2, color, depthTest);
                    DrawLine(p3, p4, color, depthTest);
                    DrawLine(p1, p3, color, depthTest);
                    DrawLine(p2, p4, color, depthTest);
                }
            }
        }

        public void DrawWireSphere(Matrix transform, float radius, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            for (float j = 0; j < 180; j += 45)
            {
                for (float i = 0; i < 360; i += 45)
                {
                    Vector3 p1 = Vector3.Transform(PointOnSphere(Vector3.Zero, radius, MathHelper.ToRadians(i), MathHelper.ToRadians(j)), transform);
                    Vector3 p2 = Vector3.Transform(PointOnSphere(Vector3.Zero, radius, MathHelper.ToRadians(i + 45), MathHelper.ToRadians(j)), transform);
                    Vector3 p3 = Vector3.Transform(PointOnSphere(Vector3.Zero, radius, MathHelper.ToRadians(i), MathHelper.ToRadians(j + 45)), transform);
                    Vector3 p4 = Vector3.Transform(PointOnSphere(Vector3.Zero, radius, MathHelper.ToRadians(i + 45), MathHelper.ToRadians(j + 45)), transform);

                    DrawLine(p1, p2, color, depthTest);
                    DrawLine(p3, p4, color, depthTest);
                    DrawLine(p1, p3, color, depthTest);
                    DrawLine(p2, p4, color, depthTest);
                }
            }
        }

        public void DrawWireShape(Vector3[] positionArray, ushort[] indexArray, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            for (int i = 0; i < indexArray.Length; i += 2)
                DrawLine(positionArray[indexArray[i]], positionArray[indexArray[i + 1]], color, depthTest);
        }

        public void DrawWireShape(Matrix transform, Vector3[] positionArray, ushort[] indexArray, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            for (int i = 0; i < indexArray.Length; i += 2)
                DrawLine(Vector3.Transform(positionArray[indexArray[i]], transform), Vector3.Transform(positionArray[indexArray[i + 1]], transform), color, depthTest);
        }

        static ushort[] cubeIndices = new ushort[] { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 };

        public void DrawWireFrustum(BoundingFrustum frustum, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            DrawWireShape(frustum.GetCorners(), cubeIndices, color, depthTest);
        }

        public void DrawWireBox(BoundingBox bounds, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            DrawWireShape(bounds.GetCorners(), cubeIndices, color, depthTest);
        }

        public void DrawWireBox(Matrix transform, BoundingBox bounds, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            DrawWireShape(transform, bounds.GetCorners(), cubeIndices, color, depthTest);
        }

        public void DrawWireBox(Matrix transform, float x, float y, float z, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            DrawWireShape(transform, new BoundingBox(new Vector3(-x, -y, -z), new Vector3(x, y, z)).GetCorners(), cubeIndices, color, depthTest);
        }

        public void DrawWireCylinder(Matrix transform, float radius, float height, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            Vector3 heightVec = new Vector3(0, height / 2, 0);
            Vector3 offsetXVec = new Vector3(radius, 0, 0);
            Vector3 offsetZVec = new Vector3(0, 0, radius);
            for (float i = 0; i < 360; i += 22.5f)
            {
                Vector3 p1 = PointOnSphere(Vector3.Zero, radius, i * DataExtensions.M_DEGTORAD, 90 * DataExtensions.M_DEGTORAD);
                Vector3 p2 = PointOnSphere(Vector3.Zero, radius, (i + 22.5f) * DataExtensions.M_DEGTORAD, 90 * DataExtensions.M_DEGTORAD);
                DrawLine(Vector3.Transform((p1 - heightVec), transform), Vector3.Transform((p2 - heightVec), transform), color, depthTest);
                DrawLine(Vector3.Transform((p1 + heightVec), transform), Vector3.Transform((p2 + heightVec), transform), color, depthTest);
            }
            DrawLine(Vector3.Transform(-heightVec + offsetXVec, transform), Vector3.Transform(heightVec + offsetXVec, transform), color, depthTest);
            DrawLine(Vector3.Transform(-heightVec + -offsetXVec, transform), Vector3.Transform(heightVec - offsetXVec, transform), color, depthTest);
            DrawLine(Vector3.Transform(-heightVec + offsetZVec, transform), Vector3.Transform(heightVec + offsetZVec, transform), color, depthTest);
            DrawLine(Vector3.Transform(-heightVec + -offsetZVec, transform), Vector3.Transform(heightVec - offsetZVec, transform), color, depthTest);
        }

        public void DrawWireCone(Matrix transform, float r, float h, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            Vector3 heightVec = new Vector3(0, h, 0);
            Vector3 offsetXVec = new Vector3(r, 0, 0);
            Vector3 offsetZVec = new Vector3(0, 0, r);

            for (float i = 0; i < 360; i += 22.5f)
            {
                Vector3 pt1 = PointOnSphere(Vector3.Zero, r, i * DataExtensions.M_DEGTORAD, 90 * DataExtensions.M_DEGTORAD);
                Vector3 pt2 = PointOnSphere(Vector3.Zero, r, (i + 22.5f) * DataExtensions.M_DEGTORAD, 90 * DataExtensions.M_DEGTORAD);

                DrawLine(Vector3.Transform((pt1 - heightVec), transform), Vector3.Transform((pt2 - heightVec), transform), color, depthTest);
            }

            DrawLine(Vector3.Transform((-heightVec + offsetXVec), transform), Vector3.Transform((heightVec), transform), color , depthTest);
            DrawLine(Vector3.Transform((-heightVec + -offsetXVec), transform), Vector3.Transform((heightVec), transform), color, depthTest);
            DrawLine(Vector3.Transform((-heightVec + offsetZVec), transform), Vector3.Transform((heightVec), transform), color , depthTest);
            DrawLine(Vector3.Transform((-heightVec + -offsetZVec), transform), Vector3.Transform((heightVec), transform), color, depthTest);
        }

        /// <summary>
        /// Renders a circular ring (tessellated circle)
        /// </summary>
        /// <param name="origin">Center point for the ring</param>
        /// <param name="majorAxis">Direction of the major-axis of the circle</param>
        /// <param name="minorAxis">Direction of hte minor-axis of the circle</param>
        /// <param name="color">Color of the ring lines</param>
        public void DrawWireRing(Vector3 origin, Vector3 majorAxis, Vector3 minorAxis, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            const int RING_SEGMENTS = 32;
            const float fAngleDelta = 2.0F * (float)Math.PI / RING_SEGMENTS;

            float cosDelta = (float)Math.Cos(fAngleDelta);
            float sinDelta = (float)Math.Sin(fAngleDelta);

            float cosAcc = 1;
            float sinAcc = 0;

            Vector3 zeroPos = Vector3.Zero;
            for (int i = 0; i < RING_SEGMENTS; ++i)
            {
                Vector3 pos = new Vector3(majorAxis.X * cosAcc + minorAxis.X * sinAcc + origin.X,
                                          majorAxis.Y * cosAcc + minorAxis.Y * sinAcc + origin.Y,
                                          majorAxis.Z * cosAcc + minorAxis.Z * sinAcc + origin.Z);
                if (i == 0)
                    zeroPos = pos;

                float newCos = cosAcc * cosDelta - sinAcc * sinDelta;
                float newSin = cosAcc * sinDelta + sinAcc * cosDelta;

                cosAcc = newCos;
                sinAcc = newSin;

                Vector3 nextPos;
                if (i == RING_SEGMENTS - 1)
                    nextPos = zeroPos;
                else
                {
                    nextPos = new Vector3(majorAxis.X * cosAcc + minorAxis.X * sinAcc + origin.X,
                                          majorAxis.Y * cosAcc + minorAxis.Y * sinAcc + origin.Y,
                                          majorAxis.Z * cosAcc + minorAxis.Z * sinAcc + origin.Z);
                }

                DrawLine(pos, nextPos, color, depthTest);
            }
        }

        public void DrawCross(Vector3 pt, float radius, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            DrawLine(pt - Vector3.UnitX * radius, pt + Vector3.UnitX * radius, color, depthTest);
            DrawLine(pt - Vector3.UnitY * radius, pt + Vector3.UnitY * radius, color, depthTest);
            DrawLine(pt - Vector3.UnitZ * radius, pt + Vector3.UnitZ * radius, color, depthTest);
        }

        /// <summary>
        /// Draw a ray of the given length
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="color"></param>
        /// <param name="length"></param>
        public void DrawRay(Ray ray, float length, Color color, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
        {
            DrawLine(ray.Position, ray.Position + ray.Direction * length, color, depthTest);
        }

        #endregion

        #region Triangle Mesh Drawing Functions

        public delegate Vector3 ShapeFunction(Vector2 coord);

        #region Parametric Functions

        public static Vector3 SphereFunction(Vector2 coord)
        {
            float phi = (float)(coord.X * Math.PI);
            float theta = (float)(coord.Y * 2 * Math.PI);
            return new Vector3(
                (float)(Math.Cos(theta) * Math.Sin(phi)),
                (float)(Math.Sin(theta) * Math.Sin(phi)),
                (float)Math.Cos(phi));
        }

        public static Vector3 CylinderFunction(Vector2 coord)
        {
            float theta = (float)(coord.Y * 2 * Math.PI);
            return new Vector3(
                (float)Math.Sin(theta),
                (float)Math.Cos(theta),
                coord.X);
        }

        public static Vector3 Torus(Vector2 coord)
        {
            float major = 1;
            float minor = 0.5f; // userdata
            float theta = coord.X * 2 * (float)Math.PI;
            float phi = coord.Y * 2 * (float)Math.PI;
            float beta = major + minor * (float)Math.Cos(phi);
            return new Vector3((float)Math.Cos(theta) * beta,
                        (float)Math.Sin(theta) * beta,
                        (float)Math.Sin(phi) * minor);
        }

        #endregion

        public void DrawMeshParametric(ShapeFunction fn, Color color, int slices = 32, int stacks = 32, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
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
                DrawTriangle(pts[indices[i]], pts[indices[i + 1]], pts[indices[i + 2]], color, depthTest);
        }

        public void DrawMeshParametric(ShapeFunction fn, Matrix transform, Color color, int slices = 16, int stacks = 8, DebugDrawDepth depthTest = DebugDrawDepth.Pass)
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
                DrawTriangle(pts[indices[i]], pts[indices[i + 1]], pts[indices[i + 2]], color, depthTest);
        }

        #endregion

        #region DrawData capture and playback

        public DebugDrawData DrawData { get { return data_; } }

        public DebugDrawData SaveData()
        {
            DebugDrawData d = new DebugDrawData();
            for (int i = 0; i < 3; ++i)
            {
                if (data_.lines_[i].Count > 0)
                    d.lines_[i].AddRange(data_.lines_[i]);
                if (data_.tris_[i].Count > 0)
                    d.tris_[i].AddRange(data_.tris_[i]);
            }
            return d;
        }

        public void AppendData(DebugDrawData savedData)
        {
            for (int i = 0; i < 3; ++i)
            {
                if (savedData.lines_[i].Count > 0)
                    data_.lines_[i].AddRange(savedData.lines_[i]);
                if (savedData.tris_[i].Count > 0)
                    data_.tris_[i].AddRange(savedData.tris_[i]);
            }
        }

        #endregion
    }
}
