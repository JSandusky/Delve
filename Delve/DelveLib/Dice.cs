using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DelveLib
{
    public struct DieFace
    {
        public int FaceCt;
        public int SymbolCt;
        public int SurgeCt;

        public DieFace(int fn, int sn, int sc)
        {
            FaceCt = fn;
            SymbolCt = sn;
            SurgeCt = sc;
        }

        public bool IsNil()
        {
            return FaceCt == 0 && SymbolCt == 0 && SurgeCt == 0;
        }

        public DieFace Add(DieFace rhs)
        {
            return new DieFace(FaceCt + rhs.FaceCt, SymbolCt + rhs.SymbolCt, SurgeCt + rhs.SurgeCt);
        }
    }

    public static class Dice
    {
        public static readonly DieFace[] GreenDie = 
        {
            new DieFace(0,1,0),
            new DieFace(0,0,1),
            new DieFace(1,0,1),
            new DieFace(1,1,0),
            new DieFace(0,1,1),
            new DieFace(1,1,1)
        };

        public static readonly DieFace[] BlueDie =
        {
            new DieFace(0,0,0),
            new DieFace(2,2,1),
            new DieFace(3,2,0),
            new DieFace(4,2,0),
            new DieFace(5,1,0),
            new DieFace(6,1,1)
        };

        public static readonly DieFace[] RedDie =
        {
            new DieFace(0,1,0),
            new DieFace(0,2,0),
            new DieFace(0,2,0),
            new DieFace(0,2,0),
            new DieFace(0,3,0),
            new DieFace(0,3,1)
        };

        public static readonly DieFace[] YellowDie =
        {
            new DieFace(1,0,1),
            new DieFace(1,1,0),
            new DieFace(2,1,0),
            new DieFace(0,1,1),
            new DieFace(0,2,0),
            new DieFace(0,2,1)
        };

        public static readonly int[,] ArmorDie = {
            { 0, 0, 0, 1, 1, 2 }, // Brown
            { 0, 1, 1, 1, 2, 3 }, // Gray
            { 0, 2, 2, 2, 3, 4 }  // Black
        };

        // Roll and accumulate
        public static DieFace Roll(this DieFace[] list, System.Random r, int ct = 1)
        {
            DieFace ret = new DieFace(0,0,0);
            for (int i = 0; i < ct; ++i)
            {
                DieFace hit = list[r.Next(0, 6)];
                // Complete miss.
                if (hit.FaceCt == 0 && hit.SurgeCt == 0 && hit.SymbolCt == 0)
                    return new DieFace(0, 0, 0);
                ret.FaceCt += hit.FaceCt;
                ret.SymbolCt += hit.SymbolCt;
                ret.SurgeCt += hit.SurgeCt;
            }
            return ret;
        }

        public static int RollDefense(System.Random r, int brownCt, int grayCt, int blackCt)
        {
            int result = 0;
            for (int i = 0; i < brownCt; ++i)
                result += ArmorDie[0, r.Next(0, 6)];
            for (int i = 0; i < grayCt; ++i)
                result += ArmorDie[1, r.Next(0, 6)];
            for (int i = 0; i < blackCt; ++i)
                result += ArmorDie[2, r.Next(0, 6)];
            return result;
        }
    }
}
