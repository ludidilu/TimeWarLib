using System;
using System.Collections.Generic;

namespace TimeWarLib
{
    internal static class BattleCore
    {
        private static Func<int, ICardSDS> getCardData;
        private static Func<int, IHeroSDS> getHeroData;
        private static Func<int, ISpellSDS> getSpellData;

        private static Dictionary<int, Hero> heroDic = new Dictionary<int, Hero>();

        private static List<Hero> dieHeroList = new List<Hero>();

        internal static void Init(Func<int, ICardSDS> _getCardData, Func<int, IHeroSDS> _getHeroData, Func<int, ISpellSDS> _getSpellData)
        {
            getCardData = _getCardData;
            getHeroData = _getHeroData;
            getSpellData = _getSpellData;
        }

        internal static void Start(Dictionary<int, int[]> _commands, State[] _states, Hero[][] _heroMap, int _roundNum, int _targetRoundNum)
        {
            for (int i = 0; i < BattleConst.mapHeight; i++)
            {
                for (int m = 0; m < BattleConst.mapWidth; m++)
                {
                    Hero hero = _heroMap[i][m];

                    if (hero != null)
                    {
                        heroDic.Add(hero.pos, hero);
                    }
                }
            }

            for (int i = _roundNum; i < _targetRoundNum; i++)
            {
                UseCard(i, _states, _commands, _heroMap);

                HeroMove(_states, _heroMap);

                HeroAttack(_heroMap);

                HeroMove(_states, _heroMap);

                HeroRecover();
            }

            heroDic.Clear();
        }

        internal static void Start(Dictionary<int, int[]> _commands, State[] _states, Hero[][] _heroMap, int _roundNum, int _targetRoundNum, out State[] _statesResult, out Hero[][] _heroMapReslt)
        {
            _statesResult = new State[BattleConst.mapHeight];

            _heroMapReslt = new Hero[BattleConst.mapHeight][];

            for (int i = 0; i < BattleConst.mapHeight; i++)
            {
                _statesResult[i] = _states[i];

                Hero[] heroArr = _heroMapReslt[i];

                if (heroArr == null)
                {
                    heroArr = new Hero[BattleConst.mapWidth];

                    _heroMapReslt[i] = heroArr;
                }

                for (int m = 0; m < BattleConst.mapWidth; m++)
                {
                    Hero hero = _heroMap[i][m];

                    if (hero != null)
                    {
                        Hero newHero = hero.Clone();

                        heroArr[m] = newHero;
                    }
                    else
                    {
                        heroArr[m] = null;
                    }
                }
            }

            Start(_commands, _statesResult, _heroMapReslt, _roundNum, _targetRoundNum);
        }

        private static void UseCard(int _roundNum, State[] _states, Dictionary<int, int[]> _commands, Hero[][] _heroMap)
        {
            int[] tmpCommands;

            bool b = _commands.TryGetValue(_roundNum, out tmpCommands);

            if (b)
            {
                for (int i = 0; i < BattleConst.mapHeight; i++)
                {
                    if (_states[i] == State.N)
                    {
                        int cardID = tmpCommands[i];

                        if (cardID != 0 && _heroMap[i][0] == null)
                        {
                            ICardSDS cardSDS = getCardData(cardID);

                            if (cardSDS.GetIsHero())
                            {
                                Hero hero = new Hero();

                                hero.Init(true, getHeroData(cardSDS.GetUseID()), i);

                                _heroMap[i][0] = hero;

                                heroDic.Add(hero.pos, hero);
                            }
                            else
                            {
                                CastSpell(true, cardSDS.GetUseID(), i);
                            }
                        }

                        cardID = tmpCommands[BattleConst.mapHeight + i];

                        if (cardID != 0 && _heroMap[i][BattleConst.mapWidth - 1] == null)
                        {
                            ICardSDS cardSDS = getCardData(cardID);

                            if (cardSDS.GetIsHero())
                            {
                                Hero hero = new Hero();

                                hero.Init(false, getHeroData(cardSDS.GetUseID()), i);

                                _heroMap[i][BattleConst.mapWidth - 1] = hero;

                                heroDic.Add(hero.pos, hero);
                            }
                            else
                            {
                                CastSpell(false, cardSDS.GetUseID(), i);
                            }
                        }
                    }
                }
            }
        }

        private static void CastSpell(bool _isMine, int _id, int _posX)
        {

        }

        private static void HeroMove(State[] _states, Hero[][] _heroMap)
        {
            for (int i = 0; i < BattleConst.mapHeight; i++)
            {
                bool isMineFirst = i % 2 == 0;

                if (isMineFirst)
                {
                    for (int m = BattleConst.mapWidth - 2; m > -1; m--)
                    {
                        if (_heroMap[i][m] != null)
                        {
                            _heroMap[i][m].Move(_heroMap, heroDic);
                        }
                    }

                    for (int m = 1; m < BattleConst.mapWidth; m++)
                    {
                        if (_heroMap[i][m] != null)
                        {
                            _heroMap[i][m].Move(_heroMap, heroDic);
                        }
                    }
                }
                else
                {
                    for (int m = 1; m < BattleConst.mapWidth; m++)
                    {
                        if (_heroMap[i][m] != null)
                        {
                            _heroMap[i][m].Move(_heroMap, heroDic);
                        }
                    }

                    for (int m = BattleConst.mapWidth - 2; m > -1; m--)
                    {
                        if (_heroMap[i][m] != null)
                        {
                            _heroMap[i][m].Move(_heroMap, heroDic);
                        }
                    }
                }
            }

            for (int i = 0; i < BattleConst.mapHeight; i++)
            {
                if (_states[i] == State.N)
                {
                    Hero[] tmpArr = _heroMap[i];

                    if (tmpArr[0] != null && !tmpArr[0].isMine)
                    {
                        _states[i] = State.O;

                        RemoveLineHero(_heroMap, i);
                    }
                    else if (tmpArr[BattleConst.mapHeight - 1] != null && tmpArr[BattleConst.mapHeight - 1].isMine)
                    {
                        _states[i] = State.M;

                        RemoveLineHero(_heroMap, i);
                    }
                }
            }
        }

        private static void HeroAttack(Hero[][] _heroMap)
        {
            Dictionary<int, Hero>.ValueCollection.Enumerator enumerator = heroDic.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                enumerator.Current.Attack(_heroMap);
            }

            enumerator = heroDic.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                Hero hero = enumerator.Current;

                if (hero.nowHp < 1)
                {
                    dieHeroList.Add(hero);

                    _heroMap[hero.x][hero.y] = null;
                }
            }

            if (dieHeroList.Count > 0)
            {
                for (int i = 0; i < dieHeroList.Count; i++)
                {
                    Hero hero = dieHeroList[i];

                    hero.Die();

                    heroDic.Remove(hero.pos);
                }

                dieHeroList.Clear();
            }
        }

        private static void HeroRecover()
        {
            Dictionary<int, Hero>.ValueCollection.Enumerator enumerator = heroDic.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                enumerator.Current.Recover();
            }
        }

        private static void RemoveLineHero(Hero[][] _heroMap, int _x)
        {
            Hero[] tmpArr = _heroMap[_x];

            for (int i = 0; i < BattleConst.mapWidth; i++)
            {
                Hero hero = tmpArr[i];

                if (hero != null)
                {
                    tmpArr[i] = null;

                    heroDic.Remove(hero.pos);
                }
            }
        }
    }
}
