using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

namespace DelveLib
{
    public class JointData
    {
        public SkeletonData Skeleton { get; set; }
        public int Index { get; private set; }
        public string Name { get; set; }
        public uint Flags { get; set; }
        public uint Capabilities { get; set; }
        public JointData Parent { get; set; }
        public JointData SourceJoint { get; set; }
        public List<JointData> Children { get; private set; } = new List<JointData>();

        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }

        public Vector3 LocalPosition { get { return Parent != null ? Vector3.Transform(Position, Parent.InverseTransform) : Position; } }

        public List<object> Attachments { get; private set; } = new List<object>();

        public void SetIndex(int idx) { Index = idx; }

        public JointData Duplicate()
        {
            return new JointData { Name = this.Name, Flags = this.Flags, Capabilities = this.Capabilities, Position = this.Position, Rotation = this.Rotation, Scale = this.Scale };
        }

        public Vector3 ModelSpacePosition
        {
            get
            {
                return Vector3.Transform(Position, ModelSpaceTransform);
            }
        }

        public Quaternion ModelSpaceRotation
        {
            get
            {
                return ModelSpaceTransform.Rotation * Rotation;
            }
        }

        public Vector3 ModelSpaceScale
        {
            get
            {
                return ModelSpaceTransform.Scale * Scale;
            }
        }

        public Matrix ModelSpaceTransform
        {
            get
            {
                if (Parent != null)
                    return Matrix.Invert(Parent.ModelSpaceTransform) * Transform;
                return Transform;
            }
        }

        public Matrix InverseTransform { get { return Matrix.Invert(Transform); } }

        public Matrix Transform
        {
            get
            {
                return Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateTranslation(Position);
            }
        }

        public bool HasChildren { get { return Children.Count > 0; } }
    }

    public class SkeletonData
    {
        public JointData Root { get; set; }
        public List<JointData> Inline { get; private set; } = new List<JointData>();

        public void AddJoint(JointData parent, JointData child)
        {
            if (parent == null)
                Root = child;
            else
            {
                parent.Children.Add(child);
                child.Parent = parent;
            }

            child.Skeleton = this;
            child.SetIndex(Inline.Count);
            Inline.Add(child);
        }
    }
}
