using UnityEngine;

// ============================================================
// TileData — 타일 1칸의 런타임 상태를 저장하는 데이터 클래스
//
// TileMapManager.tileDataMap(Dictionary<Vector3Int, TileData>)에
// 타일 좌표를 키로 저장되어 있다.
// 씬 시작 시 모든 지형 Tilemap을 순회해 자동 생성됨.
// ============================================================
[System.Serializable]
public class TileData
{
    // 타일 지형 종류 (Plain, River, Farmland, Resource, City)
    public TileType type;

    // 이 타일을 점령한 문명 ID
    // -1 = 미점령 / 0 = 플레이어 / 1~3 = AI
    public int ownerCivID;

    // 이 타일 위에 있는 건물 타입 (None = 건물 없음)
    // BuildingInstance와 중복 추적되지만 빠른 타입 조회용으로 유지
    public BuildingType building;

    // 기본값: Plain, 미점령, 건물 없음
    public TileData(TileType tileType = TileType.Plain, int owner = -1)
    {
        type       = tileType;
        ownerCivID = owner;
        building   = BuildingType.None;
    }
}
