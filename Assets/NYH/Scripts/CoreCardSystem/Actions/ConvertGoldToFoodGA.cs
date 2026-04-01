using NYH.CoreCardSystem;
using UnityEngine;

public class ConvertGoldToFoodGA : GameAction
{
    public int Percent { get; private set; }

    public ConvertGoldToFoodGA(int percent)
    {
        Percent = percent;
    }
}
