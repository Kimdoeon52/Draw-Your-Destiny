using NYH.CoreCardSystem;
using UnityEngine;

public class IncreasePopulationffect : Effect
{
    [Header("증가 시킬 인구 한도")]
    [SerializeField] private int increasePopulationGA;

    public override GameAction GetGameAction()
    {
        return new IncreasePopulationGA(increasePopulationGA);
    }
}