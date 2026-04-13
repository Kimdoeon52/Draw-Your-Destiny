using System.Collections.Generic;
using NYH.CoreCardSystem;
using UnityEngine;

public class ContinueBehaviourByBuildingEffect : Effect
{
    [Header("받을 건물 타입")]
    [SerializeField] private BuildingType buildingType;

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new ContinueBehaviourByBuildingGA(sourceCard, effectIndex + 1, buildingType);
    }
}
