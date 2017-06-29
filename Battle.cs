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
                }

                for (int m = 0; m < sds.GetCanDoTimeActionRound().Length; m++)
                {
                    int tmpRoundNum = sds.GetCanDoTimeActionRound()[m];

                    if (!commandInitList.Contains(tmpRoundNum))
                    {
                        commandInitList.Add(tmpRoundNum);
                    }
                }
            }
        }

        private List<int> mCards;
        private List<int> oCards;

        public Dictionary<int, int> mHandCards { private set; get; }
        public Dictionary<int, int> oHandCards { private set; get; }

        private Hero[][] recHeroMap;

        private int recRoundNum;

        private State[] recStates;

        public int roundNum { private set; get; }

        public Dictionary<int, int[]> commands { private set; get; }

        private Dictionary<int, int[]> commandsTime;

        public State actionState { private set; get; }

        private bool asyncWillOver;

        public Battle()
        {
            recHeroMap = new Hero[BattleConst.mapHeight][];

            for (int i = 0; i < BattleConst.mapHeight; i++)
            {
                Hero[] arr = new Hero[BattleConst.mapWidth];

                recHeroMap[i] = arr;
            }

            commands = new Dictionary<int, int[]>();

            commandsTime = new Dictionary<int, int[]>();

            for (int i = 0; i < commandInitList.Count; i++)
            {
                int[] tmpCommands = new int[BattleConst.mapHeight * 2];

                commands.Add(commandInitList[i], tmpCommands);

                int[] tmpCommandsTime = new int[BattleConst.mapHeight * 2];

                commandsTime.Add(commandInitList[i], tmpCommandsTime);
            }

            recStates = new State[BattleConst.mapHeight];
        }

        public void ServerStart(int[] _mCards, int[] _oCards)
        {
            recRoundNum = roundNum = 0;

            IRoundSDS roundSDS = getRoundData(roundNum);

            if (roundSDS.GetSyncAction())
            {
                actionState = State.N;
            }
            else
            {
                actionState = random.NextDouble() < 0.5 ? State.M : State.O;
            }
        }

        private void ServerGetActionCommand(bool _isMine, int _roundNum, int _cardUid, int _posX)
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

            Dictionary<int, int> handCards = _isMine ? mHandCards : oHandCards;

            int cardID;

            if (!handCards.TryGetValue(_cardUid, out cardID))
            {
                throw new Exception("action error3!");
            }

            int[] command = commands[_roundNum];

            int posX = _isMine ? _posX : _posX + BattleConst.mapHeight;

            if (command[posX] != 0)
            {
                throw new Exception("action error4!");
            }

            ICardSDS cardSDS = getCardData(cardID);

            int power = GetPower(isNowAction, _isMine);

            if (power < cardSDS.GetCost())
            {
                throw new Exception("action error5!");
            }

            command[posX] = cardID;

            commandsTime[_roundNum][posX] = roundNum;

            handCards.Remove(_cardUid);
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
                CheckSyncResult(_isMine);
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
                    int[] commandTime = commandsTime[i];

                    int start = _isMine ? 0 : BattleConst.mapHeight;

                    for (int m = start; m < start + BattleConst.mapHeight; m++)
                    {
                        int cardID = command[m];

                        if (cardID != 0 && (commandTime[m] == i) == _isNowPower)
                        {
                            ICardSDS cardSDS = getCardData(cardID);

                            power -= cardSDS.GetCost();
                        }
                    }
                }
            }

            return power;
        }

        private void CheckSyncResult(bool _isMine)
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

        private void CheckAsyncResult()
        {
            State result = GetBattleResult();

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
                        RoundMoveForward();
                    }
                    else
                    {
                        asyncWillOver = true;

                        actionState = State.M;
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
                    if (asyncWillOver)
                    {
                        RoundMoveForward();
                    }
                    else
                    {
                        asyncWillOver = true;

                        actionState = State.O;
                    }
                }
            }
            else
            {
                if (asyncWillOver)
                {
                    RoundMoveForward();
                }
                else
                {
                    asyncWillOver = true;

                    if (actionState == State.M)
                    {
                        actionState = State.O;
                    }
                    else
                    {
                        actionState = State.M;
                    }
                }
            }
        }

        private State GetStatesResult(State[] _states)
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

        private void RoundMoveForward()
        {
            roundNum++;

            int targetRoundNum = int.MaxValue;

            for (int i = roundNum; i < maxRoundNum; i++)
            {
                IRoundSDS roundSDS = getRoundData(i);

                if (roundSDS.GetCanDoAcion())
                {
                    if (i < targetRoundNum)
                    {
                        targetRoundNum = i;
                    }
                }

                for (int m = 0; m < roundSDS.GetCanDoTimeActionRound().Length; m++)
                {
                    int tmpRoundNum = roundSDS.GetCanDoTimeActionRound()[m];

                    if (tmpRoundNum < targetRoundNum)
                    {
                        targetRoundNum = tmpRoundNum;
                    }
                }
            }

            if (targetRoundNum == int.MaxValue)
            {
                BattleOver(GetBattleResult());
            }
            else
            {
                if (targetRoundNum > recRoundNum)
                {
                    BattleCore.Start(commands, recStates, recHeroMap, recRoundNum, targetRoundNum);

                    recRoundNum = targetRoundNum;
                }

                RefreshActionState();
            }
        }

        private void RefreshActionState()
        {
            IRoundSDS roundSDS = getRoundData(roundNum);

            if (roundSDS.GetSyncAction())
            {
                actionState = State.N;
            }
            else
            {
                IRoundSDS lastRoundSDS = getRoundData(roundNum - 1);

                if (lastRoundSDS.GetSyncAction())
                {
                    State result = GetBattleResult();

                    if (result == State.M)
                    {
                        actionState = State.O;
                    }
                    else if (result == State.O)
                    {
                        actionState = State.M;
                    }
                    else
                    {
                        actionState = random.NextDouble() < 0.5 ? State.M : State.O;
                    }
                }
                else
                {
                    asyncWillOver = false;

                    //actionState = actionState == State.M ? State.O : State.M;
                }
            }
        }

        private State GetBattleResult()
        {
            State[] resultStates;

            Hero[][] resultHeroMap;

            BattleCore.Start(commands, recStates, recHeroMap, recRoundNum, maxRoundNum, out resultStates, out resultHeroMap);

            State result = GetStatesResult(resultStates);

            return result;
        }

        private void BattleOver(State _state)
        {

        }

        public void ServerRefreshData(bool _isMine)
        {

        }
    }
}
