using NYH.CoreCardSystem;
using UnityEngine;

public class CostPlusGA : GameAction
{
	public Card SourceCard { get; set; }
	public int Cost { get; private set; }

	public CostPlusGA(Card sourceCard, int cost)
	{
		SourceCard = sourceCard;
		Cost = cost;
	}
}
