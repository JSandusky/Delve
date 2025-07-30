using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DelveLib
{
    public class Element
    {
        public int Index; // index of the element
        public string Name; // "Fire"
        public Element Opposing; // If there's an 'opposite' this will be hit "Positive Energy" vs "Negative Energy"
                
        static List<Element> Elements = new List<Element>();
    }

    public static class OpCodes
    {
    }
}
