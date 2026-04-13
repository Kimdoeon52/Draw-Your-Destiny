using NYH.CoreCardSystem;
using UnityEngine;

public class ContinueBehaviourByBuildingGA : GameAction
{
    private BuildingType buildingType;

    public Card SourceCard { get; }
    public int StartEffectIndex { get; }
    public BuildingType TargetBuildingType { get; }

    public ContinueBehaviourByBuildingGA(Card sourceCard, int startEffectIndex, BuildingType targetBuildingType)
    {
        SourceCard = sourceCard;
        StartEffectIndex = startEffectIndex;
        TargetBuildingType = targetBuildingType;
    }
}
