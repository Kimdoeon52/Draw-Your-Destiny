using NYH.CoreCardSystem;
using UnityEngine;

public class IncreaseFoodGA : GameAction
{
    public int Amount { get; private set; }

    public IncreaseFoodGA(int amount)
    {
        Amount = amount;
    }
}
