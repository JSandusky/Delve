using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DelveLib.EC
{
    public static class EventID
    {
        public static readonly int QUADTREE_UPDATE = 1;
        public static readonly int UPDATE = 2;
        public static readonly int PRE_UPDATE = 3;
        public static readonly int POST_UPDATE = 4;
        public static readonly int FIXED_UPDATE = 5;
    }
}
