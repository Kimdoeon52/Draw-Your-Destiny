using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class ZeroCostHandEffect : Effect
{
    [Header("변경 시킬 손패의 카드의 코스트")]
    [SerializeField] private int handCostAmount;
    [Header("변경 시킬 손패의 카드 수")]
    [SerializeField] private int handCardAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        ZeroCostHandGA zeroCostHandGA = new(handCostAmount, handCardAmount);
        return zeroCostHandGA;
    }
    
    public override Dictionary<string, string> GetDescriptionTokens()
    {
        return new Dictionary<string, string>
        {
            { "handCostAmount", handCostAmount.ToString() },
            { "handCardAmount", handCardAmount.ToString() }
        };
    }
}
