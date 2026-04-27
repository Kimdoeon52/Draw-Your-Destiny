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

    // visual에 부착된 BuildingBehaviour 캐시 (프리팹 기반 건물만 존재, 빈 GO 방식은 null)
    [System.NonSerialized]
    public BuildingBehaviour behaviour;

    // 노드 이탈 시 저장되는 런타임 상태 스냅샷 (재진입 시 LoadState로 복원)
    public BuildingRuntimeState savedState = new BuildingRuntimeState();

    // isUniqueGlobal 건물(Lab 등)이 중복 보유될 때 활성화 여부
    // WorldMapManager.RebalanceUniqueGlobalBuildings()가 소유권 변경 시마다 재조정
    // false = 건물은 존재하지만 효과를 내지 않음 (플레이어가 같은 타입 건물을 이미 다른 노드에 보유 중)
    public bool isActive = true;

    // 잔해 상태 — 전투 승리로 노드 점거 후 일부 건물(Mansion/House/Market/Farm)이 남는 상태.
    // 잔해는 효과/생산 정지(isActive=false)로 처리, 전체 재건 카드 호출 시 false로 복구.
    public bool isRuin = false;

    // populationCapBonus가 한 번이라도 GameManager에 적용됐는지 추적.
    // PlaceBuilding 또는 RestoreCapturedNode에서 적용 후 true로 설정.
    // 잔해 -> 재건 시 이미 true면 재적용하지 않음 (이중 적용 방지).
    public bool populationCapApplied = false;

    // 현재 건물 타입 — data가 null이면 None 반환
    public BuildingType type => data != null ? data.buildingType : BuildingType.None;
}
