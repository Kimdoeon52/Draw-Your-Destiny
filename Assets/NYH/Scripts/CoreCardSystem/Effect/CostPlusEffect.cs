using NYH.CoreCardSystem;
using UnityEngine;

public class CostPlusEffect : Effect
{
	[Header("바뀔 코스트 수")]
	[SerializeField] private int costAmount;

	public override GameAction GetGameAction(int effectIndex, Card sourceCard)
	{
		return new CostPlusGA(sourceCard, costAmount);
	}
}
