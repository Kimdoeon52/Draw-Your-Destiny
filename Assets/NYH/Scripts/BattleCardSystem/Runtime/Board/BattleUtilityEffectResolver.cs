namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 포션/덫처럼 "유닛을 선택해서 쓰는 카드"가 아닌,
    /// 전투 그리드 또는 전장 전체를 기준으로 발동하는 유틸리티 카드의
    /// 실제 영향 칸과 적용 대상 유닛을 계산합니다.
    /// </summary>
    internal static class BattleUtilityEffectResolver
    {
        /// <summary>
        /// 범위형 포션이 선택한 칸을 기준으로 어떤 칸들에 영향을 주는지 계산합니다.
        /// </summary>
        public static HashSet<Vector2Int> ResolvePotionImpactCells(BattlePotionEffect potionEffect, Vector2Int targetGrid)
        {
            HashSet<Vector2Int> result = new();
            if (potionEffect == null)
            {
                return result;
            }

            // 전체형 포션은 특정 칸을 기준으로 범위를 계산하지 않으므로 빈 집합을 반환합니다.
            if (potionEffect.TargetingType == BattlePotionTargetingType.All)
            {
                return result;
            }

            return BattleAttackImpactCellResolver.ResolveImpactCells(
                targetGrid,
                targetGrid,
                potionEffect.Range,
                potionEffect.ImpactPattern,
                potionEffect.CustomImpactPattern,
                BattleAttackPatternOriginMode.RangedPattern);
        }

        /// <summary>
        /// 포션의 설정에 따라 실제 효과를 받을 유닛 목록을 계산합니다.
        /// </summary>
        public static List<BattleUnit> ResolvePotionTargets(BattleBoardSystem boardSystem, BattlePotionEffect potionEffect, Vector2Int targetGrid)
        {
            if (boardSystem == null || potionEffect == null)
            {
                return new List<BattleUnit>();
            }

            // 전체형 포션은 보드 전체에서 팀 필터에 맞는 유닛을 직접 찾습니다.
            if (potionEffect.TargetingType == BattlePotionTargetingType.All)
            {
                return ResolveUnitsByTeamFilter(boardSystem, potionEffect.OwnerTeam, potionEffect.GlobalTargetFilter);
            }

            HashSet<Vector2Int> impactCells = ResolvePotionImpactCells(potionEffect, targetGrid);
            return boardSystem.GetUnitsInCells(potionEffect.OwnerTeam, impactCells, potionEffect.ImpactTargetFilter);
        }

        /// <summary>
        /// 덫이 발동했을 때, 밟힌 칸을 기준으로 어떤 칸들에 영향을 주는지 계산합니다.
        /// </summary>
        public static HashSet<Vector2Int> ResolveTrapImpactCells(BattleTrapEffect trapEffect, Vector2Int steppedCell)
        {
            HashSet<Vector2Int> result = new();
            if (trapEffect == null)
            {
                return result;
            }

            return BattleAttackImpactCellResolver.ResolveImpactCells(
                steppedCell,
                steppedCell,
                trapEffect.ImpactRange,
                trapEffect.ImpactPattern,
                trapEffect.CustomImpactPattern,
                BattleAttackPatternOriginMode.RangedPattern);
        }

        /// <summary>
        /// 덫이 발동했을 때 실제 효과를 받을 유닛 목록을 계산합니다.
        /// </summary>
        public static List<BattleUnit> ResolveTrapTargets(BattleBoardSystem boardSystem, BattleTrapEffect trapEffect, Vector2Int steppedCell)
        {
            if (boardSystem == null || trapEffect == null)
            {
                return new List<BattleUnit>();
            }

            HashSet<Vector2Int> impactCells = ResolveTrapImpactCells(trapEffect, steppedCell);
            return boardSystem.GetUnitsInCells(trapEffect.OwnerTeam, impactCells, trapEffect.ImpactTargetFilter);
        }

        /// <summary>
        /// 전장 전체에서 특정 팀 기준 필터에 맞는 유닛만 추려냅니다.
        /// 전체형 포션처럼 칸 선택이 없는 카드에서 사용됩니다.
        /// </summary>
        public static List<BattleUnit> ResolveUnitsByTeamFilter(
            BattleBoardSystem boardSystem,
            BattleTeam sourceTeam,
            BattleUnitTargetFilter targetFilter)
        {
            List<BattleUnit> result = new();
            if (boardSystem == null)
            {
                return result;
            }

            BattleUnit[] allUnits = Object.FindObjectsByType<BattleUnit>(FindObjectsSortMode.None);
            for (int i = 0; i < allUnits.Length; i++)
            {
                BattleUnit unit = allUnits[i];
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                if (!BattleUnitTargetFilterUtility.Matches(sourceTeam, unit, targetFilter))
                {
                    continue;
                }

                result.Add(unit);
            }

            return result;
        }
    }
}
