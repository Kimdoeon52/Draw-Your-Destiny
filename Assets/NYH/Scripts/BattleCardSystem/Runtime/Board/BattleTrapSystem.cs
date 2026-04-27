namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    internal sealed class BattleInstalledTrap
    {
        public BattleInstalledTrap(BattleCard sourceCard, BattleTrapEffect trapEffect, Vector2Int installedCell)
        {
            SourceCard = sourceCard;
            TrapEffect = trapEffect;
            InstalledCell = installedCell;
            RemainingTriggerCount = trapEffect != null ? trapEffect.TriggerCount : 1;
        }

        public BattleCard SourceCard { get; }
        public BattleTrapEffect TrapEffect { get; }
        public Vector2Int InstalledCell { get; }
        public int RemainingTriggerCount { get; set; }
        public BattleTeam OwnerTeam => TrapEffect != null ? TrapEffect.OwnerTeam : BattleTeam.Player;
        public BattleTrapVisibilityRule VisibilityRule => TrapEffect != null ? TrapEffect.VisibilityRule : BattleTrapVisibilityRule.HiddenFromOpposingTeam;
    }

    internal static class BattleTrapSystem
    {
        private static readonly List<BattleInstalledTrap> InstalledTraps = new();

        public static bool HasAnyTrapAt(Vector2Int cell)
        {
            return TryGetInstalledTrap(cell, out _);
        }

        public static void ClearInstalledTraps()
        {
            InstalledTraps.Clear();
        }

        public static bool HasVisibleTrapAt(Vector2Int cell, BattleTeam viewerTeam)
        {
            for (int i = 0; i < InstalledTraps.Count; i++)
            {
                BattleInstalledTrap trap = InstalledTraps[i];
                if (trap == null || trap.InstalledCell != cell)
                {
                    continue;
                }

                if (trap.OwnerTeam == viewerTeam || trap.VisibilityRule == BattleTrapVisibilityRule.VisibleToAll)
                {
                    return true;
                }
            }

            return false;
        }

        public static HashSet<Vector2Int> GetVisibleTrapCells(BattleTeam viewerTeam)
        {
            HashSet<Vector2Int> result = new();
            for (int i = 0; i < InstalledTraps.Count; i++)
            {
                BattleInstalledTrap trap = InstalledTraps[i];
                if (trap == null)
                {
                    continue;
                }

                if (trap.OwnerTeam == viewerTeam || trap.VisibilityRule == BattleTrapVisibilityRule.VisibleToAll)
                {
                    result.Add(trap.InstalledCell);
                }
            }

            return result;
        }

        public static bool TryInstallTrap(BattleCard sourceCard, BattleTrapEffect trapEffect, Vector2Int targetCell)
        {
            if (sourceCard == null || trapEffect == null || BattleBoardSystem.Instance == null)
            {
                return false;
            }

            if (!BattleTargetingQueryService.IsValidTrapInstallCell(BattleBoardSystem.Instance, targetCell))
            {
                return false;
            }

            if (TryGetInstalledTrap(targetCell, out _))
            {
                return false;
            }

            InstalledTraps.Add(new BattleInstalledTrap(sourceCard, trapEffect, targetCell));
            return true;
        }

        public static void TryTriggerTrapsAt(BattleUnit steppedUnit)
        {
            if (steppedUnit == null || !steppedUnit.IsAlive || BattleBoardSystem.Instance == null)
            {
                return;
            }

            for (int i = InstalledTraps.Count - 1; i >= 0; i--)
            {
                BattleInstalledTrap trap = InstalledTraps[i];
                if (trap == null || trap.InstalledCell != steppedUnit.GridPosition)
                {
                    continue;
                }

                BattleTrapEffect trapEffect = trap.TrapEffect;
                if (trapEffect == null || !CanTrapTriggerOnUnit(trapEffect, steppedUnit))
                {
                    continue;
                }

                List<BattleUnit> resolvedTargets = BattleTargetingQueryService.ResolveTrapImpactTargets(
                    BattleBoardSystem.Instance,
                    trapEffect,
                    steppedUnit.GridPosition);

                BattleEffectContext context = new(
                    trap.SourceCard,
                    null,
                    steppedUnit,
                    steppedUnit.GridPosition,
                    null,
                    BattleBoardSystem.Instance,
                    BattleCardSystem.Instance);

                BattleCardActionFactory.ApplyResolvedBattleEffects(
                    trap.SourceCard,
                    context,
                    resolvedTargets,
                    typeof(BattleTrapEffect),
                    typeof(BattlePotionEffect),
                    typeof(BattleMoveEffect),
                    typeof(BattleAttackEffect));

                trap.RemainingTriggerCount--;
                if (trap.RemainingTriggerCount <= 0)
                {
                    InstalledTraps.RemoveAt(i);
                }
            }
        }

        private static bool TryGetInstalledTrap(Vector2Int cell, out BattleInstalledTrap trap)
        {
            for (int i = 0; i < InstalledTraps.Count; i++)
            {
                BattleInstalledTrap candidate = InstalledTraps[i];
                if (candidate != null && candidate.InstalledCell == cell)
                {
                    trap = candidate;
                    return true;
                }
            }

            trap = null;
            return false;
        }

        private static bool CanTrapTriggerOnUnit(BattleTrapEffect trapEffect, BattleUnit steppedUnit)
        {
            if (trapEffect == null || steppedUnit == null)
            {
                return false;
            }

            return trapEffect.TriggerTargetRule switch
            {
                BattleTrapTriggerTargetRule.AlliesOnly => steppedUnit.Team == trapEffect.OwnerTeam,
                BattleTrapTriggerTargetRule.AllUnits => true,
                _ => steppedUnit.Team != trapEffect.OwnerTeam,
            };
        }
    }
}
