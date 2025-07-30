using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DelveLib.EC
{
    public class Scene : GameObject
    {
        public Signal<float> PreUpdate = new Signal<float>();
        public Signal<float> Update = new Signal<float>();
        public Signal<float> PostUpdate = new Signal<float>();
        public Signal<float> FixedUpdate = new Signal<float>();
        public Signal<QuadTree> UpdateQuadtree = new Signal<QuadTree>();

        public Scene()
        {
            RegisterSignal(EventID.PRE_UPDATE, PreUpdate);
            RegisterSignal(EventID.UPDATE, Update);
            RegisterSignal(EventID.POST_UPDATE, PostUpdate);
            RegisterSignal(EventID.FIXED_UPDATE, FixedUpdate);
            RegisterSignal(EventID.QUADTREE_UPDATE, UpdateQuadtree);
        }
    }
}
