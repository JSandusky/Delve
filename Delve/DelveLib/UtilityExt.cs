using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DelveLib
{
    public static class SystemDrawing
    {
        public static float Clamp01(float val)
        {
            return Math.Max(0.0f, Math.Min(val, 1.0f));
        }

        public static int Clamp(int val, int min, int max)
        {
            return Math.Max(min, Math.Min(val, max));
        }

        public static float Clamp(float val, float min, float max)
        {
            return Math.Max(min, Math.Min(val, max));
        }

        static System.Drawing.Color Mul(System.Drawing.Color lhs, float val)
        {
            return System.Drawing.Color.FromArgb(
                (byte)(lhs.A * val),
                (byte)(lhs.R * val),
                (byte)(lhs.G * val),
                (byte)(lhs.B * val));
        }

        static System.Drawing.Color Add(System.Drawing.Color lhs, System.Drawing.Color rhs)
        {
            return System.Drawing.Color.FromArgb(
                (byte)(lhs.A + rhs.A),
                (byte)(lhs.R + rhs.R),
                (byte)(lhs.G + rhs.G),
                (byte)(lhs.B + rhs.B));
        }

        public static System.Drawing.Color GetPixelBilinear(this System.Drawing.Bitmap bmp, float x, float y)
        {
            x = (float)(x - Math.Floor(x));
            y = (float)(y - Math.Floor(y));
            x = Clamp(x * bmp.Width - 0.5f, 0.0f, (float)(bmp.Width - 1));
            y = Clamp(y * bmp.Height - 0.5f, 0.0f, (float)(bmp.Height - 1));

            int xI = (int)x;
            int yI = (int)y;

            float xF = (float)(x - Math.Floor(x));
            float yF = (float)(y - Math.Floor(y));

            int xA = Mathf.Wrap(xI, 0, bmp.Width - 1);
            int yA = Mathf.Wrap(yI, 0, bmp.Height - 1);
            int xB = Mathf.Wrap(xI + 1, 0, bmp.Width - 1);
            int yB = Mathf.Wrap(yI, 0, bmp.Height - 1);
            int xC = Mathf.Wrap(xI, 0, bmp.Width - 1);
            int yC = Mathf.Wrap(yI + 1, 0, bmp.Height - 1);
            int xD = Mathf.Wrap(xI + 1, 0, bmp.Width - 1);
            int yD = Mathf.Wrap(yI + 1, 0, bmp.Height - 1);
            System.Drawing.Color topValue = Add(
                Mul(
                    bmp.GetPixel(xA, yA), (1.0f - xF)),
                Mul(
                    bmp.GetPixel(xB, yB), xF));

            System.Drawing.Color bottomValue = Add(
                Mul(
                    bmp.GetPixel(xC, yC), (1.0f - xF)),
                Mul(
                    bmp.GetPixel(xD, yD), xF));
            return Add(Mul(topValue, 1.0f - yF), Mul(bottomValue, yF));
        }
    }
}
