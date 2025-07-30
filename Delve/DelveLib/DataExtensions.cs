using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

namespace DelveLib
{
    public enum SymmetricAxis
    {
        None,
        XAxis,
        YAxis,
        ZAxis
    }

    public static class DataExtensions
    {
        public const float M_PI = 3.14159265358979323846264338327950288f;
        public const float M_HALF_PI = M_PI * 0.5f;

        public const float M_EPSILON = 0.000001f;
        public const float M_LARGE_EPSILON = 0.00005f;
        public const float M_MIN_NEARCLIP = 0.01f;
        public const float M_MAX_FOV = 160.0f;
        public const float M_LARGE_VALUE = 100000000.0f;
        public const float M_DEGTORAD = M_PI / 180.0f;
        public const float M_DEGTORAD_2 = M_PI / 360.0f;    // M_DEGTORAD / 2.f
        public const float M_RADTODEG = 1.0f / M_DEGTORAD;
        public const float M_RADTODEG_2 = 1.0f / M_DEGTORAD_2;

        /// <summary>
        /// Create from Pitch, Yaw, Roll Euler angles (XNA std is YPR)
        /// </summary>
        /// <param name="vec">Pitch Yaw Roll</param>
        /// <returns></returns>
        public static Quaternion QuaternionFromEuler(this Vector3 vec)
        {
            float XX = vec.X * M_DEGTORAD_2;
            float YY = vec.Y * M_DEGTORAD_2;
            float ZZ = vec.Z * M_DEGTORAD_2;
            float sinX = (float)Math.Sin(XX);
            float cosX = (float)Math.Cos(XX);
            float sinY = (float)Math.Sin(YY);
            float cosY = (float)Math.Cos(YY);
            float sinZ = (float)Math.Sin(ZZ);
            float cosZ = (float)Math.Cos(ZZ);
            
            return new Quaternion(
                cosY * sinX * cosZ + sinY * cosX * sinZ,
                sinY * cosX * cosZ - cosY * sinX * sinZ,
                cosY * cosX * sinZ - sinY * sinX * cosZ,
                cosY * cosX * cosZ + sinY * sinX * sinZ);
        }

        /// <summary>
        /// Create from Pitch, Yaw, Roll Euler angles (XNA std is YPR)
        /// </summary>
        /// <param name="vec">Pitch Yaw Roll</param>
        /// <returns></returns>
        public static Matrix MatrixFromEuler(this Vector3 vec)
        {
            float xx = (float)vec.X * M_DEGTORAD_2;
            float yy = (float)vec.Y * M_DEGTORAD_2;
            float zz = (float)vec.Z * M_DEGTORAD_2;
            return Matrix.CreateFromYawPitchRoll(yy, xx, zz);
        }

        /// <summary>
        /// Return Pitch Yaw Roll euler angles (XNA std is YPR)
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Vector3 ToEuler(this Matrix m)
        {
            return m.Rotation.ToEuler();
        }

        public static Vector3 MakeSymmetric(this Vector3 vec, SymmetricAxis sym)
        {
            switch (sym)
            {
                case SymmetricAxis.None:
                    return vec;
                case SymmetricAxis.XAxis:
                    return new Vector3(-vec.X, vec.Y, vec.Z);
                case SymmetricAxis.YAxis:
                    return new Vector3(vec.X, -vec.Y, vec.Z);
                case SymmetricAxis.ZAxis:
                    return new Vector3(vec.X, vec.Y, -vec.Z);
            }
            return vec;
        }

        /// <summary>
        /// Return Pitch Yaw Roll euler angles (XNA std is YPR)
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static Vector3 ToEuler(this Quaternion q)
        {
            // Derivation from http://www.geometrictools.com/Documentation/EulerAngles.pdf
            // Order of rotations: Z first, then X, then Y
            float check = 2.0f * (-q.Y * q.Z + q.W * q.X);

            if (check < -0.995f)
            {
                return new Vector3(
                    -90.0f,
                    0.0f,
                    (float)Math.Round(-Math.Atan2(2.0f * (q.X * q.Z - q.W * q.Y), 1.0f - 2.0f * (q.Y * q.Y + q.Z * q.Z)) * M_RADTODEG, 4)
                );
            }
            else if (check > 0.995f)
            {
                return new Vector3(
                    90.0f,
                    0.0f,
                    (float)Math.Round(Math.Atan2(2.0f * (q.X * q.Z - q.W * q.Y), 1.0f - 2.0f * (q.Y * q.Y + q.Z * q.Z)) * M_RADTODEG, 4)
                );
            }
            else
            {
                return new Vector3(
                    (float)Math.Round(Math.Asin(check) * M_RADTODEG, 4),
                    (float)Math.Round(Math.Atan2(2.0f * (q.X * q.Z + q.W * q.Y), 1.0f - 2.0f * (q.X * q.X + q.Y * q.Y)) * M_RADTODEG, 4),
                    (float)Math.Round(Math.Atan2(2.0f * (q.X * q.Y + q.W * q.Z), 1.0f - 2.0f * (q.X * q.X + q.Z * q.Z)) * M_RADTODEG, 4)
                );
            }
        }

