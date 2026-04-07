using NYH.CoreCardSystem;
using UnityEngine;

public class CountCardByTypeGA : GameAction
{
	public CardType _CardType { get; private set; }

	public CountCardByTypeGA(CardType cardType)
	{
		_CardType = cardType;
	}
}
