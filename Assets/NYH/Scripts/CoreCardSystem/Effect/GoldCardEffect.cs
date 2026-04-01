using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class GoldCardEffect : Effect
{
    [Header("획득할 골드 수")]
    [SerializeField] private int costAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new GoldCardGA(costAmount);
    }
   public override Dictionary<string, string> GetDescriptionTokens()
    {
        return new Dictionary<string, string>
        {
            { "costAmount", costAmount.ToString() }
        };
    }

}

