using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class ResearchpointsEffect : Effect
{
    [Header("증가 시킬 연구 포인트")]
    [SerializeField] private int resarchPointAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new ResearchpointsGA(resarchPointAmount);
    }

    public override Dictionary<string, string> GetDescriptionTokens()
    {
        return new Dictionary<string, string>
        {
            { "resarchPointAmount", resarchPointAmount.ToString() }
        };
    }

}