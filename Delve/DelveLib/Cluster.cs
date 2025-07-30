using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DelveLib
{
    public class Cluster<T>
    {
        List<T>[,] cells;
        int range;
        int cellSize;

        void Init(int dim)
        {
            cells = new List<T>[dim,dim];
            for (int x = 0; x < dim; ++x)
                for (int y = 0; y < dim; ++y)
                    cells[x,y] = new List<T>();
        }

        public Cluster(int dim, int cellSize)
        {
            this.cellSize = cellSize;
            range = dim / cellSize;
            Init(range);
        }

        public void Add(Vector2 pt, T obj)
        {
            int x = (int)(pt.X / cellSize);
            int y = (int)(pt.Y / cellSize);
            x = Math.Max(0, Math.Min(x, range - 1));
            y = Math.Max(0, Math.Min(y, range - 1));

            cells[x,y].Add(obj);
        }

        public List<T> GetBox(float left, float top, float right, float bottom)
        {
            List<T> ret = new List<T>();

            int minx = (int)(Math.Floor(left) / cellSize);
            int maxx = (int)(Math.Ceiling(right) / cellSize);
            minx = Math.Max(0, Math.Min(minx, range - 1));
            maxx = Math.Max(0, Math.Min(maxx, range - 1));

            int miny = (int)(Math.Floor(bottom) / cellSize);
            miny = Math.Max(0, Math.Min(miny, range - 1));
            int maxy = (int)(Math.Ceiling(top) / cellSize);
            maxy = Math.Max(0, Math.Min(maxy, range - 1));

            for (int x = minx; x <= maxx; ++x)
                for (int y = miny; y <= maxy; ++y)
                    ret.AddRange(cells[x,y]);

            return ret;
        }
    }
}
