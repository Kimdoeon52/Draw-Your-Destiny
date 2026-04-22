namespace NYH.BattleCardSystem
{
    /// <summary>
    /// 범위 효과가 실제로 적용될 유닛 팀을 판정합니다.
    /// 공격/회복 이펙트가 같은 필터 규칙을 공유하도록 한곳에 모았습니다.
    /// </summary>
    internal static class BattleUnitTargetFilterUtility
    {
        public static bool Matches(BattleUnit sourceUnit, BattleUnit targetUnit, BattleUnitTargetFilter filter)
        {
            if (sourceUnit == null || targetUnit == null)
            {
                return false;
            }

            return filter switch
            {
                BattleUnitTargetFilter.AlliesOnly => targetUnit.Team == sourceUnit.Team,
                BattleUnitTargetFilter.AllUnits => true,
                _ => targetUnit.Team != sourceUnit.Team,
            };
        }
    }
}
