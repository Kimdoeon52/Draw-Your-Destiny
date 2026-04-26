using NYH.CoreCardSystem;
using UnityEngine;

public class ShowNextCardGA : GameAction
{
    public int CardAmount { get;private set; }
    public ShowNextCardGA(int cardAmount)
    {
        CardAmount = cardAmount;
    }
}
