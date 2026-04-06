using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class GenerateHumanEffect : Effect
{
    [Header("생성할 유닛 정보")]
    [SerializeField] private PlayerUnitInfoByJob unitInfo;
    [Header("생성할 수")]
    [SerializeField] private int unitAmount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        int finalAmount = CardModifierSystem.Apply(sourceCard, unitAmount);
        return new GenerateHumanGA(unitInfo, finalAmount);
    }

    public override Dictionary<string, string> GetDescriptionTokens(Card sourceCard)
    {
        return new Dictionary<string, string>
        {
            { "unitAmount", CardModifierSystem.Apply(sourceCard, unitAmount).ToString() },
            { "unitInfo", unitInfo != null ? unitInfo.job.ToString() : string.Empty }
        };
    }
}
