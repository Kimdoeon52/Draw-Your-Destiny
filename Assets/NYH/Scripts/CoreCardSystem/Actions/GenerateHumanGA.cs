using NYH.CoreCardSystem;
using UnityEngine;

public class GenerateHumanGA : GameAction
{
    public PlayerUnitInfoByJob _UnitInfo { get; private set; }
    public int Amount { get; private set; }

    public GenerateHumanGA(PlayerUnitInfoByJob unitInfo, int amount)
    {
        _UnitInfo = unitInfo;
        Amount = amount;
    }
}
