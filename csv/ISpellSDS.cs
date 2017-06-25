using System.Collections.Generic;

public interface ISpellSDS
{
    bool GetTargetEnemy();
    KeyValuePair<int, int>[] GetTargetPos();
    SplashType GetSplashType();
    KeyValuePair<int, int>[] GetSplashPos();
}
