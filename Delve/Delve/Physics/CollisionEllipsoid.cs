using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

namespace Delve.Physics
{
    public class CollisionEllipsoid
    {
        Vector3 eSize;
        private float epsilon = 0.00001f;
        private float collisionBuffer = 0.00002f;

        private int collisionDepth = 0;
        private int maxCollisionDepth = 5;

        /*
         * @eSize Ellipsoid size
         */
        public CollisionEllipsoid(Vector3 eSize)
        {
            this.eSize = eSize;
        }

        //At what velocity do we not care to check for a collision, to prevent vibrations.
        public float Epsilon { get { return epsilon; } set { epsilon = value; } }

        //How close do we allow the ellipsoid to move towards the triangle. We don't want to let the player move right
        //on the triangle because then they would end up intersected.
        public float CollisionBuffer { get { return collisionBuffer; } set { collisionBuffer = value; } }

        public Vector3 Size {  get { return eSize; } set { eSize = value; } }

        public int MaxCollisionDepth { set { maxCollisionDepth = value; } get { return maxCollisionDepth; } }

        /*
         * @position The current ellipsoid position
         * @velocity Dir+speed they want to move
         * @triangles Up to you to supply which triangles to check for a collision. Could be the entire scene
         * @return The new position after handling collisions.
         * or use OcTree, etc.
         */
        public Vector3 CollideWithWorld(Quaternion orientation, Vector3 position, Vector3 velocity, List<CollisionTriangle> triangles)
        {
            Quaternion invOrient = Quaternion.Inverse(orientation);
            Vector3 transformedESize = Vector3.Transform(eSize, orientation);

            // record old eSize for restoring
            Vector3 oldESize = eSize;
            eSize = transformedESize;

            //transform to e space
            Vector3 ePosition = position / eSize;
            Vector3 eVelocity = velocity / eSize;

            collisionDepth = 0;
            ePosition = CollideWithWorldESpace(ePosition, eVelocity, triangles);

            // restore old e space
            eSize = oldESize;
            //transform out of espace
            return ePosition * Vector3.Transform(transformedESize, invOrient);
        }

        private Vector3 CollideWithWorldESpace(Vector3 position, Vector3 velocity, List<CollisionTriangle> triangles, int pass = 0)
        {
            if (pass > 8)
                return position;

            if (velocity.LengthSquared() < epsilon)
                return position;

            Vector3 destPos = position + velocity;
            if (triangles.Count == 0)
                return destPos;

            if (collisionDepth >= 5)
                return position;

            collisionDepth++;
            bool foundCollision = false;
            float nearestDistance = -1f;
            Vector3 nearestSphereIntersectPoint = Vector3.Zero;
            Vector3 nearestPolygonIntersectPoint = Vector3.Zero;

            // reuse the same triangle over and over
            CollisionTriangle newT = new CollisionTriangle();
            for (int i = 0; i < triangles.Count; ++i)
            {
                CollisionTriangle t = triangles[i];
                newT.a = t.a / eSize;
                newT.b = t.b / eSize;
                newT.c = t.c / eSize;
                newT.Calculate();

                Plane plane = new Plane(newT.a, newT.b, newT.c);
                Vector3? sphereIntersectPoint = position - newT.normal;

                Vector3 planeIntersectPoint = new Vector3();

                float planeDist = plane.DotCoordinate(sphereIntersectPoint.Value);
                if (planeDist < 0)
                {
                    Ray ray = new Ray(sphereIntersectPoint.Value, newT.normal);
                    float? rayDist = ray.Intersects(plane);
                    if (rayDist.HasValue)
                        planeIntersectPoint = ray.Position + ray.Direction * rayDist.Value;
                }
                else
                {
                    Ray ray = new Ray(sphereIntersectPoint.Value, Vector3.Normalize(velocity));
                    float? rayDist = ray.Intersects(plane);
                    if (rayDist.HasValue)
                        planeIntersectPoint = ray.Position + ray.Direction * rayDist.Value;
                }

                //assume its plane point
                Vector3 polygonIntersectPoint = planeIntersectPoint;
                if (!IsPointInTriangle(polygonIntersectPoint, newT))
                {
                    polygonIntersectPoint = ClosestPointOnTriangle(ref newT, ref polygonIntersectPoint);

                    //maybe it doesn't even hit the sphere?
                    float distToSphere = IntersectRaySphere(polygonIntersectPoint, Vector3.Normalize(-velocity), position, 1.0f);
                    if (distToSphere > 0)
                    {
                        var negVel = -Vector3.Normalize(velocity);
                        sphereIntersectPoint = new Vector3(polygonIntersectPoint.X + distToSphere * negVel.X,
                                polygonIntersectPoint.Y + distToSphere * negVel.Y,
                                polygonIntersectPoint.Z + distToSphere * negVel.Z);
                    }
                    else
                    {
                        //no collision
                        sphereIntersectPoint = null;
                        continue;
                    }
                }

                if (sphereIntersectPoint != null)
                {
                    float distToSphere = Vector3.Distance(sphereIntersectPoint.Value, polygonIntersectPoint);
                    if ((distToSphere > 0) && (distToSphere <= velocity.Length()))
                    {
                        if (foundCollision == false || distToSphere < nearestDistance)
                        {
                            nearestDistance = distToSphere;
                            nearestSphereIntersectPoint = sphereIntersectPoint.Value;
                            nearestPolygonIntersectPoint = polygonIntersectPoint;
                            foundCollision = true;
                        }
                    }
                }
            }

            if (foundCollision)
            {
                Vector3 moveVel = Vector3.Zero;
                if (nearestDistance >= collisionBuffer)
                {
                    moveVel = velocity;
                    moveVel = Vector3.Normalize(moveVel) * (nearestDistance - collisionBuffer);
                }
                else
                {
                    moveVel = -velocity;
                    moveVel = Vector3.Normalize(moveVel) * Math.Abs(nearestDistance - collisionBuffer);
                }

                Vector3 newPosition = position + moveVel;
                Vector3 slidePlaneNormal = Vector3.Normalize(newPosition - nearestPolygonIntersectPoint);
                Vector3 slideVector = velocity - (slidePlaneNormal * Vector3.Dot(slidePlaneNormal, velocity));

                //slideVector.addLocal(moveVel);

                //setLength(slideVector, velocity.length());

                Vector3 newVelocity = slideVector;

                return CollideWithWorldESpace(newPosition, newVelocity, triangles, pass + 1);
            }

            return position + velocity;
        }

