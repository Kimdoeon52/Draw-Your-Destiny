namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 타겟팅 상태를 그리드 프리뷰에 필요한 표시 데이터로 변환합니다.
    /// 입력 처리, 상태 전환, 카드 실행 요청은 담당하지 않고 이동/공격 프리뷰 계산과 표시 위임만 담당합니다.
    /// </summary>
    internal sealed class BattleTargetingPreviewCoordinator
    {
        private readonly BattleTargetingPreviewPresenter previewPresenter;

        public BattleTargetingPreviewCoordinator(BattleTargetingPreviewPresenter previewPresenter)
        {
            this.previewPresenter = previewPresenter;
        }

        public void Clear()
        {
            previewPresenter?.Clear();
        }

        public void ShowSelectableUnits(IEnumerable<BattleUnit> units)
        {
            previewPresenter?.ShowSelectableUnits(units);
        }

        public void RefreshMovePreview(BattleTargetingState state)
        {
            if (state == null)
            {
                Clear();
                return;
            }

            Vector2Int? hoverCell = state.HasLastMoveHoverCell && state.SelectableMoveCells.Contains(state.LastHoveredMoveCell)
                ? state.LastHoveredMoveCell
                : (Vector2Int?)null;
            IEnumerable<Vector2Int> pathCells = state.DrawnMovePath.Count > 0 ? state.DrawnMovePath : null;
            IEnumerable<Vector2Int> previewAttackCells = ResolveMovePreviewAttackCells(state);

            previewPresenter?.ShowMoveSelection(
                state.PendingUserUnit,
                state.SelectableMoveCells,
                pathCells,
                hoverCell,
                state.SelectedAttackTargetPositions,
                previewAttackCells);
        }

        public void RefreshAttackPreview(BattleTargetingState state, Vector2Int? hoveredGrid)
        {
            if (state == null)
            {
                Clear();
                return;
            }

            IEnumerable<Vector2Int> impactCells = null;
            IEnumerable<BattleUnit> impactTargets = null;
            IEnumerable<Vector2Int> pathCells = state.ConfirmedMovePath.Count > 0 ? state.ConfirmedMovePath : null;
            IEnumerable<Vector2Int> attackCells = state.SelectableAttackCells;

            if (hoveredGrid.HasValue
                && state.SelectableAttackCells.Contains(hoveredGrid.Value)
                && BattleBoardSystem.Instance != null
                && state.PendingBattleCard != null
                && state.PendingUserUnit != null)
            {
                attackCells = BattleTargetingQueryService.ResolvePreviewAttackDisplayCells(
                    BattleBoardSystem.Instance,
                    state.PendingBattleCard,
                    state.PendingUserUnit,
                    state.ConfirmedMovePath,
                    hoveredGrid.Value,
                    state.SelectableAttackCells);
                impactCells = BattleTargetingQueryService.ResolvePreviewAttackCells(
                    BattleBoardSystem.Instance,
                    state.PendingBattleCard,
                    state.PendingUserUnit,
                    state.ConfirmedMovePath,
                    hoveredGrid.Value);
                impactTargets = BattleTargetingQueryService.ResolvePreviewImpactTargets(
                    BattleBoardSystem.Instance,
                    state.PendingBattleCard,
                    state.PendingUserUnit,
                    state.ConfirmedMovePath,
                    hoveredGrid.Value);
            }

            previewPresenter?.ShowAttackSelection(
                state.PendingUserUnit,
                attackCells,
                pathCells,
                state.SelectedAttackTargetPositions,
                impactCells,
                impactTargets);
        }

        private static IEnumerable<Vector2Int> ResolveMovePreviewAttackCells(BattleTargetingState state)
        {
            if (state.PendingTargetingMode != BattleCardTargetingMode.MoveThenAttack
                || state.PendingBattleCard == null
                || state.PendingUserUnit == null
                || BattleBoardSystem.Instance == null)
            {
                return null;
            }

            Vector2Int previewOrigin = state.DrawnMovePath.Count > 0
                ? state.DrawnMovePath[state.DrawnMovePath.Count - 1]
                : state.PendingUserUnit.GridPosition;

            return BattleTargetingQueryService.ResolveAttackSelectionCells(
                BattleBoardSystem.Instance,
                state.PendingUserUnit,
                previewOrigin,
                state.PendingBattleCard);
        }
    }
}
