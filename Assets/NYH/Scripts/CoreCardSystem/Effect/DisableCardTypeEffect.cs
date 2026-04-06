using NYH.CoreCardSystem;
using UnityEngine;
using System.Collections.Generic;

public class DisableCardTypeEffect : Effect
{
    [Header("비활성화 시킬 카드 타입")]
    [SerializeField] private CardType targetType;
    [Header("비활성화 지속 턴 수")]
    [SerializeField] private int durationTurns = 1;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new DisableCardTypeGA(targetType, durationTurns);
    }

    public override Dictionary<string, string> GetDescriptionTokens(Card sourceCard)
    {
        return new Dictionary<string, string>
        {
            { "targetType", targetType.ToString() },
            { "durationTurns", durationTurns.ToString() }
        };
    }
}
