using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DelveLib
{
    public interface SceneTreeItem
    {
        SceneTreePartitioner PartitionNode { get; set; }
    }

    public interface SceneTreePartitioner
    {
        SceneTreePartitioner Insert(SceneTreeItem item, BoundingBox bounds);
        void Remove(SceneTreeItem item);
        SceneTreePartitioner Top { get; }
    }


    /// <summary>
    /// Ephemeral QuadTree. 
    /// [creation]<-
    ///     QuadTree root = new QuadTree(new Rectangle(-1000, -1000, 2000, 2000));
    ///     root.Init(32); //32 deep
    /// >[/creation]
    /// Quasi-dynamic, items should remove themselves then reinsert themselves if they've moved
    /// [code]<-
    ///     myItem.PartitionNode.Remove(myItem);
    ///     myItem.PartitionNode.Top.Insert(myItem, myItemBoundingBox);
    /// >[/code]
    /// </summary>
    public class QuadTree : SceneTreePartitioner
    {
        static int TopLeft = 0;
        static int TopRight = 0;
        static int BottomLeft = 0;
        static int BottomRight = 0;

        QuadTree parent_;
        QuadTree[] children_;
        BoundingBox bounds_;
        int depth_;
        Rectangle rect_;
        List<SceneTreeItem> contents_ = new List<SceneTreeItem>();

        public SceneTreePartitioner Top
        {
            get
            {
                QuadTree current = this;
                while (current.parent_ != null)
                    current = current.parent_;
                return current;
            }
        }

        public QuadTree(Rectangle rect)
        {
            rect_ = rect;
            depth_ = 0;
            CalculateBounds();
        }

        protected QuadTree(QuadTree parent, Rectangle rect, int depth)
        {
            depth_ = depth;
            rect_ = rect;
            parent_ = parent;
            CalculateBounds();
        }

        public void Init(int maxDepth)
        {
            Init(0, maxDepth);
        }

        void Init(int depth, int maxDepth)
        { 
            if (depth < maxDepth)
            {
                Divide();
                for (int i = 0; i < 4; ++i)
                    children_[i].Init(depth + 1, maxDepth);
            }
        }

        void CalculateBounds()
        {
            bounds_ = new BoundingBox(new Vector3(rect_.Left, -10000, rect_.Top), new Vector3(rect_.Right, 10000, rect_.Bottom));
        }

        Rectangle GetTopLeft() { return new Rectangle(rect_.X, rect_.Y, rect_.Width / 2, rect_.Height / 2); }
        Rectangle GetTopRight() { return new Rectangle(rect_.X + rect_.Width/2, rect_.Y, rect_.Width / 2, rect_.Height / 2); }
        Rectangle GetBottomLeft() { return new Rectangle(rect_.X, rect_.Y + rect_.Height/2, rect_.Width / 2, rect_.Height / 2); }
        Rectangle GetBottomRight() { return new Rectangle(rect_.X + rect_.Width / 2, rect_.Y + rect_.Height/2, rect_.Width / 2, rect_.Height / 2); }

        public void Divide()
        {
            children_ = new QuadTree[]
            {
                new QuadTree(this, GetTopLeft(), depth_ + 1),
                new QuadTree(this, GetTopRight(), depth_ + 1),
                new QuadTree(this, GetBottomLeft(), depth_ + 1),
                new QuadTree(this, GetBottomRight(), depth_ + 1)
            };
        }

        public SceneTreePartitioner Insert(SceneTreeItem item, BoundingBox bound)
        {
            if (children_ != null)
            {
                for (int i = 0; i < 4; ++i)
                { 
                    if (children_[i].InsertionContains(bound))
                    {
                        var ret = children_[i].Insert(item, bound);
                        if (ret != null)
                            return ret;
                    }
                }
            }
            contents_.Add(item);
            item.PartitionNode = this;
            return this;
        }

        public void Remove(SceneTreeItem item)
        {
            contents_.Remove(item);
        }

        public void Collect(List<SceneTreeItem> items, BoundingBox bounds)
        {
            if (SelectionContains(bounds))
            {
                items.AddRange(contents_);
                if (children_ != null)
                {
                    for (int i = 0; i < 4; ++i)
                        children_[i].Collect(items, bounds);
                }
            }
        }

        public void Collect(List<SceneTreeItem> items, BoundingSphere sphere)
        {
            if (SelectionContains(sphere))
            {
                items.AddRange(contents_);
                if (children_ != null)
                {
                    for (int i = 0; i < 4; ++i)
                        children_[i].Collect(items, sphere);
                }
            }
        }

        public void Collect(List<SceneTreeItem> items, BoundingFrustum frustum)
        {
            if (Contains(frustum))
            {
                items.AddRange(contents_);
                if (children_ != null)
                {
                    for (int i = 0; i < 4; ++i)
                        children_[i].Collect(items, frustum);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InsertionContains(BoundingBox bounds)
        {
            Vector2 min = bounds.Min.XZ();
            Vector2 max = bounds.Max.XZ();
            return (min.X > rect_.X && min.Y > rect_.Y && max.X < rect_.Right && max.Y < rect_.Bottom);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool SelectionContains(BoundingBox bounds)
        {
            return bounds.Contains(bounds_) != ContainmentType.Disjoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SelectionContains(BoundingSphere sphere)
        {
            return sphere.Contains(bounds_) != ContainmentType.Disjoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(BoundingSphere sphere)
        {
            Vector2 sphereCenter = sphere.Center.XZ();
            float r2 = sphere.Radius * sphere.Radius;
            Vector2 tl = sphereCenter - new Vector2(rect_.Left, rect_.Top);
            Vector2 tr = sphereCenter - new Vector2(rect_.Right, rect_.Top);
            Vector2 bl = sphereCenter - new Vector2(rect_.Left, rect_.Bottom);
            Vector2 br = sphereCenter - new Vector2(rect_.Right, rect_.Bottom);
            return tl.LengthSquared() < r2 || tr.LengthSquared() < r2 ||
                bl.LengthSquared() < r2 || br.LengthSquared() < r2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(BoundingFrustum frustum)
        {
            return frustum.Contains(bounds_) != ContainmentType.Disjoint;
        }
    }
}
