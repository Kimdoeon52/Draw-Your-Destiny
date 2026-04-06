using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class ConvertGoldToFoodEffect : Effect
{
	[Header("바꿀 퍼센트")]
	[SerializeField] private int percentAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        int finalAmount = CardModifierSystem.Apply(sourceCard, percentAmount);
        return new ConvertGoldToFoodGA(finalAmount);
    }

    public override Dictionary<string, string> GetDescriptionTokens(Card sourceCard)
    {
        return new Dictionary<string, string>
        {
            { "percentAmount", CardModifierSystem.Apply(sourceCard, percentAmount).ToString() }
        };
    }
}
