using NYH.CoreCardSystem;
using UnityEngine;

public class AddResearchByCurResearchEffect : Effect
{
    [Header("추가할 연구포인트 양")]
    [SerializeField] private int amount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new AddResearchByCurResearchGA(amount);
    }
}
