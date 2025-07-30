using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using ImGuiCLI;
using DelveLib;

namespace Delve.Objects
{
    public abstract class Component : IDisposable
    {
        protected GameObject owner_;

        public GameObject Owner {
            get { return owner_; }
            set
            {
                if (owner_ != value)
                {
                    owner_ = value;
                    OnOwnerChanged();
                }
            }
        }

        public virtual void Dispose() { }
        public virtual void OnOwnerChanged() { }
        public abstract void Save(System.IO.BinaryWriter stream);
        public abstract void Load(System.IO.BinaryReader stream);
    }

    [Serializable]
    public partial class GameObject : DelveLib.Visual3D
    {
        Scene scene_;
        GameObject parent_;
        string name_;
        Vector3 position_;
        Quaternion rotation_;
        Vector3 scale_ = Vector3.One;
        // Cached data
        Vector3 worldPosition_;
        Quaternion worldRotation_;
        Vector3 worldScale_;
        Matrix transform_;
        Matrix worldTransform_;
        Matrix inverseTransform_;
        Matrix inverseWorldTransform_;
        bool transformDirty_ = true;

        public Scene Scene { get { return scene_; } set { scene_ = value; } }
        public string Name { get { return name_; } set { name_ = value; } }
        public Vector3 Position { get { return position_; } set { position_ = value; IsDirty = transformDirty_  = true; } }
        public Quaternion Rotation { get { return rotation_; } set { rotation_ = value; IsDirty = transformDirty_ = true; } }
        public Vector3 Scale { get { return scale_; } set { scale_ = value; IsDirty = transformDirty_ = true; } }

        public Vector3 WorldPosition {  get { UpdateTransform(); return worldPosition_; }
            set
            {
                if (parent_ != null)
                    position_ = Vector3.Transform(value, parent_.InverseWorldTransform);
                else
                    position_ = value;
                transformDirty_ = true;
            }
        }
        public Quaternion WorldRotation { get { UpdateTransform(); return worldRotation_; }
            set
            {
                if (parent_ != null)
                    rotation_ = Quaternion.Inverse(parent_.WorldRotation) * value;
                else
                    rotation_ = value;
                transformDirty_ = true;
            }
        }
        public Vector3 WorldScale { get { UpdateTransform(); return worldScale_; }
            set
            {
                if (parent_ != null)
                    scale_ = value / parent_.WorldScale;
                else
                    scale_ = value;
                transformDirty_ = true;
            }
        }

        public Matrix Transform { get {
                UpdateTransform();
                return transform_;
            }
        }
        public Matrix InverseTransform { get
            {
                UpdateTransform();
                return inverseTransform_;
            }
        }
        public Matrix WorldTransform { get { UpdateTransform(); return worldTransform_; } }
        public Matrix InverseWorldTransform { get { UpdateTransform(); return inverseWorldTransform_; } }

        void UpdateTransform()
        {
            if (transformDirty_)
            {
                transform_ = Matrix.CreateScale(scale_) * Matrix.CreateFromQuaternion(rotation_) * Matrix.CreateTranslation(position_);
                inverseTransform_ = Matrix.Invert(transform_);
                if (parent_ != null)
                    worldTransform_ = parent_.WorldTransform * transform_;
                inverseWorldTransform_ = Matrix.Invert(worldTransform_);
                worldTransform_.Decompose(out worldScale_, out worldRotation_, out worldPosition_);
            }
            transformDirty_ = false;
        }

        public GameObject Parent { get { return parent_; } set
            {
                if (parent_ == value)
                    return;
                if (parent_ != null)
                    parent_.Children.Remove(this);
                parent_ = value;
                parent_.Children.Add(this);
                transformDirty_ = true;
            }
        }

        public List<Component> Components { get; private set; } = new List<Component>();
        public List<GameObject> Children { get; private set; } = new List<GameObject>();

        public void AddChild(GameObject obj) { obj.Parent = this; }
        public void RemoveChild(GameObject obj) { obj.Parent = null; }
        public void AddComponent(Component comp)
        {
            if (comp.Owner == this)
                return;
            if (comp.Owner != null)
                comp.Owner.RemoveComponent(comp);
            Components.Add(comp);
            comp.Owner = this;
        }
        public void RemoveComponent(Component comp)
        {
            if (Components.Contains(comp))
            {
                Components.Remove(comp);
                comp.Owner = null;
            }
        }

        public virtual float? RayTest(ref Ray ray)
        {
            return null;
        }
        // To be called on the chosen object of a ray test for post operations
        public virtual void PostRay(ref Ray ray, int codeID) { }

        public Vector3 ObjectUp
        {
            get { return Vector3.TransformNormal(Vector3.UnitY, transform_); }
        }

        public Vector3 ObjectRight
        {
            get { return Vector3.TransformNormal(Vector3.UnitX, transform_); }
        }

        public Vector3 ObjectForward
        {
            get { return Vector3.TransformNormal(Vector3.UnitZ, transform_); }
        }

        public virtual void DrawEditor()
        {
            ImGuiCli.PushID(GetHashCode());
            ImGuiEx.DragFloatN_Colored("Position", ref position_);
            Vector3 euler = rotation_.ToEuler();
            if (ImGuiEx.DragFloatN_Colored("Rotation", ref euler))
                rotation_ = DataExtensions.QuaternionFromEuler(euler);
            if (ImGuiEx.DragFloatN_Colored("Scale", ref scale_))
            {
                scale_.X = Math.Max(scale_.X, 0.001f);
                scale_.Y = Math.Max(scale_.Y, 0.001f);
                scale_.Z = Math.Max(scale_.Z, 0.001f);
            }
            ImGuiCli.PopID();
        }

        public virtual void DrawDebug(DebugRenderer debug)
        {
            debug.DrawLine(position_, position_ + ObjectRight, Color.Red, DebugDrawDepth.Always);
            debug.DrawLine(position_, position_ + ObjectUp, Color.Green, DebugDrawDepth.Always);
            debug.DrawLine(position_, position_ + ObjectForward, Color.Blue, DebugDrawDepth.Always);
        }
    }
}