        public static float Abs(this float value)
        {
            return Math.Abs(value);
        }

        public static bool IsNearlyEqual(this float value1, float value2, float difference = Epsilon)
        {
            return (value1 - value2).Abs() < difference;
        }

        public const float Epsilon = 0.0001f;

        public static int Round(this float value)
        {
            return (int)Math.Round(value);
        }

        public static float Round(this float value, int decimals)
        {
            return (float)Math.Round(value, decimals);
        }

        public static float Sin(float degrees)
        {
            return (float)Math.Sin(degrees * Pi / 180.0f);
        }

        public const float Pi = 3.14159265359f;

        public static float Cos(float degrees)
        {
            return (float)Math.Cos(degrees * Pi / 180.0f);
        }

        public static float Tan(float degrees)
        {
            return (float)Math.Tan(degrees * Pi / 180.0f);
        }

        public static float Asin(float value)
        {
            return (float)Math.Asin(value) * 180 / Pi;
        }

        public static float Acos(float value)
        {
            return (float)Math.Acos(value) * 180 / Pi;
        }

        public static float Atan2(float y, float x)
        {
            return (float)Math.Atan2(y, x) * 180 / Pi;
        }

        public static float Sqrt(float value)
        {
            return (float)Math.Sqrt(value);
        }

        public static int Clamp(this int value, int min, int max)
        {
            return value > max ? max : (value < min ? min : value);
        }

        public static float Clamp(this float value, float min, float max)
        {
            return value > max ? max : (value < min ? min : value);
        }

        public static float Lerp(this float value1, float value2, float percentage)
        {
            return value1 * (1 - percentage) + value2 * percentage;
        }

        public static float RadiansToDegrees(this float radians)
        {
            return (radians * 180.0f) / Pi;
        }

        public static float DegreesToRadians(this float degrees)
        {
            return (degrees * Pi) / 180.0f;
        }

        public static bool IsFinite(this float value)
        {
            return float.IsInfinity(value) == false && float.IsNaN(value) == false;
        }

        public static float Max(float value1, float value2)
        {
            return value1 > value2 ? value1 : value2;
        }

        public static int Max(int value1, int value2)
        {
            return value1 > value2 ? value1 : value2;
        }

        public static float Min(float value1, float value2)
        {
            return value1 < value2 ? value1 : value2;
        }

        public static int Min(int value1, int value2)
        {
            return value1 < value2 ? value1 : value2;
        }

        public static int GetNearestMultiple(this int value, int multipleValue)
        {
            int min = ((int)(value / (float)multipleValue)) * multipleValue;
            int max = ((int)(value / (float)multipleValue) + 1) * multipleValue;

            return max - value < value - min ? max : min;
        }

        public static float InvSqrt(this float value)
        {
            return 1.0f / Sqrt(value);
        }

        public static float WrapRotationToMinus180ToPlus180(float degrees)
        {
            degrees = (float)Math.IEEERemainder(degrees, 360);
            return degrees <= -180 ? degrees + 360 : (degrees > 180 ? degrees - 360 : degrees);
        }

        /// <summary>
        /// http://en.wikipedia.org/wiki/Line%E2%80%93line_intersection
        /// </summary>
        public static bool IsLineIntersectingWith(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End)
        {
            float denominator = (line2End.Y - line2Start.Y) * (line1End.X - line1Start.X) -
                                                    ((line2End.X - line2Start.X) * (line1End.Y - line1Start.Y));
            float ua = ((line2End.X - line2Start.X) * (line1Start.Y - line2Start.Y) -
                                    (line2End.Y - line2Start.Y) * (line1Start.X - line2Start.X)) / denominator;
            float ub = ((line1End.X - line1Start.X) * (line1Start.Y - line2Start.Y) -
                                    (line1End.Y - line1Start.Y) * (line1Start.X - line2Start.X)) / denominator;
            return ua >= 0f && ua <= 1f && ub >= 0f && ub <= 1f;
        }

        public static Vector2 ParseVector2(string str)
        {
            string[] parts = str.Split(' ');
            Vector2 ret = new Vector2();
            if (parts.Length != 2)
                return ret;

            ret.X = float.Parse(parts[0]);
            ret.Y = float.Parse(parts[1]);
            return ret;
        }

        public static Vector3 ParseVector3(string str)
        {
            string[] parts = str.Split(' ');
            Vector3 ret = new Vector3();
            if (parts.Length != 3)
                return ret;

            ret.X = float.Parse(parts[0]);
            ret.Y = float.Parse(parts[1]);
            ret.Z = float.Parse(parts[2]);
            return ret;
        }

        public static Vector4 ParseVector4(string str)
        {
            string[] parts = str.Split(' ');
            Vector4 ret = new Vector4();
            if (parts.Length != 4)
                return ret;

            ret.X = float.Parse(parts[0]);
            ret.Y = float.Parse(parts[1]);
            ret.Z = float.Parse(parts[2]);
            ret.W = float.Parse(parts[4]);
            return ret;
        }
    }
}
