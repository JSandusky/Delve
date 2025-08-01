﻿namespace DefaultEcs.Technical
{
    internal readonly struct ComponentFlag
    {
        #region Fields

        private static ComponentFlag _lastFlag;

        public readonly int Index;
        public readonly uint Bit;

        #endregion

        #region Initialisation

        static ComponentFlag()
        {
            _lastFlag = new ComponentFlag(0, 1u);
        }

        private ComponentFlag(int index, uint bit)
        {
            Index = index;
            Bit = bit;
        }

        #endregion

        #region Methods

        public static ComponentFlag GetNextFlag()
        {
            lock (typeof(ComponentFlag))
            {
                ComponentFlag flag = _lastFlag;
                _lastFlag = _lastFlag.Bit != 0x8000_0000 ? new ComponentFlag(_lastFlag.Index, _lastFlag.Bit << 1) : new ComponentFlag(_lastFlag.Index + 1, 1u);

                return flag;
            }
        }

        #endregion
    }
}
