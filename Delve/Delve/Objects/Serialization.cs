using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using Microsoft.Xna.Framework;

namespace Delve.Objects
{
    public partial class GameObject
    {
        public static void WriteName(Component obj, BinaryWriter writer)
        {

        }

        public static Component CreateComponent(BinaryReader rdr)
        {
            return null;
        }

        public static void WriteName(GameObject obj, BinaryWriter writer)
        {
            if (obj is BillboardStrip)
                writer.Write("BillboardStrip");
            else if (obj is ModelObject)
                writer.Write("ModelObject");
            else if (obj is PortalObject)
                writer.Write("PortalObject");
            else if (obj is GameObject)
                writer.Write("SceneObject");
        }

        public static GameObject Create(BinaryReader rdr)
        {
            string name = rdr.ReadString();
            if (name == "SceneObject")
                return new GameObject();
            if (name == "ModelObject")
                return new ModelObject();
            if (name == "BillboardStrip")
                return new BillboardStrip();
            if (name == "PortalObject")
                return new PortalObject();
            return null;
        }

        public virtual void Save(BinaryWriter stream)
        {
            stream.Write(position_.X);
            stream.Write(position_.Y);
            stream.Write(position_.Z);

            stream.Write(rotation_.X);
            stream.Write(rotation_.Y);
            stream.Write(rotation_.Z);
            stream.Write(rotation_.W);

            stream.Write(scale_.X);
            stream.Write(scale_.Y);
            stream.Write(scale_.Z);

            stream.Write(Components.Count);
            for (int i = 0; i < Components.Count; ++i)
            {
                WriteName(Components[i], stream);
                Components[i].Save(stream);
            }
        }

        public virtual void Load(BinaryReader stream)
        {
            position_.X = stream.ReadSingle();
            position_.Y = stream.ReadSingle();
            position_.Z = stream.ReadSingle();

            rotation_.X = stream.ReadSingle();
            rotation_.Y = stream.ReadSingle();
            rotation_.Z = stream.ReadSingle();
            rotation_.W = stream.ReadSingle();

            scale_.X = stream.ReadSingle();
            scale_.Y = stream.ReadSingle();
            scale_.Z = stream.ReadSingle();

            int compCount = stream.ReadInt32();
            for (int i = 0; i < compCount; ++i)
            {
                Component c = CreateComponent(stream);
                c.Load(stream);
            }
        }
    }

    public partial class ObjectGroup
    {
        public void Save(BinaryWriter stream)
        {
            stream.Write(GroupName);
            stream.Write(Objects.Count);
            for (int i = 0; i < Objects.Count; ++i)
                stream.Write(Objects[i].Scene.Objects.IndexOf(Objects[i]));
        }
        public void Load(Scene scene, BinaryReader stream)
        {
            GroupName = stream.ReadString();
            int ct = stream.ReadInt32();
            for (int i = 0; i < ct; ++i)
            {
                int idx = stream.ReadInt32();
                Objects.Add(scene.Objects[idx]);
            }
        }
    }

    public partial class Scene
    {
        public void Save(BinaryWriter stream)
        {
            stream.Write(SceneName);
            stream.Write(Objects.Count);
            for (int i = 0; i < Objects.Count; ++i)
            {
                GameObject.WriteName(Objects[i], stream);
                Objects[i].Save(stream);
            }

            stream.Write(groups_.Count);
            for (int i = 0; i < groups_.Count; ++i)
                groups_[i].Save(stream);
        }

        public void Load(BinaryReader stream)
        {
            SceneName = stream.ReadString();
            int objCt = stream.ReadInt32();
            for (int i = 0; i < objCt; ++i)
            {
                var obj = GameObject.Create(stream);
                obj.Load(stream);
                Objects.Add(obj);
                obj.Scene = this;
            }

            groups_.Clear();
            int grpCt = stream.ReadInt32();
            for (int i = 0; i < grpCt; ++i)
            {
                ObjectGroup grp = new ObjectGroup();
                grp.Load(this, stream);
                groups_.Add(grp);
            }
        }
    }

    public partial class BillboardStrip
    {
        public override void Save(BinaryWriter stream)
        {
            base.Save(stream);
            stream.Write(Curve.Count);
            for (int i = 0; i < Curve.Count; ++i)
            {
                var key = Curve.GetKey(i);
                stream.Write(key.X);
                stream.Write(key.Y);
                stream.Write(key.Z);
            }
        }

        public override void Load(BinaryReader stream)
        {
            base.Load(stream);
            int ct = stream.ReadInt32();
            for (int i = 0; i < ct; ++i)
            {
                Vector3 v = new Vector3();
                v.X = stream.ReadSingle();
                v.Y = stream.ReadSingle();
                v.Z = stream.ReadSingle();
                Curve.AddKey(i, v);
            }
            if (Curve.Count > 0)
                Curve.ComputeTangents();
        }
    }

    public partial class ModelObject
    {
        public override void Save(BinaryWriter stream)
        {
            base.Save(stream);
            stream.Write(animationDuration_);
            stream.Write(AnimPoint.HasValue);
            if (AnimPoint.HasValue)
            {
                stream.Write(AnimPoint.Value.M11);
                stream.Write(AnimPoint.Value.M12);
                stream.Write(AnimPoint.Value.M13);
                stream.Write(AnimPoint.Value.M14);
                stream.Write(AnimPoint.Value.M21);
                stream.Write(AnimPoint.Value.M22);
                stream.Write(AnimPoint.Value.M23);
                stream.Write(AnimPoint.Value.M24);
                stream.Write(AnimPoint.Value.M31);
                stream.Write(AnimPoint.Value.M32);
                stream.Write(AnimPoint.Value.M33);
                stream.Write(AnimPoint.Value.M34);
                stream.Write(AnimPoint.Value.M41);
                stream.Write(AnimPoint.Value.M42);
                stream.Write(AnimPoint.Value.M43);
                stream.Write(AnimPoint.Value.M44);
            }
        }

        public override void Load(BinaryReader stream)
        {
            base.Load(stream);
            animationDuration_ = stream.ReadSingle();
            bool hasAnimPoint = stream.ReadBoolean();
            if (hasAnimPoint)
            {
                Matrix m = new Matrix();
                m.M11 = stream.ReadSingle();
                m.M12 = stream.ReadSingle();
                m.M13 = stream.ReadSingle();
                m.M14 = stream.ReadSingle();
                m.M21 = stream.ReadSingle();
                m.M22 = stream.ReadSingle();
                m.M23 = stream.ReadSingle();
                m.M24 = stream.ReadSingle();
                m.M31 = stream.ReadSingle();
                m.M32 = stream.ReadSingle();
                m.M33 = stream.ReadSingle();
                m.M34 = stream.ReadSingle();
                m.M41 = stream.ReadSingle();
                m.M42 = stream.ReadSingle();
                m.M43 = stream.ReadSingle();
                m.M44 = stream.ReadSingle();
                AnimPoint = m;
            }
        }
    }
}
