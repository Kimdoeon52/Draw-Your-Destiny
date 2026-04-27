namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 카드 타겟팅 상태를 BattleGridPreviewSystem 호출로 변환합니다.
    /// 타겟팅 규칙 계산이나 입력 처리는 담당하지 않고, 프리뷰 표시 순서만 담당합니다.
    /// </summary>
    internal sealed class BattleTargetingPreviewPresenter
    {
        private readonly BattleGridPreviewSystem gridPreviewSystem;

        public BattleTargetingPreviewPresenter(BattleGridPreviewSystem gridPreviewSystem)
        {
            this.gridPreviewSystem = gridPreviewSystem;
        }

        public void Clear()
        {
            gridPreviewSystem?.Clear();
            gridPreviewSystem?.ResetAllUnitColorsImmediate();
        }

        public void ShowSelectableUnits(IEnumerable<BattleUnit> units)
        {
            gridPreviewSystem?.Clear();
            gridPreviewSystem?.ResetAllUnitColorsImmediate();
            gridPreviewSystem?.ShowUnitBorders(units);
        }

        public void ShowMoveSelection(
            BattleUnit selectedUnit,
            IEnumerable<Vector2Int> moveCells,
            IEnumerable<Vector2Int> pathCells,
            Vector2Int? hoverCell,
            IReadOnlyList<Vector2Int> selectedAttackPositions,
            IEnumerable<Vector2Int> previewAttackCells)
        {
            gridPreviewSystem?.ShowMoveCells(moveCells);
            gridPreviewSystem?.ShowHoverCellBorder(hoverCell);
            gridPreviewSystem?.ShowUnitHighlights(new[] { selectedUnit });
            gridPreviewSystem?.ShowImpactUnitBorders(null);
            gridPreviewSystem?.ShowAttackSelectionOrder(selectedAttackPositions);
            gridPreviewSystem?.ShowPathCells(pathCells);
            gridPreviewSystem?.ShowAttackCells(previewAttackCells);
            gridPreviewSystem?.ShowAttackImpactCells(null);
        }

        public void ShowAttackSelection(
            BattleUnit selectedUnit,
            IEnumerable<Vector2Int> attackCells,
            IEnumerable<Vector2Int> confirmedMovePath,
            IReadOnlyList<Vector2Int> selectedAttackPositions,
            IEnumerable<Vector2Int> impactCells,
            IEnumerable<BattleUnit> impactTargets)
        {
            gridPreviewSystem?.ShowMoveCells(null);
            gridPreviewSystem?.ShowUnitHighlights(new[] { selectedUnit });
            gridPreviewSystem?.ShowAttackCells(attackCells);
            gridPreviewSystem?.ShowAttackSelectionOrder(selectedAttackPositions);
            gridPreviewSystem?.ShowHoverCellBorder(null);
            gridPreviewSystem?.ShowPathCells(confirmedMovePath);
            gridPreviewSystem?.ShowAttackImpactCells(impactCells);
            gridPreviewSystem?.ShowImpactUnitBorders(impactTargets);
        }
    }
}
