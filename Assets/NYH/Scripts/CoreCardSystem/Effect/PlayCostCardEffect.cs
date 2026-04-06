using NYH.CoreCardSystem;
using UnityEngine;

public class PlayCostCardEffect : Effect
{
    [Header("카드 사용시 없앨 골드")]
    [SerializeField] private int playCardCost;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        int actualCostToPay = (sourceCard != null) ? sourceCard.Cost : playCardCost;

        PlayCostGA playCostGA = new(actualCostToPay);
        return playCostGA;
    }
}
