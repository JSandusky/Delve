using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DelveLib.EC
{
    public struct ObjectID
    {
        int ID;
        short Generation;
        short Type; // negatives are reserved for engine internals
    }

    struct EventSub
    {
        ObjectID sender;
        int eventID;
    }

    public class GameObject : EventObject, IDisposable
    {
        Scene scene_;
        GameObject parent_;
        string name_;
        // caching
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

        public bool IsDirty { get; set; } = true;

        public Vector3 Position { get { return position_; } set { position_ = value; IsDirty = transformDirty_ = true; SignalTransformed(); } }
        public Quaternion Rotation { get { return rotation_; } set { rotation_ = value; IsDirty = transformDirty_ = true; SignalTransformed(); } }
        public Vector3 Scale { get { return scale_; } set { scale_ = value; IsDirty = transformDirty_ = true; SignalTransformed(); } }

        public Vector3 WorldPosition
        {
            get { UpdateTransform(); return worldPosition_; }
            set
            {
                if (parent_ != null)
                    position_ = Vector3.Transform(value, parent_.InverseWorldTransform);
                else
                    position_ = value;
                transformDirty_ = true;
                SignalTransformed();
            }
        }
        public Quaternion WorldRotation
        {
            get { UpdateTransform(); return worldRotation_; }
            set
            {
                if (parent_ != null)
                    rotation_ = Quaternion.Inverse(parent_.WorldRotation) * value;
                else
                    rotation_ = value;
                transformDirty_ = true;
                SignalTransformed();
            }
        }
        public Vector3 WorldScale
        {
            get { UpdateTransform(); return worldScale_; }
            set
            {
                if (parent_ != null)
                    scale_ = value / parent_.WorldScale;
                else
                    scale_ = value;
                transformDirty_ = true;
                SignalTransformed();
            }
        }

        public Matrix Transform
        {
            get
            {
                UpdateTransform();
                return transform_;
            }
        }
        public Matrix InverseTransform
        {
            get
            {
                UpdateTransform();
                return inverseTransform_;
            }
        }
        public Matrix WorldTransform { get { UpdateTransform(); return worldTransform_; } }
        public Matrix InverseWorldTransform { get { UpdateTransform(); return inverseWorldTransform_; } }

        public Scene Scene { get { return scene_; } }

        public List<GameObject> Children { get; private set; } = new List<GameObject>();
        public List<Component> Components { get; private set; } = new List<Component>();

        protected GameObject() { }

        public GameObject CreateChild()
        {
            GameObject child = new GameObject { scene_ = this.scene_ };
            Children.Add(child);
            return child;
        }

        public T AddComponent<T>() where T : Component, new()
        {
            T r = new T();
            Components.Add(r);
            r.Owner = this; // will add it to us
            return r;
        }

        public T GetComponent<T>() where T : Component
        {
            for (int i = 0; i < Components.Count; ++i)
            {
                var v = Components[i] as T;
                if (v != null)
                    return v;
            }
            return null;
        }

        public void RemoveComponent(Component c)
        {
            c.Owner = null;
            Components.Remove(c);
            c.Dispose();
        }

        public List<T> GetComponents<T>() where T : Component, new()
        {
            List<T> ret = new List<T>();
            for (int i = 0; i < Components.Count; ++i)
            {
                var v = Components[i] as T;
                if (v != null)
                    ret.Add(v);
            }
            return ret;
        }

        public bool HasComponent<T>() where T : Component
        {
            return GetComponent<T>() != null;
        }

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

        void SignalTransformed()
        {
            for (int i = 0; i < Components.Count; ++i)
                Components[i].OwnerTransformed();
            for (int i = 0; i < Children.Count; ++i)
                Children[i].SignalTransformed();
        }

        public Vector3 ObjectUp
        {
            get { return Vector3.TransformNormal(Vector3.UnitY, worldTransform_); }
        }

        public Vector3 ObjectRight
        {
            get { return Vector3.TransformNormal(Vector3.UnitX, worldTransform_); }
        }

        public Vector3 ObjectForward
        {
            get { return Vector3.TransformNormal(Vector3.UnitZ, worldTransform_); }
        }

        public void LookAt(Vector3 lookAt)
        {
            WorldRotation = Quaternion.CreateFromRotationMatrix(Matrix.CreateLookAt(worldPosition_, lookAt, Vector3.UnitY));
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
