using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class DiscardRandomEffect : Effect
{
    [Header("랜덤으로 버릴 장수")]
    [SerializeField] private int discardAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        int finalAmount = CardModifierSystem.Apply(sourceCard, discardAmount);
        return new DiscardRandomGA(finalAmount);
    }

    public override Dictionary<string, string> GetDescriptionTokens(Card sourceCard)
    {
        return new Dictionary<string, string>
        {
            { "discardAmount", CardModifierSystem.Apply(sourceCard, discardAmount).ToString() }
        };
    }
}
