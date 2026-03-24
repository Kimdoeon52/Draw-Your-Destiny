using System.Collections.Generic;
using UnityEngine;

// 배치된 건물 인스턴스 — 건물 하나당 1개
public class BuildingInstance
{
    public BuildingData data;           // 현재 BuildingData (업그레이드 시 교체됨)
    public Vector3Int origin;           // GetOrigin 기준점
    public List<Vector3Int> footprint;  // 건물이 차지하는 모든 타일
    public int ownerCivID;
    public bool wasEverSeen;            // 플레이어가 Visible 상태에서 본 적 있는지
    public GameObject visual;           // SpriteRenderer 참조용

    // 편의 프로퍼티
    public BuildingType type => data != null ? data.buildingType : BuildingType.None;
}
