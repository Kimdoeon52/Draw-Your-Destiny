using NYH.CoreCardSystem;
using UnityEngine;

public class CountCardByTypeEffect : Effect
{
	[Header("찾을 카드 타입")]
	[SerializeField] CardType cardType;

	public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
	{
		return new CountCardByTypeGA(cardType);
	}
}
