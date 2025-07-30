using System;
using System.IO;

namespace DelveLib
{
    public delegate void StatisticChanged();

    public class SimpleStatistic
    {
        [SerializeField]
        protected int score_ = 0;

        public int Score
        {
            get { return score_; }
            set { score_ = value; Change(); }
        }

        public virtual int Value { get { return Score; } }

        /// Event will be invoked when stats are changed
        public event StatisticChanged OnChange;

        /// <summary>
        /// Returns the score after whiping the value to zero.
        /// </summary>
        /// <param name="justTesting">Whether to whipe the value or not - encourages shared code for "examine" vs "execute" paths</param>
        public int Consume(bool justTesting = false)
        {
            if (justTesting)
                return score_;

            int ret = score_;
            score_ = 0;
            Change();
            return ret;
        }

        /// <summary>
        /// Performs null check on the event and invokes it if necessary
        /// </summary>
        protected void Change()
        {
            if (OnChange != null)
                OnChange();
        }

        public virtual void Serialize(BinaryWriter stream)
        {
            stream.Write(score_);
        }

        public virtual void Deserialize(BinaryReader stream)
        {
            score_ = stream.ReadInt32();
        }

        public override string ToString()
        {
            return score_.ToString();
        }
    }

    /// <summary>
    /// A Statistic is less a graph point in context and more a relatively fixed though fluctuating value
    /// </summary>
    [Serializable]
    public class Statistic : SimpleStatistic
    {
        [SerializeField]
        protected int damage_ = 0;

        [SerializeField]
        protected int drain_ = 0;

        [SerializeField]
        protected int temporary_ = 0;

        [SerializeField]
        protected bool penalized_ = false;

        public int Damage
        {
            get { return damage_; }
            set { damage_ = Math.Max(0, value); Change(); }
        }

        public void AddDamage(int amount)
        {
            Damage = damage_ + amount;
        }

        public int Drain
        {
            get { return drain_; }
            set { drain_ = value; Change(); }
        }

        public int Temporary
        {
            get { return temporary_; }
            set { temporary_ = value; Change(); }
        }

        public bool Penalized
        {
            get { return penalized_; }
            set { penalized_ = value; Change(); }
        }

        /// Is the value less than -1 or less
        public bool IsBelowZero { get { return Value < 0; } }

        /// Is the value zero or less?
        public bool IsZero { get { return Value <= 0; } }

        /// Is the Value less than half of the max value?
        public bool IsLessThanHalf { get { return Value < MaxValue / 2; } }

        /// Have we reached a point where our value is the inverse of our maximum? (ie totally dead)
        public bool Inverted { get { return Value <= -MaxValue; } }

        /// Gets the current value of the ability score.
        public override int Value { get { return Score - Damage - Drain + Temporary; } }

        /// Get the maximum value.
        public int MaxValue { get { return Score - Drain + Temporary; } }

        /// Gets the current value without temporary points
        public int TrueValue { get { return Score - Damage - Drain; } }

        // Get the true max without including temporaries
        public int TrueMaxValue { get { return Score - Drain; } }

        /// <summary>
        /// Returns the scored modifiers, D20/DND would be (Value - 10) / 2
        /// </summary>
        public int Modifier { get { return (Value - 10) / 2; } }

        /// <summary>
        /// Permanently transfers drain to score.
        /// </summary>
        public void CommitDrain()
        {
            score_ -= drain_;
            drain_ = 0;
            Change();
        }

        /// <summary>
        /// Remove temporary values without allowing damage to become "lethal"
        /// As temporary points are removed damage is also removed
        /// </summary>
        public void ClearTemporarySafe(int amount = -1)
        {
            damage_ = Math.Max(0, damage_ - amount > 0 ? amount : temporary_);
            temporary_ = amount > 0 ? temporary_ - amount : temporary_;
            Change();
        }

        public override void Serialize(BinaryWriter stream)
        {
            base.Serialize(stream);
            stream.Write(damage_);
            stream.Write(temporary_);
            stream.Write(drain_);
        }

        public override void Deserialize(BinaryReader stream)
        {
            base.Deserialize(stream);
            damage_ = stream.ReadInt32();
            temporary_ = stream.ReadInt32();
            drain_ = stream.ReadInt32();
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}", Value, MaxValue);
        }
    }

    /// <summary>
    /// Variation of SimpleStatistic that includes a "cooldown"
    /// May either "decay" or outright flip
    /// </summary>
    [Serializable]
    public class WindowedStatistic : SimpleStatistic
    {
        protected float timeLeft_;

        [SerializeField]
        protected float windowTime_;

        /// If we decay then we decrement the score towards zero, otherwise outright zero it
        [SerializeField]
        protected bool decay_;

        public float WindowTime
        {
            get { return windowTime_; }
            set { windowTime_ = value; }
        }

        public bool Decay
        {
            get { return decay_; }
            set { decay_ = value; }
        }

        public bool Ready { get { return timeLeft_ <= 0; } }

        public float CoolDown { get { return Math.Max(timeLeft_, 0.0f); } }

        public void Start()
        {
            timeLeft_ = windowTime_;
        }

        public void UpdateDelta(float td)
        {
            if (timeLeft_ > 0 && windowTime_ > 0) // Need to check for cooldown?
            {
                timeLeft_ -= td;
                if (timeLeft_ <= 0)
                {
                    Score = (decay_ ? Math.Max(0, Score - 1) : 0);
                    if (decay_ && Score > 0) // if we still have decay left then restart the cooldown
                        timeLeft_ = windowTime_;
                }
            }
        }

        public override void Serialize(BinaryWriter stream)
        {
            base.Serialize(stream);
            stream.Write(windowTime_);
            stream.Write(decay_);
        }

        public override void Deserialize(BinaryReader stream)
        {
            base.Deserialize(stream);
            windowTime_ = stream.ReadSingle();
            decay_ = stream.ReadBoolean();
        }
    }
}
