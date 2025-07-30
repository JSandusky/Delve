using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using g3;

namespace DelveLib
{
    public static class G3Ext
    {
        public static Vector2 ToMonoGame(this Vector2d vec) { return new Vector2((float)vec.x, (float)vec.y); }
        public static Vector2[] ToMonoGame(this Vector2d[] vec) {
            Vector2[] ret = new Vector2[vec.Length];
            for (int i = 0; i < ret.Length; ++i)
                ret[i] = vec[i].ToMonoGame();
            return ret;
        }
        public static Vector2d ToG3(this Vector2 vec) { return new Vector2d(vec.X, vec.Y); }
        public static Vector2d[] ToG3(this Vector2[] vec)
        {
            Vector2d[] ret = new Vector2d[vec.Length];
            for (int i = 0; i < ret.Length; ++i)
                ret[i] = vec[i].ToG3();
            return ret;
        }


        public static Vector3 ToMonoGame(this Vector3d vec) { return new Vector3((float)vec.x, (float)vec.y, (float)vec.z); }
        public static Vector3[] ToMonoGame(this Vector3d[] vec)
        {
            Vector3[] ret = new Vector3[vec.Length];
            for (int i = 0; i < ret.Length; ++i)
                ret[i] = vec[i].ToMonoGame();
            return ret;
        }
        public static Vector3d ToG3(this Vector3 vec) { return new Vector3d(vec.X, vec.Y, vec.Z); }
        public static Vector3d[] ToG3(this Vector3[] vec)
        {
            Vector3d[] ret = new Vector3d[vec.Length];
            for (int i = 0; i < ret.Length; ++i)
                ret[i] = vec[i].ToG3();
            return ret;
        }
    }
}
