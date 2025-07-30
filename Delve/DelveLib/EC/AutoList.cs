using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DelveLib.EC
{
    public class AutoList<T> : List<T>
    {
        private AutoList() { }
        
        static AutoList<T> instance_;
        static Dictionary<int, List<T>> taggedLists_;

        public static AutoList<T> Inst()
        {
            if (instance_ == null)
                instance_ = new AutoList<T>();
            return instance_;
        }

        public static void AddTagged(int tag, T obj)
        {
            if (taggedLists_ == null)
                taggedLists_ = new Dictionary<int, List<T>>();
            List<T> ret = null;
            if (taggedLists_.TryGetValue(tag, out ret))
            {
                ret.Add(obj);
                return;
            }
            ret = new List<T>();
            ret.Add(obj);
            taggedLists_.Add(tag, ret);
        }

        public static void RemoveTagged(int tag, T obj)
        {
            if (taggedLists_ == null)
                return;
            List<T> ret = null;
            if (taggedLists_.TryGetValue(tag, out ret))
                ret.Remove(obj);
        }

        public static void ClearTagged()
        {
            taggedLists_.Clear();
        }
    }

    public static class AutoListHelper
    {
        public static void AddAutoList<T>(this T item) { AutoList<T>.Inst().Add((T)item); }

        public static void RemoveAutoList<T>(this T item) { AutoList<T>.Inst().Remove(item); }

        public static void Tag<T>(this T item, int tag) { AutoList<T>.AddTagged(tag, item); }

        public static void UnTag<T>(this T item, int tag) { AutoList<T>.RemoveTagged(tag, item); }
    }

    public class ALTest
    {
        ALTest()
        {
            this.AddAutoList();
        }

        void Dispose()
        {
            this.RemoveAutoList();
        }
    }
}
