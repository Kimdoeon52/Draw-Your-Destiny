using NYH.CoreCardSystem;
using UnityEngine;

public class ReturnToHandGA : GameAction
{
    public Card TargetCard { get; private set; }

    public ReturnToHandGA(Card targetCard) 
    {
        TargetCard = targetCard;
    }
}
