namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;

    /// <summary>
    /// 배틀 카드가 특정 병종 유닛에게 사용 가능한지 판단합니다.
    /// 카드의 허용 병종 목록이 비어 있으면 모든 병종이 사용할 수 있습니다.
    /// </summary>
    internal static class BattleCardUnitTypeRestriction
    {
        public static bool CanUserUnitPlay(BattleCard battleCard, BattleUnit userUnit)
        {
            if (battleCard == null || userUnit == null)
            {
                return false;
            }

            IReadOnlyList<UnitType> allowedTypes = battleCard.AllowedUserUnitTypes;
            if (allowedTypes == null || allowedTypes.Count == 0)
            {
                return true;
            }

            UnitType unitType = userUnit.UnitType;
            for (int i = 0; i < allowedTypes.Count; i++)
            {
                if (allowedTypes[i] == unitType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