        private Vector3 ClosestPointOnTriangle(ref CollisionTriangle t, ref Vector3 p)
        {
            Vector3 rab = ClosestPointOnLine(ref t.a, ref t.b, ref p);
            Vector3 rbc = ClosestPointOnLine(ref t.b, ref t.c, ref p);
            Vector3 rca = ClosestPointOnLine(ref t.c, ref t.a, ref p);

            float dAB = (p - rab).Length();
            float dBC = (p - rbc).Length();
            float dCA = (p - rca).Length();

            float min = dAB;
            Vector3 result = rab;
            if (dBC < min)
            {
                min = dBC;
                result = rbc;
            }
            if (dCA < min)
                result = rca;
            return result;
        }

        private Vector3 ClosestPointOnLine(ref Vector3 a, ref Vector3 b, ref Vector3 p)
        {
            // Determine t (the length of the vector from ‘a’ to ‘p’)
            Vector3 c = p - a;
            Vector3 V = b - a;

            float d = V.Length();
            V.Normalize();
            float t = Vector3.Dot(V, c);

            // Check to see if ‘t’ is beyond the extents of the line segment
            if (t < 0.0f)
                return a;
            if (t > d)
                return b;

            // Return the point between ‘a’ and ‘b’
            //set length of V to t. V is normalized so this is easy
            V.X = V.X * t;
            V.Y = V.Y * t;
            V.Z = V.Z * t;

            return a + V;
        }

        private float IntersectRaySphere(Vector3 rO, Vector3 rV, Vector3 sO, float sR)
        {
            Vector3 Q = sO - rO;

            float c = Q.Length();
            float v = Vector3.Dot(Q, rV);
            float d = sR * sR - (c * c - v * v);

            // If there was no intersection, return -1
            if (d < 0.0)
                return -1.0f;

            // Return the distance to the [first] intersecting point
            return (float)(v - Math.Sqrt(d));
        }

        private static void SetLength(ref Vector3 v, float l)
        {
            float len = (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            v.X *= l / len;
            v.Y *= l / len;
            v.Z *= l / len;
        }

        private bool IsPointInSphere(Vector3 point, Vector3 sO, float sR)
        {
            float d = (point - sO).Length();

            if (d <= sR)
                return true;
            return false;
        }

        private bool IsPointInTriangle(Vector3 point, CollisionTriangle triangle)
        {
            Vector3 e10 = triangle.b - triangle.a;
            Vector3 e20 = triangle.c - triangle.a;

            float a = Vector3.Dot(e10, e10);
            float b = Vector3.Dot(e10, e20);
            float c = Vector3.Dot(e20, e20);
            float ac_bb = (a * c) - (b * b);

            //VECTOR vp(point.x-pa.x, point.y-pa.y, point.z-pa.z);
            Vector3 pa = triangle.a;
            Vector3 vp = new Vector3(point.X - pa.X, point.Y - pa.Y, point.Z - pa.Z);

            float d = Vector3.Dot(vp, e10);
            float e = Vector3.Dot(vp, e20);
            float x = (d * c) - (e * b);
            float y = (e * a) - (d * b);
            float z = x + y - ac_bb;

            return z < 0 && x >= 0 && y >= 0;
        }

    }
}
