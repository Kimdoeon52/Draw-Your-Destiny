namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 전투 중 실제로 보드 위에 설치되어 있는 덫 1개의 상태를 보관합니다.
    /// 카드 원본, 설치 위치, 남은 발동 횟수, 소유 팀, 가시성 규칙을 함께 유지합니다.
    /// </summary>
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
        public GameObject VisualInstance { get; set; }
        public BattleTeam OwnerTeam => TrapEffect != null ? TrapEffect.OwnerTeam : BattleTeam.Player;
        public BattleTrapVisibilityRule VisibilityRule => TrapEffect != null ? TrapEffect.VisibilityRule : BattleTrapVisibilityRule.HiddenFromOpposingTeam;
    }

    /// <summary>
    /// 전투 중 설치된 모든 덫의 상태를 관리하는 런타임 시스템입니다.
    /// 설치 가능 여부, 가시성, 이동 후 발동, 남은 횟수 차감, 전투 종료 시 정리를 담당합니다.
    /// </summary>
    internal static class BattleTrapSystem
    {
        private static readonly List<BattleInstalledTrap> InstalledTraps = new();

        /// <summary>
        /// 팀/가시성과 무관하게 해당 칸에 어떤 덫이든 존재하는지 확인합니다.
        /// 설치 가능 여부를 판단할 때 사용합니다.
        /// </summary>
        public static bool HasAnyTrapAt(Vector2Int cell)
        {
            return TryGetInstalledTrap(cell, out _);
        }

        /// <summary>
        /// 전투 종료나 리셋 시 설치된 덫 상태를 전부 비웁니다.
        /// 덱 제거와 별개로, 보드 위 런타임 상태와 외형 오브젝트를 정리합니다.
        /// </summary>
        public static void ClearInstalledTraps()
        {
            for (int i = 0; i < InstalledTraps.Count; i++)
            {
                DestroyTrapVisual(InstalledTraps[i]);
            }

            InstalledTraps.Clear();
        }

        /// <summary>
        /// viewerTeam 기준으로 해당 칸의 덫이 실제로 보이는지 확인합니다.
        /// 소유 팀은 항상 볼 수 있고, 공개 덫은 모든 팀이 볼 수 있습니다.
        /// </summary>
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

        /// <summary>
        /// viewerTeam이 현재 확인할 수 있는 덫 위치만 모아서 반환합니다.
        /// 상대에게 숨겨진 덫은 여기 포함되지 않습니다.
        /// </summary>
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

        /// <summary>
        /// 유효한 설치 칸이라면 덫 카드 1장을 해당 위치에 설치합니다.
        /// 같은 칸에는 v1 기준으로 덫 1개만 둘 수 있습니다.
        /// </summary>
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

            BattleInstalledTrap installedTrap = new(sourceCard, trapEffect, targetCell);
            installedTrap.VisualInstance = CreateTrapVisual(trapEffect, targetCell);
            InstalledTraps.Add(installedTrap);
            return true;
        }

        /// <summary>
        /// 유닛이 이동을 마친 뒤 현재 칸에 설치된 덫이 있는지 확인하고,
        /// 발동 조건에 맞는 덫을 순서대로 처리합니다.
        /// </summary>
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

                // 덫은 "밟은 칸"을 기준으로 범위를 계산한 뒤 payload 이펙트들을 적용합니다.
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
                    DestroyTrapVisual(trap);
                    InstalledTraps.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 특정 칸에 실제 설치된 덫 상태가 있는지 찾습니다.
        /// </summary>
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

        /// <summary>
        /// 덫 설정 기준으로 steppedUnit이 실제 발동 대상인지 판별합니다.
        /// </summary>
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

        private static GameObject CreateTrapVisual(BattleTrapEffect trapEffect, Vector2Int installedCell)
        {
            if (trapEffect == null || trapEffect.InstalledTrapVisualPrefab == null)
            {
                return null;
            }

            GameObject prefab = trapEffect.InstalledTrapVisualPrefab;
            Vector3 spawnPosition = BattleUnit.GetWorldPositionForGrid(installedCell, prefab.transform.position.z);
            return Object.Instantiate(prefab, spawnPosition, prefab.transform.rotation);
        }

        private static void DestroyTrapVisual(BattleInstalledTrap trap)
        {
            if (trap?.VisualInstance == null)
            {
                return;
            }

            Object.Destroy(trap.VisualInstance);
            trap.VisualInstance = null;
        }
    }
}
