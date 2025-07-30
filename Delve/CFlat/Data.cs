using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFlat
{
    [Flags]
    public enum InstanceTraits
    {
        Value,
        RawPointer,
        SharedPtr,
        WeakPtr
    }

    public class TypeInfo
    {

    }
}
