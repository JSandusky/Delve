using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace DelveLib
{
    // Swap to pop array of structs
    public class StructArray<T> where T : struct
    {
        public T[] items_;
        public readonly int ObjectSize;
        public int Count { get; set; } = 0;
        public int Capacity { get; private set; } = 0;

        void SSet(params T[] data)
        {
            
        }

        public StructArray(int capacity, int sz)
        {
            ObjectSize = sz;
            Resize(capacity);
            Count = 0;
        }

        public void Resize(int newSize)
        {
            Array.Resize(ref items_, newSize);
            Capacity = newSize;
        }

        public ref T GetNextItem()
        {
            int idx = GetNextIndex();
            return ref items_[idx];
        }

        public int GetNextIndex()
        {
            if (Count == Capacity)
                return -1;
            ++Count;
            return Count - 1;
        }

        public void Remove(int idx)
        {
            if (idx == -1)
                return;

            items_[idx] = items_[Count - 1];
            --Count;
        }

        public void Save()
        {
            unsafe
            {
                using (var memStream = new System.IO.MemoryStream())
                {
                    byte[] data = new byte[256];
                    using (var binWriter = new System.IO.BinaryWriter(memStream))
                    {
                        Buffer.BlockCopy(items_, 0, data, 0, ObjectSize * Count);
                        binWriter.Write(data);
                    }
                }
            }
        }
    }
}
