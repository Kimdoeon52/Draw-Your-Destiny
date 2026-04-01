using NYH.CoreCardSystem;
using UnityEngine;

public class ConvertGoldToFoodEffect : Effect
{
	[Header("바꿀 퍼센트")]
	[SerializeField] private int percent;

	public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
	{
		return new ConvertGoldToFoodGA(percent);
	}
}
