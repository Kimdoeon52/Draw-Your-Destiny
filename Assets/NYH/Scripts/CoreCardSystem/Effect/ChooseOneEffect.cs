using System.Collections.Generic;
using NYH.CoreCardSystem;
using UnityEngine;

public class ChooseOneEffect : Effect
{
    [Header("선택을 띄울 카드의 수")]
    [SerializeField] private int choseOneAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        int finalAmount = CardModifierSystem.Apply(sourceCard, choseOneAmount);
        return new ChooseOneGA(finalAmount);
    }

    public override Dictionary<string, string> GetDescriptionTokens(Card sourceCard)
    {
        return new Dictionary<string, string>
        {
            { "choseOneAmount", CardModifierSystem.Apply(sourceCard, choseOneAmount).ToString() }
        };
    }
}