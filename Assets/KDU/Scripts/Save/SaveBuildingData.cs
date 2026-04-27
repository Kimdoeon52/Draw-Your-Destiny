using System;

// ============================================================
// SaveBuildingData — 건물 1개의 저장용 데이터
//
// BuildingInstance의 런타임 상태를 JSON 직렬화 가능한 형태로 변환.
// SO 참조(BuildingData)는 문자열 id로 치환.
// footprint는 origin + SO의 width/height로 재계산 가능하므로 저장하지 않음.
// visual/behaviour는 런타임 전용이므로 저장하지 않음.
// ============================================================
[Serializable]
public class SaveBuildingData
{
    // BuildingData SO의 id 필드 — 로드 시 딕셔너리로 SO 복원
    public string buildingDataId;

    // 건물 기준점 (Vector3Int를 분리 저장)
    public int originX;
    public int originY;
    public int originZ;

    // 소유 문명
    public int ownerCivID;

    // isUniqueGlobal 건물의 활성화 여부
    public bool isActive;

    // 잔해 상태 (전투 승리 후 점거된 건물)
    public bool isRuin;

    // populationCapBonus가 GameManager에 적용됐는지 여부 (이중 적용 방지)
    public bool populationCapApplied;

    // 건물 런타임 상태 (tick, activeCount, waiting 등)
    public BuildingRuntimeState savedState = new BuildingRuntimeState();
}
