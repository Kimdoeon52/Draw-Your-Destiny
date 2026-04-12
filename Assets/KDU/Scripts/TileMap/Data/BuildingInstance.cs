using System.Collections.Generic;
using UnityEngine;

// ============================================================
// BuildingInstance — 맵에 실제로 배치된 건물 1개의 런타임 상태
//
// BuildingData(ScriptableObject)가 "설계도"라면,
// BuildingInstance는 "실제로 놓인 건물 한 채"다.
//
// TileMapManager에서 관리:
//   allBuildings          — 전체 건물 리스트
//   buildingInstanceMap   — 타일 좌표 → 인스턴스 빠른 조회용 Dictionary
// ============================================================
public class BuildingInstance
{
    // 이 건물의 설계도 (크기, 비용, 효과 등 정적 데이터)
    // 시대 업그레이드 시 새 BuildingData로 교체됨
    public BuildingData data;

    // StarCraft 방식 기준점 (좌하단 모서리 또는 중앙, 크기에 따라 다름)
    // buildingInstanceMap에서 origin 기준으로 조회
    public Vector3Int origin;

    // 건물이 차지하는 모든 타일 좌표 목록 (1×1이면 1개, 2×2이면 4개 등)
    // 충돌 검사, 건물 철거 시 이 목록으로 순회
    public List<Vector3Int> footprint;

    // 건물 소유 문명 ID (0=플레이어, 1~3=AI, -1=중립)
    public int ownerCivID;

    // 화면에 표시되는 스프라이트 오브젝트 (SpriteRenderer 포함)
    public GameObject visual;

    // 현재 건물 타입 — data가 null이면 None 반환
    public BuildingType type => data != null ? data.buildingType : BuildingType.None;
}
