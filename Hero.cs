using System.Collections.Generic;

namespace TimeWarLib
{
    public class Hero
    {
        public IHeroSDS sds { private set; get; }

        public int nowHp { private set; get; }

        public int x { private set; get; }

        public int y { private set; get; }

        public int pos
        {
            get
            {
                return x * BattleConst.mapWidth + y;
            }
        }

        public bool isMine { private set; get; }

        private int fix;

        private int moveNum;

        internal void Init(bool _isMine, IHeroSDS _sds, int _x)
        {
            isMine = _isMine;

            fix = isMine ? 1 : -1;

            sds = _sds;

            x = _x;

            y = isMine ? 0 : BattleConst.mapWidth - 1;

            nowHp = _sds.GetHp();

            moveNum = sds.GetMove();
        }

        internal void Init(bool _isMine, IHeroSDS _sds, int _x, int _y, int _nowHp)
        {
            isMine = _isMine;

            fix = isMine ? 1 : -1;

            sds = _sds;

            x = _x;

            y = _y;

            nowHp = _nowHp;

            moveNum = sds.GetMove();
        }

        internal void Move(Hero[][] _heroMap, Dictionary<int, Hero> _heroDic)
        {
            int posY = -1;

            int i = 1;

            while (moveNum > 0)
            {
                int tmpY = y + i * fix;

                if (tmpY < 0 || tmpY >= BattleConst.mapWidth)
                {
                    break;
                }

                if (_heroMap[x][tmpY] != null)
                {
                    break;
                }

                posY = tmpY;

                moveNum--;

                i++;
            }

            if (posY != -1)
            {
                _heroMap[x][y] = null;

                _heroDic.Remove(pos);

                y = posY;

                _heroMap[x][y] = this;

                _heroDic.Add(pos, this);
            }
        }

        internal void Attack(Hero[][] _heroMap)
        {
            for (int i = 0; i < sds.GetStopAttackPos().Length; i++)
            {
                KeyValuePair<int, int> pair = sds.GetStopAttackPos()[i];

                int tmpX = x + pair.Key * fix;

                if (tmpX < 0 || tmpX >= BattleConst.mapHeight)
                {
                    continue;
                }

                int tmpY = x + pair.Value * fix;

                if (tmpY < 0 || tmpY >= BattleConst.mapWidth)
                {
                    continue;
                }

                Hero tmpHero = _heroMap[tmpX][tmpY];

                if (tmpHero != null && tmpHero.isMine != isMine)
                {
                    return;
                }
            }

            Hero targetHero = null;

            for (int i = 0; i < sds.GetTargetPos().Length; i++)
            {
                KeyValuePair<int, int> pair = sds.GetTargetPos()[i];

                int tmpX = x + pair.Key * fix;

                if (tmpX < 0 || tmpX >= BattleConst.mapHeight)
                {
                    continue;
                }

                int tmpY = y + pair.Value * fix;

                if (tmpY < 0 || tmpY >= BattleConst.mapWidth)
                {
                    continue;
                }

                Hero tmpHero = _heroMap[tmpX][tmpY];

                if (tmpHero != null && tmpHero.isMine != isMine)
                {
                    tmpHero.BeDamage(this);

                    if (sds.GetSplashType() != SplashType.SPLASH_OTHER_TARGETS)
                    {
                        targetHero = tmpHero;

                        break;
                    }
                }
            }

            if (targetHero != null && sds.GetSplashType() == SplashType.SPLASH_TARGET_POS)
            {
                for (int i = 0; i < sds.GetSplashPos().Length; i++)
                {
                    KeyValuePair<int, int> pair = sds.GetSplashPos()[i];

                    int tmpX = targetHero.x + pair.Key * fix;

                    if (tmpX < 0 || tmpX >= BattleConst.mapHeight)
                    {
                        continue;
                    }

                    int tmpY = targetHero.y + pair.Value * fix;

                    if (tmpY < 0 || tmpY >= BattleConst.mapWidth)
                    {
                        continue;
                    }

                    Hero tmpHero = _heroMap[tmpX][tmpY];

                    if (tmpHero != null && tmpHero.isMine != isMine)
                    {
                        tmpHero.BeDamage(this);
                    }
                }
            }
        }

        private void BeDamage(Hero _hero)
        {
            nowHp -= _hero.sds.GetAttack();
        }

        internal void Die()
        {

        }

        internal void Recover()
        {
            moveNum = sds.GetMove();
        }

        internal Hero Clone()
        {
            Hero hero = new Hero();

            hero.Init(isMine, sds, x, y, nowHp);

            return hero;
        }
    }
}
