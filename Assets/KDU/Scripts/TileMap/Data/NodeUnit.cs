// ============================================================
// NodeUnit — 노드에 주둔 중인 유닛 부대 1개의 런타임 상태
//
// NodeData.units 리스트에 저장됨.
// 건물 생산 시 count++, 전투 후 생존 인원으로 count 갱신.
//
// 스탯 조회: UnitDatabase.Instance.GetStat(unitType)
// 부대 체력: stat.maxHP * count
// ============================================================
[System.Serializable]
public class NodeUnit
{
    public UnitType unitType;
    public int count;
}
