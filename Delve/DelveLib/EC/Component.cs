using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DelveLib.EC
{
    public class Component : EventObject, IDisposable
    {
        GameObject owner_;

        public GameObject Owner {
            get { return owner_; }
            set
            {
                if (owner_ == value)
                    return;
                if (owner_ != null)
                    owner_.Components.Remove(this);
                var old = owner_;
                owner_ = value;
                OwnerChanged(old);
            }
        }
        public Scene Scene { get { return owner_.Scene; } }

        public virtual void OwnerChanged(GameObject oldOwner) { }
        public virtual void OwnerTransformed() { }
        public virtual void Dispose() {
            base.Dispose();
        }
    }
}
