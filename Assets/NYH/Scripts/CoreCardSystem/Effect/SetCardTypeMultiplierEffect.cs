using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class SetCardTypeMultiplierEffect : Effect
{
    [Header("배율을 적용할 카드 타입")]
    [SerializeField] private CardType targetType = CardType.Money;

    [Header("적용할 배율")]
    [SerializeField] private float multiplier = 2f;

    [Header("지속 턴 수 (-1이면 무제한)")]
    [SerializeField] private int durationTurns = 1;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new SetCardTypeMultiplierGA(targetType, multiplier, durationTurns);
    }

    public override Dictionary<string, string> GetDescriptionTokens(Card sourceCard)
    {
        return new Dictionary<string, string>
        {
            { "targetType", targetType.ToString() },
            { "multiplier", multiplier.ToString("0.##") },
            { "durationTurns", durationTurns.ToString() }
        };
    }
}