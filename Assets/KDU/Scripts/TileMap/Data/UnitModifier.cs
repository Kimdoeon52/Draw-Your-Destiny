// ============================================================
// UnitModifier — 유닛 보정값 1건
//
// 카드/건물/시대 효과 등으로 부여되는 스탯 보정.
// UnitDatabase.AddModifier()로 등록, RemoveModifier()로 해제.
//
// 사용법 (카드 팀원):
//   var mod = new UnitModifier { bonusAttack = 3, source = "전사의 격려" };
//   UnitDatabase.Instance.AddModifier(civID, UnitType.RockWarrior, mod);
//   // 해제 시
//   UnitDatabase.Instance.RemoveModifier(civID, UnitType.RockWarrior, mod);
// ============================================================
[System.Serializable]
public class UnitModifier
{
    public int bonusAttack;
    public int bonusDefense;
    public int bonusHP;

    // 어디서 건 버프인지 식별용 (디버깅 + 중복 방지)
    public string source;
}
