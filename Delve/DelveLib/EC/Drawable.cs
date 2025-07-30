using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DelveLib.EC
{ 
    public abstract class Drawable : Component, SceneTreeItem
    {
        public SceneTreePartitioner PartitionNode { get; set; }

        protected abstract BoundingBox GetBounds();

        public override void OwnerChanged(GameObject oldOwner)
        {
            base.OwnerChanged(oldOwner);
            if (oldOwner != null)
                oldOwner.Scene.UpdateQuadtree.Unsubscribe(this);
            if (PartitionNode != null)
                PartitionNode.Remove(this);
            PartitionNode = null;
            if (Owner != null)
                Owner.Scene.UpdateQuadtree.Subscribe(this, Scene_UpdateQuadtree, 0);
        }

        private void Scene_UpdateQuadtree(QuadTree e)
        {
            if (PartitionNode != null)
                PartitionNode.Remove(this);
            e.Insert(this, GetBounds());
        }
    }
}
