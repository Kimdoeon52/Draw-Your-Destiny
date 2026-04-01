using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class ConvertGoldToFoodEffect : Effect
{
	[Header("바꿀 퍼센트")]
	[SerializeField] private int percentAmount;

	public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
	{
		return new ConvertGoldToFoodGA(percentAmount);
	}

	public override Dictionary<string, string> GetDescriptionTokens()
    {
        return new Dictionary<string, string>
        {
            { "percentAmount", percentAmount.ToString() }
        };
    }
	
}
