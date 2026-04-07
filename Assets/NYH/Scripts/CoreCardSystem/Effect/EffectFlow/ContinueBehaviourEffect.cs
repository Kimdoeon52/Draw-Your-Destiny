using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class ContinueBehaviourEffect : Effect
{
    [Header("반복할 턴 수")]
    [SerializeField] private int turnAmount;

    public override GameAction GetGameAction(int effectIndex, Card sourceCard)
    {
        int finalAmount = CardModifierSystem.Apply(sourceCard, turnAmount);
        return new ContinueBehaviourGA(sourceCard, effectIndex + 1, finalAmount);
    }

    public override Dictionary<string, string> GetDescriptionTokens(Card sourceCard)
    {
        return new Dictionary<string, string>
        {
            { "turnAmount", CardModifierSystem.Apply(sourceCard, turnAmount).ToString() }
        };
    }
}
