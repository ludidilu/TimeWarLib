using System;
using System.Collections.Generic;

namespace TimeWarLib
{
    public enum State
    {
        N,
        M,
        O
    }

    public class Battle
    {
        private static Func<int, IRoundSDS> getRoundData;
        private static Func<int, ICardSDS> getCardData;

        public static int maxRoundNum { private set; get; }

        public static int maxActionRound { private set; get; }

        private static List<int> commandInitList;

        private static Random random = new Random();

        public static void Init(int _maxRoundNum, Func<int, IRoundSDS> _getRoundData, Func<int, ICardSDS> _getCardData, Func<int, IHeroSDS> _getHeroData, Func<int, ISpellSDS> _getSpellData)
        {
            maxRoundNum = _maxRoundNum;
            getRoundData = _getRoundData;

            getCardData = _getCardData;

            BattleCore.Init(_getCardData, _getHeroData, _getSpellData);

            for (int i = 0; i < maxRoundNum; i++)
            {
                IRoundSDS sds = getRoundData(i);

                if (sds.GetCanDoAcion())
                {
                    if (!commandInitList.Contains(i))
                    {
                        commandInitList.Add(i);
                    }

                    maxActionRound = i;
                }

                for (int m = 0; m < sds.GetCanDoTimeActionRound().Length; m++)
                {
                    int tmpRoundNum = sds.GetCanDoTimeActionRound()[m];

                    if (!commandInitList.Contains(tmpRoundNum))
                    {
                        commandInitList.Add(tmpRoundNum);
                    }

                    maxActionRound = i;
                }
            }
        }

        private List<int> mCards;
        private List<int> oCards;

        public List<int> mHandCards { private set; get; }
        public List<int> oHandCards { private set; get; }

        private Hero[][] recHeroMap;

        private int recRoundNum;

        private State[] recStates;

        public Hero[][] heroMap { private set; get; }

        public int roundNum { private set; get; }

        public State[] states { private set; get; }

        public Dictionary<int, int[]> commands { private set; get; }

        private Dictionary<int, bool[]> commandsTime;

        private State actionState;

        private bool asyncWillOver;

        public Battle()
        {
            heroMap = new Hero[BattleConst.mapHeight][];

            recHeroMap = new Hero[BattleConst.mapHeight][];

            for (int i = 0; i < BattleConst.mapHeight; i++)
            {
                Hero[] arr = new Hero[BattleConst.mapWidth];

                heroMap[i] = arr;

                arr = new Hero[BattleConst.mapWidth];

                recHeroMap[i] = arr;
            }

            commands = new Dictionary<int, int[]>();

            commandsTime = new Dictionary<int, bool[]>();

            for (int i = 0; i < commandInitList.Count; i++)
            {
                int[] tmpCommands = new int[BattleConst.mapHeight * 2];

                commands.Add(commandInitList[i], tmpCommands);

                bool[] tmpCommandsTime = new bool[BattleConst.mapHeight * 2];

                commandsTime.Add(commandInitList[i], tmpCommandsTime);
            }

            states = new State[BattleConst.mapHeight];

            recStates = new State[BattleConst.mapHeight];
        }

        public void ServerStart(int[] _mCards, int[] _oCards)
        {
            recRoundNum = roundNum = 0;
        }

