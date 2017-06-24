using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeWarLib
{
    public class Hero
    {
        public IHeroSDS sds { private set; get; }
        public int nowHp { private set; get; }

        public KeyValuePair<int, int> pos { private set; get; }

        private int fix;

        private bool m_isMine;

        public bool isMine
        {
            private set
            {
                m_isMine = value;

                fix = m_isMine ? 1 : -1;
            }

            get
            {
                return m_isMine;
            }
        }

        private Battle battle;

        internal Hero(Battle _battle, bool _isMine, IHeroSDS _sds, int _posX)
        {
            isMine = _isMine;
            sds = _sds;
            nowHp = _sds.GetHp();

            if (isMine)
            {
                pos = new KeyValuePair<int, int>(_posX, 0);
            }
            else
            {
                pos = new KeyValuePair<int, int>(_posX, BattleConst.mapWidth - 1);
            }
        }

        internal void Move()
        {
            int posY = -1;

            for (int i = 0; i < sds.GetMove(); i++)
            {
                int y = pos.Value + (i + i) * fix;

                if (y < 0 || y >= BattleConst.mapWidth)
                {
                    break;
                }

                if (battle.heroMap[pos.Key][y] != null)
                {
                    break;
                }

                posY = y;
            }

            if (posY != -1)
            {
                battle.heroMap[pos.Key][pos.Value] = null;

                battle.heroMap[pos.Key][posY] = this;

                pos = new KeyValuePair<int, int>(pos.Key, posY);
            }
        }

        internal void Attack()
        {
            for (int i = 0; i < sds.GetStopAttackPos().Length; i++)
            {
                KeyValuePair<int, int> pair = sds.GetStopAttackPos()[i];

                int x = pos.Key + pair.Key * fix;

                if (x < 0 || x >= BattleConst.mapHeight)
                {
                    continue;
                }

                int y = pos.Value + pair.Value * fix;

                if (y < 0 || y >= BattleConst.mapWidth)
                {
                    continue;
                }

                Hero tmpHero = battle.heroMap[x][y];

                if (tmpHero != null && tmpHero.isMine != isMine)
                {
                    return;
                }
            }

            Hero targetHero = null;

            for (int i = 0; i < sds.GetTargetPos().Length; i++)
            {
                KeyValuePair<int, int> pair = sds.GetTargetPos()[i];

                int x = pos.Key + pair.Key * fix;

                if (x < 0 || x >= BattleConst.mapHeight)
                {
                    continue;
                }

                int y = pos.Value + pair.Value * fix;

                if (y < 0 || y >= BattleConst.mapWidth)
                {
                    continue;
                }

                Hero tmpHero = battle.heroMap[x][y];

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

                    int x = targetHero.pos.Key + pair.Key * fix;

                    if (x < 0 || x >= BattleConst.mapHeight)
                    {
                        continue;
                    }

                    int y = targetHero.pos.Value + pair.Value * fix;

                    if (y < 0 || y >= BattleConst.mapWidth)
                    {
                        continue;
                    }

                    Hero tmpHero = battle.heroMap[x][y];

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
    }
}
