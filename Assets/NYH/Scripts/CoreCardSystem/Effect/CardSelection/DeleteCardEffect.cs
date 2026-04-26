using NYH.CoreCardSystem;
using UnityEngine;

public class DeleteCardEffect : Effect
{
    [Header("삭제할 카드 수")]
    [SerializeField] private int cardAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new DeleteCardGA(cardAmount);
    }
}
