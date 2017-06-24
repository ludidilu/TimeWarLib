using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeWarLib
{
    public class Battle
    {
        enum ActionState
        {
            NO_ACTIONED,
            M_ACTIONED,
            O_ACTIONED
        }

        internal static Func<int, IRoundSDS> getRoundData;
        internal static Func<int, ICardSDS> getCardData;
        internal static Func<int, IHeroSDS> getHeroData;
        internal static Func<int, ISpellSDS> getSpellData;

        public static int maxRoundNum { private set; get; }

        private static int maxCanDoActionRoundNum;

        private static Random random = new Random();

        public static void Init(int _maxRoundNum, Func<int, IRoundSDS> _getRoundData, Func<int, ICardSDS> _getCardData, Func<int, IHeroSDS> _getHeroData, Func<int, ISpellSDS> _getSpellData)
        {
            maxRoundNum = _maxRoundNum;
            getRoundData = _getRoundData;
            getCardData = _getCardData;
            getHeroData = _getHeroData;
            getSpellData = _getSpellData;

            for (int i = 0; i < maxRoundNum; i++)
            {
                IRoundSDS sds = getRoundData(i);

                if (sds.GetCanDoAcion() || sds.GetCanDoTimeActionRound().Length > 0)
                {
                    maxCanDoActionRoundNum++;
                }
                else
                {
                    break;
                }
            }
        }

        private List<int> mCards;
        private List<int> oCards;

        public List<int> mHandCards { private set; get; }
        public List<int> oHandCards { private set; get; }

        public Hero[][] heroMap { private set; get; }

        public int roundNum { private set; get; }

        public int[][] commands { private set; get; }

        private ActionState actionState;

        public Battle()
        {
            heroMap = new Hero[BattleConst.mapHeight][];

            for (int i = 0; i < BattleConst.mapHeight; i++)
            {
                Hero[] arr = new Hero[BattleConst.mapWidth];

                heroMap[i] = arr;
            }

            commands = new int[maxCanDoActionRoundNum][];

            for (int i = 0; i < maxCanDoActionRoundNum; i++)
            {
                commands[i] = new int[BattleConst.mapHeight * 2];
            }
        }

        public void ServerStart(int[] _mCards, int[] _oCards)
        {
            roundNum = 0;

            actionState = ActionState.NO_ACTIONED;
        }

        private void ServerGetCommand(bool _isMine, int _cardID, int _posX)
        {

        }



        private void BattleStart()
        {
            UseCard();

            HeroMove();

            HeroAttack();
        }

        private void UseCard()
        {
            int[] tmpCommands = commands[roundNum];

            for (int i = 0; i < BattleConst.mapHeight; i++)
            {
                int cardID = tmpCommands[i];

                if (cardID != 0 && heroMap[i][0] == null)
                {
                    ICardSDS cardSDS = getCardData(cardID);

                    if (cardSDS.GetIsHero())
                    {
                        Hero hero = new Hero(this, true, getHeroData(cardSDS.GetUseID()), i);

                        heroMap[i][0] = hero;
                    }
                    else
                    {
                        CastSpell(true, cardSDS.GetUseID(), i);
                    }
                }
            }

            for (int i = 0; i < BattleConst.mapHeight; i++)
            {
                int cardID = tmpCommands[BattleConst.mapHeight + i];

                if (cardID != 0 && heroMap[i][BattleConst.mapWidth - 1] == null)
                {
                    ICardSDS cardSDS = getCardData(cardID);

                    if (cardSDS.GetIsHero())
                    {
                        Hero hero = new Hero(this, false, getHeroData(cardSDS.GetUseID()), i);

                        heroMap[i][BattleConst.mapWidth - 1] = hero;
                    }
                    else
                    {
                        CastSpell(false, cardSDS.GetUseID(), i);
                    }
                }
            }
        }

        private void CastSpell(bool _isMine, int _id, int _posX)
        {

        }

        private void HeroMove()
        {
            for (int i = 0; i < BattleConst.mapHeight; i++)
            {
                bool isMineFirst = i % 2 == 0;

                if (isMineFirst)
                {
                    for (int m = BattleConst.mapWidth - 2; m > -1; m--)
                    {
                        if (heroMap[i][m] != null)
                        {
                            heroMap[i][m].Move();
                        }
                    }

                    for (int m = 1; m < BattleConst.mapWidth; m++)
                    {
                        if (heroMap[i][m] != null)
                        {
                            heroMap[i][m].Move();
                        }
                    }
                }
                else
                {
                    for (int m = 1; m < BattleConst.mapWidth; m++)
                    {
                        if (heroMap[i][m] != null)
                        {
                            heroMap[i][m].Move();
                        }
                    }

                    for (int m = BattleConst.mapWidth - 2; m > -1; m--)
                    {
                        if (heroMap[i][m] != null)
                        {
                            heroMap[i][m].Move();
                        }
                    }
                }
            }
        }

        private void HeroAttack()
        {
            for (int i = 0; i < BattleConst.mapHeight; i++)
            {
                for (int m = 0; m < BattleConst.mapWidth; m++)
                {
                    Hero hero = heroMap[i][m];

                    if (hero != null)
                    {
                        hero.Attack();
                    }
                }
            }

            for (int i = 0; i < BattleConst.mapHeight; i++)
            {
                for (int m = 0; m < BattleConst.mapWidth; m++)
                {
                    Hero hero = heroMap[i][m];

                    if (hero != null && hero.nowHp < 1)
                    {
                        hero.Die();

                        heroMap[i][m] = null;
                    }
                }
            }
        }
    }
}
