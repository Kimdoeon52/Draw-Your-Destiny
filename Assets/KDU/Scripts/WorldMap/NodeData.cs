using System.Collections.Generic;

// ============================================================
// NodeData — 월드맵 노드 1개의 런타임 상태
//
// WorldMapManager.allNodes 리스트에서 nodeID로 관리됨.
// 영지 진입/이탈 시 NodeDataManager가 이 데이터를 기반으로
// 타일맵 복원 및 건물 저장·복원을 처리한다.
// ============================================================
[System.Serializable]
public class NodeData
{
    public int nodeID;

    // 인접 노드 ID 목록 — 공격 선언 및 이동 가능 여부 판단에 사용
    public List<int> adjacentNodeIDs = new List<int>();

    // 소유 문명 ID
    // -1 = 빈 노드 / 0 = 플레이어 / 1~3 = AI
    public int ownerCivID = -1;

    // 영주성 재건 여부
    // false이면 영지 진입 후 배치 UI 전체 잠금
    public bool isMansionBuilt = false;

    // 플레이어 유닛 주둔 여부
    // ownerCivID와 무관하게 유닛이 있으면 영지 진입 가능
    // 전투 승리 시 true, 유닛 철수 시 false
    public bool hasPlayerUnits = false;

    // 노드 보유 자원
    public int gold = 0;
    public int food = 0;
    public int research = 0;

    public List<NodeUnit> units = new List<NodeUnit>();

    // 현재 이 노드에 배치된 건물 목록
    // 영지 이탈 시 TileMapManager의 allBuildings가 여기에 저장됨
    // visual 필드는 이탈 시 null로 클리어됨 (재진입 시 재생성)
    public List<BuildingInstance> buildings = new List<BuildingInstance>();

    // AI 영지 상태 (영지 단위로 누적되는 수치)
    public int farmCount = 0;
    public int maxHuman = 10;

    // 플레이어 영지 인구 수용량 (0~50). House/Mansion 등이 누적시킴.
    // 전역 인구 한도(ResourceManager.maxPopulation) 기여도는 min(capacity, 20).
    // 즉 0~20 구간은 전역 한도와 노드 수용량 둘 다 늘어나고, 20 초과분은 노드 수용량만 늘어남.
    public int playerPopulationCapacity = 0;

    // 이 노드의 노화(Old) 상태 유닛 수.
    // HumanBase.OnBecomeOld 발생 시 GameManager가 +1, OnBecomeRemoved(노화 유닛 풀 반환) 시 -1.
    public int oldUnitCount = 0;
}
