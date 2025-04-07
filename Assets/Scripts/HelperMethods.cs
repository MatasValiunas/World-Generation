using UnityEngine;

public static class HelperMethods
{
    public static void SetRandomizerSeed(int seed = 0)
    {
        if (seed == 0)
        {
            Random.InitState(System.Environment.TickCount);     // random seed
        }
        else
        {
            Random.InitState(seed);
        }
    }
}