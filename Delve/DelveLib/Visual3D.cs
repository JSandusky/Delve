using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DelveLib
{
    public abstract class VisualAnimation3D
    {
        public virtual void Prepare(Visual3D owner) {  }
        public abstract bool Update(Visual3D target, float timeStep);
        public abstract void ForceFinished(Visual3D target);
    }

    public class Visual3D : SceneTreeItem
    {
        VisualAnimation3D animation_;

        public virtual BoundingBox GetBounds() { return new BoundingBox(); }

        public bool IsDirty { get; set; } = true;

        public VisualAnimation3D ActiveAnimation {
            get { return animation_; }
            set
            {
                if (animation_ != null)
                    animation_.ForceFinished(this);
                animation_ = value;
                if (animation_ != null)
                    animation_.Prepare(this);
            }
        }

        public SceneTreePartitioner PartitionNode { get; set; }

        public virtual void Destroy()
        {
            if (PartitionNode != null)
                PartitionNode.Remove(this);
        }

        public virtual void Update(float td)
        {
            if (IsDirty && PartitionNode != null)
            {
                PartitionNode.Remove(this);
                PartitionNode.Top.Insert(this, GetBounds());
            }
        }

        public void UpdateAnimations(float td)
        {
            if (animation_ != null)
            {
                if (animation_.Update(this, td))
                    animation_ = null;
            }
        }
    }
}
