using NYH.CoreCardSystem;
using UnityEngine;

public class interestGA : GameAction
{
    public int MinusGold { get; private set; }
    public int TurnAmount { get; private set; }
    public int PlusGold { get; private set; }

    public interestGA(int minusGold, int turnAmount, int plusGold)
    {
        MinusGold = minusGold;
        TurnAmount = turnAmount;
        PlusGold = plusGold;
    }
}
