using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DelveLib;
using Microsoft.Xna.Framework;

namespace Delve.Physics
{
    public struct CollisionTriangle
    {
        public Vector3 a, b, c;
        public Vector3 normal;
        public Vector3 centroid;
        public float radius;

        public void Calculate()
        {
            var ab = Vector3.Normalize(b - a);
            var ac = Vector3.Normalize(c - a);
            normal = Vector3.Cross(ab, ac);
            centroid = (a + b + c) / 3;
            radius = Math.Max(Vector3.Distance(centroid, a), Math.Max(Vector3.Distance(centroid, b), Vector3.Distance(centroid, c)));
        }

        public CollisionTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.centroid = this.normal = c;
            this.radius = 0;
            Calculate();
        }
    }

    public static class ColTriExt
    {
        public static Vector3? Intersection(this Ray ray, ref CollisionTriangle tri)
        {
            return ray.Intersection(tri.a, tri.b, tri.c);
        }

        public static bool Contains(this BoundingBox bounds, ref CollisionTriangle tri)
        {
            BoundingSphere s = new BoundingSphere(tri.centroid, tri.radius);
            return bounds.Contains(s) != ContainmentType.Disjoint;
        }

        public static List<CollisionTriangle> GenerateCollisionTris(this MeshData mesh)
        {
            List<CollisionTriangle> ret = new List<CollisionTriangle>();

            var indices = mesh.GetIndices();
            var vertices = mesh.GetVertices();

            for (int i = 0; i < indices.Count; i += 3)
            {
                ret.Add(new CollisionTriangle(
                    vertices[indices[i]].Position,
                    vertices[indices[i+1]].Position,
                    vertices[indices[i+2]].Position
                ));
            }

            return ret;
        }
    }

}
