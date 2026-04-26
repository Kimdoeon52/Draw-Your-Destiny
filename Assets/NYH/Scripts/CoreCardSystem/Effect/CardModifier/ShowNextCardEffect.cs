using NYH.CoreCardSystem;
using UnityEngine;

public class ShowNextCardEffect : Effect
{
    [Header("볼 카드의 수")]
    [SerializeField] private int cardAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new ShowNextCardGA(cardAmount);
    }
}
