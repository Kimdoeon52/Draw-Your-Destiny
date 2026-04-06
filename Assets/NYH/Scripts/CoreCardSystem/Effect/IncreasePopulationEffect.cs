using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class IncreasePopulationffect : Effect
{
    [Header("利앷? ?쒗궗 ?멸뎄 ?쒕룄")]
    [SerializeField] private int increasePopulationAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        int finalAmount = CardModifierSystem.Apply(sourceCard, increasePopulationAmount);
        return new IncreasePopulationGA(finalAmount);
    }

    public override Dictionary<string, string> GetDescriptionTokens(Card sourceCard)
    {
        return new Dictionary<string, string>
        {
            { "increasePopulationAmount", CardModifierSystem.Apply(sourceCard, increasePopulationAmount).ToString() }
        };
    }
}