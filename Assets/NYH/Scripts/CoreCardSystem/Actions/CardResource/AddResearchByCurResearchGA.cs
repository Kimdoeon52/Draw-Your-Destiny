using NYH.CoreCardSystem;
using UnityEngine;

public class AddResearchByCurResearchGA : GameAction
{
    public int Amount { get; private set; }

    public AddResearchByCurResearchGA(int amount)
    {
        Amount = amount;
    }
}
