using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class IncreasePopulationffect : Effect
{
    [Header("증가 시킬 인구 한도")]
    [SerializeField] private int increasePopulationAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new IncreasePopulationGA(increasePopulationAmount);
    }

    public override Dictionary<string, string> GetDescriptionTokens()
    {
        return new Dictionary<string, string>
        {
            { "increasePopulationAmount", increasePopulationAmount.ToString() }
        };
    }
}