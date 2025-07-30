using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

namespace DelveLib.Map
{
    public static class MapG3Ext
    {
        public static SideDef GetRightMost(this List<SideDef> list)
        {
            SideDef curBest = list[0];
            float x = float.MinValue;
            foreach (SideDef s in list)
            {
                if (s.Line.Start.Position.X > x)
                {
                    curBest = s;
                    x = s.Line.Start.Position.X;
                }
                if (s.Line.End.Position.X > x)
                {
                    curBest = s;
                    x = s.Line.End.Position.X;
                }
            }
            return curBest;
        }

        public static SideDef GetNextSide(SideDef current, Sector s)
        {
            if (current.IsFront)
            {
                foreach (SideDef side in s.Sides)
                {
                    if (side == current)
                        continue;
                    if (side.IsFront)
                    {
                        if (side.Line.Start == current.Line.End)
                            return side;
                    }
                    else
                    {
                        if (side.Line.End == current.Line.End)
                            return side;
                    }
                }
            }
            else
            {
                foreach (SideDef side in s.Sides)
                {
                    if (side == current)
                        continue;
                    if (side.IsFront)
                    {
                        if (side.Line.Start == current.Line.Start)
                            return side;
                    }
                    else if (side.Line.End == current.Line.Start)
                    {
                        return side;
                    }
                }
            }
            return null;
        }

        public static void FillVertices(this Sector s, SideDef start, List<Vector2> pts)
        {
            List<SideDef> hit = new List<SideDef>();
            SideDef current = start;
            hit.Add(start);
            if (start.IsFront)
            {
                pts.Add(start.Line.Start.Position);
                pts.Add(start.Line.End.Position);
            }
            else
            {
                pts.Add(start.Line.End.Position);
                pts.Add(start.Line.Start.Position);
            }
            int tries = 0;
            do
            {
                current = GetNextSide(current, s);
                if (current != null && hit.Contains(current) == false)
                {
                    hit.Add(current);
                    if (current.IsFront)
                        pts.Add(current.Line.End.Position);
                    else
                        pts.Add(current.Line.Start.Position);
                    tries = 0;
                }
                else
                    ++tries;
                if (tries > 10)
                    break;
            } while (current != null && current != start);
            pts.Remove(pts.Last());
        }

        public static List<g3.Vector2d> GetSectorVerts(this Sector s)
        {
            //List<g3.Vector2d> verts = new List<g3.Vector2d>();

            List<Vector2> vec = new List<Vector2>();
            s.FillVertices(s.Sides.GetRightMost(), vec);
            return vec.ToArray().ToG3().ToList();
            //for (int i = 0; i < s.Vertices.Count; ++i)
            //    verts.Add(s.Vertices[i].Position.ToG3());
            //return verts;
        }

        public static g3.Polygon2d GetSectorPolygon(this Sector s)
        {
            return new g3.Polygon2d(GetSectorVerts(s));
        }

        public static List<Vector2> ToPointList(this SideDef[] chain)
        {
            List<Vector2> ret = new List<Vector2>();
            for (int i = 0; i < chain.Length; ++i)
            {
                if (chain[i].IsFront)
                    ret.Add(chain[i].Line.Start.Position);
                else
                    ret.Add(chain[i].Line.End.Position);
            }
            if (chain.Length > 0)
            {
                if (chain[chain.Length - 1].IsFront)
                    ret.Add(chain[chain.Length - 1].Line.End.Position);
                else
                    ret.Add(chain[chain.Length - 1].Line.Start.Position);
            }
            return ret;
        }

