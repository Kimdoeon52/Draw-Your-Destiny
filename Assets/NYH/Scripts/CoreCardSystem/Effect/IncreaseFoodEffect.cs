using NYH.CoreCardSystem;
using UnityEngine;

public class IncreaseFoodEffect : Effect
{
    [Header("추가할 식량 수")]
    [SerializeField] private int amount;
    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new IncreaseFoodGA(amount);
    }
}
