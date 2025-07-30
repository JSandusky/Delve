using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DelveLib.Map
{
    public struct Size
    {
        public int Width;
        public int Height;

        public Size(int w, int h) { Width = w;  Height = h; }
    }

    public struct RectangleF
    {
        public float Left;
        public float Right;
        public float Top;
        public float Bottom;

        public bool Intersects(RectangleF r)
        {
            return false;
        }
    }

    public class BlockEntry
    {
        #region ================== Variables

        // Members
        private List<LineDef> lines;
        private List<Thing> things;
        private List<Sector> sectors;
        private List<Vertex> verts; //mxd

        #endregion

        #region ================== Properties

        public List<LineDef> Lines { get { return lines; } }
        public List<Thing> Things { get { return things; } }
        public List<Sector> Sectors { get { return sectors; } }
        public List<Vertex> Vertices { get { return verts; } } //mxd
        public List<VerletParticle> Particles { get; private set; } = new List<VerletParticle>();

        #endregion

        #region ================== Constructor

        // Constructor for empty block
        public BlockEntry()
        {
            lines = new List<LineDef>(2);
            things = new List<Thing>(2);
            sectors = new List<Sector>(2);
            verts = new List<Vertex>(2); //mxd
        }

        #endregion
    }

    public class BlockMap<BE> where BE : BlockEntry, new()
    {
        #region ================== Constants

        #endregion

        #region ================== Variables

        // Blocks
        protected BE[,] blockmap;
        protected int blocksizeshift;
        protected int blocksize;
        protected Size size;
        protected Rectangle range;
        protected Vector2 rangelefttop;

        // State
        private bool isdisposed;

        #endregion

        #region ================== Properties

        public bool IsDisposed { get { return isdisposed; } }
        public Size Size { get { return size; } }
        public Rectangle Range { get { return range; } }
        public int BlockSize { get { return blocksize; } }
        internal BE[,] Map { get { return blockmap; } }

        #endregion

        #region ================== Constructor / Disposer

        // Constructor
        public BlockMap(Rectangle range)
        {
            Initialize(range, 128);
        }

        // Constructor
        public BlockMap(Rectangle range, int blocksize)
        {
            Initialize(range, blocksize);
        }

        // This initializes the blockmap
        private void Initialize(Rectangle range, int blocksize)
        {
            // Initialize
            this.range = range;
            this.blocksizeshift = BitsForInt(blocksize);
            this.blocksize = 1 << blocksizeshift;
            if ((this.blocksize != blocksize) || (this.blocksize <= 1)) throw new ArgumentException("Block size must be a power of 2 greater than 1");
            rangelefttop = new Vector2(range.Left, range.Top);
            Point lefttop = new Point((int)range.Left >> blocksizeshift, (int)range.Top >> blocksizeshift);
            Point rightbottom = new Point((int)range.Right >> blocksizeshift, (int)range.Bottom >> blocksizeshift);
            size = new Size((rightbottom.X - lefttop.X) + 1, (rightbottom.Y - lefttop.Y) + 1);
            blockmap = new BE[size.Width, size.Height];
            Clear();
        }

        // Disposer
        public void Dispose()
        {
            // Not already disposed?
            if (!isdisposed)
            {
                // Clean up
                blockmap = null;

                // Done
                isdisposed = true;
            }
        }

        #endregion

        #region ================== Methods

        // This returns the block coordinates
        internal Point GetBlockCoordinates(Vector2 v)
        {
            return new Point((int)(v.X - range.Left) >> blocksizeshift,
                             (int)(v.Y - range.Top) >> blocksizeshift);
        }

        // This returns the block center in world coordinates
        protected Vector2 GetBlockCenter(Point p)
        {
            return new Vector2(((p.X << blocksizeshift) + (blocksize >> 1)) + range.Left,
                                ((p.Y << blocksizeshift) + (blocksize >> 1)) + range.Top);
        }

        // This returns true when the given block is inside range
        internal bool IsInRange(Point p)
        {
            return (p.X >= 0) && (p.X < size.Width) && (p.Y >= 0) && (p.Y < size.Height);
        }

        // This returns true when the given block is inside range
        public bool IsInRange(Vector2 p)
        {
            return (p.X >= range.Left) && (p.X < range.Right) && (p.Y >= range.Top) && (p.Y < range.Bottom);
        }

        // This crops a point into the range
        protected Point CropToRange(Point p)
        {
            return new Point(Math.Min(Math.Max(p.X, 0), (int)(size.Width - 1)),
                             Math.Min(Math.Max(p.Y, 0), (int)(size.Height - 1)));
        }

        // This crops a point into the range
        protected int CropToRangeX(int x)
        {
            return Math.Min(Math.Max(x, 0), (int)(size.Width - 1));
        }

        // This crops a point into the range
        protected int CropToRangeY(int y)
        {
            return Math.Min(Math.Max(y, 0), (int)(size.Height - 1));
        }

        // This clears the blockmap
        public virtual void Clear()
        {
            for (int x = 0; x < size.Width; x++)
            {
                for (int y = 0; y < size.Height; y++)
                {
                    blockmap[x, y] = new BE();
                }
            }
        }

        // This returns a blocks at the given coordinates, if any
        // Returns null when out of range
        public virtual BE GetBlockAt(Vector2 pos)
        {
            // Calculate block coordinates
            Point p = GetBlockCoordinates(pos);
            return IsInRange(p) ? blockmap[p.X, p.Y] : null;
        }

        // This returns a range of blocks in a square
        public virtual List<BE> GetSquareRange(RectangleF rect)
        {
            // Calculate block coordinates
            Point lt = GetBlockCoordinates(new Vector2(rect.Left, rect.Top));
            Point rb = GetBlockCoordinates(new Vector2(rect.Right, rect.Bottom));

            // Crop coordinates to range
            lt = CropToRange(lt);
            rb = CropToRange(rb);

            // Go through the range to make a list
            int entriescount = ((rb.X - lt.X) + 1) * ((rb.Y - lt.Y) + 1);
            List<BE> entries = new List<BE>(entriescount);
            for (int x = lt.X; x <= rb.X; x++)
            {
                for (int y = lt.Y; y <= rb.Y; y++)
                {
                    entries.Add(blockmap[x, y]);
                }
            }

            // Return list
            return entries;
        }

        // This returns all blocks along the given line
        public virtual List<BE> GetLineBlocks(Vector2 v1, Vector2 v2)
        {
            int dirx, diry;

            // Estimate number of blocks we will go through and create list
            int entriescount = (int)(XNAExt.ManhattanDistance(v1, v2) * 2.0f) / blocksize;
            List<BE> entries = new List<BE>(entriescount);

            // Find start and end block
            Point pos = GetBlockCoordinates(v1);
            Point end = GetBlockCoordinates(v2);
            v1 -= rangelefttop;
            v2 -= rangelefttop;

            // Horizontal straight line?
            if (pos.Y == end.Y)
            {
                // Simple loop
                pos.X = CropToRangeX(pos.X);
                end.X = CropToRangeX(end.X);
                if (IsInRange(new Point(pos.X, pos.Y)))
                {
                    dirx = Math.Sign(v2.X - v1.Y);
                    if (dirx != 0)
                    {
                        for (int x = pos.X; x != end.X; x += dirx)
                        {
                            entries.Add(blockmap[x, pos.Y]);
                        }
                    }
                    entries.Add(blockmap[end.X, end.Y]);
                }
            }
            // Vertical straight line?
            else if (pos.X == end.X)
            {
                // Simple loop
                pos.Y = CropToRangeY(pos.Y);
                end.Y = CropToRangeY(end.Y);
                if (IsInRange(new Point(pos.X, pos.Y)))
                {
                    diry = Math.Sign(v2.Y - v1.Y);
                    if (diry != 0)
                    {
                        for (int y = pos.Y; y != end.Y; y += diry)
                        {
                            entries.Add(blockmap[pos.X, y]);
                        }
                    }
                    entries.Add(blockmap[end.X, end.Y]);
                }
            }
            else
            {
                // Add this block
                if (IsInRange(pos)) entries.Add(blockmap[pos.X, pos.Y]);

                // Moving outside the block?
                if (pos != end)
                {
                    // Calculate current block edges
                    float cl = pos.X * blocksize;
                    float cr = (pos.X + 1) * blocksize;
                    float ct = pos.Y * blocksize;
                    float cb = (pos.Y + 1) * blocksize;

                    // Line directions
                    dirx = Math.Sign(v2.X - v1.X);
                    diry = Math.Sign(v2.Y - v1.Y);

                    // Calculate offset and delta movement over x
                    float posx, deltax;
                    if (dirx >= 0)
                    {
                        posx = (cr - v1.X) / (v2.X - v1.X);
                        deltax = blocksize / (v2.X - v1.X);
                    }
                    else
                    {
                        // Calculate offset and delta movement over x
                        posx = (v1.X - cl) / (v1.X - v2.X);
                        deltax = blocksize / (v1.X - v2.X);
                    }

                    // Calculate offset and delta movement over y
                    float posy, deltay;
                    if (diry >= 0)
                    {
                        posy = (cb - v1.Y) / (v2.Y - v1.Y);
                        deltay = blocksize / (v2.Y - v1.Y);
                    }
                    else
                    {
                        posy = (v1.Y - ct) / (v1.Y - v2.Y);
                        deltay = blocksize / (v1.Y - v2.Y);
                    }

                    // Continue while not reached the end
                    while (pos != end)
                    {
                        // Check in which direction to move
                        if (posx < posy)
                        {
                            // Move horizontally
                            posx += deltax;
                            if (pos.X != end.X) pos.X += dirx;
                        }
                        else
                        {
                            // Move vertically
                            posy += deltay;
                            if (pos.Y != end.Y) pos.Y += diry;
                        }

                        // Add lines to this block
                        if (IsInRange(pos)) entries.Add(blockmap[pos.X, pos.Y]);
                    }
                }
            }

            // Return list
            return entries;
        }

        // This puts things in the blockmap
        public virtual void AddThingsSet(ICollection<Thing> things)
        {
            foreach (Thing t in things) AddThing(t);
        }

        // This puts a thing in the blockmap
        public virtual void AddThing(Thing t)
        {
            Point p = GetBlockCoordinates(t.Position.XY());
            if (IsInRange(p)) blockmap[p.X, p.Y].Things.Add(t);
        }

        public virtual void AddParticle(VerletParticle part)
        { 
            Point p = GetBlockCoordinates(new Vector2(part.position_.X, part.position_.Y));
            if (IsInRange(p)) 
                blockmap[p.X, p.Y].Particles.Add(part);
            else
                blockmap[0,0].Particles.Add(part);
        }

        //mxd. This puts vertices in the blockmap
        public virtual void AddVerticesSet(ICollection<Vertex> verts)
        {
            foreach (Vertex v in verts) AddVertex(v);
        }

        //mxd. This puts a vertex in the blockmap
        public virtual void AddVertex(Vertex v)
        {
            Point p = GetBlockCoordinates(v.Position.XY());
            if (IsInRange(p)) blockmap[p.X, p.Y].Vertices.Add(v);
        }

        // This puts sectors in the blockmap
        public virtual void AddSectorsSet(ICollection<Sector> sectors)
        {
            foreach (Sector s in sectors) AddSector(s);
        }

        // This puts a sector in the blockmap
        public virtual void AddSector(Sector s)
        {
            if (s.Polygon == null)
                s.CalculatePolygon();
            //mxd. Check range. Sector can be bigger than blockmap range
            if (!range.Intersects(s.bounds))
                return;
            //
            Point p1 = GetBlockCoordinates(new Vector2(s.bounds.Left, s.bounds.Top));
            Point p2 = GetBlockCoordinates(new Vector2(s.bounds.Right, s.bounds.Bottom));
            p1 = CropToRange(p1);
            p2 = CropToRange(p2);
            for (int x = p1.X; x <= p2.X; x++)
            {
                for (int y = p1.Y; y <= p2.Y; y++)
                    blockmap[x, y].Sectors.Add(s);
            }
        }

        // This puts a whole set of linedefs in the blocks they cross
        public virtual void AddLinedefsSet(ICollection<LineDef> lines)
        {
            foreach (LineDef l in lines) AddLinedef(l);
        }

        // This puts a single linedef in all blocks it crosses
        public virtual void AddLinedef(LineDef line)
        {
            int dirx, diry;

            // Get coordinates
            Vector2 v1 = line.Start.Position;
            Vector2 v2 = line.End.Position;

            // Find start and end block
            Point pos = GetBlockCoordinates(v1);
            Point end = GetBlockCoordinates(v2);
            v1 -= rangelefttop;
            v2 -= rangelefttop;

            // Horizontal straight line?
            if (pos.Y == end.Y)
            {
                // Simple loop
                pos.X = CropToRangeX(pos.X);
                end.X = CropToRangeX(end.X);
                if (IsInRange(new Point(pos.X, pos.Y)))
                {
                    dirx = Math.Sign(v2.X - v1.X);
                    if (dirx != 0)
                    {
                        for (int x = pos.X; x != end.X; x += dirx)
                        {
                            blockmap[x, pos.Y].Lines.Add(line);
                        }
                    }
                    blockmap[end.X, end.Y].Lines.Add(line);
                }
            }
            // Vertical straight line?
            else if (pos.X == end.X)
            {
                // Simple loop
                pos.Y = CropToRangeY(pos.Y);
                end.Y = CropToRangeY(end.Y);
                if (IsInRange(new Point(pos.X, pos.Y)))
                {
                    diry = Math.Sign(v2.Y - v1.Y);
                    if (diry != 0)
                    {
                        for (int y = pos.Y; y != end.Y; y += diry)
                        {
                            blockmap[pos.X, y].Lines.Add(line);
                        }
                    }
                    blockmap[end.X, end.Y].Lines.Add(line);
                }
            }
            else
            {
                // Add lines to this block
                if (IsInRange(pos)) blockmap[pos.X, pos.Y].Lines.Add(line);

                // Moving outside the block?
                if (pos != end)
                {
                    // Calculate current block edges
                    float cl = pos.X * blocksize;
                    float cr = (pos.X + 1) * blocksize;
                    float ct = pos.Y * blocksize;
                    float cb = (pos.Y + 1) * blocksize;

                    // Line directions
                    dirx = Math.Sign(v2.X - v1.X);
                    diry = Math.Sign(v2.Y - v1.Y);

                    // Calculate offset and delta movement over x
                    float posx, deltax;
                    if (dirx == 0)
                    {
                        posx = float.MaxValue;
                        deltax = float.MaxValue;
                    }
                    else if (dirx > 0)
                    {
                        posx = (cr - v1.X) / (v2.X - v1.X);
                        deltax = blocksize / (v2.X - v1.X);
                    }
                    else
                    {
                        // Calculate offset and delta movement over x
                        posx = (v1.X - cl) / (v1.X - v2.X);
                        deltax = blocksize / (v1.X - v2.X);
                    }

                    // Calculate offset and delta movement over y
                    float posy, deltay;
                    if (diry == 0)
                    {
                        posy = float.MaxValue;
                        deltay = float.MaxValue;
                    }
                    else if (diry > 0)
                    {
                        posy = (cb - v1.Y) / (v2.Y - v1.Y);
                        deltay = blocksize / (v2.Y - v1.Y);
                    }
                    else
                    {
                        posy = (v1.Y - ct) / (v1.Y - v2.Y);
                        deltay = blocksize / (v1.Y - v2.Y);
                    }

                    // Continue while not reached the end
                    while (pos != end)
                    {
                        // Check in which direction to move
                        if (posx < posy)
                        {
                            // Move horizontally
                            posx += deltax;
                            if (pos.X != end.X) pos.X += dirx;
                        }
                        else
                        {
                            // Move vertically
                            posy += deltay;
                            if (pos.Y != end.Y) pos.Y += diry;
                        }

                        // Add lines to this block
                        if (IsInRange(pos)) blockmap[pos.X, pos.Y].Lines.Add(line);
                    }
                }
            }
        }

#endregion

        internal static bool Int2Bool(int v)
        {
            return (v != 0);
        }

        // This calculates the bits needed for a number
        public static int BitsForInt(int v)
        {
            int[] LOGTABLE = new[] {
              0, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3,
              4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
              5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
              5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
              6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
              6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
              6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
              6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
              7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
              7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
              7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
              7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
              7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
              7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
              7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
              7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7 };

            int r;  // r will be lg(v)
            int t, tt;

            if (Int2Bool(tt = v >> 16))
                r = Int2Bool(t = tt >> 8) ? 24 + LOGTABLE[t] : 16 + LOGTABLE[tt];
            else
                r = Int2Bool(t = v >> 8) ? 8 + LOGTABLE[t] : LOGTABLE[v];

            return r;
        }
    }
}
