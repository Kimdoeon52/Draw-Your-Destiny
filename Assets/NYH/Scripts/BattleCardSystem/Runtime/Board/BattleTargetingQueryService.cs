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
        // 카드 이동 이펙트와 유닛 속도를 합쳐 실제 이동 예산을 계산합니다.
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

        // 현재 카드 사용 조건을 만족하고 실제로 이동/공격 후보가 있는 플레이어 유닛을 찾습니다.
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

        // 화면 좌표를 전투 그리드 좌표로 변환합니다.
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

        // 선택된 이동 경로의 총 이동 비용을 계산합니다.
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

        // 드래그 중인 이동 경로를 현재 hover 셀까지 자연스럽게 연장합니다.
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

        // 이동 타겟 클릭을 경로 추가/되돌리기/초기화 같은 편집 결과로 변환합니다.
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

        // 마우스 드래그 입력을 이동 경로 편집으로 적용합니다.
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

        // 공격 카드가 클릭할 수 있는 조준 후보 셀을 계산합니다.
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

        // 유닛 직접 선택이 아니라 빈 땅/셀을 조준하는 공격인지 확인합니다.
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

        // 현재 hover/선택 기준으로 실제 피격 프리뷰 셀을 계산합니다.
        public static HashSet<Vector2Int> ResolvePreviewAttackCells(
            BattleBoardSystem boardSystem,
            BattleCard battleCard,
            BattleUnit userUnit,
            IReadOnlyList<Vector2Int> confirmedMovePath,
            Vector2Int targetGrid)
        {
            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(battleCard);
            Vector2Int attackOrigin = confirmedMovePath != null && confirmedMovePath.Count > 0
                ? confirmedMovePath[confirmedMovePath.Count - 1]
                : userUnit != null ? userUnit.GridPosition : Vector2Int.zero;

            if (ShouldUseBlockingMeleeLinePreview(attackEffect, attackOrigin, targetGrid))
            {
                return BuildMeleeLinePathPreview(
                    boardSystem,
                    userUnit,
                    attackEffect,
                    attackOrigin,
                    targetGrid,
                    attackEffect.TargetingRange);
            }

            HashSet<Vector2Int> rawImpactCells = ResolveRawPreviewAttackCells(
                boardSystem,
                battleCard,
                userUnit,
                confirmedMovePath,
                targetGrid);
            rawImpactCells = FilterBlockedPreviewImpactCells(
                boardSystem,
                battleCard,
                userUnit,
                attackOrigin,
                targetGrid,
                rawImpactCells);
            if (attackEffect == null
                || attackEffect.BlocksBehindTargets
                || attackEffect.HitsAllTargetsInRange
                || attackEffect.TargetCount <= 0)
            {
                return rawImpactCells;
            }

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

            if (attackEffect.ImpactPattern == BattleAttackPattern.Line)
            {
                return LimitLinePreviewCellsToHitPath(rawImpactCells, attackOrigin, previewTargets);
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

        // 빨간 조준 범위와 노란 피격 범위를 분리 표시할 때 사용할 표시용 공격 셀을 계산합니다.
        public static HashSet<Vector2Int> ResolvePreviewAttackDisplayCells(
            BattleBoardSystem boardSystem,
            BattleCard battleCard,
            BattleUnit userUnit,
            IReadOnlyList<Vector2Int> confirmedMovePath,
            Vector2Int targetGrid,
            IEnumerable<Vector2Int> selectableAttackCells)
        {
            HashSet<Vector2Int> result = selectableAttackCells != null
                ? new HashSet<Vector2Int>(selectableAttackCells)
                : new HashSet<Vector2Int>();

            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(battleCard);
            if (attackEffect == null
                || !attackEffect.BlocksBehindTargets
                || attackEffect.PatternOriginMode != BattleAttackPatternOriginMode.MeleePattern)
            {
                return result;
            }

            foreach (Vector2Int cell in ResolveRawPreviewAttackCells(
                         boardSystem,
                         battleCard,
                         userUnit,
                         confirmedMovePath,
                         targetGrid))
            {
                result.Add(cell);
            }

            return result;
        }

        private static bool ShouldUseBlockingMeleeLinePreview(
            BattleAttackEffect attackEffect,
            Vector2Int attackOrigin,
            Vector2Int targetGrid)
        {
            if (attackEffect == null)
            {
                return false;
            }

            if (attackEffect.PatternOriginMode != BattleAttackPatternOriginMode.MeleePattern
                || attackEffect.HitsAllTargetsInRange
                || attackEffect.BlocksBehindTargets
                || attackEffect.CustomImpactPattern != null)
            {
                return false;
            }

            bool usesLineLikeImpact = attackEffect.ImpactPattern == BattleAttackPattern.Line
                || attackEffect.ImpactPattern == BattleAttackPattern.None;
            if (!usesLineLikeImpact)
            {
                return false;
            }

            bool usesLineLikeTargeting = attackEffect.TargetingPattern == BattleAttackPattern.Line
                || attackEffect.TargetingPattern == BattleAttackPattern.None;
            if (!usesLineLikeTargeting)
            {
                return false;
            }

            Vector2Int delta = targetGrid - attackOrigin;
            return (delta.x == 0 && delta.y != 0)
                || (delta.y == 0 && delta.x != 0);
        }

        private static HashSet<Vector2Int> BuildMeleeLinePathPreview(
            BattleBoardSystem boardSystem,
            BattleUnit userUnit,
            BattleAttackEffect attackEffect,
            Vector2Int attackOrigin,
            Vector2Int targetGrid,
            int maxPreviewRange)
        {
            HashSet<Vector2Int> result = new();
            Vector2Int delta = targetGrid - attackOrigin;
            Vector2Int direction;
            int maxDistance;

            if (delta.x == 0 && delta.y != 0)
            {
                direction = delta.y > 0 ? Vector2Int.up : Vector2Int.down;
                maxDistance = Mathf.Max(1, maxPreviewRange);
            }
            else if (delta.y == 0 && delta.x != 0)
            {
                direction = delta.x > 0 ? Vector2Int.right : Vector2Int.left;
                maxDistance = Mathf.Max(1, maxPreviewRange);
            }
            else
            {
                return result;
            }

            for (int i = 1; i <= maxDistance; i++)
            {
                Vector2Int cell = attackOrigin + (direction * i);
                result.Add(cell);

                BattleUnit unit = boardSystem != null ? boardSystem.GetUnitAt(cell) : null;
                if (unit != null
                    && unit != userUnit
                    && unit.IsAlive
                    && BattleUnitTargetFilterUtility.Matches(userUnit, unit, attackEffect.ImpactTargetFilter))
                {
                    break;
                }
            }

            return result;
        }

        private static HashSet<Vector2Int> LimitLinePreviewCellsToHitPath(
            HashSet<Vector2Int> rawImpactCells,
            Vector2Int attackOrigin,
            IReadOnlyList<BattleUnit> previewTargets)
        {
            HashSet<Vector2Int> result = new();
            if (rawImpactCells == null || previewTargets == null || previewTargets.Count == 0)
            {
                return rawImpactCells ?? result;
            }

            int maxDistance = 0;
            for (int i = 0; i < previewTargets.Count; i++)
            {
                BattleUnit target = previewTargets[i];
                if (target == null)
                {
                    continue;
                }

                maxDistance = Mathf.Max(maxDistance, ManhattanDistance(attackOrigin, target.GridPosition));
            }

            if (maxDistance <= 0)
            {
                return rawImpactCells;
            }

            foreach (Vector2Int cell in rawImpactCells)
            {
                if (cell != attackOrigin && ManhattanDistance(attackOrigin, cell) <= maxDistance)
                {
                    result.Add(cell);
                }
            }

            return result.Count > 0 ? result : rawImpactCells;
        }

        private static HashSet<Vector2Int> FilterBlockedPreviewImpactCells(
            BattleBoardSystem boardSystem,
            BattleCard battleCard,
            BattleUnit attacker,
            Vector2Int attackOrigin,
            Vector2Int targetGrid,
            HashSet<Vector2Int> rawImpactCells)
        {
            if (boardSystem == null || battleCard == null || attacker == null)
            {
                return rawImpactCells;
            }

            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(battleCard);
            if (attackEffect == null || !attackEffect.BlocksBehindTargets)
            {
                return rawImpactCells;
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
                attackEffect.BlocksBehindTargets,
                attackEffect.ImpactPattern,
                attackEffect.CustomImpactPattern,
                attackEffect.PatternOriginMode,
                attackEffect.ImpactTargetFilter);

            return BattleAttackQueryService.FilterBlockedMeleeImpactCells(
                rawImpactCells,
                attacker,
                attackOrigin,
                targetGrid,
                previewAttack,
                EnumerateBoardUnits(boardSystem, rawImpactCells));
        }

        private static IEnumerable<KeyValuePair<Vector2Int, BattleUnit>> EnumerateBoardUnits(
            BattleBoardSystem boardSystem,
            IEnumerable<Vector2Int> cells)
        {
            if (boardSystem == null || cells == null)
            {
                yield break;
            }

            foreach (Vector2Int cell in cells)
            {
                BattleUnit unit = boardSystem.GetUnitAt(cell);
                if (unit != null)
                {
                    yield return new KeyValuePair<Vector2Int, BattleUnit>(cell, unit);
                }
            }
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

            HashSet<Vector2Int> attackCells = BattleAttackImpactCellResolver.ResolveImpactCells(
                attackOrigin,
                targetGrid,
                attackEffect.ImpactRange,
                attackEffect.ImpactPattern,
                attackEffect.CustomImpactPattern,
                attackEffect.PatternOriginMode);

            // 공격 미리보기에서는 카드 패턴이 자기 칸을 포함하더라도 공격자 본인은 표시하지 않습니다.
            attackCells.Remove(attackOrigin);
            return attackCells;
        }

        // 프리뷰 피격 셀 안에서 실제로 강조할 유닛 목록을 계산합니다.
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
                bool isAttackPreview = BattleEffectResolver.GetAttackEffect(battleCard) != null;
                if (unit == null
                    || (isAttackPreview && unit == userUnit)
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

        // 프리뷰용 공격 대상 유닛을 카드 필터/타겟 수 규칙에 맞춰 계산합니다.
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
                attackEffect.BlocksBehindTargets,
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

        private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

    }

    internal enum BattleMovePathEditResult
    {
        None,
        Changed,
        Confirm,
    }
}
