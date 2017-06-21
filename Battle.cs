using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeWarLib
{
    public class Battle
    {
        internal static Func<int, IRoundSDS> getRoundData;
        internal static Func<int, IHeroSDS> getHeroData;
        internal static Func<int, IMagicSDS> getMagicData;

        public static int maxRoundNum { private set; get; }

        public static void Init(int _maxRoundNum, Func<int, IRoundSDS> _getRoundData, Func<int, IHeroSDS> _getHeroData, Func<int, IMagicSDS> _getMagicData)
        {
            maxRoundNum = _maxRoundNum;
            getRoundData = _getRoundData;
            getHeroData = _getHeroData;
            getMagicData = _getMagicData;
        }

        public void ServerStart()
        {

        }
    }
}
