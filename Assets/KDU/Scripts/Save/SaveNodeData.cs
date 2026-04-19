using System;
using System.Collections.Generic;

// ============================================================
// SaveNodeData — 노드 1개의 저장용 데이터
//
// NodeData의 런타임 상태를 JSON 직렬화 가능한 형태로 변환.
// adjacentNodeIDs는 고정값이지만 안전을 위해 함께 저장.
// ============================================================
[Serializable]
public class SaveNodeData
{
    public int nodeID;
    public List<int> adjacentNodeIDs = new List<int>();

    public int ownerCivID;
    public bool isMansionBuilt;
    public bool hasPlayerUnits;

    // 노드별 자원
    public int gold;
    public int food;
    public int research;

    // 유닛 주둔 상태
    public List<NodeUnit> units = new List<NodeUnit>();

    // 건물
    public List<SaveBuildingData> buildings = new List<SaveBuildingData>();

    // AI 영지 수치
    public int farmCount;
    public int maxHuman;
}
