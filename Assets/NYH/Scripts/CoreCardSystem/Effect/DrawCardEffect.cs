using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class DrawCardEffect : Effect
{
    [Header("드로우할 카드의 수")]
    [SerializeField] private int drawAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        DrawCardsGA drawCardsGA = new(drawAmount);
        return drawCardsGA;
    }

    public override Dictionary<string, string> GetDescriptionTokens()
    {
        return new Dictionary<string, string>
        {
            { "drawAmount", drawAmount.ToString() }
        };
    }
}
