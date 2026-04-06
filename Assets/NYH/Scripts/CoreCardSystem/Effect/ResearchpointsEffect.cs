using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class ResearchpointsEffect : Effect
{
    [Header("증가 시킬 연구 포인트")]
    [SerializeField] private int resarchPointAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        int finalAmount = CardModifierSystem.Apply(sourceCard, resarchPointAmount);
        return new ResearchpointsGA(finalAmount);
    }

    public override Dictionary<string, string> GetDescriptionTokens(Card sourceCard)
    {
        return new Dictionary<string, string>
        {
            { "resarchPointAmount", CardModifierSystem.Apply(sourceCard, resarchPointAmount).ToString() }
        };
    }
}