        private void ServerGetActionCommand(bool _isMine, int _roundNum, int _cardID, int _posX)
        {
            if ((actionState == State.O && _isMine) || (actionState == State.M && !_isMine))
            {
                throw new Exception("action error0!");
            }

            IRoundSDS roundSDS = getRoundData(roundNum);

            bool isNowAction = _roundNum == roundNum;

            if (isNowAction)
            {
                if (!roundSDS.GetCanDoAcion())
                {
                    throw new Exception("action error1!");
                }
            }
            else
            {
                if (Array.IndexOf(roundSDS.GetCanDoTimeActionRound(), _roundNum) == -1)
                {
                    throw new Exception("action error2!");
                }
            }

            List<int> handCards = _isMine ? mHandCards : oHandCards;

            int cardIndex = handCards.IndexOf(_cardID);

            if (cardIndex == -1)
            {
                throw new Exception("action error3!");
            }

            int[] command = commands[_roundNum];

            int posX = _isMine ? _posX : _posX + BattleConst.mapHeight;

            if (command[posX] != 0)
            {
                throw new Exception("action error4!");
            }

            ICardSDS cardSDS = getCardData(_cardID);

            int power = GetPower(isNowAction, _isMine);

            if (power < cardSDS.GetCost())
            {
                throw new Exception("action error5!");
            }

            command[posX] = _cardID;

            commandsTime[_roundNum][posX] = isNowAction;

            handCards.RemoveAt(cardIndex);
        }

        private void ServerGetEndCommand(bool _isMine)
        {
            IRoundSDS roundSDS = getRoundData(roundNum);

            if ((actionState == State.O && _isMine) || (actionState == State.M && !_isMine))
            {
                throw new Exception("action error0!");
            }

            if (roundSDS.GetSyncAction())
            {
                if (actionState == State.N)
                {
                    actionState = _isMine ? State.M : State.O;
                }
                else
                {
                    RoundMoveForward();
                }
            }
            else
            {
                CheckAsyncResult();
            }
        }

        private int GetPower(bool _isNowPower, bool _isMine)
        {
            int power = 0;

            for (int i = 0; i < roundNum; i++)
            {
                IRoundSDS roundSDS = getRoundData(i);

                power += _isNowPower ? roundSDS.GetPower() : roundSDS.GetTimePower();

                int[] command;

                bool b = commands.TryGetValue(i, out command);

                if (b)
                {
                    bool[] commandTime = commandsTime[i];

                    int start = _isMine ? 0 : BattleConst.mapHeight;

                    for (int m = start; m < start + BattleConst.mapHeight; m++)
                    {
                        int cardID = command[m];

                        if (cardID != 0 && commandTime[m] == _isNowPower)
                        {
                            ICardSDS cardSDS = getCardData(cardID);

                            power -= cardSDS.GetCost();
                        }
                    }
                }
            }

            return power;
        }

        private State GetBattleResult(State[] _states)
        {
            int m = 0;

            int o = 0;

            for (int i = 0; i < BattleConst.mapHeight; i++)
            {
                State state = _states[i];

                if (state == State.M)
                {
                    m++;
                }
                else if (state == State.O)
                {
                    o++;
                }
            }

            if (m == o)
            {
                return State.N;
            }
            else if (m > o)
            {
                return State.M;
            }
            else
            {
                return State.O;
            }
        }

        private void CheckAsyncResult()
        {
            State[] tmpState;

            Hero[][] tmpHeroMap;

            BattleCore.Start(commands, recStates, recHeroMap, recRoundNum, maxRoundNum, out tmpState, out tmpHeroMap);

            State result = GetBattleResult(tmpState);

            if (result == State.O)
            {
                if (actionState == State.M)
                {
                    BattleOver(State.O);
                }
                else
                {
                    if (asyncWillOver)
                    {

                    }
                }
            }
            else if (result == State.M)
            {
                if (actionState == State.O)
                {
                    BattleOver(State.M);
                }
                else
                {

                }
            }
            else
            {

            }
        }

        private void RoundMoveForward()
        {
            IRoundSDS roundSDS = getRoundData(roundNum);

            if (roundSDS.GetSyncAction())
            {
               
            }
            else
            {
                
            }
        }

        private void RefreshActionState()
        {
            IRoundSDS sds = getRoundData(roundNum);

            if (sds.GetSyncAction())
            {
                actionState = State.N;
            }
            else
            {

            }
        }

        private void BattleOver(State _state)
        {

        }
    }
}
