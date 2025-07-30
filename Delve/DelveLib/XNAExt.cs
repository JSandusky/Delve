using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DelveLib
{
    public static class XNAExt
    {
        public static float AngleBetween(this Vector2 v, Vector2 rhs)
        {
            return Mathf.Acos(Vector2.Dot(v, rhs) / (v.Length() * rhs.Length()));
        }

        public static Vector3 Abs(this Vector3 v)
        {
            return new Vector3(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));
        }

        public static Plane CreatePlane(Vector3 pos, Vector3 nor)
        {
            Vector3 n = Vector3.Normalize(nor);
            Vector3 absNormal = n.Abs();
            float d = -Vector3.Dot(n, pos);
            return new Plane(n, d);
        }

        public static Vector3 Intersection(this Ray r, Plane p)
        {
            float? hitDist = r.Intersects(p);
            if (hitDist.HasValue)
                return r.Position + r.Direction * hitDist.Value;
            return new Vector3(1000, 1000, 1000);
        }

        public static bool Intersects(this Ray ray, Vector3 tri0, Vector3 tri1, Vector3 tri2)
        {
            Vector2 barycentric = new Vector2();

            // Find vectors for two edges sharing vert0
            Vector3 edge1 = tri1 - tri0;
            Vector3 edge2 = tri2 - tri0;

            // Begin calculating determinant - also used to calculate barycentricU parameter
            Vector3 pvec = Vector3.Cross(ray.Direction, edge2);

            // If determinant is near zero, ray lies in plane of triangle
            float det = Vector3.Dot(edge1, pvec);
            if (det < 0.0001f)
                return false;

            // Calculate distance from vert0 to ray origin
            Vector3 tvec = ray.Position - tri0;

            // Calculate barycentricU parameter and test bounds
            barycentric.X = Vector3.Dot(tvec, pvec);
            if (barycentric.X < 0.0f || barycentric.X > det)
                return false;

            // Prepare to test barycentricV parameter
            Vector3 qvec = Vector3.Cross(tvec, edge1);

            // Calculate barycentricV parameter and test bounds
            barycentric.Y = Vector3.Dot(ray.Direction, qvec);
            if (barycentric.Y < 0.0f || barycentric.X + barycentric.Y > det)
                return false;

            return true;
        }

        public static Vector3? Intersection(this Ray ray, Vector3 tri0, Vector3 tri1, Vector3 tri2)
        {
            float? dist = ray.IntersectionDistance(tri0, tri1, tri2);
            if (dist.HasValue)
                return ray.Position + ray.Direction * dist.Value;
            return null;
        }

        public static float? IntersectionDistance(this Ray ray, Vector3 tri0, Vector3 tri1, Vector3 tri2)
        {
            Vector2 barycentric = new Vector2();

            // Find vectors for two edges sharing vert0
            Vector3 edge1 = tri1 - tri0;
            Vector3 edge2 = tri2 - tri0;

            // Begin calculating determinant - also used to calculate barycentricU parameter
            Vector3 pvec = Vector3.Cross(ray.Direction, edge2);

            // If determinant is near zero, ray lies in plane of triangle
            float det = Vector3.Dot(edge1, pvec);
            if (det < 0.0001f)
                return null;
            float invDet = 1.0f / det;

            // Calculate distance from vert0 to ray origin
            Vector3 tvec = ray.Position - tri0;

            // Calculate barycentricU parameter and test bounds
            barycentric.X = Vector3.Dot(tvec, pvec) * invDet;
            if (barycentric.X < 0.0f || barycentric.X > 1.0f)
                return null;

            // Prepare to test barycentricV parameter
            Vector3 qvec = Vector3.Cross(tvec, edge1);

            // Calculate barycentricV parameter and test bounds
            barycentric.Y = Vector3.Dot(ray.Direction, qvec) * invDet;
            if (barycentric.Y < 0.0f || barycentric.X + barycentric.Y > 1.0f)
                return null;

            return Vector3.Dot(ray.Direction, edge2) * invDet;
        }

        public static bool Intersects(this Ray ray, BoundingBox bounds, Matrix transform)
        {
            Matrix inverseTrans = Matrix.Invert(transform);
            Ray newRay = new Ray(ray.Position + transform.Translation, Vector3.TransformNormal(ray.Direction, inverseTrans));
            return ray.Intersects(bounds).HasValue;
        }

        public static bool Intersects(this Ray ray, Vector3 tri0, Vector3 tri1, Vector3 tri2, ref float pickDistance, ref Vector2 barycentric)
        {
            barycentric = new Vector2();

            // Find vectors for two edges sharing vert0
            Vector3 edge1 = tri1 - tri0;
            Vector3 edge2 = tri2 - tri0;

            // Begin calculating determinant - also used to calculate barycentricU parameter
            Vector3 pvec = Vector3.Cross(ray.Direction, edge2);

            // If determinant is near zero, ray lies in plane of triangle
            float det = Vector3.Dot(edge1, pvec);
            if (det < 0.0001f)
                return false;

            // Calculate distance from vert0 to ray origin
            Vector3 tvec = ray.Position - tri0;

            // Calculate barycentricU parameter and test bounds
            barycentric.X = Vector3.Dot(tvec, pvec);
            if (barycentric.X < 0.0f || barycentric.X > det)
                return false;

            // Prepare to test barycentricV parameter
            Vector3 qvec = Vector3.Cross(tvec, edge1);

            // Calculate barycentricV parameter and test bounds
            barycentric.Y = Vector3.Dot(ray.Direction, qvec);
            if (barycentric.Y < 0.0f || barycentric.X + barycentric.Y > det)
                return false;

            // Calculate pickDistance, scale parameters, ray intersects triangle
            pickDistance = Vector3.Dot(edge2, qvec);
            float fInvDet = 1.0f / det;
            pickDistance *= fInvDet;
            barycentric.X *= fInvDet;
            barycentric.Y *= fInvDet;

            return true;
        }

        public static float DistanceBetweenSegments(Vector3 P0, Vector3 P1, Vector3 S0, Vector3 S1)
        {
            Vector3 u = P1 - P0;
            Vector3 v = S1 - S0;
            Vector3 w = P0 - S0;
            float a = Vector3.Dot(u, u);         // always >= 0
            float b = Vector3.Dot(u, v);
            float c = Vector3.Dot(v, v);         // always >= 0
            float d = Vector3.Dot(u, w);
            float e = Vector3.Dot(v, w);
            float D = a * c - b * b;        // always >= 0
            float sc, sN, sD = D;       // sc = sN / sD, default sD = D >= 0
            float tc, tN, tD = D;       // tc = tN / tD, default tD = D >= 0

            // compute the line parameters of the two closest points
            if (D < 0.00001f)
            { // the lines are almost parallel
                sN = 0.0f;         // force using point P0 on segment S1
                sD = 1.0f;         // to prevent possible division by 0.0 later
                tN = e;
                tD = c;
            }
            else
            {                 // get the closest points on the infinite lines
                sN = (b * e - c * d);
                tN = (a * e - b * d);
                if (sN < 0.0f)
                {        // sc < 0 => the s=0 edge is visible
                    sN = 0.0f;
                    tN = e;
                    tD = c;
                }
                else if (sN > sD)
                {  // sc > 1  => the s=1 edge is visible
                    sN = sD;
                    tN = e + b;
                    tD = c;
                }
            }

            if (tN < 0.0)
            {            // tc < 0 => the t=0 edge is visible
                tN = 0.0f;
                // recompute sc for this edge
                if (-d < 0.0)
                    sN = 0.0f;
                else if (-d > a)
                    sN = sD;
                else
                {
                    sN = -d;
                    sD = a;
                }
            }
            else if (tN > tD)
            {      // tc > 1  => the t=1 edge is visible
                tN = tD;
                // recompute sc for this edge
                if ((-d + b) < 0.0)
                    sN = 0;
                else if ((-d + b) > a)
                    sN = sD;
                else
                {
                    sN = (-d + b);
                    sD = a;
                }
            }
            // finally do the division to get sc and tc
            sc = (Mathf.Abs(sN) < 0.00001f ? 0.0f : sN / sD);
            tc = (Mathf.Abs(tN) < 0.00001f ? 0.0f : tN / tD);

            // get the difference of the two closest points
            Vector3 dP = w + (sc * u) - (tc * v);  // =  S1(sc) - S2(tc)

            return dP.Length();   // return the closest distance
        }

        public static float RayLineDistance(this Ray r, Vector3 p0, Vector3 p1)
        {
            return DistanceBetweenSegments(r.Position, r.Position + r.Direction * 500, p0, p1);
        }

        public static float MaxElement(this Vector3 v)
        {
            return Math.Max(v.X, Math.Max(v.Y, v.Z));
        }

        public static Vector3 Project(this Plane p, Vector3 point)
        {
            return point - p.Normal * (Vector3.Dot(p.Normal, point) + p.D);
        }

        public static Vector3 Project(this Plane p, Vector3 point, float rad)
        {
            return point - p.Normal * (Vector3.Dot(p.Normal, point) + p.D + rad);
        }

        public static Vector2 Rotate(this Vector2 vec, float degrees)
        {
            float cosVal = (float)Math.Cos(MathHelper.ToRadians(degrees));
            float sinVal = (float)Math.Sin(MathHelper.ToRadians(degrees));
            float newX = vec.X * cosVal - vec.Y * sinVal;
            float newY = vec.X * sinVal + vec.Y * cosVal;
            return new Vector2(newX, newY);
        }

        public static float ManhattanDistance(Vector2 v, Vector2 vv)
        {
            Vector2 e = v - vv;
            return Math.Abs(e.X) + Math.Abs(e.Y);
        }

        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static T Rand<T>(this IList<T> list)
        {
            return list[rng.Next(0, list.Count)];
        }

        public static float Rand(this System.Random r, float min, float max)
        {
            return (float)(min + r.NextDouble() * (max - min));
        }

        public static Vector3 Random(System.Random r, Vector3 min, Vector3 max)
        {
            return min + (max - min) * (float)r.NextDouble();
        }

        public static Vector2 Random(System.Random r, Vector2 min, Vector2 max)
        {
            return min + (max - min) * (float)r.NextDouble();
        }

        public static float PingPong(float x, float mod)
        {
            x = x % (mod * 2.0f);
            return x >= mod ? (2.0f * mod - x) : x;
        }

        public static Vector3 PingPong(Vector3 a, Vector3 b, float time, float period)
        {
            float frac = PingPong(time, period);
            return Vector3.Lerp(a, b, frac);
        }

        public static Vector2 PingPong(Vector2 a, Vector2 b, float time, float period)
        {
            float frac = PingPong(time, period);
            return Vector2.Lerp(a, b, frac);
        }

        public static Color PingPong(Color a, Color b, float time, float period)
        {
            float frac = PingPong(time, period);
            return Color.Lerp(a, b, frac);
        }

        public static float Distance(this Plane p, Vector3 pt)
        {
            return p.DotCoordinate(pt);
        }

        public static Quaternion FromStartEnd(Vector3 start, Vector3 end)
        {
            Vector3 normStart = Vector3.Normalize(start);
            Vector3 normEnd = Vector3.Normalize(end);
            float d = Vector3.Dot(normStart, normEnd);

            if (d > -1.0f + M_EPSILON)
            {
                Vector3 c = Vector3.Cross(normStart, normEnd);
                float s = (float)Math.Sqrt((1.0f + d) * 2.0f);
                float invS = 1.0f / s;
                return new Quaternion(c.X * invS, c.Y * invS, c.Z * invS, 0.5f * s);
            }
            else
            {
                Vector3 axis = Vector3.Cross(Vector3.Right, normStart);
                if (axis.Length() < M_EPSILON)
                    axis = Vector3.Cross(Vector3.Up, normStart);

                return Quaternion.CreateFromAxisAngle(axis, MathHelper.ToRadians(180));
            }
        }

        static readonly float M_EPSILON = 0.00001f;
    }

    public static class VectorExtensions
    {
        // Vector2 Swizzles
        public static Vector2 XX(this Vector2 v) { return new Vector2(v.X, v.X); }
        public static Vector2 XY(this Vector2 v) { return new Vector2(v.X, v.Y); }
        public static Vector2 YX(this Vector2 v) { return new Vector2(v.Y, v.X); }
        public static Vector2 YY(this Vector2 v) { return new Vector2(v.Y, v.Y); }
        public static Vector3 XXX(this Vector2 v) { return new Vector3(v.X, v.X, v.X); }
        public static Vector3 XXY(this Vector2 v) { return new Vector3(v.X, v.X, v.Y); }
        public static Vector3 XYX(this Vector2 v) { return new Vector3(v.X, v.Y, v.X); }
        public static Vector3 XYY(this Vector2 v) { return new Vector3(v.X, v.Y, v.Y); }
        public static Vector3 YXX(this Vector2 v) { return new Vector3(v.Y, v.X, v.X); }
        public static Vector3 YXY(this Vector2 v) { return new Vector3(v.Y, v.X, v.Y); }
        public static Vector3 YYX(this Vector2 v) { return new Vector3(v.Y, v.Y, v.X); }
        public static Vector3 YYY(this Vector2 v) { return new Vector3(v.Y, v.Y, v.Y); }
        // Vector4 swizzles
        public static Vector4 XXXX(this Vector2 v) { return new Vector4(v.X, v.X, v.X, v.X); }
        public static Vector4 XXXY(this Vector2 v) { return new Vector4(v.X, v.X, v.X, v.Y); }
        public static Vector4 XXYX(this Vector2 v) { return new Vector4(v.X, v.X, v.Y, v.X); }
        public static Vector4 XXYY(this Vector2 v) { return new Vector4(v.X, v.X, v.Y, v.Y); }
        public static Vector4 XYXX(this Vector2 v) { return new Vector4(v.X, v.Y, v.X, v.X); }
        public static Vector4 XYXY(this Vector2 v) { return new Vector4(v.X, v.Y, v.X, v.Y); }
        public static Vector4 XYYX(this Vector2 v) { return new Vector4(v.X, v.Y, v.Y, v.X); }
        public static Vector4 XYYY(this Vector2 v) { return new Vector4(v.X, v.Y, v.Y, v.Y); }
        public static Vector4 YXXX(this Vector2 v) { return new Vector4(v.Y, v.X, v.X, v.X); }
        public static Vector4 YXXY(this Vector2 v) { return new Vector4(v.Y, v.X, v.X, v.Y); }
        public static Vector4 YXYX(this Vector2 v) { return new Vector4(v.Y, v.X, v.Y, v.X); }
        public static Vector4 YXYY(this Vector2 v) { return new Vector4(v.Y, v.X, v.Y, v.Y); }
        public static Vector4 YYXX(this Vector2 v) { return new Vector4(v.Y, v.Y, v.X, v.X); }
        public static Vector4 YYXY(this Vector2 v) { return new Vector4(v.Y, v.Y, v.X, v.Y); }
        public static Vector4 YYYX(this Vector2 v) { return new Vector4(v.Y, v.Y, v.Y, v.X); }
        public static Vector4 YYYY(this Vector2 v) { return new Vector4(v.Y, v.Y, v.Y, v.Y); }
        // Vector3 Swizzles
        public static Vector2 XX(this Vector3 v) { return new Vector2(v.X, v.X); }
        public static Vector2 XY(this Vector3 v) { return new Vector2(v.X, v.Y); }
        public static Vector2 XZ(this Vector3 v) { return new Vector2(v.X, v.Z); }
        public static Vector2 YX(this Vector3 v) { return new Vector2(v.Y, v.X); }
        public static Vector2 YY(this Vector3 v) { return new Vector2(v.Y, v.Y); }
        public static Vector2 YZ(this Vector3 v) { return new Vector2(v.Y, v.Z); }
        public static Vector2 ZX(this Vector3 v) { return new Vector2(v.Z, v.X); }
        public static Vector2 ZY(this Vector3 v) { return new Vector2(v.Z, v.Y); }
        public static Vector2 ZZ(this Vector3 v) { return new Vector2(v.Z, v.Z); }
        public static Vector3 XXX(this Vector3 v) { return new Vector3(v.X, v.X, v.X); }
        public static Vector3 XXY(this Vector3 v) { return new Vector3(v.X, v.X, v.Y); }
        public static Vector3 XXZ(this Vector3 v) { return new Vector3(v.X, v.X, v.Z); }
        public static Vector3 XYX(this Vector3 v) { return new Vector3(v.X, v.Y, v.X); }
        public static Vector3 XYY(this Vector3 v) { return new Vector3(v.X, v.Y, v.Y); }
        public static Vector3 XYZ(this Vector3 v) { return new Vector3(v.X, v.Y, v.Z); }
        public static Vector3 XZX(this Vector3 v) { return new Vector3(v.X, v.Z, v.X); }
        public static Vector3 XZY(this Vector3 v) { return new Vector3(v.X, v.Z, v.Y); }
        public static Vector3 XZZ(this Vector3 v) { return new Vector3(v.X, v.Z, v.Z); }
        public static Vector3 YXX(this Vector3 v) { return new Vector3(v.Y, v.X, v.X); }
        public static Vector3 YXY(this Vector3 v) { return new Vector3(v.Y, v.X, v.Y); }
        public static Vector3 YXZ(this Vector3 v) { return new Vector3(v.Y, v.X, v.Z); }
        public static Vector3 YYX(this Vector3 v) { return new Vector3(v.Y, v.Y, v.X); }
        public static Vector3 YYY(this Vector3 v) { return new Vector3(v.Y, v.Y, v.Y); }
        public static Vector3 YYZ(this Vector3 v) { return new Vector3(v.Y, v.Y, v.Z); }
        public static Vector3 YZX(this Vector3 v) { return new Vector3(v.Y, v.Z, v.X); }
        public static Vector3 YZY(this Vector3 v) { return new Vector3(v.Y, v.Z, v.Y); }
        public static Vector3 YZZ(this Vector3 v) { return new Vector3(v.Y, v.Z, v.Z); }
        public static Vector3 ZXX(this Vector3 v) { return new Vector3(v.Z, v.X, v.X); }
        public static Vector3 ZXY(this Vector3 v) { return new Vector3(v.Z, v.X, v.Y); }
        public static Vector3 ZXZ(this Vector3 v) { return new Vector3(v.Z, v.X, v.Z); }
        public static Vector3 ZYX(this Vector3 v) { return new Vector3(v.Z, v.Y, v.X); }
        public static Vector3 ZYY(this Vector3 v) { return new Vector3(v.Z, v.Y, v.Y); }
        public static Vector3 ZYZ(this Vector3 v) { return new Vector3(v.Z, v.Y, v.Z); }
        public static Vector3 ZZX(this Vector3 v) { return new Vector3(v.Z, v.Z, v.X); }
        public static Vector3 ZZY(this Vector3 v) { return new Vector3(v.Z, v.Z, v.Y); }
        public static Vector3 ZZZ(this Vector3 v) { return new Vector3(v.Z, v.Z, v.Z); }
        // Vector4 swizzles
        public static Vector4 XXXX(this Vector3 v) { return new Vector4(v.X, v.X, v.X, v.X); }
        public static Vector4 XXXY(this Vector3 v) { return new Vector4(v.X, v.X, v.X, v.Y); }
        public static Vector4 XXXZ(this Vector3 v) { return new Vector4(v.X, v.X, v.X, v.Z); }
        public static Vector4 XXYX(this Vector3 v) { return new Vector4(v.X, v.X, v.Y, v.X); }
        public static Vector4 XXYY(this Vector3 v) { return new Vector4(v.X, v.X, v.Y, v.Y); }
        public static Vector4 XXYZ(this Vector3 v) { return new Vector4(v.X, v.X, v.Y, v.Z); }
        public static Vector4 XXZX(this Vector3 v) { return new Vector4(v.X, v.X, v.Z, v.X); }
        public static Vector4 XXZY(this Vector3 v) { return new Vector4(v.X, v.X, v.Z, v.Y); }
        public static Vector4 XXZZ(this Vector3 v) { return new Vector4(v.X, v.X, v.Z, v.Z); }
        public static Vector4 XYXX(this Vector3 v) { return new Vector4(v.X, v.Y, v.X, v.X); }
        public static Vector4 XYXY(this Vector3 v) { return new Vector4(v.X, v.Y, v.X, v.Y); }
        public static Vector4 XYXZ(this Vector3 v) { return new Vector4(v.X, v.Y, v.X, v.Z); }
        public static Vector4 XYYX(this Vector3 v) { return new Vector4(v.X, v.Y, v.Y, v.X); }
        public static Vector4 XYYY(this Vector3 v) { return new Vector4(v.X, v.Y, v.Y, v.Y); }
        public static Vector4 XYYZ(this Vector3 v) { return new Vector4(v.X, v.Y, v.Y, v.Z); }
        public static Vector4 XYZX(this Vector3 v) { return new Vector4(v.X, v.Y, v.Z, v.X); }
        public static Vector4 XYZY(this Vector3 v) { return new Vector4(v.X, v.Y, v.Z, v.Y); }
        public static Vector4 XYZZ(this Vector3 v) { return new Vector4(v.X, v.Y, v.Z, v.Z); }
        public static Vector4 XZXX(this Vector3 v) { return new Vector4(v.X, v.Z, v.X, v.X); }
        public static Vector4 XZXY(this Vector3 v) { return new Vector4(v.X, v.Z, v.X, v.Y); }
        public static Vector4 XZXZ(this Vector3 v) { return new Vector4(v.X, v.Z, v.X, v.Z); }
        public static Vector4 XZYX(this Vector3 v) { return new Vector4(v.X, v.Z, v.Y, v.X); }
        public static Vector4 XZYY(this Vector3 v) { return new Vector4(v.X, v.Z, v.Y, v.Y); }
        public static Vector4 XZYZ(this Vector3 v) { return new Vector4(v.X, v.Z, v.Y, v.Z); }
        public static Vector4 XZZX(this Vector3 v) { return new Vector4(v.X, v.Z, v.Z, v.X); }
        public static Vector4 XZZY(this Vector3 v) { return new Vector4(v.X, v.Z, v.Z, v.Y); }
        public static Vector4 XZZZ(this Vector3 v) { return new Vector4(v.X, v.Z, v.Z, v.Z); }
        public static Vector4 YXXX(this Vector3 v) { return new Vector4(v.Y, v.X, v.X, v.X); }
        public static Vector4 YXXY(this Vector3 v) { return new Vector4(v.Y, v.X, v.X, v.Y); }
        public static Vector4 YXXZ(this Vector3 v) { return new Vector4(v.Y, v.X, v.X, v.Z); }
        public static Vector4 YXYX(this Vector3 v) { return new Vector4(v.Y, v.X, v.Y, v.X); }
        public static Vector4 YXYY(this Vector3 v) { return new Vector4(v.Y, v.X, v.Y, v.Y); }
        public static Vector4 YXYZ(this Vector3 v) { return new Vector4(v.Y, v.X, v.Y, v.Z); }
        public static Vector4 YXZX(this Vector3 v) { return new Vector4(v.Y, v.X, v.Z, v.X); }
        public static Vector4 YXZY(this Vector3 v) { return new Vector4(v.Y, v.X, v.Z, v.Y); }
        public static Vector4 YXZZ(this Vector3 v) { return new Vector4(v.Y, v.X, v.Z, v.Z); }
        public static Vector4 YYXX(this Vector3 v) { return new Vector4(v.Y, v.Y, v.X, v.X); }
        public static Vector4 YYXY(this Vector3 v) { return new Vector4(v.Y, v.Y, v.X, v.Y); }
        public static Vector4 YYXZ(this Vector3 v) { return new Vector4(v.Y, v.Y, v.X, v.Z); }
        public static Vector4 YYYX(this Vector3 v) { return new Vector4(v.Y, v.Y, v.Y, v.X); }
        public static Vector4 YYYY(this Vector3 v) { return new Vector4(v.Y, v.Y, v.Y, v.Y); }
        public static Vector4 YYYZ(this Vector3 v) { return new Vector4(v.Y, v.Y, v.Y, v.Z); }
        public static Vector4 YYZX(this Vector3 v) { return new Vector4(v.Y, v.Y, v.Z, v.X); }
        public static Vector4 YYZY(this Vector3 v) { return new Vector4(v.Y, v.Y, v.Z, v.Y); }
        public static Vector4 YYZZ(this Vector3 v) { return new Vector4(v.Y, v.Y, v.Z, v.Z); }
        public static Vector4 YZXX(this Vector3 v) { return new Vector4(v.Y, v.Z, v.X, v.X); }
        public static Vector4 YZXY(this Vector3 v) { return new Vector4(v.Y, v.Z, v.X, v.Y); }
        public static Vector4 YZXZ(this Vector3 v) { return new Vector4(v.Y, v.Z, v.X, v.Z); }
        public static Vector4 YZYX(this Vector3 v) { return new Vector4(v.Y, v.Z, v.Y, v.X); }
        public static Vector4 YZYY(this Vector3 v) { return new Vector4(v.Y, v.Z, v.Y, v.Y); }
        public static Vector4 YZYZ(this Vector3 v) { return new Vector4(v.Y, v.Z, v.Y, v.Z); }
        public static Vector4 YZZX(this Vector3 v) { return new Vector4(v.Y, v.Z, v.Z, v.X); }
        public static Vector4 YZZY(this Vector3 v) { return new Vector4(v.Y, v.Z, v.Z, v.Y); }
        public static Vector4 YZZZ(this Vector3 v) { return new Vector4(v.Y, v.Z, v.Z, v.Z); }
        public static Vector4 ZXXX(this Vector3 v) { return new Vector4(v.Z, v.X, v.X, v.X); }
        public static Vector4 ZXXY(this Vector3 v) { return new Vector4(v.Z, v.X, v.X, v.Y); }
        public static Vector4 ZXXZ(this Vector3 v) { return new Vector4(v.Z, v.X, v.X, v.Z); }
        public static Vector4 ZXYX(this Vector3 v) { return new Vector4(v.Z, v.X, v.Y, v.X); }
        public static Vector4 ZXYY(this Vector3 v) { return new Vector4(v.Z, v.X, v.Y, v.Y); }
        public static Vector4 ZXYZ(this Vector3 v) { return new Vector4(v.Z, v.X, v.Y, v.Z); }
        public static Vector4 ZXZX(this Vector3 v) { return new Vector4(v.Z, v.X, v.Z, v.X); }
        public static Vector4 ZXZY(this Vector3 v) { return new Vector4(v.Z, v.X, v.Z, v.Y); }
        public static Vector4 ZXZZ(this Vector3 v) { return new Vector4(v.Z, v.X, v.Z, v.Z); }
        public static Vector4 ZYXX(this Vector3 v) { return new Vector4(v.Z, v.Y, v.X, v.X); }
        public static Vector4 ZYXY(this Vector3 v) { return new Vector4(v.Z, v.Y, v.X, v.Y); }
        public static Vector4 ZYXZ(this Vector3 v) { return new Vector4(v.Z, v.Y, v.X, v.Z); }
        public static Vector4 ZYYX(this Vector3 v) { return new Vector4(v.Z, v.Y, v.Y, v.X); }
        public static Vector4 ZYYY(this Vector3 v) { return new Vector4(v.Z, v.Y, v.Y, v.Y); }
        public static Vector4 ZYYZ(this Vector3 v) { return new Vector4(v.Z, v.Y, v.Y, v.Z); }
        public static Vector4 ZYZX(this Vector3 v) { return new Vector4(v.Z, v.Y, v.Z, v.X); }
        public static Vector4 ZYZY(this Vector3 v) { return new Vector4(v.Z, v.Y, v.Z, v.Y); }
        public static Vector4 ZYZZ(this Vector3 v) { return new Vector4(v.Z, v.Y, v.Z, v.Z); }
        public static Vector4 ZZXX(this Vector3 v) { return new Vector4(v.Z, v.Z, v.X, v.X); }
        public static Vector4 ZZXY(this Vector3 v) { return new Vector4(v.Z, v.Z, v.X, v.Y); }
        public static Vector4 ZZXZ(this Vector3 v) { return new Vector4(v.Z, v.Z, v.X, v.Z); }
        public static Vector4 ZZYX(this Vector3 v) { return new Vector4(v.Z, v.Z, v.Y, v.X); }
        public static Vector4 ZZYY(this Vector3 v) { return new Vector4(v.Z, v.Z, v.Y, v.Y); }
        public static Vector4 ZZYZ(this Vector3 v) { return new Vector4(v.Z, v.Z, v.Y, v.Z); }
        public static Vector4 ZZZX(this Vector3 v) { return new Vector4(v.Z, v.Z, v.Z, v.X); }
        public static Vector4 ZZZY(this Vector3 v) { return new Vector4(v.Z, v.Z, v.Z, v.Y); }
        public static Vector4 ZZZZ(this Vector3 v) { return new Vector4(v.Z, v.Z, v.Z, v.Z); }
        // Vector4 Swizzles
        public static Vector2 XX(this Vector4 v) { return new Vector2(v.X, v.X); }
        public static Vector2 XY(this Vector4 v) { return new Vector2(v.X, v.Y); }
        public static Vector2 XZ(this Vector4 v) { return new Vector2(v.X, v.Z); }
        public static Vector2 XW(this Vector4 v) { return new Vector2(v.X, v.W); }
        public static Vector2 YX(this Vector4 v) { return new Vector2(v.Y, v.X); }
        public static Vector2 YY(this Vector4 v) { return new Vector2(v.Y, v.Y); }
        public static Vector2 YZ(this Vector4 v) { return new Vector2(v.Y, v.Z); }
        public static Vector2 YW(this Vector4 v) { return new Vector2(v.Y, v.W); }
        public static Vector2 ZX(this Vector4 v) { return new Vector2(v.Z, v.X); }
        public static Vector2 ZY(this Vector4 v) { return new Vector2(v.Z, v.Y); }
        public static Vector2 ZZ(this Vector4 v) { return new Vector2(v.Z, v.Z); }
        public static Vector2 ZW(this Vector4 v) { return new Vector2(v.Z, v.W); }
        public static Vector2 WX(this Vector4 v) { return new Vector2(v.W, v.X); }
        public static Vector2 WY(this Vector4 v) { return new Vector2(v.W, v.Y); }
        public static Vector2 WZ(this Vector4 v) { return new Vector2(v.W, v.Z); }
        public static Vector2 WW(this Vector4 v) { return new Vector2(v.W, v.W); }
        public static Vector3 XXX(this Vector4 v) { return new Vector3(v.X, v.X, v.X); }
        public static Vector3 XXY(this Vector4 v) { return new Vector3(v.X, v.X, v.Y); }
        public static Vector3 XXZ(this Vector4 v) { return new Vector3(v.X, v.X, v.Z); }
        public static Vector3 XXW(this Vector4 v) { return new Vector3(v.X, v.X, v.W); }
        public static Vector3 XYX(this Vector4 v) { return new Vector3(v.X, v.Y, v.X); }
        public static Vector3 XYY(this Vector4 v) { return new Vector3(v.X, v.Y, v.Y); }
        public static Vector3 XYZ(this Vector4 v) { return new Vector3(v.X, v.Y, v.Z); }
        public static Vector3 XYW(this Vector4 v) { return new Vector3(v.X, v.Y, v.W); }
        public static Vector3 XZX(this Vector4 v) { return new Vector3(v.X, v.Z, v.X); }
        public static Vector3 XZY(this Vector4 v) { return new Vector3(v.X, v.Z, v.Y); }
        public static Vector3 XZZ(this Vector4 v) { return new Vector3(v.X, v.Z, v.Z); }
        public static Vector3 XZW(this Vector4 v) { return new Vector3(v.X, v.Z, v.W); }
        public static Vector3 XWX(this Vector4 v) { return new Vector3(v.X, v.W, v.X); }
        public static Vector3 XWY(this Vector4 v) { return new Vector3(v.X, v.W, v.Y); }
        public static Vector3 XWZ(this Vector4 v) { return new Vector3(v.X, v.W, v.Z); }
        public static Vector3 XWW(this Vector4 v) { return new Vector3(v.X, v.W, v.W); }
        public static Vector3 YXX(this Vector4 v) { return new Vector3(v.Y, v.X, v.X); }
        public static Vector3 YXY(this Vector4 v) { return new Vector3(v.Y, v.X, v.Y); }
        public static Vector3 YXZ(this Vector4 v) { return new Vector3(v.Y, v.X, v.Z); }
        public static Vector3 YXW(this Vector4 v) { return new Vector3(v.Y, v.X, v.W); }
        public static Vector3 YYX(this Vector4 v) { return new Vector3(v.Y, v.Y, v.X); }
        public static Vector3 YYY(this Vector4 v) { return new Vector3(v.Y, v.Y, v.Y); }
        public static Vector3 YYZ(this Vector4 v) { return new Vector3(v.Y, v.Y, v.Z); }
        public static Vector3 YYW(this Vector4 v) { return new Vector3(v.Y, v.Y, v.W); }
        public static Vector3 YZX(this Vector4 v) { return new Vector3(v.Y, v.Z, v.X); }
        public static Vector3 YZY(this Vector4 v) { return new Vector3(v.Y, v.Z, v.Y); }
        public static Vector3 YZZ(this Vector4 v) { return new Vector3(v.Y, v.Z, v.Z); }
        public static Vector3 YZW(this Vector4 v) { return new Vector3(v.Y, v.Z, v.W); }
        public static Vector3 YWX(this Vector4 v) { return new Vector3(v.Y, v.W, v.X); }
        public static Vector3 YWY(this Vector4 v) { return new Vector3(v.Y, v.W, v.Y); }
        public static Vector3 YWZ(this Vector4 v) { return new Vector3(v.Y, v.W, v.Z); }
        public static Vector3 YWW(this Vector4 v) { return new Vector3(v.Y, v.W, v.W); }
        public static Vector3 ZXX(this Vector4 v) { return new Vector3(v.Z, v.X, v.X); }
        public static Vector3 ZXY(this Vector4 v) { return new Vector3(v.Z, v.X, v.Y); }
        public static Vector3 ZXZ(this Vector4 v) { return new Vector3(v.Z, v.X, v.Z); }
        public static Vector3 ZXW(this Vector4 v) { return new Vector3(v.Z, v.X, v.W); }
        public static Vector3 ZYX(this Vector4 v) { return new Vector3(v.Z, v.Y, v.X); }
        public static Vector3 ZYY(this Vector4 v) { return new Vector3(v.Z, v.Y, v.Y); }
        public static Vector3 ZYZ(this Vector4 v) { return new Vector3(v.Z, v.Y, v.Z); }
        public static Vector3 ZYW(this Vector4 v) { return new Vector3(v.Z, v.Y, v.W); }
        public static Vector3 ZZX(this Vector4 v) { return new Vector3(v.Z, v.Z, v.X); }
        public static Vector3 ZZY(this Vector4 v) { return new Vector3(v.Z, v.Z, v.Y); }
        public static Vector3 ZZZ(this Vector4 v) { return new Vector3(v.Z, v.Z, v.Z); }
        public static Vector3 ZZW(this Vector4 v) { return new Vector3(v.Z, v.Z, v.W); }
        public static Vector3 ZWX(this Vector4 v) { return new Vector3(v.Z, v.W, v.X); }
        public static Vector3 ZWY(this Vector4 v) { return new Vector3(v.Z, v.W, v.Y); }
        public static Vector3 ZWZ(this Vector4 v) { return new Vector3(v.Z, v.W, v.Z); }
        public static Vector3 ZWW(this Vector4 v) { return new Vector3(v.Z, v.W, v.W); }
        public static Vector3 WXX(this Vector4 v) { return new Vector3(v.W, v.X, v.X); }
        public static Vector3 WXY(this Vector4 v) { return new Vector3(v.W, v.X, v.Y); }
        public static Vector3 WXZ(this Vector4 v) { return new Vector3(v.W, v.X, v.Z); }
        public static Vector3 WXW(this Vector4 v) { return new Vector3(v.W, v.X, v.W); }
        public static Vector3 WYX(this Vector4 v) { return new Vector3(v.W, v.Y, v.X); }
        public static Vector3 WYY(this Vector4 v) { return new Vector3(v.W, v.Y, v.Y); }
        public static Vector3 WYZ(this Vector4 v) { return new Vector3(v.W, v.Y, v.Z); }
        public static Vector3 WYW(this Vector4 v) { return new Vector3(v.W, v.Y, v.W); }
        public static Vector3 WZX(this Vector4 v) { return new Vector3(v.W, v.Z, v.X); }
        public static Vector3 WZY(this Vector4 v) { return new Vector3(v.W, v.Z, v.Y); }
        public static Vector3 WZZ(this Vector4 v) { return new Vector3(v.W, v.Z, v.Z); }
        public static Vector3 WZW(this Vector4 v) { return new Vector3(v.W, v.Z, v.W); }
        public static Vector3 WWX(this Vector4 v) { return new Vector3(v.W, v.W, v.X); }
        public static Vector3 WWY(this Vector4 v) { return new Vector3(v.W, v.W, v.Y); }
        public static Vector3 WWZ(this Vector4 v) { return new Vector3(v.W, v.W, v.Z); }
        public static Vector3 WWW(this Vector4 v) { return new Vector3(v.W, v.W, v.W); }
        // Vector4 swizzles
        public static Vector4 XXXX(this Vector4 v) { return new Vector4(v.X, v.X, v.X, v.X); }
        public static Vector4 XXXY(this Vector4 v) { return new Vector4(v.X, v.X, v.X, v.Y); }
        public static Vector4 XXXZ(this Vector4 v) { return new Vector4(v.X, v.X, v.X, v.Z); }
        public static Vector4 XXXW(this Vector4 v) { return new Vector4(v.X, v.X, v.X, v.W); }
        public static Vector4 XXYX(this Vector4 v) { return new Vector4(v.X, v.X, v.Y, v.X); }
        public static Vector4 XXYY(this Vector4 v) { return new Vector4(v.X, v.X, v.Y, v.Y); }
        public static Vector4 XXYZ(this Vector4 v) { return new Vector4(v.X, v.X, v.Y, v.Z); }
        public static Vector4 XXYW(this Vector4 v) { return new Vector4(v.X, v.X, v.Y, v.W); }
        public static Vector4 XXZX(this Vector4 v) { return new Vector4(v.X, v.X, v.Z, v.X); }
        public static Vector4 XXZY(this Vector4 v) { return new Vector4(v.X, v.X, v.Z, v.Y); }
        public static Vector4 XXZZ(this Vector4 v) { return new Vector4(v.X, v.X, v.Z, v.Z); }
        public static Vector4 XXZW(this Vector4 v) { return new Vector4(v.X, v.X, v.Z, v.W); }
        public static Vector4 XXWX(this Vector4 v) { return new Vector4(v.X, v.X, v.W, v.X); }
        public static Vector4 XXWY(this Vector4 v) { return new Vector4(v.X, v.X, v.W, v.Y); }
        public static Vector4 XXWZ(this Vector4 v) { return new Vector4(v.X, v.X, v.W, v.Z); }
        public static Vector4 XXWW(this Vector4 v) { return new Vector4(v.X, v.X, v.W, v.W); }
        public static Vector4 XYXX(this Vector4 v) { return new Vector4(v.X, v.Y, v.X, v.X); }
        public static Vector4 XYXY(this Vector4 v) { return new Vector4(v.X, v.Y, v.X, v.Y); }
        public static Vector4 XYXZ(this Vector4 v) { return new Vector4(v.X, v.Y, v.X, v.Z); }
        public static Vector4 XYXW(this Vector4 v) { return new Vector4(v.X, v.Y, v.X, v.W); }
        public static Vector4 XYYX(this Vector4 v) { return new Vector4(v.X, v.Y, v.Y, v.X); }
        public static Vector4 XYYY(this Vector4 v) { return new Vector4(v.X, v.Y, v.Y, v.Y); }
        public static Vector4 XYYZ(this Vector4 v) { return new Vector4(v.X, v.Y, v.Y, v.Z); }
        public static Vector4 XYYW(this Vector4 v) { return new Vector4(v.X, v.Y, v.Y, v.W); }
        public static Vector4 XYZX(this Vector4 v) { return new Vector4(v.X, v.Y, v.Z, v.X); }
        public static Vector4 XYZY(this Vector4 v) { return new Vector4(v.X, v.Y, v.Z, v.Y); }
        public static Vector4 XYZZ(this Vector4 v) { return new Vector4(v.X, v.Y, v.Z, v.Z); }
        public static Vector4 XYZW(this Vector4 v) { return new Vector4(v.X, v.Y, v.Z, v.W); }
        public static Vector4 XYWX(this Vector4 v) { return new Vector4(v.X, v.Y, v.W, v.X); }
        public static Vector4 XYWY(this Vector4 v) { return new Vector4(v.X, v.Y, v.W, v.Y); }
        public static Vector4 XYWZ(this Vector4 v) { return new Vector4(v.X, v.Y, v.W, v.Z); }
        public static Vector4 XYWW(this Vector4 v) { return new Vector4(v.X, v.Y, v.W, v.W); }
        public static Vector4 XZXX(this Vector4 v) { return new Vector4(v.X, v.Z, v.X, v.X); }
        public static Vector4 XZXY(this Vector4 v) { return new Vector4(v.X, v.Z, v.X, v.Y); }
        public static Vector4 XZXZ(this Vector4 v) { return new Vector4(v.X, v.Z, v.X, v.Z); }
        public static Vector4 XZXW(this Vector4 v) { return new Vector4(v.X, v.Z, v.X, v.W); }
        public static Vector4 XZYX(this Vector4 v) { return new Vector4(v.X, v.Z, v.Y, v.X); }
        public static Vector4 XZYY(this Vector4 v) { return new Vector4(v.X, v.Z, v.Y, v.Y); }
        public static Vector4 XZYZ(this Vector4 v) { return new Vector4(v.X, v.Z, v.Y, v.Z); }
        public static Vector4 XZYW(this Vector4 v) { return new Vector4(v.X, v.Z, v.Y, v.W); }
        public static Vector4 XZZX(this Vector4 v) { return new Vector4(v.X, v.Z, v.Z, v.X); }
        public static Vector4 XZZY(this Vector4 v) { return new Vector4(v.X, v.Z, v.Z, v.Y); }
        public static Vector4 XZZZ(this Vector4 v) { return new Vector4(v.X, v.Z, v.Z, v.Z); }
        public static Vector4 XZZW(this Vector4 v) { return new Vector4(v.X, v.Z, v.Z, v.W); }
        public static Vector4 XZWX(this Vector4 v) { return new Vector4(v.X, v.Z, v.W, v.X); }
        public static Vector4 XZWY(this Vector4 v) { return new Vector4(v.X, v.Z, v.W, v.Y); }
        public static Vector4 XZWZ(this Vector4 v) { return new Vector4(v.X, v.Z, v.W, v.Z); }
        public static Vector4 XZWW(this Vector4 v) { return new Vector4(v.X, v.Z, v.W, v.W); }
        public static Vector4 XWXX(this Vector4 v) { return new Vector4(v.X, v.W, v.X, v.X); }
        public static Vector4 XWXY(this Vector4 v) { return new Vector4(v.X, v.W, v.X, v.Y); }
        public static Vector4 XWXZ(this Vector4 v) { return new Vector4(v.X, v.W, v.X, v.Z); }
        public static Vector4 XWXW(this Vector4 v) { return new Vector4(v.X, v.W, v.X, v.W); }
        public static Vector4 XWYX(this Vector4 v) { return new Vector4(v.X, v.W, v.Y, v.X); }
        public static Vector4 XWYY(this Vector4 v) { return new Vector4(v.X, v.W, v.Y, v.Y); }
        public static Vector4 XWYZ(this Vector4 v) { return new Vector4(v.X, v.W, v.Y, v.Z); }
        public static Vector4 XWYW(this Vector4 v) { return new Vector4(v.X, v.W, v.Y, v.W); }
        public static Vector4 XWZX(this Vector4 v) { return new Vector4(v.X, v.W, v.Z, v.X); }
        public static Vector4 XWZY(this Vector4 v) { return new Vector4(v.X, v.W, v.Z, v.Y); }
        public static Vector4 XWZZ(this Vector4 v) { return new Vector4(v.X, v.W, v.Z, v.Z); }
        public static Vector4 XWZW(this Vector4 v) { return new Vector4(v.X, v.W, v.Z, v.W); }
        public static Vector4 XWWX(this Vector4 v) { return new Vector4(v.X, v.W, v.W, v.X); }
        public static Vector4 XWWY(this Vector4 v) { return new Vector4(v.X, v.W, v.W, v.Y); }
        public static Vector4 XWWZ(this Vector4 v) { return new Vector4(v.X, v.W, v.W, v.Z); }
        public static Vector4 XWWW(this Vector4 v) { return new Vector4(v.X, v.W, v.W, v.W); }
        public static Vector4 YXXX(this Vector4 v) { return new Vector4(v.Y, v.X, v.X, v.X); }
        public static Vector4 YXXY(this Vector4 v) { return new Vector4(v.Y, v.X, v.X, v.Y); }
        public static Vector4 YXXZ(this Vector4 v) { return new Vector4(v.Y, v.X, v.X, v.Z); }
        public static Vector4 YXXW(this Vector4 v) { return new Vector4(v.Y, v.X, v.X, v.W); }
        public static Vector4 YXYX(this Vector4 v) { return new Vector4(v.Y, v.X, v.Y, v.X); }
        public static Vector4 YXYY(this Vector4 v) { return new Vector4(v.Y, v.X, v.Y, v.Y); }
        public static Vector4 YXYZ(this Vector4 v) { return new Vector4(v.Y, v.X, v.Y, v.Z); }
        public static Vector4 YXYW(this Vector4 v) { return new Vector4(v.Y, v.X, v.Y, v.W); }
        public static Vector4 YXZX(this Vector4 v) { return new Vector4(v.Y, v.X, v.Z, v.X); }
        public static Vector4 YXZY(this Vector4 v) { return new Vector4(v.Y, v.X, v.Z, v.Y); }
        public static Vector4 YXZZ(this Vector4 v) { return new Vector4(v.Y, v.X, v.Z, v.Z); }
        public static Vector4 YXZW(this Vector4 v) { return new Vector4(v.Y, v.X, v.Z, v.W); }
        public static Vector4 YXWX(this Vector4 v) { return new Vector4(v.Y, v.X, v.W, v.X); }
        public static Vector4 YXWY(this Vector4 v) { return new Vector4(v.Y, v.X, v.W, v.Y); }
        public static Vector4 YXWZ(this Vector4 v) { return new Vector4(v.Y, v.X, v.W, v.Z); }
        public static Vector4 YXWW(this Vector4 v) { return new Vector4(v.Y, v.X, v.W, v.W); }
        public static Vector4 YYXX(this Vector4 v) { return new Vector4(v.Y, v.Y, v.X, v.X); }
        public static Vector4 YYXY(this Vector4 v) { return new Vector4(v.Y, v.Y, v.X, v.Y); }
        public static Vector4 YYXZ(this Vector4 v) { return new Vector4(v.Y, v.Y, v.X, v.Z); }
        public static Vector4 YYXW(this Vector4 v) { return new Vector4(v.Y, v.Y, v.X, v.W); }
        public static Vector4 YYYX(this Vector4 v) { return new Vector4(v.Y, v.Y, v.Y, v.X); }
        public static Vector4 YYYY(this Vector4 v) { return new Vector4(v.Y, v.Y, v.Y, v.Y); }
        public static Vector4 YYYZ(this Vector4 v) { return new Vector4(v.Y, v.Y, v.Y, v.Z); }
        public static Vector4 YYYW(this Vector4 v) { return new Vector4(v.Y, v.Y, v.Y, v.W); }
        public static Vector4 YYZX(this Vector4 v) { return new Vector4(v.Y, v.Y, v.Z, v.X); }
        public static Vector4 YYZY(this Vector4 v) { return new Vector4(v.Y, v.Y, v.Z, v.Y); }
        public static Vector4 YYZZ(this Vector4 v) { return new Vector4(v.Y, v.Y, v.Z, v.Z); }
        public static Vector4 YYZW(this Vector4 v) { return new Vector4(v.Y, v.Y, v.Z, v.W); }
        public static Vector4 YYWX(this Vector4 v) { return new Vector4(v.Y, v.Y, v.W, v.X); }
        public static Vector4 YYWY(this Vector4 v) { return new Vector4(v.Y, v.Y, v.W, v.Y); }
        public static Vector4 YYWZ(this Vector4 v) { return new Vector4(v.Y, v.Y, v.W, v.Z); }
        public static Vector4 YYWW(this Vector4 v) { return new Vector4(v.Y, v.Y, v.W, v.W); }
        public static Vector4 YZXX(this Vector4 v) { return new Vector4(v.Y, v.Z, v.X, v.X); }
        public static Vector4 YZXY(this Vector4 v) { return new Vector4(v.Y, v.Z, v.X, v.Y); }
        public static Vector4 YZXZ(this Vector4 v) { return new Vector4(v.Y, v.Z, v.X, v.Z); }
        public static Vector4 YZXW(this Vector4 v) { return new Vector4(v.Y, v.Z, v.X, v.W); }
        public static Vector4 YZYX(this Vector4 v) { return new Vector4(v.Y, v.Z, v.Y, v.X); }
        public static Vector4 YZYY(this Vector4 v) { return new Vector4(v.Y, v.Z, v.Y, v.Y); }
        public static Vector4 YZYZ(this Vector4 v) { return new Vector4(v.Y, v.Z, v.Y, v.Z); }
        public static Vector4 YZYW(this Vector4 v) { return new Vector4(v.Y, v.Z, v.Y, v.W); }
        public static Vector4 YZZX(this Vector4 v) { return new Vector4(v.Y, v.Z, v.Z, v.X); }
        public static Vector4 YZZY(this Vector4 v) { return new Vector4(v.Y, v.Z, v.Z, v.Y); }
        public static Vector4 YZZZ(this Vector4 v) { return new Vector4(v.Y, v.Z, v.Z, v.Z); }
        public static Vector4 YZZW(this Vector4 v) { return new Vector4(v.Y, v.Z, v.Z, v.W); }
        public static Vector4 YZWX(this Vector4 v) { return new Vector4(v.Y, v.Z, v.W, v.X); }
        public static Vector4 YZWY(this Vector4 v) { return new Vector4(v.Y, v.Z, v.W, v.Y); }
        public static Vector4 YZWZ(this Vector4 v) { return new Vector4(v.Y, v.Z, v.W, v.Z); }
        public static Vector4 YZWW(this Vector4 v) { return new Vector4(v.Y, v.Z, v.W, v.W); }
        public static Vector4 YWXX(this Vector4 v) { return new Vector4(v.Y, v.W, v.X, v.X); }
        public static Vector4 YWXY(this Vector4 v) { return new Vector4(v.Y, v.W, v.X, v.Y); }
        public static Vector4 YWXZ(this Vector4 v) { return new Vector4(v.Y, v.W, v.X, v.Z); }
        public static Vector4 YWXW(this Vector4 v) { return new Vector4(v.Y, v.W, v.X, v.W); }
        public static Vector4 YWYX(this Vector4 v) { return new Vector4(v.Y, v.W, v.Y, v.X); }
        public static Vector4 YWYY(this Vector4 v) { return new Vector4(v.Y, v.W, v.Y, v.Y); }
        public static Vector4 YWYZ(this Vector4 v) { return new Vector4(v.Y, v.W, v.Y, v.Z); }
        public static Vector4 YWYW(this Vector4 v) { return new Vector4(v.Y, v.W, v.Y, v.W); }
        public static Vector4 YWZX(this Vector4 v) { return new Vector4(v.Y, v.W, v.Z, v.X); }
        public static Vector4 YWZY(this Vector4 v) { return new Vector4(v.Y, v.W, v.Z, v.Y); }
        public static Vector4 YWZZ(this Vector4 v) { return new Vector4(v.Y, v.W, v.Z, v.Z); }
        public static Vector4 YWZW(this Vector4 v) { return new Vector4(v.Y, v.W, v.Z, v.W); }
        public static Vector4 YWWX(this Vector4 v) { return new Vector4(v.Y, v.W, v.W, v.X); }
        public static Vector4 YWWY(this Vector4 v) { return new Vector4(v.Y, v.W, v.W, v.Y); }
        public static Vector4 YWWZ(this Vector4 v) { return new Vector4(v.Y, v.W, v.W, v.Z); }
        public static Vector4 YWWW(this Vector4 v) { return new Vector4(v.Y, v.W, v.W, v.W); }
        public static Vector4 ZXXX(this Vector4 v) { return new Vector4(v.Z, v.X, v.X, v.X); }
        public static Vector4 ZXXY(this Vector4 v) { return new Vector4(v.Z, v.X, v.X, v.Y); }
        public static Vector4 ZXXZ(this Vector4 v) { return new Vector4(v.Z, v.X, v.X, v.Z); }
        public static Vector4 ZXXW(this Vector4 v) { return new Vector4(v.Z, v.X, v.X, v.W); }
        public static Vector4 ZXYX(this Vector4 v) { return new Vector4(v.Z, v.X, v.Y, v.X); }
        public static Vector4 ZXYY(this Vector4 v) { return new Vector4(v.Z, v.X, v.Y, v.Y); }
        public static Vector4 ZXYZ(this Vector4 v) { return new Vector4(v.Z, v.X, v.Y, v.Z); }
        public static Vector4 ZXYW(this Vector4 v) { return new Vector4(v.Z, v.X, v.Y, v.W); }
        public static Vector4 ZXZX(this Vector4 v) { return new Vector4(v.Z, v.X, v.Z, v.X); }
        public static Vector4 ZXZY(this Vector4 v) { return new Vector4(v.Z, v.X, v.Z, v.Y); }
        public static Vector4 ZXZZ(this Vector4 v) { return new Vector4(v.Z, v.X, v.Z, v.Z); }
        public static Vector4 ZXZW(this Vector4 v) { return new Vector4(v.Z, v.X, v.Z, v.W); }
        public static Vector4 ZXWX(this Vector4 v) { return new Vector4(v.Z, v.X, v.W, v.X); }
        public static Vector4 ZXWY(this Vector4 v) { return new Vector4(v.Z, v.X, v.W, v.Y); }
        public static Vector4 ZXWZ(this Vector4 v) { return new Vector4(v.Z, v.X, v.W, v.Z); }
        public static Vector4 ZXWW(this Vector4 v) { return new Vector4(v.Z, v.X, v.W, v.W); }
        public static Vector4 ZYXX(this Vector4 v) { return new Vector4(v.Z, v.Y, v.X, v.X); }
        public static Vector4 ZYXY(this Vector4 v) { return new Vector4(v.Z, v.Y, v.X, v.Y); }
        public static Vector4 ZYXZ(this Vector4 v) { return new Vector4(v.Z, v.Y, v.X, v.Z); }
        public static Vector4 ZYXW(this Vector4 v) { return new Vector4(v.Z, v.Y, v.X, v.W); }
        public static Vector4 ZYYX(this Vector4 v) { return new Vector4(v.Z, v.Y, v.Y, v.X); }
        public static Vector4 ZYYY(this Vector4 v) { return new Vector4(v.Z, v.Y, v.Y, v.Y); }
        public static Vector4 ZYYZ(this Vector4 v) { return new Vector4(v.Z, v.Y, v.Y, v.Z); }
        public static Vector4 ZYYW(this Vector4 v) { return new Vector4(v.Z, v.Y, v.Y, v.W); }
        public static Vector4 ZYZX(this Vector4 v) { return new Vector4(v.Z, v.Y, v.Z, v.X); }
        public static Vector4 ZYZY(this Vector4 v) { return new Vector4(v.Z, v.Y, v.Z, v.Y); }
        public static Vector4 ZYZZ(this Vector4 v) { return new Vector4(v.Z, v.Y, v.Z, v.Z); }
        public static Vector4 ZYZW(this Vector4 v) { return new Vector4(v.Z, v.Y, v.Z, v.W); }
        public static Vector4 ZYWX(this Vector4 v) { return new Vector4(v.Z, v.Y, v.W, v.X); }
        public static Vector4 ZYWY(this Vector4 v) { return new Vector4(v.Z, v.Y, v.W, v.Y); }
        public static Vector4 ZYWZ(this Vector4 v) { return new Vector4(v.Z, v.Y, v.W, v.Z); }
        public static Vector4 ZYWW(this Vector4 v) { return new Vector4(v.Z, v.Y, v.W, v.W); }
        public static Vector4 ZZXX(this Vector4 v) { return new Vector4(v.Z, v.Z, v.X, v.X); }
        public static Vector4 ZZXY(this Vector4 v) { return new Vector4(v.Z, v.Z, v.X, v.Y); }
        public static Vector4 ZZXZ(this Vector4 v) { return new Vector4(v.Z, v.Z, v.X, v.Z); }
        public static Vector4 ZZXW(this Vector4 v) { return new Vector4(v.Z, v.Z, v.X, v.W); }
        public static Vector4 ZZYX(this Vector4 v) { return new Vector4(v.Z, v.Z, v.Y, v.X); }
        public static Vector4 ZZYY(this Vector4 v) { return new Vector4(v.Z, v.Z, v.Y, v.Y); }
        public static Vector4 ZZYZ(this Vector4 v) { return new Vector4(v.Z, v.Z, v.Y, v.Z); }
        public static Vector4 ZZYW(this Vector4 v) { return new Vector4(v.Z, v.Z, v.Y, v.W); }
        public static Vector4 ZZZX(this Vector4 v) { return new Vector4(v.Z, v.Z, v.Z, v.X); }
        public static Vector4 ZZZY(this Vector4 v) { return new Vector4(v.Z, v.Z, v.Z, v.Y); }
        public static Vector4 ZZZZ(this Vector4 v) { return new Vector4(v.Z, v.Z, v.Z, v.Z); }
        public static Vector4 ZZZW(this Vector4 v) { return new Vector4(v.Z, v.Z, v.Z, v.W); }
        public static Vector4 ZZWX(this Vector4 v) { return new Vector4(v.Z, v.Z, v.W, v.X); }
        public static Vector4 ZZWY(this Vector4 v) { return new Vector4(v.Z, v.Z, v.W, v.Y); }
        public static Vector4 ZZWZ(this Vector4 v) { return new Vector4(v.Z, v.Z, v.W, v.Z); }
        public static Vector4 ZZWW(this Vector4 v) { return new Vector4(v.Z, v.Z, v.W, v.W); }
        public static Vector4 ZWXX(this Vector4 v) { return new Vector4(v.Z, v.W, v.X, v.X); }
        public static Vector4 ZWXY(this Vector4 v) { return new Vector4(v.Z, v.W, v.X, v.Y); }
        public static Vector4 ZWXZ(this Vector4 v) { return new Vector4(v.Z, v.W, v.X, v.Z); }
        public static Vector4 ZWXW(this Vector4 v) { return new Vector4(v.Z, v.W, v.X, v.W); }
        public static Vector4 ZWYX(this Vector4 v) { return new Vector4(v.Z, v.W, v.Y, v.X); }
        public static Vector4 ZWYY(this Vector4 v) { return new Vector4(v.Z, v.W, v.Y, v.Y); }
        public static Vector4 ZWYZ(this Vector4 v) { return new Vector4(v.Z, v.W, v.Y, v.Z); }
        public static Vector4 ZWYW(this Vector4 v) { return new Vector4(v.Z, v.W, v.Y, v.W); }
        public static Vector4 ZWZX(this Vector4 v) { return new Vector4(v.Z, v.W, v.Z, v.X); }
        public static Vector4 ZWZY(this Vector4 v) { return new Vector4(v.Z, v.W, v.Z, v.Y); }
        public static Vector4 ZWZZ(this Vector4 v) { return new Vector4(v.Z, v.W, v.Z, v.Z); }
        public static Vector4 ZWZW(this Vector4 v) { return new Vector4(v.Z, v.W, v.Z, v.W); }
        public static Vector4 ZWWX(this Vector4 v) { return new Vector4(v.Z, v.W, v.W, v.X); }
        public static Vector4 ZWWY(this Vector4 v) { return new Vector4(v.Z, v.W, v.W, v.Y); }
        public static Vector4 ZWWZ(this Vector4 v) { return new Vector4(v.Z, v.W, v.W, v.Z); }
        public static Vector4 ZWWW(this Vector4 v) { return new Vector4(v.Z, v.W, v.W, v.W); }
        public static Vector4 WXXX(this Vector4 v) { return new Vector4(v.W, v.X, v.X, v.X); }
        public static Vector4 WXXY(this Vector4 v) { return new Vector4(v.W, v.X, v.X, v.Y); }
        public static Vector4 WXXZ(this Vector4 v) { return new Vector4(v.W, v.X, v.X, v.Z); }
        public static Vector4 WXXW(this Vector4 v) { return new Vector4(v.W, v.X, v.X, v.W); }
        public static Vector4 WXYX(this Vector4 v) { return new Vector4(v.W, v.X, v.Y, v.X); }
        public static Vector4 WXYY(this Vector4 v) { return new Vector4(v.W, v.X, v.Y, v.Y); }
        public static Vector4 WXYZ(this Vector4 v) { return new Vector4(v.W, v.X, v.Y, v.Z); }
        public static Vector4 WXYW(this Vector4 v) { return new Vector4(v.W, v.X, v.Y, v.W); }
        public static Vector4 WXZX(this Vector4 v) { return new Vector4(v.W, v.X, v.Z, v.X); }
        public static Vector4 WXZY(this Vector4 v) { return new Vector4(v.W, v.X, v.Z, v.Y); }
        public static Vector4 WXZZ(this Vector4 v) { return new Vector4(v.W, v.X, v.Z, v.Z); }
        public static Vector4 WXZW(this Vector4 v) { return new Vector4(v.W, v.X, v.Z, v.W); }
        public static Vector4 WXWX(this Vector4 v) { return new Vector4(v.W, v.X, v.W, v.X); }
        public static Vector4 WXWY(this Vector4 v) { return new Vector4(v.W, v.X, v.W, v.Y); }
        public static Vector4 WXWZ(this Vector4 v) { return new Vector4(v.W, v.X, v.W, v.Z); }
        public static Vector4 WXWW(this Vector4 v) { return new Vector4(v.W, v.X, v.W, v.W); }
        public static Vector4 WYXX(this Vector4 v) { return new Vector4(v.W, v.Y, v.X, v.X); }
        public static Vector4 WYXY(this Vector4 v) { return new Vector4(v.W, v.Y, v.X, v.Y); }
        public static Vector4 WYXZ(this Vector4 v) { return new Vector4(v.W, v.Y, v.X, v.Z); }
        public static Vector4 WYXW(this Vector4 v) { return new Vector4(v.W, v.Y, v.X, v.W); }
        public static Vector4 WYYX(this Vector4 v) { return new Vector4(v.W, v.Y, v.Y, v.X); }
        public static Vector4 WYYY(this Vector4 v) { return new Vector4(v.W, v.Y, v.Y, v.Y); }
        public static Vector4 WYYZ(this Vector4 v) { return new Vector4(v.W, v.Y, v.Y, v.Z); }
        public static Vector4 WYYW(this Vector4 v) { return new Vector4(v.W, v.Y, v.Y, v.W); }
        public static Vector4 WYZX(this Vector4 v) { return new Vector4(v.W, v.Y, v.Z, v.X); }
        public static Vector4 WYZY(this Vector4 v) { return new Vector4(v.W, v.Y, v.Z, v.Y); }
        public static Vector4 WYZZ(this Vector4 v) { return new Vector4(v.W, v.Y, v.Z, v.Z); }
        public static Vector4 WYZW(this Vector4 v) { return new Vector4(v.W, v.Y, v.Z, v.W); }
        public static Vector4 WYWX(this Vector4 v) { return new Vector4(v.W, v.Y, v.W, v.X); }
        public static Vector4 WYWY(this Vector4 v) { return new Vector4(v.W, v.Y, v.W, v.Y); }
        public static Vector4 WYWZ(this Vector4 v) { return new Vector4(v.W, v.Y, v.W, v.Z); }
        public static Vector4 WYWW(this Vector4 v) { return new Vector4(v.W, v.Y, v.W, v.W); }
        public static Vector4 WZXX(this Vector4 v) { return new Vector4(v.W, v.Z, v.X, v.X); }
        public static Vector4 WZXY(this Vector4 v) { return new Vector4(v.W, v.Z, v.X, v.Y); }
        public static Vector4 WZXZ(this Vector4 v) { return new Vector4(v.W, v.Z, v.X, v.Z); }
        public static Vector4 WZXW(this Vector4 v) { return new Vector4(v.W, v.Z, v.X, v.W); }
        public static Vector4 WZYX(this Vector4 v) { return new Vector4(v.W, v.Z, v.Y, v.X); }
        public static Vector4 WZYY(this Vector4 v) { return new Vector4(v.W, v.Z, v.Y, v.Y); }
        public static Vector4 WZYZ(this Vector4 v) { return new Vector4(v.W, v.Z, v.Y, v.Z); }
        public static Vector4 WZYW(this Vector4 v) { return new Vector4(v.W, v.Z, v.Y, v.W); }
        public static Vector4 WZZX(this Vector4 v) { return new Vector4(v.W, v.Z, v.Z, v.X); }
        public static Vector4 WZZY(this Vector4 v) { return new Vector4(v.W, v.Z, v.Z, v.Y); }
        public static Vector4 WZZZ(this Vector4 v) { return new Vector4(v.W, v.Z, v.Z, v.Z); }
        public static Vector4 WZZW(this Vector4 v) { return new Vector4(v.W, v.Z, v.Z, v.W); }
        public static Vector4 WZWX(this Vector4 v) { return new Vector4(v.W, v.Z, v.W, v.X); }
        public static Vector4 WZWY(this Vector4 v) { return new Vector4(v.W, v.Z, v.W, v.Y); }
        public static Vector4 WZWZ(this Vector4 v) { return new Vector4(v.W, v.Z, v.W, v.Z); }
        public static Vector4 WZWW(this Vector4 v) { return new Vector4(v.W, v.Z, v.W, v.W); }
        public static Vector4 WWXX(this Vector4 v) { return new Vector4(v.W, v.W, v.X, v.X); }
        public static Vector4 WWXY(this Vector4 v) { return new Vector4(v.W, v.W, v.X, v.Y); }
        public static Vector4 WWXZ(this Vector4 v) { return new Vector4(v.W, v.W, v.X, v.Z); }
        public static Vector4 WWXW(this Vector4 v) { return new Vector4(v.W, v.W, v.X, v.W); }
        public static Vector4 WWYX(this Vector4 v) { return new Vector4(v.W, v.W, v.Y, v.X); }
        public static Vector4 WWYY(this Vector4 v) { return new Vector4(v.W, v.W, v.Y, v.Y); }
        public static Vector4 WWYZ(this Vector4 v) { return new Vector4(v.W, v.W, v.Y, v.Z); }
        public static Vector4 WWYW(this Vector4 v) { return new Vector4(v.W, v.W, v.Y, v.W); }
        public static Vector4 WWZX(this Vector4 v) { return new Vector4(v.W, v.W, v.Z, v.X); }
        public static Vector4 WWZY(this Vector4 v) { return new Vector4(v.W, v.W, v.Z, v.Y); }
        public static Vector4 WWZZ(this Vector4 v) { return new Vector4(v.W, v.W, v.Z, v.Z); }
        public static Vector4 WWZW(this Vector4 v) { return new Vector4(v.W, v.W, v.Z, v.W); }
        public static Vector4 WWWX(this Vector4 v) { return new Vector4(v.W, v.W, v.W, v.X); }
        public static Vector4 WWWY(this Vector4 v) { return new Vector4(v.W, v.W, v.W, v.Y); }
        public static Vector4 WWWZ(this Vector4 v) { return new Vector4(v.W, v.W, v.W, v.Z); }
        public static Vector4 WWWW(this Vector4 v) { return new Vector4(v.W, v.W, v.W, v.W); }


    }
}
