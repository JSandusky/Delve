using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DelveLib;

namespace Delve.Objects
{
    public abstract class Drawable : Component, DelveLib.SceneTreeItem
    {
        public SceneTreePartitioner PartitionNode { get; set; }

        public override void OnOwnerChanged()
        {
            Owner.Scene.UpdateQuadTree += Scene_UpdateQuadTree;
        }

        public override void Dispose()
        {
            base.Dispose();
            Owner.Scene.UpdateQuadTree -= Scene_UpdateQuadTree;
            if (PartitionNode != null)
                PartitionNode.Remove(this);
        }

        private void Scene_UpdateQuadTree(object sender, QuadTree e)
        {
            if (PartitionNode == null)
                PartitionNode = e.Insert(this, new Microsoft.Xna.Framework.BoundingBox());
            else
            {
                PartitionNode.Remove(this);
                e.Insert(this, new Microsoft.Xna.Framework.BoundingBox());
            }
        }
    }

    public class StaticModel : Drawable
    {
        MeshData meshData_;
        public override void Load(BinaryReader stream)
        {
            throw new NotImplementedException();
        }

        public override void Save(BinaryWriter stream)
        {
            throw new NotImplementedException();
        }
    }

    public class AnimatedModel : StaticModel
    {
        public List<GameObject> Bones { get; private set; } = new List<GameObject>();

        public override void Load(BinaryReader stream)
        {
            throw new NotImplementedException();
        }

        public override void Save(BinaryWriter stream)
        {
            throw new NotImplementedException();
        } 
    }
}
