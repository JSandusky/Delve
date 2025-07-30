using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DelveLib;

namespace Delve.Objects
{
    public partial class ModelObject : GameObject
    {
        MeshData mesh_;
        Graphics.IMeshBatchEffect effect_;
        Matrix? animPointA_;
        float animationDuration_ = 5.0f;

        public MeshData Mesh {
            get { return mesh_; }
            set {
                if (mesh_ != null)
                    mesh_.Dispose();
                mesh_ = value;
            }
        }

        public Graphics.IMeshBatchEffect Effect { get { return effect_; } set { effect_ = value; } }

        public float AnimCycleLength { get { return animationDuration_; } set { animationDuration_ = value; } }

        public void Create()
        {
            if (HasAnimationCycle)
                ActiveAnimation = new CycleSceneObjectAnimation(AnimPoint.Value, animationDuration_ / 2);
        }

        public void Draw(Graphics.MeshBatch batch)
        {
            if (mesh_ != null && effect_ != null)
                batch.Add(BoundingSphere.CreateFromBoundingBox(mesh_.Bounds), mesh_.VertexBuffer, mesh_.IndexBuffer, Transform, effect_);
        }

        public Matrix? AnimPoint { get { return animPointA_; } set { animPointA_ = value; } }

        public bool HasAnimationCycle { get { return animPointA_.HasValue; } }
    }
}
