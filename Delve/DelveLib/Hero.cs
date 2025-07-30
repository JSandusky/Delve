using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DelveLib
{

    public class AttackStats
    {
        public Statistic BlueAttack;
        public Statistic RedAttack;
        public Statistic YellowAttack;
        public Statistic GreenAttack;

        public DieFace RollAttack(System.Random r)
        {
            DieFace v = new DieFace(0, 0, 0);
            if (BlueAttack.Value > 0)
            {
                DieFace res = Dice.Roll(Dice.BlueDie, r, BlueAttack.Value);
                if (res.IsNil())
                    return res;
                v = v.Add(res);
            }

            if (RedAttack.Value > 0)
                v = v.Add(Dice.Roll(Dice.RedDie, r, RedAttack.Value));
            if (YellowAttack.Value > 0)
                v = v.Add(Dice.Roll(Dice.YellowDie, r, YellowAttack.Value));
            if (GreenAttack.Value > 0)
                v = v.Add(Dice.Roll(Dice.GreenDie, r, GreenAttack.Value));

            return v;
        }
    }

    public class DefenseStats
    {
        public Statistic BrownDefense;
        public Statistic GrayDefense;
        public Statistic BlackDefense;
        public Statistic[] ElementalResistances;

        public int RollDefense(System.Random r)
        {
            return Dice.RollDefense(r, BrownDefense.Value, GrayDefense.Value, BlackDefense.Value);
        }
    }

    public enum ItemClass
    {
        OneHanded,
        TwoHanded,
        Armor,
        Trinket
    }

    public class Item
    {
        public AttackStats Attack;
        public DefenseStats Defense;
        public string Name;
        public string Flavor;
        public ItemClass Clazz = ItemClass.OneHanded;
    }

    public class Consumable
    {

    }

    public interface IDamageable
    { }

    public class Monster : IDamageable
    {
        public Statistic Speed;
        public Statistic Health;
        public AttackStats Attack;
        public DefenseStats Defense;
    }

    public class Hero : IDamageable
    {
        // Core stats
        public Statistic Speed;
        public Statistic Health;
        public Statistic Stamina;
        public Statistic Defense;

        // Attributes
        public Statistic Might;
        public Statistic Knowledge;
        public Statistic Willpower;
        public Statistic Awareness;

        // Tracker for surge
        public Statistic Surge;

        public Item LeftHand;
        public Item RightHand;
        public Item Armor;
        public Item Trinket;
        public Consumable ItemA;
        public Consumable ItemB;
    }
}
