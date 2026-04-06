using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class GoldCardEffect : Effect
{
    [Header("획득할 골드 수")]
    [SerializeField] private int costAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        int finalAmount = CardModifierSystem.Apply(sourceCard, costAmount);
        return new GoldCardGA(finalAmount);
    }

    public override Dictionary<string, string> GetDescriptionTokens(Card sourceCard)
    {
        return new Dictionary<string, string>
        {
            { "costAmount", CardModifierSystem.Apply(sourceCard, costAmount).ToString() }
        };
    }
}
