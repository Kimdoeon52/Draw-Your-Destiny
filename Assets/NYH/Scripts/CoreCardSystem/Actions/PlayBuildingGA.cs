using NYH.CoreCardSystem;
using UnityEngine;

// [PlayBuildingGA.cs]
public class PlayBuildingGA : GameAction
{
    public BuildingData Data { get; private set; }
    public Vector3Int TargetPos { get; private set; }

    public PlayBuildingGA(BuildingData data, Vector3Int pos)
    {
        this.Data = data;
        this.TargetPos = pos;
    }

    // 이 안에서 실제 TileMapManager를 호출하여 건물을 짓는 로직을 작성합니다.
    // (지금 ActionSystem 구조에 맞춰서 실행 메서드를 오버라이드하게 됩니다.)
}