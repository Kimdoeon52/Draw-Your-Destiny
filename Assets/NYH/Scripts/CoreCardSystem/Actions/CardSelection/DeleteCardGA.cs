using NYH.CoreCardSystem;
using UnityEngine;

public class DeleteCardGA : GameAction
{
    public int CardAmount { get; private set; }

    public DeleteCardGA(int cardAmount)
    {
        CardAmount = cardAmount;
    }
}
