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
        return new GenerateHumanGA(unitInfo, unitAmount);
    }

        public override Dictionary<string, string> GetDescriptionTokens()
    {
        return new Dictionary<string, string>
        {
            { "unitAmount", unitAmount.ToString() },
            { "unitInfo", unitInfo.ToString() }
        };
    }

}
