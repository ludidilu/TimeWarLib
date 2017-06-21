using System.Collections.Generic;

public enum SplashType
{
    NO_SPLASH,
    SPLASH_OTHER_TARGETS,
    SPLASH_TARGET_POS
}

public interface IHeroSDS
{
    int GetCost();
    int GetHp();
    int GetAttack();
    int GetMove();
    KeyValuePair<int, int>[] GetStopAttackPos();
    KeyValuePair<int, int>[] GetTargetPos();
    SplashType GetSplashType();
    KeyValuePair<int, int>[] GetSplashPos();
}
