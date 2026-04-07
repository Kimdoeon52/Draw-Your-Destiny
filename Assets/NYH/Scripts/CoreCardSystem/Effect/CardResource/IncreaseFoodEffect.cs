using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class IncreaseFoodEffect : Effect
{
    [Header("추가할 식량 수")]
    [SerializeField] private int foodAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        int finalAmount = CardModifierSystem.Apply(sourceCard, foodAmount);
        return new IncreaseFoodGA(finalAmount);
    }

    public override Dictionary<string, string> GetDescriptionTokens(Card sourceCard)
    {
        return new Dictionary<string, string>
        {
            { "foodAmount", CardModifierSystem.Apply(sourceCard, foodAmount).ToString() }
        };
    }
}
