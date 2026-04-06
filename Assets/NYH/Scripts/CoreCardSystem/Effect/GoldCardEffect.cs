using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class GoldCardEffect : Effect
{
    [Header("È¹µæÇÒ °ñµå ¼ö")]
    [SerializeField] private int costAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        int finalAmount = CardModifierSystem.Apply(sourceCard, costAmount);
        return new GoldCardGA(finalAmount);
    }

    public override Dictionary<string, string> GetDescriptionTokens()
    {
        return new Dictionary<string, string>
        {
            { "costAmount", costAmount.ToString() }
        };
    }
}
