using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DelveLib.Proc
{
    public static class BoundsExt
    {
        public static BoundingBox Merge(this BoundingBox lhs, BoundingBox rhs)
        {
            return new BoundingBox
            {
                Min = Vector3.Min(lhs.Min, rhs.Min),
                Max = Vector3.Min(lhs.Max, rhs.Max)
            };
        }
    }

    class OcclusionGroup
    {
        public BoundingBox bb;
        public List<BoundingBox> volumes = new List<BoundingBox>();

        public OcclusionGroup()
        {
            bb = new BoundingBox();
            volumes.Capacity = 16;
        }

        void AddVolume(BoundingBox bounds)
        {
            if (volumes.Count == 0)
                bb = bounds;
            else
                bb = bb.Merge(bounds);
            volumes.Add(bounds);
        }

        void Clear()
        {
            bb = new BoundingBox();
            volumes.Clear();
        }

        void SortY()
        {
            volumes.Sort((l, r) =>
            {
                bool lLess = l.Min.Y < r.Min.Y;
                bool rLess = l.Min.Y > r.Min.Y;
                if (lLess)
                    return -1;
                if (rLess)
                    return 1;
                return 0;
            });
        }
        void SortZ()
        {
            volumes.Sort((l, r) =>
            {
                bool lLess = l.Min.Z < r.Min.Z;
                bool rLess = l.Min.Z > r.Min.Z;
                if (lLess)
                    return -1;
                if (rLess)
                    return 1;
                return 0;
            });
        }
    }

    public class OcclusionArea
    {
        public BoundingBox box;
        public string name;
        public VolumePartitioner parent;

        public OcclusionArea(BoundingBox bb, String name, VolumePartitioner parent)
        {
            this.parent = parent;
            this.name = name;
            this.box = bb;
        }

        public BoundingBox Intersection(BoundingBox other)
        {
            if (!other.Intersects(other))
                return new BoundingBox();

            Vector3 min;
            Vector3 max;

            min.X = other.Min.X > box.Min.X ? other.Min.X : box.Min.X;
            min.Y = other.Min.Y > box.Min.Y ? other.Min.Y : box.Min.Y;
            min.Z = other.Min.Z > box.Min.Z ? other.Min.Z : box.Min.Z;

            max.X = other.Max.X < box.Max.X ? other.Max.X : box.Max.X;
            max.Y = other.Max.Y < box.Max.Y ? other.Max.Y : box.Max.Y;
            max.Z = other.Max.Z < box.Max.Z ? other.Max.Z : box.Max.Z;

            return new BoundingBox(min, max);
        }
    }
}
