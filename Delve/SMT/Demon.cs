using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMT
{
    public enum Elemental
    {
        Physical,
        Fire,
        Ice,
        Electric,
        Force,
        Holy,
        Dark,
        AllMighty,
        Count
    }

    public enum ElementalResponse
    {
        None,
        Weak,
        Resist,
        Null,
        Reflect,
        Drain
    }

    public enum DemonColor
    {
        Clear,
        Aragami,
        Protector,
        Pyschic,
        Elementalist
    }

    public enum AutoStyle
    {
        Attack,
        MagicAttack,
        Heal,
        Buff,
        Debuff
    }

    public class Skill
    {

    }

    public class DemonData
    {
        public string FamilyName;
        public string Name;
        public string FlavorText;
        public int Stars { get; set; }
        public int Grade { get; set; }
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Magic { get; set; }
        public int Vitality { get; set; }
        public int Luck { get; set; }
        public AutoStyle AutoStyle { get; set; }

        public ElementalResponse[] Weaknesses = new ElementalResponse[] {
            ElementalResponse.None,
            ElementalResponse.None,
            ElementalResponse.None,
            ElementalResponse.None,
            ElementalResponse.None,
            ElementalResponse.None,
            ElementalResponse.None,
            ElementalResponse.None
        };
        public List<Skill> BaseSkills;
        public List<Skill>[] ColorSkills;
    }

    public class Demon
    {
        public DemonData Data { get; private set; }
        public int Stars { get; set; }
        public int Exp { get; set; }
        public int Level { get; set; }
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Magic { get; set; }
        public int Vitality { get; set; }
        public int Luck { get; set; }

        public int MaxLevel { get { return 20 + Stars * 5; } }
        public int HP { get { return (int)Math.Floor(Vitality * 4.7 + Level * 7.4); } }
        public int PATK {get { return (int)Math.Floor(Strength * 2.1 + Level* 5.6 + 50); } }
        public int MATK {get { return (int)Math.Floor(Magic * 2.1 + Level * 5.6 + 50); } }
        public int PDEF {get { return (int)Math.Floor(Vitality * 1.1 + Strength * 0.5 + Level* 5.6 + 50); } }
        public int MDEF { get { return (int)Math.Floor(Vitality * 1.1 + Magic * 0.5 + Level * 5.6 + 50); } }

        public int CurrentHP = 0;
        public int RepelMag = 0;
        public int RepelPhys = 0;
        public int Tarunda = 0;
        public int Tarukaja = 0;
        public int Rakunda = 0;
        public int Rakukaja = 0;
        public int Sukunda = 0;
        public int Sukakaja = 0;
        public int HealModifier = 0;

        public int AccuracyModifier = 0;
        public int EvasionModifier = 0;
        public ElementalResponse[] Weaknesses = new ElementalResponse[] {
            ElementalResponse.None,
            ElementalResponse.None,
            ElementalResponse.None,
            ElementalResponse.None,
            ElementalResponse.None,
            ElementalResponse.None,
            ElementalResponse.None,
            ElementalResponse.None
        };

        public void ApplyCaps()
        {
            Tarukaja = Math.Min(3, Math.Max(Tarukaja, 0));
            Tarunda = Math.Min(3, Math.Max(Tarunda, 0));
            Rakunda = Math.Min(3, Math.Max(Rakunda, 0));
            Rakukaja = Math.Min(3, Math.Max(Rakukaja, 0));
            Sukunda = Math.Min(3, Math.Max(Sukunda, 0));
            Sukakaja = Math.Min(3, Math.Max(Sukakaja, 0));
        }

        public int CalculateDamage(Demon attacker, Elemental type, int attackPower, bool isPhys, bool charged, float resCritMult)
        {
            var value = ((isPhys ? attacker.PATK : attacker.MATK) - (isPhys ? PDEF : MDEF) * 0.5) * 0.4 *
            (attackPower) * (charged ? 2.25 : 1.0) * resCritMult;

            value *= (1 + attacker.Tarukaja + Tarunda - attacker.Rakunda - Rakukaja);

            // reflection and drain should be done elsewhere.
            if (Weaknesses[(int)type] == ElementalResponse.Weak)
                value *= 1.5f;
            else if (Weaknesses[(int)type] == ElementalResponse.Resist)
                value *= 0.7f;
            else if (Weaknesses[(int)type] == ElementalResponse.Null)
                value = 0.0;
            return (int)Math.Floor(value);
        }

        public int CalculateHeal(Demon healer, int healPower, int modAmt)
        {
            var value = MATK * (healPower * (1 + modAmt / 100.0) + 5) * (1 + HealModifier / 100.0);
            return (int)Math.Floor(value);
        }

        public int CalculateAccuracy(Demon victim, int baseAccuracy, int extraAcc)
        {
            var tempAccuracy = baseAccuracy + (Agility - victim.Agility) + (Luck - victim.Luck) * (1 * Math.Min(victim.Sukunda, 1) / Math.Min(Sukakaja, 1));
            return Math.Max(tempAccuracy + extraAcc + (Luck - victim.Luck), 20);
        }

        public int CalculateCritChance(Demon victim, int baseCritChance)
        {
            return Math.Max(baseCritChance + AccuracyModifier - victim.EvasionModifier + (Luck - victim.Luck), 20);
        }
    }
}
