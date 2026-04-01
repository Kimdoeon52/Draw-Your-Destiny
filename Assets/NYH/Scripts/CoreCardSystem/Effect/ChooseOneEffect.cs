using System.Collections.Generic;
using NYH.CoreCardSystem;
using UnityEngine;

public class ChooseOneEffect : Effect
{
    [Header("선택을 띄울 카드의 수")]
    [SerializeField] private int choseOneAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new ChooseOneGA(choseOneAmount);
    }

    public override Dictionary<string, string> GetDescriptionTokens()
    {
        return new Dictionary<string, string>
        {
            { "choseOneAmount", choseOneAmount.ToString() }
        };
    }
}