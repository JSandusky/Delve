using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DelveLib.U
{
    public class Object
    {
        internal IntPtr pointer_;
    }

    public class ScriptableObject
    {

    }

    public class GameObject : Object
    {
        Transform transform_;

        public bool activeInHierarchy
        {
            get;
            set;
        }

        public bool activeSelf
        {
            get;
        }

        public int layer
        {
            get; set;
        }

        public SceneManagement.Scene scene
        {
            get;
            set;
        }

        public Transform transform
        {
            get { return transform_; }
        }
    }

    public class Component : Object
    {
        GameObject object_;

        public GameObject gameObject { get { return object_; } }
    }

    public class Behaviour : Component
    {

    }
    
    public class MonoBehaviour : Behaviour
    {

    }

    /// <summary>
    /// Transform is a special case component.
    /// </summary>
    public class Transform : Component
    {
        
    }
}
