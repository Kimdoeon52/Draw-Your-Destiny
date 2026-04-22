namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 플레이어가 카드를 조준할 때 필요한 이동/공격 후보를 계산합니다.
    /// UI 입력 상태나 프리뷰 렌더링은 담당하지 않고, 순수한 타겟팅 규칙만 담당합니다.
    /// </summary>
    internal static class BattleTargetingQueryService
    {
        public static int ResolveMoveBudget(BattleCard battleCard, BattleUnit userUnit)
        {
            if (battleCard == null || userUnit == null)
            {
                return 0;
            }

            BattleMoveEffect moveEffect = BattleEffectResolver.GetMoveEffect(battleCard);
            if (moveEffect == null)
            {
                return 0;
            }

            return moveEffect.IncludeSourceUnitSpeed
                ? Mathf.Max(0, moveEffect.Amount + userUnit.CurrentSpeed)
                : Mathf.Max(0, moveEffect.Amount);
        }

        public static List<BattleUnit> FindUsablePlayerUnits(BattleCard battleCard)
        {
            List<BattleUnit> result = new();
            BattleBoardSystem boardSystem = BattleBoardSystem.Instance;
            BattleUnit[] allUnits = UnityEngine.Object.FindObjectsByType<BattleUnit>(FindObjectsSortMode.None);

            foreach (BattleUnit unit in allUnits)
            {
                if (unit == null || unit.Team != BattleTeam.Player || !unit.IsAlive)
                {
                    continue;
                }

                if (!BattleCardUnitTypeRestriction.CanUserUnitPlay(battleCard, unit))
                {
                    continue;
                }

                BattleCardTargetingMode targetingMode = BattleCardTargetingUtility.ResolveTargetingMode(battleCard);
                if ((targetingMode == BattleCardTargetingMode.MoveOnly || targetingMode == BattleCardTargetingMode.MoveThenAttack)
                    && boardSystem != null)
                {
                    int moveBudget = ResolveMoveBudget(battleCard, unit);
                    HashSet<Vector2Int> moveCells = boardSystem.GetSelectableMoveCells(unit, moveBudget);

                    if (targetingMode == BattleCardTargetingMode.MoveOnly && moveCells.Count == 0)
                    {
                        continue;
                    }

                    if (targetingMode == BattleCardTargetingMode.MoveThenAttack)
                    {
                        bool canAttackFromCurrent = ResolveAttackSelectionCells(
                            boardSystem,
                            unit,
                            unit.GridPosition,
                            battleCard).Count > 0;

                        if (!canAttackFromCurrent && moveCells.Count == 0)
                        {
                            continue;
                        }
                    }
                }

                if ((targetingMode == BattleCardTargetingMode.AttackOnly
                    || targetingMode == BattleCardTargetingMode.AttackThenMove) && boardSystem != null)
                {
                    HashSet<Vector2Int> attackCells = ResolveAttackSelectionCells(
                        boardSystem,
                        unit,
                        unit.GridPosition,
                        battleCard);

                    if (attackCells.Count == 0)
                    {
                        continue;
                    }
                }

                result.Add(unit);
            }

            return result;
        }

        public static Vector2Int ResolveTargetGridPosition(Vector2 screenPosition)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return new Vector2Int(int.MinValue, int.MinValue);
            }

            Vector3 worldPosition = camera.ScreenToWorldPoint(new Vector3(
                screenPosition.x,
                screenPosition.y,
                -camera.transform.position.z));
            return BattleUnit.GetGridPositionForWorld(worldPosition);
        }

        public static int CalculatePathCost(BattleBoardSystem boardSystem, IReadOnlyList<Vector2Int> path)
        {
            if (boardSystem == null || path == null || path.Count == 0)
            {
                return 0;
            }

            int cost = 0;
            for (int i = 0; i < path.Count; i++)
            {
                cost += boardSystem.GetStepCost(path[i]);
            }

            return cost;
        }

        public static bool TryExtendMovePath(
            BattleBoardSystem boardSystem,
            BattleUnit unit,
            int moveBudget,
            List<Vector2Int> drawnMovePath,
            Vector2Int hoveredGrid)
        {
            if (boardSystem == null || unit == null || drawnMovePath == null)
            {
                return false;
            }

            Vector2Int segmentStart = drawnMovePath.Count > 0
                ? drawnMovePath[drawnMovePath.Count - 1]
                : unit.GridPosition;

            int remainingBudget = Mathf.Max(0, moveBudget - CalculatePathCost(boardSystem, drawnMovePath));
            if (remainingBudget <= 0)
            {
                return false;
            }

            if (!boardSystem.TryBuildMovePath(
                    unit,
                    segmentStart,
                    hoveredGrid,
                    remainingBudget,
                    out List<Vector2Int> pathSegment))
            {
                return false;
            }

            if (pathSegment == null || pathSegment.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < pathSegment.Count; i++)
            {
                Vector2Int cell = pathSegment[i];
                if (drawnMovePath.Count > 0 && drawnMovePath[drawnMovePath.Count - 1] == cell)
                {
                    continue;
                }

                drawnMovePath.Add(cell);
            }

            return true;
        }

        public static BattleMovePathEditResult ApplyMoveTargetClick(
            BattleBoardSystem boardSystem,
            BattleUnit unit,
            int moveBudget,
            HashSet<Vector2Int> selectableMoveCells,
            List<Vector2Int> drawnMovePath,
            Vector2Int clickedGrid)
        {
            if (boardSystem == null || unit == null || selectableMoveCells == null || drawnMovePath == null)
            {
                return BattleMovePathEditResult.None;
            }

            if (drawnMovePath.Count > 0 && clickedGrid == drawnMovePath[drawnMovePath.Count - 1])
            {
                return BattleMovePathEditResult.Confirm;
            }

            int existingIndex = drawnMovePath.IndexOf(clickedGrid);
            if (existingIndex >= 0)
            {
                drawnMovePath.RemoveRange(existingIndex + 1, drawnMovePath.Count - existingIndex - 1);
                return BattleMovePathEditResult.Changed;
            }

            if (!selectableMoveCells.Contains(clickedGrid))
            {
                return BattleMovePathEditResult.None;
            }

            if (!boardSystem.TryBuildMovePath(
                    unit,
                    unit.GridPosition,
                    clickedGrid,
                    moveBudget,
                    out List<Vector2Int> autoPath))
            {
                return BattleMovePathEditResult.None;
            }

            drawnMovePath.Clear();
            drawnMovePath.AddRange(autoPath);
            return BattleMovePathEditResult.Changed;
        }

        public static bool TryApplyMovePathDrag(
            BattleBoardSystem boardSystem,
            BattleUnit unit,
            int moveBudget,
            HashSet<Vector2Int> selectableMoveCells,
            List<Vector2Int> drawnMovePath,
            Vector2Int hoveredGrid)
        {
            if (boardSystem == null || unit == null || selectableMoveCells == null || drawnMovePath == null)
            {
                return false;
            }

            if (!selectableMoveCells.Contains(hoveredGrid))
            {
                return false;
            }

            if (drawnMovePath.Count > 0 && hoveredGrid == drawnMovePath[drawnMovePath.Count - 1])
            {
                return false;
            }

            int existingIndex = drawnMovePath.IndexOf(hoveredGrid);
            if (existingIndex >= 0)
            {
                drawnMovePath.RemoveRange(existingIndex + 1, drawnMovePath.Count - existingIndex - 1);
                return true;
            }

            return TryExtendMovePath(boardSystem, unit, moveBudget, drawnMovePath, hoveredGrid);
        }

        public static HashSet<Vector2Int> ResolveAttackSelectionCells(
            BattleBoardSystem boardSystem,
            BattleUnit attacker,
            Vector2Int attackOrigin,
            BattleCard battleCard)
        {
            HashSet<Vector2Int> result = new();
            if (boardSystem == null || attacker == null || battleCard == null)
            {
                return result;
            }

            return boardSystem.GetSelectableAttackCells(attacker, attackOrigin, battleCard);
        }

        public static bool IsGroundTargetAttack(BattleCard battleCard)
        {
            if (BattleEffectResolver.GetHealEffect(battleCard) != null)
            {
                return true;
            }

            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(battleCard);
            if (attackEffect == null)
            {
                return false;
            }

            return attackEffect.CustomTargetingPattern != null
                || attackEffect.TargetingPattern == BattleAttackPattern.Area
                || attackEffect.TargetingPattern == BattleAttackPattern.Line
                || attackEffect.TargetingPattern == BattleAttackPattern.Adjacent4
                || attackEffect.CustomImpactPattern != null
                || attackEffect.ImpactPattern == BattleAttackPattern.Area
                || attackEffect.ImpactPattern == BattleAttackPattern.Line
                || attackEffect.ImpactPattern == BattleAttackPattern.Adjacent4;
        }

        public static HashSet<Vector2Int> ResolvePreviewAttackCells(
            BattleBoardSystem boardSystem,
            BattleCard battleCard,
            BattleUnit userUnit,
            IReadOnlyList<Vector2Int> confirmedMovePath,
            Vector2Int targetGrid)
        {
            HashSet<Vector2Int> rawImpactCells = ResolveRawPreviewAttackCells(
                boardSystem,
                battleCard,
                userUnit,
                confirmedMovePath,
                targetGrid);

            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(battleCard);
            if (attackEffect == null
                || attackEffect.HitsAllTargetsInRange
                || attackEffect.TargetCount <= 0)
            {
                return rawImpactCells;
            }

            Vector2Int attackOrigin = confirmedMovePath != null && confirmedMovePath.Count > 0
                ? confirmedMovePath[confirmedMovePath.Count - 1]
                : userUnit != null ? userUnit.GridPosition : Vector2Int.zero;
            List<BattleUnit> previewTargets = ResolvePreviewAttackTargets(
                boardSystem,
                userUnit,
                attackOrigin,
                battleCard,
                targetGrid);

            if (previewTargets.Count == 0)
            {
                return rawImpactCells;
            }

            HashSet<Vector2Int> limitedImpactCells = new();
            for (int i = 0; i < previewTargets.Count; i++)
            {
                if (previewTargets[i] != null)
                {
                    limitedImpactCells.Add(previewTargets[i].GridPosition);
                }
            }

            return limitedImpactCells.Count > 0 ? limitedImpactCells : rawImpactCells;
        }

        private static HashSet<Vector2Int> ResolveRawPreviewAttackCells(
            BattleBoardSystem boardSystem,
            BattleCard battleCard,
            BattleUnit userUnit,
            IReadOnlyList<Vector2Int> confirmedMovePath,
            Vector2Int targetGrid)
        {
            HashSet<Vector2Int> result = new();
            if (boardSystem == null || battleCard == null || userUnit == null)
            {
                return result;
            }

            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(battleCard);
            BattleHealEffect healEffect = BattleEffectResolver.GetHealEffect(battleCard);
            if (attackEffect == null && healEffect == null)
            {
                result.Add(targetGrid);
                return result;
            }

            Vector2Int attackOrigin = confirmedMovePath != null && confirmedMovePath.Count > 0
                ? confirmedMovePath[confirmedMovePath.Count - 1]
                : userUnit.GridPosition;

            if (healEffect != null && attackEffect == null)
            {
                return BattleAttackImpactCellResolver.ResolveImpactCells(
                    attackOrigin,
                    targetGrid,
                    healEffect.Range,
                    healEffect.HealPattern,
                    healEffect.CustomHealPattern,
                    healEffect.HealPatternOriginMode);
            }

            return BattleAttackImpactCellResolver.ResolveImpactCells(
                attackOrigin,
                targetGrid,
                attackEffect.ImpactRange,
                attackEffect.ImpactPattern,
                attackEffect.CustomImpactPattern,
                attackEffect.PatternOriginMode);
        }

        public static List<BattleUnit> ResolvePreviewImpactTargets(
            BattleBoardSystem boardSystem,
            BattleCard battleCard,
            BattleUnit userUnit,
            IReadOnlyList<Vector2Int> confirmedMovePath,
            Vector2Int targetGrid)
        {
            List<BattleUnit> result = new();
            if (boardSystem == null || userUnit == null)
            {
                return result;
            }

            HashSet<Vector2Int> impactCells = ResolvePreviewAttackCells(
                boardSystem,
                battleCard,
                userUnit,
                confirmedMovePath,
                targetGrid);

            foreach (Vector2Int cell in impactCells)
            {
                BattleUnit unit = boardSystem.GetUnitAt(cell);
                BattleUnitTargetFilter targetFilter = ResolvePreviewTargetFilter(battleCard);
                if (unit == null
                    || !unit.IsAlive
                    || !BattleUnitTargetFilterUtility.Matches(userUnit, unit, targetFilter)
                    || result.Contains(unit))
                {
                    continue;
                }

                result.Add(unit);
            }

            return result;
        }

        public static List<BattleUnit> ResolvePreviewAttackTargets(
            BattleBoardSystem boardSystem,
            BattleUnit attacker,
            Vector2Int attackOrigin,
            BattleCard battleCard,
            Vector2Int targetGrid)
        {
            List<BattleUnit> result = new();
            if (boardSystem == null || attacker == null || battleCard == null)
            {
                return result;
            }

            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(battleCard);
            if (attackEffect == null)
            {
                return result;
            }

            BattleAttackGA previewAttack = new(
                battleCard,
                attacker,
                null,
                targetGrid,
                0,
                attackEffect.ImpactRange,
                attackEffect.TargetCount,
                attackEffect.HitsAllTargetsInRange,
                attackEffect.ImpactPattern,
                attackEffect.CustomImpactPattern,
                attackEffect.PatternOriginMode,
                attackEffect.ImpactTargetFilter);

            result.AddRange(boardSystem.GetUnitsInAttackArea(
                attacker,
                attackOrigin,
                targetGrid,
                previewAttack));

            return result;
        }

        private static BattleUnitTargetFilter ResolvePreviewTargetFilter(BattleCard battleCard)
        {
            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(battleCard);
            if (attackEffect != null)
            {
                return attackEffect.ImpactTargetFilter;
            }

            BattleHealEffect healEffect = BattleEffectResolver.GetHealEffect(battleCard);
            return healEffect != null ? healEffect.HealTargetFilter : BattleUnitTargetFilter.EnemiesOnly;
        }

    }

    internal enum BattleMovePathEditResult
    {
        None,
        Changed,
        Confirm,
    }
}
