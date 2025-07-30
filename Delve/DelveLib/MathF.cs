using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DelveLib
{
    public static class Mathf
    {
        public const float PI = 3.14159265358979323846264338327950288f;
        public const float HALF_PI = PI * 0.5f;

        public const float EPSILON = 0.000001f;
        public const float LARGE_EPSILON = 0.00005f;
        public const float MIN_NEARCLIP = 0.01f;
        public const float MAX_FOV = 160.0f;
        public const float LARGE_VALUE = 100000000.0f;
        public const float DEGTORAD = PI / 180.0f;
        public const float DEGTORAD_2 = PI / 360.0f;    // M_DEGTORAD / 2.f
        public const float RADTODEG = 1.0f / DEGTORAD;

        public static float Pow(float val, float exp)
        {
            return (float)Math.Pow(val, exp);
        }

        public static float Abs(float val)
        {
            return (float)Math.Abs(val);
        }

        public static float Log(float val)
        {
            return (float)Math.Log(val);
        }

        public static float Sqrt(float val)
        {
            return (float)Math.Sqrt(val);
        }

        public static float Sin(float val)
        {
            return (float)Math.Sin(val);
        }

        public static float Cos(float val)
        {
            return (float)Math.Cos(val);
        }

        public static float Tan(float val)
        {
            return (float)Math.Tan(val);
        }

        public static float Asin(float val)
        {
            return (float)Math.Asin(val);
        }

        public static float Acos(float val)
        {
            return (float)Math.Acos(val);
        }

        public static float Atan(float d)
        {
            return (float)Math.Atan(d);
        }

        public static float Atan2(float y, float x)
        {
            return (float)Math.Atan2(y, x);
        }

        public static float Exp(float v)
        {
            return (float)Math.Exp(v);
        }

        public static float Wrap(float val, float min, float max)
        {
            while (val > max)
                val -= max;
            while (val < min)
                val += max;
            return val;
        }

        public static int Wrap(int val, int min, int max)
        {
            while (val > max)
                val -= max;
            while (val < min)
                val += max;
            return val;
        }

        public static float Clamp01(float val)
        {
            return Math.Max(0.0f, Math.Min(val, 1.0f));
        }

        public static int Clamp(int val, int min, int max)
        {
            return Math.Max(min, Math.Min(val, max));
        }

        public static bool Between(float val, float min, float max)
        {
            return val >= min && val <= max;
        }

        public static float Clamp(float val, float min, float max)
        {
            return Math.Max(min, Math.Min(val, max));
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        public static float Hypot(float a, float b)
        {
            return Mathf.Sqrt(a * a + b * b);
        }

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            return new Vector3(
                Lerp(a.X, b.X, t),
                Lerp(a.Y, b.Y, t),
                Lerp(a.Z, b.Z, t));
        }

        public static float SmoothMin(float a, float b, float k = 0.4f)
        {
            float h = Clamp(0.5f + 0.5f * (b - a) / k, 0.0f, 1.0f);
            return Lerp(b, a, h) - k * h * (1.0f - h);
        }

        public static float Normalize(float val, float min, float max)
        {
            return (val - min) / (max - min);
        }

        public static double Normalize(double val, double min, double max)
        {
            return (val - min) / (max - min);
        }

        public static float Denormalize(float val, float min, float max)
        {
            return val * (max - min) + min;
        }

        public static float ConvertSpace(float val, float srcMin, float srcMax, float destMin, float destMax)
        {
            return ((val - srcMin) / (srcMax - srcMin)) * (destMax - destMin) + destMin;
        }

        public static float ConvertSpaceZeroRelative(float val, float srcMax, float destMax)
        {
            return (val / srcMax) * destMax;
        }
    }
}