        public static List<Vector3> ToNormals(this List<Vector2> vec)
        {
            List<Vector3> ret = new List<Vector3>();
            for (int i = 0; i < vec.Count; ++i)
            {
                int prev = i - 1;
                int next = i + 1;
                Vector2 pt = vec[i];
                Vector2 cur = new Vector2();
                Vector2 backup = new Vector2();
                if (prev >= 0)
                {
                    var prevPt = vec[prev];
                    prevPt = (pt - prevPt).Rotate(-90);
                    prevPt.Normalize();
                    backup = prevPt;
                    cur += prevPt;
                }
                else if (i == 0 && vec.First() == vec.Last())
                {
                    var prevPt = vec[vec.Count - 2];
                    prevPt = (pt - prevPt).Rotate(-90);
                    prevPt.Normalize();
                    backup = prevPt;
                    cur += prevPt;
                }
                if (next < vec.Count)
                {
                    var nextPt = vec[next];
                    nextPt = (nextPt - pt).Rotate(-90);
                    nextPt.Normalize();
                    backup = nextPt;
                    cur += nextPt;
                }
                else if (i == vec.Count - 1 && vec.First() == vec.Last())
                {
                    var nextPt = vec[1];
                    nextPt = (nextPt - pt).Rotate(-90);
                    nextPt.Normalize();
                    backup = nextPt;
                    cur += nextPt;
                }
                cur.Normalize();
                if (float.IsNaN(cur.X) || float.IsNaN(cur.Y))
                    cur = backup;
                System.Diagnostics.Debug.Assert(!float.IsNaN(cur.X));
                System.Diagnostics.Debug.Assert(!float.IsNaN(cur.Y));
                ret.Add(new Vector3(cur.X, 0, cur.Y));
            }
            return ret;
        }

        public static List<Vector3> To3D(this List<Vector2> vec)
        {
            List<Vector3> ret = new List<Vector3>();
            for (int i = 0; i < vec.Count; ++i)
                ret.Add(new Vector3(vec[i].X, 0, vec[i].Y));
            return ret;
        }

        public static List<SideDef[]> GetSectorSolidChains(this Sector s, Func<SideDef, bool> filterFunc)
        {
            List<SideDef[]> ret = new List<SideDef[]>();

            List<SideDef> sides = new List<SideDef>(s.Sides);
            sides = sides.Where(filterFunc).ToList();
            while (sides.Count > 0)
            {
                List<SideDef> lineChain = new List<SideDef>();
                for (int i = 0; i < sides.Count; ++i)
                {
                    if (lineChain.Count == 0)
                    {
                        lineChain.Add(sides[i]);
                        sides.RemoveAt(i);
                        i = -1;
                        continue;
                    }

                    var first = lineChain.First();
                    var last = lineChain.Last();
                    if (last.Line.End == sides[i].Line.Start)
                    {
                        lineChain.Add(sides[i]);
                        sides.RemoveAt(i);
                        i = -1;
                    }
                    else if (first.Line.Start == sides[i].Line.End)
                    {
                        lineChain.Insert(0, sides[i]);
                        sides.RemoveAt(i);
                        i = -1;
                    }
                }
                if (lineChain.Count > 0)
                    ret.Add(lineChain.ToArray());
            }

            //s.ClearLineMarks();
            //for (int i = 0; i < 100; ++i)
            //{
            //    List<SideDef> sides = new List<SideDef>();
            //    for (int ii = 0; ii < s.Sides.Count + 1; ++i)
            //    {
            //        foreach (var side in s.Sides)
            //        {
            //            if (side.Line.TaskMark)
            //                continue;
            //            if (side.Line.Impassable)
            //            {
            //                if (sides.Count == 0)
            //                    sides.Add(side);
            //                else
            //                {
            //                    i
            //                }
            //            }
            //        }
            //    }
            //}
            //
            //
            //bool hitPassable = false;
            //List<SideDef> working = new List<SideDef>();
            //SideDef lastSide = null;
            //foreach (var side in s.Sides)
            //{
            //    if (!side.Line.Impassable)
            //    {
            //        if (hitPassable == false)
            //        {
            //            hitPassable = true;
            //            lastSide = side;
            //        }
            //        else
            //        {
            //            if (working.Count > 0)
            //                ret.Add(working.ToArray());
            //            working.Clear();
            //            hitPassable = true;
            //            lastSide = side;
            //        }
            //    }
            //    else if (hitPassable && SideDef.IsConnected(lastSide, side))
            //    {
            //        working.Add(side);
            //        lastSide = side;
            //    }
            //}

            return ret;
        }

