using NYH.CoreCardSystem;
using UnityEngine;

public class AddCardGA : GameAction
{
    public CardData SourceCard { get; private set; }
    public int AddAmount { get; private set; }

    public AddCardGA(CardData sourceCard, int addAmount)
    {
        SourceCard = sourceCard;
        AddAmount = addAmount;
    }
}
