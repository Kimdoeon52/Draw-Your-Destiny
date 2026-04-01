using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class IncreaseFoodEffect : Effect
{
    [Header("추가할 식량 수")]
    [SerializeField] private int foodAmount;
    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new IncreaseFoodGA(foodAmount);
    }

    public override Dictionary<string, string> GetDescriptionTokens()
    {
        return new Dictionary<string, string>
        {
            { "foodAmount", foodAmount.ToString() }
        };
    }

}