        public static List<SideDef[]> GetSectorLowerChains(this Sector s)
        {
            List<SideDef[]> ret = new List<SideDef[]>();

            bool hitImpassable = false;
            List<SideDef> working = new List<SideDef>();
            SideDef lastSide = null;
            for (int z = 0; z < s.Sides.Count; ++z)
            {
                foreach (var side in s.Sides)
                {
                    if (side.Line.Meta.Impassable)
                    {
                        if (hitImpassable == false)
                        {
                            hitImpassable = true;
                            lastSide = side;
                        }
                        else if (SideDef.IsConnected(lastSide, side))
                        {
                            if (working.Count > 0)
                                ret.Add(working.ToArray());
                            working.Clear();
                            hitImpassable = false;
                        }
                    }
                    else if (hitImpassable &&
                        SideDef.IsConnected(lastSide, side)
                        && side.Sector.FloorHeight != side.Opposite.Sector.FloorHeight)
                    {
                        if (!working.Contains(side))
                            working.Add(side);
                        lastSide = side;
                    }
                }
            }

            return ret;
        }

        public static List<SideDef[]> GetSectorUpperChains(this Sector s)
        {
            List<SideDef[]> ret = new List<SideDef[]>();

            bool hitImpassable = false;
            List<SideDef> working = new List<SideDef>();
            SideDef lastSide = null;
            for (int z = 0; z < s.Sides.Count; ++z)
            {
                foreach (var side in s.Sides)
                {
                    if (side.Line.Meta.Impassable)
                    {
                        if (hitImpassable == false)
                        {
                            hitImpassable = true;
                            lastSide = side;
                        }
                        else if (SideDef.IsConnected(lastSide, side))
                        {
                            if (working.Count > 0)
                                ret.Add(working.ToArray());
                            working.Clear();
                            hitImpassable = false;
                        }
                    }
                    else if (hitImpassable && 
                        SideDef.IsConnected(lastSide, side)
                        && side.Sector.CeilingHeight != side.Opposite.Sector.CeilingHeight)
                    {
                        if (!working.Contains(side))
                            working.Add(side);
                        lastSide = side;
                    }
                }
            }

            return ret;
        }
    }

    public class EdgeFitment
    {
        public Vector2 a, b;
        public SideDef side;
        public Sector sectors;
        public float EdgeLength { get { return (a - b).Length(); } }
    }

    public class HardCorner : IComparable
    {
        public Vertex Vertex;
        public Vector2 normal;

        public int CompareTo(object obj)
        {
            if ((obj as HardCorner).Vertex == Vertex)
                return 0;
            int lhsHash = Vertex.GetHashCode();
            int rhsHash = ((HardCorner)obj).Vertex.GetHashCode();
            if (lhsHash < rhsHash)
                return -1;
            return 1;
        }
    }

    public class MapProcessor
    {
        /// <summary>
        /// Build a skirting around the sector
        /// </summary>
        public void SkirtSector(Sector s, float skirtDist, List<g3.DMesh3> holder)
        {
            g3.Polygon2d poly = s.GetSectorPolygon();
            g3.DMesh3 mesh = new g3.DMesh3(false);

            g3.Polygon2d shrunk = new g3.Polygon2d(poly);
            poly.PolyOffset(skirtDist);

            int idx = 0;
            for (int i = 0; i < poly.Vertices.Count; ++i)
            {
                int next = i + 1;
                if (next >= poly.Vertices.Count)
                    next = 0;
                
                var thisPos = poly.Vertices[i];
                var nextPos = poly.Vertices[next];
                
                var thisOuter = shrunk[i];
                var nextOuter = shrunk[next];

                mesh.AppendVertex(new g3.Vector3d(thisPos.x, s.FloorHeight, thisPos.y));
                mesh.AppendVertex(new g3.Vector3d(nextPos.x, s.FloorHeight, nextPos.y));
                mesh.AppendVertex(new g3.Vector3d(thisOuter.x, s.FloorHeight, thisOuter.y));
                mesh.AppendVertex(new g3.Vector3d(nextOuter.x, s.FloorHeight, nextOuter.y));

                mesh.AppendTriangle(idx + 0, idx + 2, idx + 3);
                mesh.AppendTriangle(idx + 0, idx + 3, idx + 1);

                idx += 4;
            }
            holder.Add(mesh);
        }

