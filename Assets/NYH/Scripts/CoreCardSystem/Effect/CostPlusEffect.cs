using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class CostPlusEffect : Effect
{
    [Header("바뀔 코스트 수")]
    [SerializeField] private int costAmount;

    public override GameAction GetGameAction(int effectIndex, Card sourceCard)
    {
        int finalAmount = CardModifierSystem.Apply(sourceCard, costAmount);
        return new CostPlusGA(sourceCard, finalAmount);
    }

    public override Dictionary<string, string> GetDescriptionTokens(Card sourceCard)
    {
        return new Dictionary<string, string>
        {
            { "costAmount", CardModifierSystem.Apply(sourceCard, costAmount).ToString() }
        };
    }
}
