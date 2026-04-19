using System;

// ============================================================
// SaveUnitModifierEntry — 유닛 보정값 1건의 저장용 데이터
//
// UnitDatabase의 modifiers 딕셔너리 구조를 JSON 직렬화 가능하게 평탄화.
// civID + unitType + UnitModifier 필드를 한 레코드에 저장.
// ============================================================
[Serializable]
public class SaveUnitModifierEntry
{
    public int civID;
    public UnitType unitType;

    public int bonusAttack;
    public int bonusDefense;
    public int bonusHP;
    public string source;
}