        public void PolygonizeSectors(List<Sector> s, bool floor, List<g3.DMesh3> holder)
        {
            List<g3.Polygon2d> polys = new List<g3.Polygon2d>();
            List<double> yPos = new List<double>();
            foreach (var sector in s)
            {
                polys.Add(sector.GetSectorPolygon());
                yPos.Add(floor ? sector.FloorHeight : sector.CeilingHeight);
            }

            for (int i = 0; i < polys.Count; ++i)
            {
                var poly = polys[i];
                g3.TriangulatedPolygonGenerator tri = new g3.TriangulatedPolygonGenerator();
                tri.Polygon = new g3.GeneralPolygon2d(poly);
                if (!floor)
                    tri.Polygon.Reverse();
                //foreach (var opoly in polys)
                //{
                //    if (opoly == poly)
                //        continue;
                //    if (poly.Contains(opoly))
                //    {
                //        var np = new g3.Polygon2d(opoly);
                //        np.Reverse();
                //        tri.Polygon.AddHole(np);
                //    }
                //}
                try
                {
                    tri.Generate();
                    for (int v = 0; v < tri.vertices.Count; ++v)
                    {
                        var vv = tri.vertices[v];
                        vv.z = vv.y;
                        vv.y = yPos[i];
                        tri.vertices[v] = vv;
                    }
                    holder.Add(tri.MakeDMesh());
                } catch (Exception) {  }
            }
        }

        /// <summary>
        /// Fit a trim curve to the ceiling or the floor of a sector.
        /// </summary>
        public static List<Vector2> FitCurveTrim(Sector s, List<Vector2> curvePts, bool floor)
        {
            List<Vector2> ret = new List<Vector2>();
            float multiplier = floor ? 1 : -1;
            float height = floor ? s.FloorHeight : s.CeilingHeight;
            float maxY = curvePts.Max(c => c.Y);
            float minY = curvePts.Min(c => c.Y);
            float rangeY = maxY - minY;

            for (int i = 0; i < curvePts.Count; ++i)
            {
                float normY = Mathf.Normalize(curvePts[i].Y, minY, maxY);
                ret.Add(new Vector2(curvePts[i].X,
                    floor ?
                        height + rangeY * normY :
                        height - (rangeY - rangeY * normY)
                ));
            }

            return ret;
        }

        /// <summary>
        /// Fit a trim curve to walls of the sector.
        /// </summary>
        public static List<Vector2> FitCurveToWall(float lower, float higher, List<Vector2> curvePts)
        {
            List<Vector2> ret = new List<Vector2>();
            float maxY = curvePts.Max(c => c.Y);
            float minY = curvePts.Min(c => c.Y);
            float rangeY = maxY - minY;
            float halfRange = rangeY / 2;

            for (int i = 0; i < curvePts.Count; ++i)
            {
                float y = curvePts[i].Y;
                if (y > halfRange)
                    y = higher - (maxY - y);
                else
                    y = lower + (y - minY);
                ret.Add(new Vector2(
                    curvePts[i].X,
                    y
                ));
            }

            return ret;
        }

