using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

namespace Delve.Objects
{
    public partial class ObjectGroup
    {
        public string GroupName;
        public List<GameObject> Objects = new List<GameObject>();

        public void Remove(GameObject obj)
        {
            Objects.Remove(obj);
        }
    }

    public static class ObjectGroupExt
    {
        public static void RemoveObject(this List<ObjectGroup> grps, GameObject obj)
        {
            for (int i = 0; i < grps.Count; ++i)
                grps[i].Remove(obj);
        }
    }

    [Serializable]
    public partial class Scene
    {
        public string SceneName = "";
        public List<GameObject> objects_ = new List<GameObject>();
        public List<ObjectGroup> groups_ = new List<ObjectGroup>();

        public Scene()
        {
            groups_.Add(new ObjectGroup { GroupName = "Default" });
        }

        /// Called every frame before update.
        public event EventHandler<float> ScenePreUpdate;
        /// Called every frame after pre-update.
        public event EventHandler<float> SceneUpdate;
        /// Called every after scene-update.
        public event EventHandler<float> ScenePostUpdate;
        /// Called every fixed-update.
        public event EventHandler<float> SceneFixedUpdate;
        /// Called every frame.
        public event EventHandler<DelveLib.QuadTree> UpdateQuadTree;

        public GameObject CreateDynamic(GameObject parent = null)
        {
            GameObject obj = new GameObject { Scene = this, Parent = parent };
            return obj;
        }

        public GameObject CreateStatic(GameObject parent = null)
        {
            GameObject obj = new GameObject { Scene = this, Parent = parent };
            return obj;
        }

        public void Add(GameObject obj)
        {
            obj.Scene = this;
            objects_.Add(obj);
            groups_[0].Objects.Add(obj);
        }

        public void Add(GameObject obj, ObjectGroup grp)
        {
            obj.Scene = this;
            grp.Objects.Add(obj);
            objects_.Add(obj);
        }

        public void Move(GameObject obj, ObjectGroup toGrp)
        {
            groups_.RemoveObject(obj);
            toGrp.Objects.Add(obj);
        }

        public void Remove(GameObject obj)
        {
            obj.Scene = null;
            objects_.Remove(obj);
            groups_.RemoveObject(obj);
        }

        public List<GameObject> Objects
        {
            get { return objects_; }
            set { objects_ = value; }
        }

        public void Update(float td)
        {
            for (int i = 0; i < objects_.Count; ++i)
            {
                objects_[i].UpdateAnimations(td);
                objects_[i].Update(td);
            }
        }

        public void Save(string filePath)
        {
            using (var fs = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                using (var rdr = new System.IO.BinaryWriter(fs))
                    Save(rdr);
            }
        }

        public static Scene Load(string filePath)
        {
            Scene ret = new Scene();
            using (var fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open))
            {
                using (var rdr = new System.IO.BinaryReader(fs))
                    ret.Load(rdr);
            }
            return ret;
        }

        public static Scene Load(System.IO.Stream stream)
        {
            Scene ret = new Scene();
            using (var rdr = new System.IO.BinaryReader(stream))
                ret.Load(rdr);
            stream.Dispose();
            return ret;
        }

        public GameObject Raycast(ref Ray ray)
        {
            float minHit = float.MaxValue;
            GameObject nearestHit = null;

            for (int i = 0; i < objects_.Count; ++i)
            {
                float? hitDist = objects_[i].RayTest(ref ray);
                if (hitDist.HasValue && hitDist.Value < minHit)
                {
                    minHit = hitDist.Value;
                    nearestHit = objects_[i];
                }
            }

            return nearestHit;
        }

        public void Draw(Graphics.MeshBatch batch)
        {
            //??for (int i = 0; i < objects_.Count; ++i)
            //??    objects_[i].Draw(batch);
        }

        public Scene Clone()
        {
            Scene ret = new Scene();
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            {
                using (var outData = new System.IO.BinaryWriter(stream))
                    this.Save(outData);
                stream.Seek(0, System.IO.SeekOrigin.Begin);

                using (var inData = new System.IO.BinaryReader(stream))
                    ret.Load(inData);
            }
            return ret;
        }
    }
}
