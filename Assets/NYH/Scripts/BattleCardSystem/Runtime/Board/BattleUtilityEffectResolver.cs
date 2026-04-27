namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    internal static class BattleUtilityEffectResolver
    {
        public static HashSet<Vector2Int> ResolvePotionImpactCells(BattlePotionEffect potionEffect, Vector2Int targetGrid)
        {
            HashSet<Vector2Int> result = new();
            if (potionEffect == null)
            {
                return result;
            }

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

        public static List<BattleUnit> ResolvePotionTargets(BattleBoardSystem boardSystem, BattlePotionEffect potionEffect, Vector2Int targetGrid)
        {
            if (boardSystem == null || potionEffect == null)
            {
                return new List<BattleUnit>();
            }

            if (potionEffect.TargetingType == BattlePotionTargetingType.All)
            {
                return ResolveUnitsByTeamFilter(boardSystem, potionEffect.OwnerTeam, potionEffect.GlobalTargetFilter);
            }

            HashSet<Vector2Int> impactCells = ResolvePotionImpactCells(potionEffect, targetGrid);
            return boardSystem.GetUnitsInCells(potionEffect.OwnerTeam, impactCells, potionEffect.ImpactTargetFilter);
        }

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

        public static List<BattleUnit> ResolveTrapTargets(BattleBoardSystem boardSystem, BattleTrapEffect trapEffect, Vector2Int steppedCell)
        {
            if (boardSystem == null || trapEffect == null)
            {
                return new List<BattleUnit>();
            }

            HashSet<Vector2Int> impactCells = ResolveTrapImpactCells(trapEffect, steppedCell);
            return boardSystem.GetUnitsInCells(trapEffect.OwnerTeam, impactCells, trapEffect.ImpactTargetFilter);
        }

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