        /// <summary>
        /// Lofts the solid walls of a sector with the given curve profile
        /// </summary>
        public List<g3.DMesh3> LoftSector(Sector s, List<Vector2> curveProfile)
        {
            List<g3.DMesh3> meshes = new List<g3.DMesh3>();
            List<SideDef[]> chains = s.GetSectorSolidChains(ss => ss.Line.IsSolid);
            if (chains.Count > 0)
            {
                foreach (SideDef[] chain in chains)
                {
                    List<Vector2> chainPts = chain.ToPointList();
                    List<Vector3> chainNormals = chainPts.ToNormals();

                    g3.Curve3LoftGenerator gen = new g3.Curve3LoftGenerator();
                    gen.Corners = chainPts.To3D().ToArray().ToG3();
                    gen.CornerNormals = chainNormals.ToArray().ToG3();
                    gen.Profile = curveProfile.ToArray().ToG3();
                    gen.Generate();
                    meshes.Add(gen.MakeDMesh());
                }
            }
            return meshes;
        }

        /// <summary>
        /// Lofts along the sidedef's using the given curve-profile,
        /// DOES NOT perform any fitting
        /// </summary>
        public g3.MeshGenerator Loft(SideDef[] chain, List<Vector2> curveProfile)
        {
            List<Vector2> chainPts = chain.ToPointList();
            List<Vector3> chainNormals = chainPts.ToNormals();

            g3.Curve3LoftGenerator gen = new g3.Curve3LoftGenerator();
            gen.Corners = chainPts.To3D().ToArray().ToG3();
            gen.CornerNormals = chainNormals.ToArray().ToG3();
            gen.Profile = curveProfile.ToArray().ToG3();
            return gen.Generate();
        }

        public List<EdgeFitment> GetSectorEdgeFitments(Sector s)
        {
            List<EdgeFitment> ret = new List<EdgeFitment>();
            foreach (var side in s.Sides)
            {
                if (side.Line.Meta.Impassable)
                {
                    ret.Add(new EdgeFitment
                    {
                        a = side.Line.Start.Position,
                        b = side.Line.End.Position,
                        side = side,
                        sectors = s // possibly redundant, but that depends on how the function is being used
                    });
                }
            }
            return ret;
        }

        public SortedSet<HardCorner> GetSectorHardPoints(Sector s, float angleTolerance)
        {
            SortedSet<HardCorner> ret = new SortedSet<HardCorner>();
            foreach (var line in s.Lines)
            {
                if (line.Meta.Impassable)
                {
                    var nextLine = line.GetNextSolid();
                    if (nextLine != null && Math.Abs(Vector2.Dot(line.FaceNormal, nextLine.FaceNormal)) < angleTolerance)
                    {
                        Vertex vert = line.GetSharedVertex(nextLine);
                        if (vert != null)
                            ret.Add(new HardCorner
                            {
                                Vertex = vert,
                                normal = Vector2.Normalize(line.FaceNormal + nextLine.FaceNormal)
                            });
                    }
                }
            }
            return ret;
        }

        public List<g3.DMesh3> ColumnizeHardCorners(SortedSet<HardCorner> corners, List<Vector2> curveProfile)
        {
            List<g3.DMesh3> ret = new List<g3.DMesh3>();

            foreach (var vert in corners)
            {
                float bottom = vert.Vertex.LowestPoint;
                float top = vert.Vertex.HighestPoint;

                var remapped = FitCurveToWall(bottom, top, curveProfile);

                g3.Curve3Axis3RevolveGenerator gen = new g3.Curve3Axis3RevolveGenerator();
                gen.Capped = false;
                gen.NoSharedVertices = false;
                gen.Axis = new g3.Frame3f(new g3.Vector3f(vert.Vertex.Position.X, 0, vert.Vertex.Position.Y));
                gen.Curve = remapped.ConvertAll(v => new g3.Vector3d(v.X, v.Y, 0)).ToArray();

                gen.Generate();
                ret.Add(gen.MakeDMesh());
            }

            return ret;
        }
    }
}
