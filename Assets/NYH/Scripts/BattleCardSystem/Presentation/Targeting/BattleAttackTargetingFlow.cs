namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 공격 타겟팅 중 hover와 클릭을 선택 결과로 변환합니다.
    /// 카드 실행, 이동 단계 전환, 프리뷰 렌더링은 담당하지 않고 공격 선택 규칙만 담당합니다.
    /// </summary>
    internal sealed class BattleAttackTargetingFlow
    {
        public bool UpdateHover(BattleTargetingState state, Vector2 screenPosition, out Vector2Int? previewGrid)
        {
            previewGrid = null;

            if (state == null || state.PendingBattleCard == null || state.PendingUserUnit == null || BattleBoardSystem.Instance == null)
            {
                return false;
            }

            Vector2Int hoveredGrid = BattleTargetingQueryService.ResolveTargetGridPosition(screenPosition);
            bool isValidHover = state.SelectableAttackCells.Contains(hoveredGrid);
            if (state.HasLastAttackHoverCell
                && hoveredGrid == state.LastHoveredAttackCell
                && state.WasLastAttackHoverValid == isValidHover)
            {
                return false;
            }

            state.HasLastAttackHoverCell = true;
            state.LastHoveredAttackCell = hoveredGrid;
            state.WasLastAttackHoverValid = isValidHover;
            previewGrid = isValidHover ? hoveredGrid : (Vector2Int?)null;
            return true;
        }

        public BattleAttackTargetSelection SelectTarget(
            BattleBoardSystem boardSystem,
            BattleCard battleCard,
            BattleUnit userUnit,
            IReadOnlyList<Vector2Int> confirmedMovePath,
            HashSet<Vector2Int> selectableAttackCells,
            List<Vector2Int> selectedAttackTargetPositions,
            Vector2Int clickedGrid,
            BattleUnit clickedUnit)
        {
            if (boardSystem == null
                || battleCard == null
                || userUnit == null
                || selectableAttackCells == null
                || selectedAttackTargetPositions == null
                || !selectableAttackCells.Contains(clickedGrid))
            {
                return BattleAttackTargetSelection.Invalid;
            }

            bool isGroundTargetAttack = BattleTargetingQueryService.IsGroundTargetAttack(battleCard, userUnit);
            Vector2Int attackOrigin = confirmedMovePath != null && confirmedMovePath.Count > 0
                ? confirmedMovePath[confirmedMovePath.Count - 1]
                : userUnit.GridPosition;

            List<BattleUnit> previewTargets = BattleTargetingQueryService.ResolvePreviewAttackTargets(
                boardSystem,
                userUnit,
                attackOrigin,
                battleCard,
                clickedGrid);

            BattleUnit resolvedTarget = ResolvePrimaryTarget(isGroundTargetAttack, clickedUnit, previewTargets);

            selectedAttackTargetPositions.Add(clickedGrid);
            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(battleCard, userUnit);
            int requiredSelectionCount = attackEffect != null ? attackEffect.SelectionCount : 1;
            bool isComplete = selectedAttackTargetPositions.Count >= requiredSelectionCount;

            return new BattleAttackTargetSelection(
                isValid: true,
                isComplete: isComplete,
                isGroundTargetAttack: isGroundTargetAttack,
                targetGrid: clickedGrid,
                resolvedTarget: isGroundTargetAttack ? null : resolvedTarget);
        }

        private static BattleUnit ResolvePrimaryTarget(
            bool isGroundTargetAttack,
            BattleUnit clickedUnit,
            List<BattleUnit> previewTargets)
        {
            if (isGroundTargetAttack || previewTargets == null || previewTargets.Count == 0)
            {
                return null;
            }

            if (clickedUnit != null && clickedUnit.Team == BattleTeam.Enemy && clickedUnit.IsAlive)
            {
                return clickedUnit;
            }

            return previewTargets[0];
        }
    }

    internal readonly struct BattleAttackTargetSelection
    {
        public static BattleAttackTargetSelection Invalid { get; } = new(
            isValid: false,
            isComplete: false,
            isGroundTargetAttack: false,
            targetGrid: Vector2Int.zero,
            resolvedTarget: null);

        public BattleAttackTargetSelection(
            bool isValid,
            bool isComplete,
            bool isGroundTargetAttack,
            Vector2Int targetGrid,
            BattleUnit resolvedTarget)
        {
            IsValid = isValid;
            IsComplete = isComplete;
            IsGroundTargetAttack = isGroundTargetAttack;
            TargetGrid = targetGrid;
            ResolvedTarget = resolvedTarget;
        }

        public bool IsValid { get; }

        public bool IsComplete { get; }

        public bool IsGroundTargetAttack { get; }

        public Vector2Int TargetGrid { get; }

        public BattleUnit ResolvedTarget { get; }
    }
}
