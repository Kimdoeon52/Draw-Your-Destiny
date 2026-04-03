using NYH.CoreCardSystem;
using UnityEngine;

// [PlayBuildingGA.cs]
public class PlayBuildingGA : GameAction
{
    public Card SourceCard { get; private set; }
    public BuildingData Data { get; private set; }
    public Vector3Int TargetPos { get; private set; }
    public bool IsTargetingMode { get; private set; }

    public PlayBuildingGA(Card sourceCard, BuildingData data, Vector3Int pos, bool isTargetingMode)
    {
        SourceCard = sourceCard;
        Data = data;
        TargetPos = pos;
        IsTargetingMode = isTargetingMode;
    }
}
