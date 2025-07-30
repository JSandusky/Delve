using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DelveLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Delve.Graphics
{
    public struct Cone
    {
        public Vector3 d;
        public float h;
        public Vector3 T;
        public float r;
    }

    public struct TileFrustum
    {
        public Plane Near;
        public Plane Far;
        public Plane Left;
        public Plane Right;
        public Plane Top;
        public Plane Bottom;
    }


    public static class TiledShading
    {
        // Convert clip space coordinates to view space
        static Vector4 ClipToView(Matrix inverseProjection, Vector4 clip)
        {
            Vector4 view = Vector4.Transform(clip, inverseProjection);
            view = view / view.W;
            return view;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TileIndex(int x, int y, int dimX)
        {
            return (y * dimX) + x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TileIndex(int x, int y, int z, int dimX, int dimY)
        {
            return (z * dimY * dimX) + (y * dimX) + x;
        }

        public static TileFrustum[] ComputeTileFrustums(Matrix projection, int tilesX, int tilesY, int tileW, int tileH, Vector2 screenDim)
        {
            Matrix invProj = Matrix.Invert(projection);
            BoundingFrustum frus = new BoundingFrustum(projection);
            TileFrustum[] ret = new TileFrustum[tilesX * tilesY];
            for (int x = 0; x < tilesX; ++x)
            {
                for (int y = 0; y < tilesY; ++y)
                    ret[TileIndex(x, y, tilesX)] = Tiled_ComputeFrustum(invProj, x, y, tileW, tileH, screenDim, frus.Near, frus.Far);
            }
            return ret;
        }

        public static TileFrustum[] Clustered_ComputeTileFrustums(Matrix projection, int tilesX, int tilesY, int tilesZ, int tileW, int tileH, int tileZ, Vector2 screenDim)
        {
            Matrix invProj = Matrix.Invert(projection);
            TileFrustum[] ret = new TileFrustum[tilesX * tilesY * tilesZ];
            for (int x = 0; x < tilesX; ++x)
            {
                for (int y = 0; y < tilesY; ++y)
                {
                    for (int z = 0; z < tilesZ; ++z)
                        ret[TileIndex(x, y, z, tilesX, tilesY)] = Clustered_ComputeFrustum(invProj, x, y, z, tileW, tileH, tileZ, screenDim);
                }
            }
            return ret;
        }

        // Convert screen space coordinates to view space.
        static Vector4 ScreenToView(Vector4 screen, Vector2 screenDimensions, Matrix inverseProjection)
        {
            // Convert to normalized texture coordinates
            Vector2 texCoord = screen.XY() / screenDimensions;

            // Convert to clip space
            Vector4 clip = new Vector4(texCoord.X * 2 - 1, (1.0f - texCoord.Y) * 2.0f - 1.0f, screen.Z, screen.W);

            return ClipToView(inverseProjection, clip);
        }

        public static TileFrustum Tiled_ComputeFrustum(Matrix inverseProjection, int x, int y, int w, int h, Vector2 screenDimensions, Plane near, Plane far)
        {
            Vector4[] screenSpace =
            {
                new Vector4(x * w, y * h, -1, 1),
                new Vector4((x+1) * w, y * h, -1, 1),
                new Vector4(x * w, (y+1) * h, -1, 1),
                new Vector4((x+1) * w, (y+1) * h, -1, 1)
            };

            Vector3[] viewSpace = { Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero };
            for (int i = 0; i < screenSpace.Length; ++i)
                viewSpace[i] = ScreenToView(screenSpace[i], screenDimensions, inverseProjection).XYZ();

            return new TileFrustum
            {
                Near = near,
                Far = far,
                Left = new Plane(Vector3.Zero, viewSpace[2], viewSpace[0]),
                Right = new Plane(Vector3.Zero, viewSpace[1], viewSpace[3]),
                Top = new Plane(Vector3.Zero, viewSpace[0], viewSpace[1]),
                Bottom = new Plane(Vector3.Zero, viewSpace[3], viewSpace[2])
            };
        }

        public static TileFrustum Clustered_ComputeFrustum(Matrix inverseProjection, int x, int y, int z, int w, int h, int d, Vector2 screenDimensions)
        {
            Vector4[] screenSpace =
            {
                new Vector4(x * w, y * h, -1, 1),
                new Vector4((x+1) * w, y * h, -1, 1),
                new Vector4(x * w, (y+1) * h, -1, 1),
                new Vector4((x+1) * w, (y+1) * h, -1, 1),

                new Vector4(x*w, y*h,  z*d, 1),
                new Vector4((x+1)*w, y*h, z*d, 1),
                new Vector4(x*w, (y+1)*h, z*d, 1),

                new Vector4(x*w, y*h,  -1,    (d+1)*z),
                new Vector4((x+1)*w, y*h, -1, (d+1)*z),
                new Vector4(x*w, (y+1)*h, -1, (d+1)*z),
            };

            Vector3[] viewSpace = { Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero,
                Vector3.Zero, Vector3.Zero, Vector3.Zero,
                Vector3.Zero, Vector3.Zero, Vector3.Zero
            };
            for (int i = 0; i < screenSpace.Length; ++i)
                viewSpace[i] = ScreenToView(screenSpace[i], screenDimensions, inverseProjection).XYZ();

            return new TileFrustum {
                Near = new Plane(viewSpace[4], viewSpace[5], viewSpace[6]),
                Far = new Plane(viewSpace[9] , viewSpace[8], viewSpace[7]),

                Left = new Plane(Vector3.Zero, viewSpace[2], viewSpace[0]),
                Right = new Plane(Vector3.Zero, viewSpace[1], viewSpace[3]),
                Top = new Plane(Vector3.Zero, viewSpace[0], viewSpace[1]),
                Bottom = new Plane(Vector3.Zero, viewSpace[3], viewSpace[2])
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool PointInsidePlane(ref Vector3 p, ref Plane plane)
        {
            return Vector3.Dot(plane.Normal, p) - plane.D < 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ConeInsidePlane(ref Cone cone, Plane plane)
        {
            Vector3 m = Vector3.Cross(Vector3.Cross(plane.Normal, cone.d), cone.d);
            Vector3 Q = cone.T + cone.d * cone.h - m * cone.r;
            return PointInsidePlane(ref cone.T, ref plane) && PointInsidePlane(ref Q, ref plane);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SphereInsideFrustum(ref Vector3 pos, float radius, ref TileFrustum frustum)
        {
            bool result = true;

            result &= Vector3.Dot(frustum.Left.Normal, pos) - frustum.Left.D >= -radius;
            result &= Vector3.Dot(frustum.Right.Normal, pos) - frustum.Right.D >= -radius;
            result &= Vector3.Dot(frustum.Top.Normal, pos) - frustum.Top.D >= -radius;
            result &= Vector3.Dot(frustum.Bottom.Normal, pos) - frustum.Bottom.D >= -radius;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ConeInsideFrustum(ref Cone cone, ref BoundingFrustum frustum)
        {
            bool result = true;

            // First check the near and far clipping planes.
            result &= (ConeInsidePlane(ref cone, frustum.Near) || ConeInsidePlane(ref cone, frustum.Far));
            result &= (ConeInsidePlane(ref cone, frustum.Left));
            result &= (ConeInsidePlane(ref cone, frustum.Right));
            result &= (ConeInsidePlane(ref cone, frustum.Top));
            result &= (ConeInsidePlane(ref cone, frustum.Bottom));

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ConeInsideFrustum(ref Cone cone, ref TileFrustum frustum)
        {
            bool result = true;

            result &= (ConeInsidePlane(ref cone, frustum.Left));
            result &= (ConeInsidePlane(ref cone, frustum.Right));
            result &= (ConeInsidePlane(ref cone, frustum.Top));
            result &= (ConeInsidePlane(ref cone, frustum.Bottom));

            return result;
        }

    }

    /// <summary>
    /// Manage the tiles and other associated data.
    /// Only size changes, clears, and offset calculation are single-threaded.
    /// Everything else uses Parallel.For and the offset data to blockcopy.
    /// </summary>
    public class TiledShadingData
    {
        TileFrustum[] cells_;
        int tileCtX_;
        int tileCtY_;
        int tileCtZ_;
        int totalTileCount_;
        int totalCones_;
        int totalPoints_;
        int[] coneOffsets_;
        int[] pointOffsets_;
        List<Cone>[] cones_;
        List<Vector4>[] pointLights_;

        public void OnScreenSizeChanged(Matrix projection, int tileSizeX, int tileSizeY, int w, int h)
        {
            tileCtX_ = (int)Math.Ceiling(w / (double)tileSizeX);
            tileCtY_ = (int)Math.Ceiling(h / (double)tileSizeY);
            tileCtX_ = Math.Max(tileCtX_, 1);
            tileCtY_ = Math.Max(tileCtY_, 1);
            totalTileCount_ = tileCtX_ * tileCtY_;
            cells_ = TiledShading.ComputeTileFrustums(projection, tileCtX_, tileCtY_, tileSizeX, tileSizeY, new Vector2(w, h));

            coneOffsets_ = new int[totalTileCount_];
            pointOffsets_ = new int[totalTileCount_];
            cones_ = new List<Cone>[totalTileCount_];
            for (int x = 0; x < totalTileCount_; ++x)
                cones_[x] = new List<Cone>();

            pointLights_ = new List<Vector4>[totalTileCount_];
            for (int x = 0; x < totalTileCount_; ++x)
                pointLights_[x] = new List<Vector4>();
        }

        public void Clustered_OnScreenSizeChanged(Matrix projection, int tileSizeX, int tileSizeY, int tileSizeZ, int w, int h, int d)
        {
            tileCtX_ = (int)Math.Ceiling(w / (double)tileSizeX);
            tileCtY_ = (int)Math.Ceiling(h / (double)tileSizeY);
            tileCtZ_ = (int)Math.Ceiling(d / (double)tileSizeZ);
            tileCtX_ = Math.Max(tileCtX_, 1);
            tileCtY_ = Math.Max(tileCtY_, 1);
            tileCtZ_ = Math.Max(tileCtZ_, 1);
            totalTileCount_ = tileCtX_ * tileCtY_ * tileCtZ_;

            if (tileCtZ_ > 1)
                cells_ = TiledShading.Clustered_ComputeTileFrustums(projection, tileCtX_, tileCtY_, tileCtZ_, tileSizeX, tileSizeY, tileSizeZ, new Vector2(w, h));
            else
                cells_ = TiledShading.ComputeTileFrustums(projection, tileCtX_, tileCtY_, tileSizeX, tileSizeY, new Vector2(w, h));

            coneOffsets_ = new int[totalTileCount_];
            pointOffsets_ = new int[totalTileCount_];
            cones_ = new List<Cone>[totalTileCount_];
            for (int x = 0; x < totalTileCount_; ++x)
                cones_[x] = new List<Cone>();

            pointLights_ = new List<Vector4>[totalTileCount_];
            for (int x = 0; x < totalTileCount_; ++x)
                pointLights_[x] = new List<Vector4>();
        }

        public void OnEndFrame()
        {
            for (int x = 0; x < totalTileCount_; ++x)
            {
                cones_[x].Clear();
                pointLights_[x].Clear();
            }
        }

        public bool AddCone(Cone cone)
        {
            bool added = false;
            Parallel.For(0, totalTileCount_, x =>
            {
                if (TiledShading.ConeInsideFrustum(ref cone, ref cells_[x]))
                {
                    cones_[x].Add(cone);
                    added = true;
                }
            });
            return added;
        }

        public bool AddPointLight(Vector4 point)
        {
            bool added = false;
            Vector3 pos = point.XYZ();
            float r = point.W;
            Parallel.For(0, totalTileCount_, x =>
            {
                if (TiledShading.SphereInsideFrustum(ref pos, r, ref cells_[x]))
                {
                    pointLights_[x].Add(point);
                    added = true;
                }
            });
            return added;
        }

        public void WriteLightTable(ConstantBuffer offsetCBuffer, StructuredBuffer pointsTable, StructuredBuffer conesTable)
        {
            // offsets are not computed in parallel
            CalculateOffsets();

            // write where to look in the structured buffers
            byte[] lightIndexTableData = new byte[4 * 4 * totalTileCount_];
            Parallel.For(0, totalTileCount_, t =>
            {
                int[] data = { pointLights_[t].Count, cones_[t].Count, pointOffsets_[t], coneOffsets_[t] };
                Buffer.BlockCopy(data, 0, lightIndexTableData, t * 4 * 4, data.Length * 4);
            });
            offsetCBuffer.SetData(lightIndexTableData);

            int coneSize = Marshal.SizeOf(typeof(Cone));
            byte[] coneLightTableData = new byte[totalCones_ * coneSize];
            Parallel.For(0, totalTileCount_, t =>
            {
                var coneList = cones_[t];
                for (int c = 0; c < coneList.Count; ++c)
                {
                    var cone = coneList[c];
                    float[] data = { cone.T.X, cone.T.Y, cone.T.Z, cone.h, cone.d.X, cone.d.Y, cone.d.Z, cone.r };
                    Buffer.BlockCopy(data, 0, coneLightTableData, (coneOffsets_[t]+c) * coneSize, data.Length * 4);
                }
            });
            conesTable.SetData(coneLightTableData);

            int pointSize = Marshal.SizeOf(typeof(Vector4));
            byte[] pointLightTableData = new byte[totalCones_ * pointSize];
            Parallel.For(0, totalTileCount_, t =>
            {
                var pointList = pointLights_[t];
                for (int c = 0; c < pointList.Count; ++c)
                {
                    var point = pointList[c];
                    float[] data = { point.X, point.Y, point.Z, point.W };
                    Buffer.BlockCopy(data, 0, pointLightTableData, (pointOffsets_[t] + c) * pointSize, data.Length * 4);
                }
            });
            pointsTable.SetData(pointLightTableData);
        }

        void CalculateOffsets()
        {
            totalCones_ = 0;
            totalPoints_ = 0;
            int coneSize = Marshal.SizeOf(typeof(Cone));
            int pointSize = Marshal.SizeOf(typeof(Vector4));
            for (int t = 0; t < totalTileCount_; ++t)
            {
                int coneCt = cones_[t].Count;
                int pointCt = pointLights_[t].Count;
                pointOffsets_[t] = totalPoints_;
                coneOffsets_[t] = totalCones_;
                totalCones_ += coneCt;
                totalPoints_ += pointCt;
            }
        }
    }
}
