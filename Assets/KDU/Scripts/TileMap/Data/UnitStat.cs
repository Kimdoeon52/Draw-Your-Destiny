// ============================================================
// UnitStat — 유닛 1종의 기본 스탯 (순수 데이터 클래스)
//
// ScriptableObject 대신 [Serializable] class로 구현.
// → JsonUtility.ToJson() / FromJson() 직접 가능.
// → UnitDatabase의 Inspector 리스트에서 편집.
//
// 런타임에 수정하지 않는다 — readonly 조회 전용.
// ============================================================
[System.Serializable]
public class UnitStat
{
    public string unitName;
    public UnitType unitType;
    public int baseAttack;
    public int baseDefense;
    public int maxHP;           // 유닛 1명 기본 체력
    public Era requiredEra;
}
