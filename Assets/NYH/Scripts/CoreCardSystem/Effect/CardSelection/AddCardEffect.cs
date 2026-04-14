using NYH.CoreCardSystem;
using UnityEngine;

public class AddCardEffect : Effect
{
    [Header("추가할 카드")]
    [SerializeField] CardData cardData;
    [Header("추가할 카드의 수")]
    [SerializeField] int addAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new AddCardGA(cardData, addAmount);
    }
}
