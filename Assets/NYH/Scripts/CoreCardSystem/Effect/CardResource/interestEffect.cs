using NYH.CoreCardSystem;
using UnityEngine;

public class interestEffect : Effect
{
    [Header("이번 턴에 잃을 골드")]
    [SerializeField] private int minusGold;
    [Header("돌려받을 턴 수")]
    [SerializeField] private int turnAmount;
    [Header("돌려받을 골드 수")]
    [SerializeField] private int plusGold;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new interestGA(minusGold, turnAmount, plusGold);
    }
}
