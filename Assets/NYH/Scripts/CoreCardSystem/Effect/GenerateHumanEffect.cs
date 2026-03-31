using UnityEngine;
using NYH.CoreCardSystem;

public class GenerateHumanEffect : Effect
{
    [Header("생성할 유닛 정보")]
    [SerializeField] private PlayerUnitInfoByJob unitInfo;
    [Header("생성할 수")]
    [SerializeField] private int amount;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new GenerateHumanGA(unitInfo, amount);
    }
}
