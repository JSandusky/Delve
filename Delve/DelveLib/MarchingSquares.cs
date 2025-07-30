using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace DelveLib
{
    public class Voxel
    {
        public bool state;

        public Vector2 position;

        public float xEdge, yEdge;

        public Vector2 xNormal, yNormal;

        public Vector2 XEdgePoint
        {
            get
            {
                return new Vector2(xEdge, position.Y);
            }
        }

        public Vector2 YEdgePoint
        {
            get
            {
                return new Vector2(position.X, yEdge);
            }
        }

        public Voxel(int x, int y, float size)
        {
            position.X = (x + 0.5f) * size;
            position.Y = (y + 0.5f) * size;

            xEdge = position.X + 0.5f * size;
            yEdge = position.Y + 0.5f * size;
            //xEdge = float.MinValue;
            //yEdge = float.MinValue;
        }

        public Voxel() { }

        public void BecomeXDummyOf(Voxel voxel, float offset)
        {
            state = voxel.state;
            position = voxel.position;
            position.X += offset;
            xEdge = voxel.xEdge + offset;
            yEdge = voxel.yEdge;
            yNormal = voxel.yNormal;
        }

        public void BecomeYDummyOf(Voxel voxel, float offset)
        {
            state = voxel.state;
            position = voxel.position;
            position.Y += offset;
            xEdge = voxel.xEdge;
            yEdge = voxel.yEdge + offset;
            xNormal = voxel.xNormal;
        }

        public void BecomeXYDummyOf(Voxel voxel, float offset)
        {
            state = voxel.state;
            position = voxel.position;
            position.X += offset;
            position.Y += offset;
            xEdge = voxel.xEdge + offset;
            yEdge = voxel.yEdge + offset;
        }
    }

    public class VoxelGrid
    {
        public int resolution;
        public VoxelGrid xNeighbor, yNeighbor, xyNeighbor;

        private Voxel[] voxels;
        private float voxelSize, gridSize;
        private float sharpFeatureLimit;
        private List<Vector3> vertices;
        private List<int> triangles;

        private Voxel dummyX, dummyY, dummyT;

        private int[] rowCacheMax, rowCacheMin;
        private int edgeCacheMin, edgeCacheMax;

        public void Initialize(int resolution, float size, float maxFeatureAngle)
        {
            sharpFeatureLimit = Mathf.Cos(maxFeatureAngle * Mathf.DEGTORAD);
            this.resolution = resolution;
            gridSize = size;
            voxelSize = size / resolution;
            voxels = new Voxel[resolution * resolution];

            dummyX = new Voxel();
            dummyY = new Voxel();
            dummyT = new Voxel();

            for (int i = 0, y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++, i++)
                {
                    CreateVoxel(i, x, y);
                }
            }

            vertices = new List<Vector3>();
            triangles = new List<int>();
            rowCacheMax = new int[resolution * 2 + 1];
            rowCacheMin = new int[resolution * 2 + 1];
            Refresh();
        }

        private void CreateVoxel(int i, int x, int y)
        {
            //GameObject o = Instantiate(voxelPrefab) as GameObject;
            //o.transform.parent = transform;
            //o.transform.localPosition = new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize, -0.01f);
            //o.transform.localScale = Vector3.one * voxelSize * 0.1f;
            //voxelMaterials[i] = o.GetComponent<MeshRenderer>().material;
            voxels[i] = new Voxel(x, y, voxelSize);
        }

        private void Refresh()
        {
            SetVoxelColors();
            Triangulate();
        }

        private void Triangulate()
        {
            vertices.Clear();
            triangles.Clear();

            FillFirstRowCache();
            TriangulateCellRows();
            if (yNeighbor != null)
                TriangulateGapRow();
        }

        private void FillFirstRowCache()
        {
            CacheFirstCorner(voxels[0]);
            int i;
            for (i = 0; i < resolution - 1; i++)
            {
                CacheNextEdgeAndCorner(i * 2, voxels[i], voxels[i + 1]);
            }
            if (xNeighbor != null)
            {
                dummyX.BecomeXDummyOf(xNeighbor.voxels[0], gridSize);
                CacheNextEdgeAndCorner(i * 2, voxels[i], dummyX);
            }
        }

        private void CacheFirstCorner(Voxel voxel)
        {
            if (voxel.state)
            {
                rowCacheMax[0] = vertices.Count;
                vertices.Add(new Vector3(voxel.position.X, voxel.position.Y, 0.0f));
                Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
            }
        }

        private void CacheNextEdgeAndCorner(int i, Voxel xMin, Voxel xMax)
        {
            if (xMin.state != xMax.state)
            {
                rowCacheMax[i + 1] = vertices.Count;
                Vector3 p;
                p.X = xMin.xEdge;
                p.Y = xMin.position.Y;
                p.Z = 0f;
                vertices.Add(p);
                Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
            }
            if (xMax.state)
            {
                rowCacheMax[i + 2] = vertices.Count;
                vertices.Add(new Vector3(xMax.position.X, xMax.position.Y, 0.0f));
                Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
            }
        }

        private void CacheNextMiddleEdge(Voxel yMin, Voxel yMax)
        {
            edgeCacheMin = edgeCacheMax;
            if (yMin.state != yMax.state)
            {
                edgeCacheMax = vertices.Count;
                Vector3 p;
                p.X = yMin.position.X;
                p.Y = yMin.yEdge;
                p.Z = 0f;
                vertices.Add(p);
                Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
            }
        }

        private void TriangulateCellRows()
        {
            int cells = resolution - 1;
            for (int i = 0, y = 0; y < cells; y++, i++)
            {
                SwapRowCaches();
                CacheFirstCorner(voxels[i + resolution]);
                CacheNextMiddleEdge(voxels[i], voxels[i + resolution]);

                for (int x = 0; x < cells; x++, i++)
                {
                    Voxel
                        a = voxels[i],
                        b = voxels[i + 1],
                        c = voxels[i + resolution],
                        d = voxels[i + resolution + 1];
                    int cacheIndex = x * 2;
                    CacheNextEdgeAndCorner(cacheIndex, c, d);
                    CacheNextMiddleEdge(b, d);
                    TriangulateCell(cacheIndex, a, b, c, d);
                }
                if (xNeighbor != null)
                {
                    TriangulateGapCell(i);
                }
            }
        }

        private void SwapRowCaches()
        {
            int[] rowSwap = rowCacheMin;
            rowCacheMin = rowCacheMax;
            rowCacheMax = rowSwap;
        }

        private void TriangulateGapCell(int i)
        {
            Voxel dummySwap = dummyT;
            dummySwap.BecomeXDummyOf(xNeighbor.voxels[i + 1], gridSize);
            dummyT = dummyX;
            dummyX = dummySwap;
            int cacheIndex = (resolution - 1) * 2;
            CacheNextEdgeAndCorner(cacheIndex, voxels[i + resolution], dummyX);
            CacheNextMiddleEdge(dummyT, dummyX);
            TriangulateCell(cacheIndex, voxels[i], dummyT, voxels[i + resolution], dummyX);
        }

        private void TriangulateGapRow()
        {
            dummyY.BecomeYDummyOf(yNeighbor.voxels[0], gridSize);
            int cells = resolution - 1;
            int offset = cells * resolution;
            SwapRowCaches();
            CacheFirstCorner(dummyY);
            CacheNextMiddleEdge(voxels[cells * resolution], dummyY);

            for (int x = 0; x < cells; x++)
            {
                Voxel dummySwap = dummyT;
                dummySwap.BecomeYDummyOf(yNeighbor.voxels[x + 1], gridSize);
                dummyT = dummyY;
                dummyY = dummySwap;
                int cacheIndex = x * 2;
                CacheNextEdgeAndCorner(cacheIndex, dummyT, dummyY);
                CacheNextMiddleEdge(voxels[x + offset + 1], dummyY);
                TriangulateCell(cacheIndex, voxels[x + offset], voxels[x + offset + 1], dummyT, dummyY);
            }

            if (xNeighbor != null)
            {
                dummyT.BecomeXYDummyOf(xyNeighbor.voxels[0], gridSize);
                int cacheIndex = cells * 2;
                CacheNextEdgeAndCorner(cacheIndex, dummyY, dummyT);
                CacheNextMiddleEdge(dummyX, dummyT);
                TriangulateCell(cacheIndex, voxels[voxels.Length - 1], dummyX, dummyY, dummyT);
            }
        }

        private void TriangulateCell(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            int cellType = 0;
            if (a.state)
            {
                cellType |= 1;
            }
            if (b.state)
            {
                cellType |= 2;
            }
            if (c.state)
            {
                cellType |= 4;
            }
            if (d.state)
            {
                cellType |= 8;
            }
            switch (cellType)
            {
            case 0: TriangulateCase0(i, a, b, c, d); break;
            case 1: TriangulateCase1(i, a, b, c, d); break;
            case 2: TriangulateCase2(i, a, b, c, d); break;
            case 3: TriangulateCase3(i, a, b, c, d); break;
            case 4: TriangulateCase4(i, a, b, c, d); break;
            case 5: TriangulateCase5(i, a, b, c, d); break;
            case 6: TriangulateCase6(i, a, b, c, d); break;
            case 7: TriangulateCase7(i, a, b, c, d); break;
            case 8: TriangulateCase8(i, a, b, c, d); break;
            case 9: TriangulateCase9(i, a, b, c, d); break;
            case 10: TriangulateCase10(i, a, b, c, d); break;
            case 11: TriangulateCase11(i, a, b, c, d); break;
            case 12: TriangulateCase12(i, a, b, c, d); break;
            case 13: TriangulateCase13(i, a, b, c, d); break;
            case 14: TriangulateCase14(i, a, b, c, d); break;
            case 15: TriangulateCase15(i, a, b, c, d); break;
            }
        }

        private bool IsSharpFeature(Vector2 n1, Vector2 n2)
        {
            if (n1.LengthSquared() == 0 || n2.LengthSquared() == 0)
                return false;
            float dot = Vector2.Dot(n1, -n2);
            return dot >= sharpFeatureLimit && dot < 0.9999f;
        }

        private static Vector2 ComputeIntersection(Vector2 p1, Vector2 n1, Vector2 p2, Vector2 n2)
        {
            Vector2 d2 = new Vector2(-n2.Y, n2.X);
            float u2 = -Vector2.Dot(n1, p2 - p1) / Vector2.Dot(n1, d2);
            return p2 + d2 * u2;
        }

        private static bool IsInsideCell(Vector2 point, Voxel min, Voxel max)
        {
            return
                point.X > min.position.X && point.Y > min.position.Y &&
                point.X < max.position.X && point.Y < max.position.Y;
        }

        private static bool IsBelowLine(Vector2 p, Vector2 start, Vector2 end)
        {
            float determinant = (end.X - start.X) * (p.Y - start.Y) - (end.Y - start.Y) * (p.X - start.X);
            return determinant < 0f;
        }

        private static bool ClampToCellMinMin(ref Vector2 point, Voxel min, Voxel max)
        {
            if (point.X > max.position.X || point.Y > max.position.Y)
                return false;
            if (point.X < min.position.X)
                point.X = min.position.X;
            if (point.Y < min.position.Y)
                point.Y = min.position.Y;
            return true;
        }

        private static bool ClampToCellMinMax(ref Vector2 point, Voxel min, Voxel max)
        {
            if (point.X > max.position.X || point.Y < min.position.Y)
                return false;
            if (point.X < min.position.X)
                point.X = min.position.X;
            if (point.Y > max.position.Y)
                point.Y = max.position.Y;
            return true;
        }

        private static bool ClampToCellMaxMin(ref Vector2 point, Voxel min, Voxel max)
        {
            if (point.X < min.position.X || point.Y > max.position.Y)
                return false;
            if (point.X > max.position.X)
                point.X = max.position.X;
            if (point.Y < min.position.Y)
                point.Y = min.position.Y;
            return true;
        }

        private static bool ClampToCellMaxMax(ref Vector2 point, Voxel min, Voxel max)
        {
            if (point.X < min.position.X || point.Y < min.position.Y)
                return false;
            if (point.X > max.position.X)
                point.X = max.position.X;
            if (point.Y > max.position.Y)
                point.Y = max.position.Y;
            return true;
        }

        private void TriangulateCase0(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
        }

        private void TriangulateCase15(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            AddQuadABCD(i);
        }

        private void TriangulateCase1(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
                if (ClampToCellMaxMax(ref point, a, d))
                {
                    AddQuadA(i, point);
                    return;
                }
            }
            AddTriangleA(i);
        }

        private void TriangulateCase2(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
                if (ClampToCellMinMax(ref point, a, d))
                {
                    AddQuadB(i, point);
                    return;
                }
            }
            AddTriangleB(i);
        }

        private void TriangulateCase4(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = c.xNormal;
            Vector2 n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
                if (ClampToCellMaxMin(ref point, a, d))
                {
                    AddQuadC(i, point);
                    return;
                }
            }
            AddTriangleC(i);
        }

        private void TriangulateCase8(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = c.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
                if (ClampToCellMinMin(ref point, a, d))
                {
                    AddQuadD(i, point);
                    return;
                }
            }
            AddTriangleD(i);
        }

        private void TriangulateCase7(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = c.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddHexagonABC(i, point);
                    return;
                }
            }
            AddPentagonABC(i);
        }

        private void TriangulateCase11(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = c.xNormal;
            Vector2 n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddHexagonABD(i, point);
                    return;
                }
            }
            AddPentagonABD(i);
        }

        private void TriangulateCase13(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddHexagonACD(i, point);
                    return;
                }
            }
            AddPentagonACD(i);
        }

        private void TriangulateCase14(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddHexagonBCD(i, point);
                    return;
                }
            }
            AddPentagonBCD(i);
        }

        private void TriangulateCase3(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.yNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.YEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddPentagonAB(i, point);
                    return;
                }
            }
            AddQuadAB(i);
        }

        private void TriangulateCase5(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = c.xNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, c.XEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddPentagonAC(i, point);
                    return;
                }
            }
            AddQuadAC(i);
        }

        private void TriangulateCase10(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = c.xNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, c.XEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddPentagonBD(i, point);
                    return;
                }
            }
            AddQuadBD(i);
        }

        private void TriangulateCase12(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.yNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.YEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddPentagonCD(i, point);
                    return;
                }
            }
            AddQuadCD(i);
        }

        private void TriangulateCase6(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            bool sharp1, sharp2;
            Vector2 point1, point2;

            Vector2 n1 = a.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                point1 = ComputeIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
                sharp1 = ClampToCellMinMax(ref point1, a, d);
            }
            else
            {
                point1.X = point1.Y = 0f;
                sharp1 = false;
            }

            n1 = c.xNormal;
            n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                point2 = ComputeIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
                sharp2 = ClampToCellMaxMin(ref point2, a, d);
            }
            else
            {
                point2.X = point2.Y = 0f;
                sharp2 = false;
            }

            if (sharp1)
            {
                if (sharp2)
                {
                    if (IsBelowLine(point2, a.XEdgePoint, point1))
                    {
                        if (IsBelowLine(point2, point1, b.YEdgePoint) || IsBelowLine(point1, point2, a.YEdgePoint))
                        {
                            TriangulateCase6Connected(i, a, b, c, d);
                            return;
                        }
                    }
                    else if (IsBelowLine(point2, point1, b.YEdgePoint) && IsBelowLine(point1, c.XEdgePoint, point2))
                    {
                        TriangulateCase6Connected(i, a, b, c, d);
                        return;
                    }
                    AddQuadB(i, point1);
                    AddQuadC(i, point2);
                    return;
                }
                if (IsBelowLine(point1, c.XEdgePoint, a.YEdgePoint))
                {
                    TriangulateCase6Connected(i, a, b, c, d);
                    return;
                }
                AddQuadB(i, point1);
                AddTriangleC(i);
                return;
            }
            if (sharp2)
            {
                if (IsBelowLine(point2, a.XEdgePoint, b.YEdgePoint))
                {
                    TriangulateCase6Connected(i, a, b, c, d);
                    return;
                }
                AddTriangleB(i);
                AddQuadC(i, point2);
                return;
            }
            AddTriangleB(i);
            AddTriangleC(i);
        }

        private void TriangulateCase6Connected(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
                if (IsInsideCell(point, a, d) && IsBelowLine(point, c.position, b.position))
                {
                    AddPentagonBCToA(i, point);
                }
                else
                {
                    AddQuadBCToA(i);
                }
            }
            else
            {
                AddQuadBCToA(i);
            }

            n1 = c.xNormal;
            n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d) && IsBelowLine(point, b.position, c.position))
                {
                    AddPentagonBCToD(i, point);
                    return;
                }
            }
            AddQuadBCToD(i);
        }

        private void TriangulateCase9(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            bool sharp1, sharp2;
            Vector2 point1, point2;
            Vector2 n1 = a.xNormal;
            Vector2 n2 = a.yNormal;

            if (IsSharpFeature(n1, n2))
            {
                point1 = ComputeIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
                sharp1 = ClampToCellMaxMax(ref point1, a, d);
            }
            else
            {
                point1.X = point1.Y = 0f;
                sharp1 = false;
            }

            n1 = c.xNormal;
            n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                point2 = ComputeIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
                sharp2 = ClampToCellMinMin(ref point2, a, d);
            }
            else
            {
                point2.X = point2.Y = 0f;
                sharp2 = false;
            }

            if (sharp1)
            {
                if (sharp2)
                {
                    if (IsBelowLine(point1, b.YEdgePoint, point2))
                    {
                        if (IsBelowLine(point1, point2, c.XEdgePoint) || IsBelowLine(point2, point1, a.XEdgePoint))
                        {
                            TriangulateCase9Connected(i, a, b, c, d);
                            return;
                        }
                    }
                    else if (IsBelowLine(point1, point2, c.XEdgePoint) && IsBelowLine(point2, a.YEdgePoint, point1))
                    {
                        TriangulateCase9Connected(i, a, b, c, d);
                        return;
                    }
                    AddQuadA(i, point1);
                    AddQuadD(i, point2);
                    return;
                }
                if (IsBelowLine(point1, b.YEdgePoint, c.XEdgePoint))
                {
                    TriangulateCase9Connected(i, a, b, c, d);
                    return;
                }
                AddQuadA(i, point1);
                AddTriangleD(i);
                return;
            }
            if (sharp2)
            {
                if (IsBelowLine(point2, a.YEdgePoint, a.XEdgePoint))
                {
                    TriangulateCase9Connected(i, a, b, c, d);
                    return;
                }
                AddTriangleA(i);
                AddQuadD(i, point2);
                return;
            }
            AddTriangleA(i);
            AddTriangleD(i);
        }

        private void TriangulateCase9Connected(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d) && IsBelowLine(point, a.position, d.position))
                {
                    AddPentagonADToB(i, point);
                }
                else
                {
                    AddQuadADToB(i);
                }
            }
            else
            {
                AddQuadADToB(i);
            }

            n1 = c.xNormal;
            n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
                if (IsInsideCell(point, a, d) && IsBelowLine(point, d.position, a.position))
                {
                    AddPentagonADToC(i, point);
                    return;
                }
            }
            AddQuadADToC(i);
        }

        private void AddQuadABCD(int i)
        {
            AddQuad(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 2], rowCacheMin[i + 2]);
        }

        private void AddTriangleA(int i)
        {
            AddTriangle(rowCacheMin[i], edgeCacheMin, rowCacheMin[i + 1]);
        }

        private void AddQuadA(int i, Vector2 extraVertex)
        {
            AddQuad(vertices.Count, rowCacheMin[i + 1], rowCacheMin[i], edgeCacheMin);
            vertices.Add(new Vector3(extraVertex.X, extraVertex.Y, 0.0f));
            Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
        }

        private void AddTriangleB(int i)
        {
            AddTriangle(rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMax);
        }

        private void AddQuadB(int i, Vector2 extraVertex)
        {
            AddQuad(vertices.Count, edgeCacheMax, rowCacheMin[i + 2], rowCacheMin[i + 1]);
            vertices.Add(new Vector3(extraVertex.X, extraVertex.Y, 0.0f));
            Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
        }

        private void AddTriangleC(int i)
        {
            AddTriangle(rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMin);
        }

        private void AddQuadC(int i, Vector2 extraVertex)
        {
            AddQuad(vertices.Count, edgeCacheMin, rowCacheMax[i], rowCacheMax[i + 1]);
            vertices.Add(new Vector3(extraVertex.X, extraVertex.Y, 0.0f));
            Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
        }

        private void AddTriangleD(int i)
        {
            AddTriangle(rowCacheMax[i + 2], edgeCacheMax, rowCacheMax[i + 1]);
        }

        private void AddQuadD(int i, Vector2 extraVertex)
        {
            AddQuad(vertices.Count, rowCacheMax[i + 1], rowCacheMax[i + 2], edgeCacheMax);
            vertices.Add(new Vector3(extraVertex.X, extraVertex.Y, 0.0f));
            Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
        }

        private void AddPentagonABC(int i)
        {
            AddPentagon(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMax, rowCacheMin[i + 2]);
        }

        private void AddHexagonABC(int i, Vector2 extraVertex)
        {
            AddHexagon(
                vertices.Count, edgeCacheMax, rowCacheMin[i + 2],
                rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1]);
            vertices.Add(new Vector3(extraVertex.X, extraVertex.Y, 0.0f));
            Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
        }

        private void AddPentagonABD(int i)
        {
            AddPentagon(rowCacheMin[i + 2], rowCacheMin[i], edgeCacheMin, rowCacheMax[i + 1], rowCacheMax[i + 2]);
        }

        private void AddHexagonABD(int i, Vector2 extraVertex)
        {
            AddHexagon(
                vertices.Count, rowCacheMax[i + 1], rowCacheMax[i + 2],
                rowCacheMin[i + 2], rowCacheMin[i], edgeCacheMin);
            vertices.Add(new Vector3(extraVertex.X, extraVertex.Y, 0.0f));
            Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
        }

        private void AddPentagonACD(int i)
        {
            AddPentagon(rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax, rowCacheMin[i + 1], rowCacheMin[i]);
        }

        private void AddHexagonACD(int i, Vector2 extraVertex)
        {
            AddHexagon(
                vertices.Count, rowCacheMin[i + 1], rowCacheMin[i],
                rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax);
            vertices.Add(new Vector3(extraVertex.X, extraVertex.Y, 0.0f));
            Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
        }

        private void AddPentagonBCD(int i)
        {
            AddPentagon(rowCacheMax[i + 2], rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMin, rowCacheMax[i]);
        }

        private void AddHexagonBCD(int i, Vector2 extraVertex)
        {
            AddHexagon(
                vertices.Count, edgeCacheMin, rowCacheMax[i],
                rowCacheMax[i + 2], rowCacheMin[i + 2], rowCacheMin[i + 1]);
            vertices.Add(new Vector3(extraVertex.X, extraVertex.Y, 0.0f));
            Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
        }

        private void AddQuadAB(int i)
        {
            AddQuad(rowCacheMin[i], edgeCacheMin, edgeCacheMax, rowCacheMin[i + 2]);
        }

        private void AddPentagonAB(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, edgeCacheMax, rowCacheMin[i + 2], rowCacheMin[i], edgeCacheMin);
            vertices.Add(new Vector3(extraVertex.X, extraVertex.Y, 0.0f));
            Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
        }

        private void AddQuadAC(int i)
        {
            AddQuad(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1], rowCacheMin[i + 1]);
        }

        private void AddPentagonAC(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, rowCacheMin[i + 1], rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1]);
            vertices.Add(new Vector3(extraVertex.X, extraVertex.Y, 0.0f));
            Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
        }

        private void AddQuadBD(int i)
        {
            AddQuad(rowCacheMin[i + 1], rowCacheMax[i + 1], rowCacheMax[i + 2], rowCacheMin[i + 2]);
        }

        private void AddPentagonBD(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, rowCacheMax[i + 1], rowCacheMax[i + 2], rowCacheMin[i + 2], rowCacheMin[i + 1]);
            vertices.Add(new Vector3(extraVertex.X, extraVertex.Y, 0.0f));
            Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
        }

        private void AddQuadCD(int i)
        {
            AddQuad(edgeCacheMin, rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax);
        }

        private void AddPentagonCD(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, edgeCacheMin, rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax);
            vertices.Add(new Vector3(extraVertex.X, extraVertex.Y, 0.0f));
            Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
        }

        private void AddQuadBCToA(int i)
        {
            AddQuad(edgeCacheMin, rowCacheMax[i], rowCacheMin[i + 2], rowCacheMin[i + 1]);
        }

        private void AddPentagonBCToA(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, edgeCacheMin, rowCacheMax[i], rowCacheMin[i + 2], rowCacheMin[i + 1]);
            vertices.Add(new Vector3(extraVertex.X, extraVertex.Y, 0.0f));
            Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
        }

        private void AddQuadBCToD(int i)
        {
            AddQuad(edgeCacheMax, rowCacheMin[i + 2], rowCacheMax[i], rowCacheMax[i + 1]);
        }

        private void AddPentagonBCToD(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, edgeCacheMax, rowCacheMin[i + 2], rowCacheMax[i], rowCacheMax[i + 1]);
            vertices.Add(new Vector3(extraVertex.X, extraVertex.Y, 0.0f));
            Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
        }

        private void AddQuadADToB(int i)
        {
            AddQuad(rowCacheMin[i + 1], rowCacheMin[i], rowCacheMax[i + 2], edgeCacheMax);
        }

        private void AddPentagonADToB(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, rowCacheMin[i + 1], rowCacheMin[i], rowCacheMax[i + 2], edgeCacheMax);
            vertices.Add(new Vector3(extraVertex.X, extraVertex.Y, 0.0f));
            Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
        }

        private void AddQuadADToC(int i)
        {
            AddQuad(rowCacheMax[i + 1], rowCacheMax[i + 2], rowCacheMin[i], edgeCacheMin);
        }

        private void AddPentagonADToC(int i, Vector2 extraVertex)
        {
            AddPentagon(vertices.Count, rowCacheMax[i + 1], rowCacheMax[i + 2], rowCacheMin[i], edgeCacheMin);
            vertices.Add(new Vector3(extraVertex.X, extraVertex.Y, 0.0f));
            Debug.Assert(vertices[vertices.Count - 1].X.IsFinite());
        }

        private void AddTriangle(int a, int b, int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        private void AddQuad(int a, int b, int c, int d)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
        }

        private void AddPentagon(int a, int b, int c, int d, int e)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
            triangles.Add(a);
            triangles.Add(d);
            triangles.Add(e);
        }

        private void AddHexagon(int a, int b, int c, int d, int e, int f)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
            triangles.Add(a);
            triangles.Add(d);
            triangles.Add(e);
            triangles.Add(a);
            triangles.Add(e);
            triangles.Add(f);
        }

        private void SetVoxelColors()
        {
            //??for (int i = 0; i < voxels.Length; i++)
            //??{
            //??    voxelMaterials[i].color = voxels[i].state ? Color.black : Color.white;
            //??}
        }

        public void Apply(System.Drawing.Bitmap bmpField, byte threshold, bool useEdgeIntercept)
        {
            for (int y = 0; y < bmpField.Height; ++y)
            {
                float fy = y / (float)bmpField.Height;
                for (int x = 0; x < bmpField.Width; ++x)
                {
                    int i = y * resolution + x;
                    float fx = x / (float)bmpField.Width;
                    var pixelValue = bmpField.GetPixel(x, y);
                    voxels[i].state = pixelValue.R > threshold;

                    float sx = 0.5f;
                    float sy = 0.5f;
                    if (useEdgeIntercept)
                    {
                        if (x < bmpField.Width - 1)
                        {
                            var nextPixelValue = bmpField.GetPixel(x + 1, y);
                            if (nextPixelValue.R < threshold)
                            {
                                float stepSize = 0.1f / (float)bmpField.Width;
                                float step = 0.1f;
                                for (; step <= 1.0f; step += stepSize)
                                {
                                    var val = bmpField.GetPixelBilinear(fx + step, fy);
                                    if (val.R < threshold)
                                    {
                                        sx = step;
                                        voxels[i].xEdge = voxels[i].position.X + step * voxelSize;
                                        voxels[i].xNormal = GetDerivative(bmpField, fx, fy, sx / bmpField.Width, 0.0f);
                                        break;
                                    }
                                }
                            }
                        }

                        if (y < bmpField.Height - 1)
                        {
                            var nextPixelValue = bmpField.GetPixel(x, y + 1);
                            if (nextPixelValue.R < threshold)
                            {
                                float stepSize = 0.1f / (float)bmpField.Width;
                                float step = 0.1f;
                                for (; step <= 1.0f; step += stepSize)
                                {
                                    var val = bmpField.GetPixelBilinear(fx, fy + step);
                                    if (val.R < threshold)
                                    {
                                        sy = step;
                                        voxels[i].yEdge = voxels[i].position.Y + step * voxelSize;
                                        voxels[i].yNormal = GetDerivative(bmpField, fx, fy, 0.0f, sy / bmpField.Height);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Refresh();
        }

        Vector2 GetDerivative(System.Drawing.Bitmap bmp, float x, float y, float sx, float sy)
        {
            float H = 0.001f;
            float dx = bmp.GetPixelBilinear(x + H + sx, y + sy).R - bmp.GetPixelBilinear(x - H + sx, y + sy).R;
            float dy = bmp.GetPixelBilinear(x + sx, y + H + sy).R - bmp.GetPixelBilinear(x + sx, y - H + sy).R;
            return Vector2.Normalize(new Vector2(dx, dy));
        }


        public MeshData ToMesh()
        {
            VertexData[] verts = new VertexData[this.vertices.Count];
            for (int i = 0; i < verts.Length; ++i)
            {
                verts[i].Position = new Vector3(vertices[i].X - voxelSize * 0.5f, 0, vertices[i].Y - voxelSize * 0.5f);
                verts[i].Normal = Vector3.UnitY;
            }
            return new MeshData(triangles.ToList(), verts.ToList());
        }
#if USE_VOXEL_STENCIL
        public void Apply(VoxelStencil stencil)
        {
            int xStart = (int)(stencil.XStart / voxelSize);
            if (xStart < 0)
            {
                xStart = 0;
            }
            int xEnd = (int)(stencil.XEnd / voxelSize);
            if (xEnd >= resolution)
            {
                xEnd = resolution - 1;
            }
            int yStart = (int)(stencil.YStart / voxelSize);
            if (yStart < 0)
            {
                yStart = 0;
            }
            int yEnd = (int)(stencil.YEnd / voxelSize);
            if (yEnd >= resolution)
            {
                yEnd = resolution - 1;
            }

            for (int y = yStart; y <= yEnd; y++)
            {
                int i = y * resolution + xStart;
                for (int x = xStart; x <= xEnd; x++, i++)
                {
                    stencil.Apply(voxels[i]);
                }
            }
            SetCrossings(stencil, xStart, xEnd, yStart, yEnd);
            Refresh();
        }

        private void SetCrossings(VoxelStencil stencil, int xStart, int xEnd, int yStart, int yEnd)
        {
            bool crossHorizontalGap = false;
            bool includeLastVerticalRow = false;
            bool crossVerticalGap = false;

            if (xStart > 0)
            {
                xStart -= 1;
            }
            if (xEnd == resolution - 1)
            {
                xEnd -= 1;
                crossHorizontalGap = xNeighbor != null;
            }
            if (yStart > 0)
            {
                yStart -= 1;
            }
            if (yEnd == resolution - 1)
            {
                yEnd -= 1;
                includeLastVerticalRow = true;
                crossVerticalGap = yNeighbor != null;
            }

            Voxel a, b;
            for (int y = yStart; y <= yEnd; y++)
            {
                int i = y * resolution + xStart;
                b = voxels[i];
                for (int x = xStart; x <= xEnd; x++, i++)
                {
                    a = b;
                    b = voxels[i + 1];
                    stencil.SetHorizontalCrossing(a, b);
                    stencil.SetVerticalCrossing(a, voxels[i + resolution]);
                }
                stencil.SetVerticalCrossing(b, voxels[i + resolution]);
                if (crossHorizontalGap)
                {
                    dummyX.BecomeXDummyOf(xNeighbor.voxels[y * resolution], gridSize);
                    stencil.SetHorizontalCrossing(b, dummyX);
                }
            }

            if (includeLastVerticalRow)
            {
                int i = voxels.Length - resolution + xStart;
                b = voxels[i];
                for (int x = xStart; x <= xEnd; x++, i++)
                {
                    a = b;
                    b = voxels[i + 1];
                    stencil.SetHorizontalCrossing(a, b);
                    if (crossVerticalGap)
                    {
                        dummyY.BecomeYDummyOf(yNeighbor.voxels[x], gridSize);
                        stencil.SetVerticalCrossing(a, dummyY);
                    }
                }
                if (crossVerticalGap)
                {
                    dummyY.BecomeYDummyOf(yNeighbor.voxels[xEnd + 1], gridSize);
                    stencil.SetVerticalCrossing(b, dummyY);
                }
                if (crossHorizontalGap)
                {
                    dummyX.BecomeXDummyOf(xNeighbor.voxels[voxels.Length - resolution], gridSize);
                    stencil.SetHorizontalCrossing(b, dummyX);
                }
            }
        }

#endif
    }
}
