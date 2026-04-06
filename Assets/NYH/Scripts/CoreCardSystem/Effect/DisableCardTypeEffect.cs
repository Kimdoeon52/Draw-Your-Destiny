using NYH.CoreCardSystem;
using UnityEngine;
using System.Collections.Generic;

public class DisableCardTypeEffect : Effect
{
    [SerializeField] private CardType targetType;
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
