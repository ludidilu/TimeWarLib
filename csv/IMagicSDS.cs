using System.Collections.Generic;

public interface IMagicSDS
{
    int GetCost();
    bool GetTargetEnemy();
    KeyValuePair<int, int>[] GetTargetPos();
    SplashType GetSplashType();
    KeyValuePair<int, int>[] GetSplashPos();
}
