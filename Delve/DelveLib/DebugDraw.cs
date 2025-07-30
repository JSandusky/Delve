//-----------------------------------------------------------------------------
// DebugDraw.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DelveLib
{
    /// <summary>
    /// Debug drawing routines for common collision shapes. These are not designed to be the most
    /// efficent way to submit geometry to the graphics device as they are intended for use in
    /// visualizing collision for debugging purposes.
    /// </summary>
    public class DebugDraw : IDisposable
    {
        #region Constants

        public const int MAX_VERTS = 2000;
        public const int MAX_INDICES = 2000;

        // Indices for drawing the edges of a cube, given the vertex ordering
        // used by Bounding(Frustum|Box|OrientedBox).GetCorners()
        static ushort[] cubeIndices = new ushort[] { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 };

        #endregion

        #region Fields

        public BasicEffect basicEffect;
        DynamicVertexBuffer vertexBuffer;
        DynamicIndexBuffer indexBuffer;

        ushort[] Indices = new ushort[MAX_INDICES];
        VertexPositionColor[] Vertices = new VertexPositionColor[MAX_VERTS];
        int IndexCount;
        int VertexCount;

        #endregion

        #region Initialization

        public DebugDraw(GraphicsDevice device)
        {
            vertexBuffer = new DynamicVertexBuffer(device, typeof(VertexPositionColor), MAX_VERTS, BufferUsage.WriteOnly);
            indexBuffer = new DynamicIndexBuffer(device, typeof(ushort), MAX_INDICES, BufferUsage.WriteOnly);
            basicEffect = new BasicEffect(device); //(device, null);
            basicEffect.LightingEnabled = false;
            basicEffect.VertexColorEnabled = true;
            basicEffect.TextureEnabled = false;
            basicEffect.World= Matrix.CreateScale(-1, 1, 1);
        }

        #endregion

        #region Dispose

        ~DebugDraw()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            lock (this)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        bool isDisposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                isDisposed = true;
                if (vertexBuffer != null)
                    vertexBuffer.Dispose();

                if (indexBuffer != null)
                    indexBuffer.Dispose();

                if (basicEffect != null)
                    basicEffect.Dispose();
            }
        }

        #endregion

        #region Draw

        /// <summary>
        /// Starts debug drawing by setting the required render states and camera information
        /// </summary>
        public void Begin(Matrix view, Matrix projection)
        {
            basicEffect.World = Matrix.Identity;
            basicEffect.View = view;
            basicEffect.Projection = projection;
            basicEffect.GraphicsDevice.BlendState = BlendState.NonPremultiplied;

            VertexCount = 0;
            IndexCount = 0;
        }

        /// <summary>
        /// Ends debug drawing and restores standard render states
        /// </summary>
        public void End()
        {
            FlushDrawing();
        }

        public void DrawWireShape(Vector3[] positionArray, ushort[] indexArray, Color color)
        {
            if (Reserve(positionArray.Length, indexArray.Length))
            {
                for (int i = 0; i < indexArray.Length; i++)
                    Indices[IndexCount++] = (ushort)(VertexCount + indexArray[i]);

                for (int i = 0; i < positionArray.Length; i++)
                    Vertices[VertexCount++] = new VertexPositionColor(positionArray[i], color);
            }
        }

        public void DrawWireShape(Matrix transform, Vector3[] positionArray, ushort[] indexArray, Color color)
        {
            if (Reserve(positionArray.Length, indexArray.Length))
            {
                for (int i = 0; i < indexArray.Length; i++)
                    Indices[IndexCount++] = (ushort)(VertexCount + indexArray[i]);

                for (int i = 0; i < positionArray.Length; i++)
                    Vertices[VertexCount++] = new VertexPositionColor(Vector3.Transform(positionArray[i], transform), color);
            }
        }

        // Draw any queued objects and reset our line buffers
        private void FlushDrawing()
        {
            lock (this)
            {
                if (IndexCount > 0 && !isDisposed)
                {
                    vertexBuffer.SetData(Vertices, 0, VertexCount, SetDataOptions.Discard);
                    indexBuffer.SetData(Indices, 0, IndexCount, SetDataOptions.Discard);

                    GraphicsDevice device = basicEffect.GraphicsDevice;
                    device.RasterizerState = RasterizerState.CullNone;
                    device.SetVertexBuffer(vertexBuffer);
                    device.Indices = indexBuffer;

                    if (vertexBuffer == null || indexBuffer == null || IndexCount == 0)
                        return;
                    foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, IndexCount / 2);
                    }

                    device.SetVertexBuffer(null);
                    device.Indices = null;
                }
                IndexCount = 0;
                VertexCount = 0;
            }
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

        /// <summary>
        /// Renders a 2D grid (must be called within a Begin/End pair)
        /// </summary>
        /// <param name="xAxis">Vector direction for the local X-axis direction of the grid</param>
        /// <param name="yAxis">Vector direction for the local Y-axis of the grid</param>
        /// <param name="origin">3D starting anchor point for the grid</param>
        /// <param name="iXDivisions">Number of divisions in the local X-axis direction</param>
        /// <param name="iYDivisions">Number of divisions in the local Y-axis direction</param>
        /// <param name="color">Color of the grid lines</param>
        public void DrawWireGrid(Vector3 xAxis, Vector3 yAxis, Vector3 origin, int iXDivisions, int iYDivisions, Color color)
        {
            Vector3 pos, step;

            pos = origin;
            step = xAxis / iXDivisions;
            for (int i = 0; i <= iXDivisions; i++)
            {
                DrawLine(pos, pos + yAxis, color);
                pos += step;
            }

            pos = origin;
            step = yAxis / iYDivisions;
            for (int i = 0; i <= iYDivisions; i++)
            {
                DrawLine(pos, pos + xAxis, color);
                pos += step;
            }
        }

        /// <summary>
        /// Renders the outline of a bounding frustum
        /// </summary>
        /// <param name="frustum">Bounding frustum to render</param>
        /// <param name="color">Color of the frustum lines</param>
        public void DrawWireFrustum(BoundingFrustum frustum, Color color)
        {
            DrawWireShape(frustum.GetCorners(), cubeIndices, color);
        }

        /// <summary>
        /// Renders the outline of an axis-aligned bounding box
        /// </summary>
        /// <param name="box">Bounding box to render</param>
        /// <param name="color">Color of the box lines</param>
        public void DrawWireBox(BoundingBox box, Color color)
        {
            DrawWireShape(box.GetCorners(), cubeIndices, color);
        }

        /// <summary>
        /// Renders the outline of an oriented bounding box
        /// </summary>
        /// <param name="box">Oriented bounding box to render</param>
        /// <param name="color">Color of the box lines</param>
        //public void DrawWireBox(BoundingOrientedBox box, Color color)
        //{
        //    DrawWireShape(box.GetCorners(), cubeIndices, color);
        //}

        /// <summary>
        /// Renders a circular ring (tessellated circle)
        /// </summary>
        /// <param name="origin">Center point for the ring</param>
        /// <param name="majorAxis">Direction of the major-axis of the circle</param>
        /// <param name="minorAxis">Direction of hte minor-axis of the circle</param>
        /// <param name="color">Color of the ring lines</param>
        public void DrawRing(Vector3 origin, Vector3 majorAxis, Vector3 minorAxis, Color color)
        {
            const int RING_SEGMENTS = 32;
            const float fAngleDelta = 2.0F * (float)Math.PI / RING_SEGMENTS;

            if (Reserve(RING_SEGMENTS, RING_SEGMENTS * 2))
            {
                for (int i = 0; i < RING_SEGMENTS; i++)
                {
                    Indices[IndexCount++] = (ushort)(VertexCount + i);
                    Indices[IndexCount++] = (ushort)(VertexCount + (i + 1) % RING_SEGMENTS);
                }
                float cosDelta = (float)Math.Cos(fAngleDelta);
                float sinDelta = (float)Math.Sin(fAngleDelta);

                float cosAcc = 1;
                float sinAcc = 0;

                for (int i = 0; i < RING_SEGMENTS; ++i)
                {
                    Vector3 pos = new Vector3(majorAxis.X * cosAcc + minorAxis.X * sinAcc + origin.X,
                                              majorAxis.Y * cosAcc + minorAxis.Y * sinAcc + origin.Y,
                                              majorAxis.Z * cosAcc + minorAxis.Z * sinAcc + origin.Z);

                    Vertices[VertexCount++] = new VertexPositionColor(pos, color);

                    float newCos = cosAcc * cosDelta - sinAcc * sinDelta;
                    float newSin = cosAcc * sinDelta + sinAcc * cosDelta;

                    cosAcc = newCos;
                    sinAcc = newSin;
                }
            }
        }

        /// <summary>
        /// Renders the outline of a bounding sphere.
        /// 
        /// This code assumes that the model and view matrices contain only rigid motion.
        /// </summary>
        /// <param name="sphere">Bounding sphere to render</param>
        /// <param name="color">Color of the outline lines</param>
        public void DrawWireSphere(BoundingSphere sphere, Color color)
        {
            // Invert the modelview matrix to get direction vectors
            // in screen space, so we can draw a circle that always
            // faces the camera.
            Matrix view = basicEffect.World * basicEffect.View;
            Matrix.Transpose(ref view, out view);
            DrawRing(sphere.Center, view.Right * sphere.Radius, view.Up * sphere.Radius, color);
        }

        public void DrawCross(Vector3 pt, float radius, Color color)
        {
            DrawLine(pt - Vector3.UnitX * radius, pt + Vector3.UnitX * radius, color);
            DrawLine(pt - Vector3.UnitY * radius, pt + Vector3.UnitY * radius, color);
            DrawLine(pt - Vector3.UnitZ * radius, pt + Vector3.UnitZ * radius, color);
        }

        /// <summary>
        /// Draw a ray of the given length
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="color"></param>
        /// <param name="length"></param>
        public void DrawRay(Ray ray, Color color, float length)
        {
            DrawLine(ray.Position, ray.Position + ray.Direction * length, color);
        }

        public void DrawLine(Vector3 v0, Vector3 v1, Color color)
        {
            if (Reserve(2, 2))
            {
                Indices[IndexCount++] = (ushort)VertexCount;
                Indices[IndexCount++] = (ushort)(VertexCount + 1);
                Vertices[VertexCount++] = new VertexPositionColor(v0, color);
                Vertices[VertexCount++] = new VertexPositionColor(v1, color);
            }
        }

        public void DrawWireTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Color color)
        {
            if (Reserve(3, 6))
            {
                Indices[IndexCount++] = (ushort)(VertexCount + 0);
                Indices[IndexCount++] = (ushort)(VertexCount + 1);
                Indices[IndexCount++] = (ushort)(VertexCount + 1);
                Indices[IndexCount++] = (ushort)(VertexCount + 2);
                Indices[IndexCount++] = (ushort)(VertexCount + 2);
                Indices[IndexCount++] = (ushort)(VertexCount + 0);

                Vertices[VertexCount++] = new VertexPositionColor(v0, color);
                Vertices[VertexCount++] = new VertexPositionColor(v1, color);
                Vertices[VertexCount++] = new VertexPositionColor(v2, color);
            }
        }

        //public void DrawWireTriangle(Triangle t, Color color)
        //{
        //    DrawWireTriangle(t.V0, t.V1, t.V2, color);
        //}

        #endregion

        #region Extended shapes

        public static Vector3 PointOnSphere(Vector3 center, float radius, float theta, float phi)
        {
            return new Vector3(
                (float)(center.X + radius * Math.Sin((float)theta) * Math.Sin((float)phi)),
                (float)(center.Y + radius * Math.Cos((float)phi)),
                (float)(center.Z + radius * Math.Cos((float)theta) * Math.Sin((float)phi))
            );
        }

        public void DrawSphere(Vector3 center, float radius, Color color)
        {
            for (float j = 0; j < 180; j += 45)
            {
                for (float i = 0; i < 360; i += 45)
                {
                    Vector3 p1 = PointOnSphere(center, radius, MathHelper.ToRadians(i), MathHelper.ToRadians(j));
                    Vector3 p2 = PointOnSphere(center, radius, MathHelper.ToRadians(i + 45), MathHelper.ToRadians(j));
                    Vector3 p3 = PointOnSphere(center, radius, MathHelper.ToRadians(i), MathHelper.ToRadians(j + 45));
                    Vector3 p4 = PointOnSphere(center, radius, MathHelper.ToRadians(i + 45), MathHelper.ToRadians(j + 45));

                    DrawLine(p1, p2, color);
                    DrawLine(p3, p4, color);
                    DrawLine(p1, p3, color);
                    DrawLine(p2, p4, color);
                }
            }
        }

        public void DrawSphere(Matrix transform, float radius, Color color)
        {
            for (float j = 0; j < 180; j += 45)
            {
                for (float i = 0; i < 360; i += 45)
                {
                    Vector3 p1 = Vector3.Transform(PointOnSphere(Vector3.Zero, radius, MathHelper.ToRadians(i), MathHelper.ToRadians(j)), transform);
                    Vector3 p2 = Vector3.Transform(PointOnSphere(Vector3.Zero, radius, MathHelper.ToRadians(i + 45), MathHelper.ToRadians(j)), transform);
                    Vector3 p3 = Vector3.Transform(PointOnSphere(Vector3.Zero, radius, MathHelper.ToRadians(i), MathHelper.ToRadians(j + 45)), transform);
                    Vector3 p4 = Vector3.Transform(PointOnSphere(Vector3.Zero, radius, MathHelper.ToRadians(i + 45), MathHelper.ToRadians(j + 45)), transform);

                    DrawLine(p1, p2, color);
                    DrawLine(p3, p4, color);
                    DrawLine(p1, p3, color);
                    DrawLine(p2, p4, color);
                }
            }
        }

        public void DrawCone(Matrix transform, float r, float h, Color color)
        {
            Vector3 heightVec = new Vector3(0, h, 0);
            Vector3 offsetXVec = new Vector3(r, 0, 0);
            Vector3 offsetZVec = new Vector3(0, 0, r);

            for (float i = 0; i < 360; i += 22.5f)
            {
                Vector3 pt1 = PointOnSphere(Vector3.Zero, r, i * DataExtensions.M_DEGTORAD, 90 * DataExtensions.M_DEGTORAD);
                Vector3 pt2 = PointOnSphere(Vector3.Zero, r, (i + 22.5f) * DataExtensions.M_DEGTORAD, 90 * DataExtensions.M_DEGTORAD);

                DrawLine(Vector3.Transform((pt1 - heightVec), transform), Vector3.Transform((pt2 - heightVec), transform), color);
            }

            DrawLine(Vector3.Transform((-heightVec + offsetXVec), transform),  Vector3.Transform((heightVec), transform), color);
            DrawLine(Vector3.Transform((-heightVec + -offsetXVec), transform), Vector3.Transform((heightVec), transform), color);
            DrawLine(Vector3.Transform((-heightVec + offsetZVec), transform),  Vector3.Transform((heightVec), transform), color);
            DrawLine(Vector3.Transform((-heightVec + -offsetZVec), transform), Vector3.Transform((heightVec), transform), color);
        }

        public void DrawCylinder(Matrix transform, float radius, float height, Color color)
        {
            Vector3 heightVec = new Vector3(0, height/2, 0);
            Vector3 offsetXVec = new Vector3(radius, 0, 0);
            Vector3 offsetZVec = new Vector3(0, 0, radius);
            for (float i = 0; i < 360; i += 22.5f)
            {
                Vector3 p1 = PointOnSphere(Vector3.Zero, radius, i * DataExtensions.M_DEGTORAD, 90 * DataExtensions.M_DEGTORAD);
                Vector3 p2 = PointOnSphere(Vector3.Zero, radius, (i + 22.5f) * DataExtensions.M_DEGTORAD, 90 * DataExtensions.M_DEGTORAD);
                DrawLine(Vector3.Transform((p1 - heightVec), transform), Vector3.Transform((p2 - heightVec), transform), color);
                DrawLine(Vector3.Transform((p1 + heightVec), transform), Vector3.Transform((p2 + heightVec), transform), color);
            }
            DrawLine(Vector3.Transform(-heightVec + offsetXVec, transform),  Vector3.Transform(heightVec + offsetXVec, transform), color);
            DrawLine(Vector3.Transform(-heightVec + -offsetXVec, transform), Vector3.Transform(heightVec - offsetXVec, transform), color);
            DrawLine(Vector3.Transform(-heightVec + offsetZVec, transform),  Vector3.Transform(heightVec + offsetZVec, transform), color);
            DrawLine(Vector3.Transform(-heightVec + -offsetZVec, transform), Vector3.Transform(heightVec - offsetZVec, transform), color);
        }

        public void DrawBox(Matrix transform, float x, float y, float z, Color color)
        {

            DrawWireShape(transform, new BoundingBox(new Vector3(-x, -y, -z), new Vector3(x, y, z)).GetCorners(), cubeIndices, color);
        }

        public void DrawCapsule(Matrix transform, float height, float radius, Color color)
        {
            int stepDegrees = 30;

            Vector3 capStart = new Vector3(0, 0, 0);
            float halfHeight = height / 2.0f;
            capStart.Z = -halfHeight;

            Vector3 capEnd = new Vector3(0, 0, 0);
            capEnd.Z = halfHeight;

            // Draw the ends
            {
                Vector3 center = capStart;
                Vector3 up = new Vector3(1, 0, 0);
                Vector3 axis = new Vector3(0, 0, -1);
                float minTh = -DataExtensions.M_HALF_PI;
                float maxTh = DataExtensions.M_HALF_PI;
                float minPs = -DataExtensions.M_HALF_PI;
                float maxPs = DataExtensions.M_HALF_PI;

                DrawSpherePatch(transform, capStart, up, axis, radius, minTh, maxTh, minPs, maxPs, color);
            }

            {
                Vector3 up = new Vector3(-1, 0, 0);
                Vector3 axis = new Vector3(0, 0, 1);
                float minTh = -DataExtensions.M_HALF_PI;
                float maxTh = DataExtensions.M_HALF_PI;
                float minPs = -DataExtensions.M_HALF_PI;
                float maxPs = DataExtensions.M_HALF_PI;
                DrawSpherePatch(transform, capEnd, up, axis, radius, minTh, maxTh, minPs, maxPs, color);
            }

            // Draw lines for the tube
            Vector3 start = transform.Translation;
            for (int i = 0; i < 360; i += stepDegrees)
            {
                capEnd.Y = capStart.Y = Mathf.Sin(i * DataExtensions.M_DEGTORAD) * radius;
                capEnd.X = capStart.X = Mathf.Cos(i * DataExtensions.M_DEGTORAD) * radius;
                DrawLine(Vector3.Transform(capStart, transform), Vector3.Transform(capEnd, transform), color);
            }
        }

        public unsafe void DrawSpherePatch(Matrix transform, Vector3 center, Vector3 up, Vector3 axis, float radius, float minTh, float maxTh, float minPs, float maxPs, Color color, bool drawCenter = false)
        {
            float stepDegrees = 30.0f;
            Vector3* vA = stackalloc Vector3[74];
            Vector3* vB = stackalloc Vector3[74];
            Vector3* pvA = vA;
            Vector3* pvB = vB;
            Vector3* pT = null;
            Vector3 npole = center + up * radius;
            Vector3 spole = center - up * radius;
            Vector3 arcStart = new Vector3();
            float step = stepDegrees * DataExtensions.M_DEGTORAD;
            Vector3 kv = up;
            Vector3 iv = axis;
            Vector3 jv = Vector3.Cross(kv, iv);
            bool drawN = false;
            bool drawS = false;
            if (minTh <= -DataExtensions.M_HALF_PI)
            {
                minTh = -DataExtensions.M_HALF_PI + step;
                drawN = true;
            }
            if (maxTh >= DataExtensions.M_HALF_PI)
            {
                maxTh = DataExtensions.M_HALF_PI - step;
                drawS = true;
            }
            if (minTh > maxTh)
            {
                minTh = -DataExtensions.M_HALF_PI + step;
                maxTh = DataExtensions.M_HALF_PI - step;
                drawN = drawS = true;
            }
            int n_hor = (int)((maxTh - minTh) / step) + 1;
            if (n_hor < 2)
                n_hor = 2;
            float step_h = (maxTh - minTh) / (float)(n_hor - 1);
            bool isClosed = false;
            if (minPs > maxPs)
            {
                minPs = -DataExtensions.M_PI + step;
                maxPs = DataExtensions.M_PI;
                isClosed = true;
            }
            else if ((maxPs - minPs) >= DataExtensions.M_PI * (float)(2.0f))
            {
                isClosed = true;
            }
            else
            {
                isClosed = false;
            }
            int n_vert = (int)((maxPs - minPs) / step) + 1;
            if (n_vert < 2) n_vert = 2;
            float step_v = (maxPs - minPs) / (float)(n_vert - 1);
            for (int i = 0; i < n_hor; i++)
            {
                float th = minTh + (float)(i) * step_h;
                float sth = radius * Mathf.Sin(th);
                float cth = radius * Mathf.Cos(th);
                for (int j = 0; j < n_vert; j++)
                {
                    float psi = minPs + j * step_v;
                    float sps = Mathf.Sin(psi);
                    float cps = Mathf.Cos(psi);
                    pvB[j] = center + cth * cps * iv + cth * sps * jv + sth * kv;

                    if (i > 0)
                        DrawLine(Vector3.Transform(pvA[j], transform), Vector3.Transform(pvB[j], transform), color);
                    else if (drawS)
                        DrawLine(Vector3.Transform(spole, transform), Vector3.Transform(pvB[j], transform), color);

                    if (j != 0)
                        DrawLine(Vector3.Transform(pvB[j-1], transform), Vector3.Transform(pvB[j], transform), color);
                    else
                        arcStart = pvB[j];

                    if ((i == (n_hor - 1)) && drawN)
                        DrawLine(Vector3.Transform(npole, transform), Vector3.Transform(pvB[j], transform), color);

                    if (drawCenter)
                    {
                        if (isClosed)
                        {
                            if (j == (n_vert - 1))
                                DrawLine(Vector3.Transform(arcStart, transform), Vector3.Transform(pvB[j], transform), color);
                        }
                        else
                        {
                            if (((i == 0) || (i == (n_hor - 1))) && ((j == 0) || (j == (n_vert - 1))))
                                DrawLine(Vector3.Transform(center, transform), Vector3.Transform(pvB[j], transform), color);
                        }
                    }
                }
                pT = pvA; pvA = pvB; pvB = pT;
            }
        }

        Vector3 POINT_ON_ELLIPSOID(float EX, float EY, float EZ, float THETA, float PHI) {
            return new Vector3(
                (float)(EX * Math.Sin((float)(THETA) * DataExtensions.M_DEGTORAD) * Math.Sin((float)(PHI) * DataExtensions.M_DEGTORAD)),
                (float)(EY * Math.Cos((float)(PHI) * DataExtensions.M_DEGTORAD)),
                (float)(EZ * Math.Cos((float)(THETA) * DataExtensions.M_DEGTORAD) * Math.Sin((float)(PHI) * DataExtensions.M_DEGTORAD)));
        }

        public void DrawEllipsoid(Matrix transform, float x, float y, float z, Color color)
        {
            for (float j = 0; j < 180; j += 22.5f)
            {
                for (float i = 0; i < 360; i += 22.5f)
                {
                    Vector3 p1 = POINT_ON_ELLIPSOID(x, y, z, i, j);
                    Vector3 p2 = POINT_ON_ELLIPSOID(x, y, z, i + 22.5f, j);
                    Vector3 p3 = POINT_ON_ELLIPSOID(x, y, z, i, j + 22.5f);
                    Vector3 p4 = POINT_ON_ELLIPSOID(x, y, z, i + 22.5f, j + 22.5f);

                    DrawLine(Vector3.Transform(p1, transform), Vector3.Transform(p2, transform), color);
                    DrawLine(Vector3.Transform(p3, transform), Vector3.Transform(p4, transform), color);
                    DrawLine(Vector3.Transform(p1, transform), Vector3.Transform(p3, transform), color);
                    DrawLine(Vector3.Transform(p2, transform), Vector3.Transform(p4, transform), color);
                }
            }
        }

        public void DrawTorus(Matrix transform, float innerRadius, float outerRadius, Color color)
        {
            // http://paulbourke.net/geometry/torus/
            int u, v, du = 30, dv = 30;
            double r0 = innerRadius, r1 = outerRadius;
            double theta, phi;
            Vector3[] p = new Vector3[4];

            for (u = 0; u < 360; u += du)
            {
                for (v = 0; v < 360; v += dv)
                {
                    theta = (u) * DataExtensions.M_DEGTORAD;
                    phi = (v) * DataExtensions.M_DEGTORAD;
                    p[0].X = (float)(Math.Cos(theta) * (r0 + r1 * Math.Cos(phi)));
                    p[0].Z = (float)(Math.Sin(theta) * (r0 + r1 * Math.Cos(phi)));
                    p[0].Y = (float)(r1 * Math.Sin(phi));
                    theta = (u + du) * DataExtensions.M_DEGTORAD;
                    phi = (v) * DataExtensions.M_DEGTORAD;
                    p[1].X = (float)(Math.Cos(theta) * (r0 + r1 * Math.Cos(phi)));
                    p[1].Z = (float)(Math.Sin(theta) * (r0 + r1 * Math.Cos(phi)));
                    p[1].Y = (float)(r1 * Math.Sin(phi));
                    theta = (u + du) * DataExtensions.M_DEGTORAD;
                    phi = (v + dv) * DataExtensions.M_DEGTORAD;
                    p[2].X = (float)(Math.Cos(theta) * (r0 + r1 * Math.Cos(phi)));
                    p[2].Z = (float)(Math.Sin(theta) * (r0 + r1 * Math.Cos(phi)));
                    p[2].Y = (float)(r1 * Math.Sin(phi));
                    theta = (u) * DataExtensions.M_DEGTORAD;
                    phi = (v + dv) * DataExtensions.M_DEGTORAD;
                    p[3].X = (float)(Math.Cos(theta) * (r0 + r1 * Math.Cos(phi)));
                    p[3].Z = (float)(Math.Sin(theta) * (r0 + r1 * Math.Cos(phi)));
                    p[3].Y = (float)(r1 * Math.Sin(phi));

                    DrawLine(Vector3.Transform(p[0], transform), Vector3.Transform(p[1], transform), color);
                    DrawLine(Vector3.Transform(p[2], transform), Vector3.Transform(p[3], transform), color);
                    DrawLine(Vector3.Transform(p[0], transform), Vector3.Transform(p[3], transform), color);
                    DrawLine(Vector3.Transform(p[1], transform), Vector3.Transform(p[2], transform), color);
                }
            }
        }

        #endregion
    }
}
