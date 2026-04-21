namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 이동 타겟팅 중 이동 가능 칸, 경로 편집, 이동 확정 결과를 계산합니다.
    /// 입력 라우팅, 프리뷰 표시, 실제 카드 실행은 담당하지 않습니다.
    /// </summary>
    internal sealed class BattleMoveTargetingFlow
    {
        public bool BeginMoveSelection(BattleTargetingState state, BattleUnit clickedUnit)
        {
            if (state == null || clickedUnit == null || BattleBoardSystem.Instance == null || state.PendingBattleCard == null)
            {
                return false;
            }

            state.PendingUserUnit = clickedUnit;
            state.CurrentMoveBudget = BattleTargetingQueryService.ResolveMoveBudget(state.PendingBattleCard, clickedUnit);
            state.SelectableMoveCells = BattleBoardSystem.Instance.GetSelectableMoveCells(clickedUnit, state.CurrentMoveBudget);
            state.DrawnMovePath.Clear();
            state.ConfirmedMovePath.Clear();
            state.SelectableAttackCells.Clear();
            state.HasLastDragCell = false;
            state.HasLastMoveHoverCell = false;
            state.HasLastAttackHoverCell = false;
            return true;
        }

        public bool UpdateHover(BattleTargetingState state, Vector2 screenPosition)
        {
            if (state == null || state.PendingBattleCard == null || state.PendingUserUnit == null || BattleBoardSystem.Instance == null)
            {
                return false;
            }

            Vector2Int hoveredGrid = BattleTargetingQueryService.ResolveTargetGridPosition(screenPosition);
            if (state.HasLastMoveHoverCell && hoveredGrid == state.LastHoveredMoveCell)
            {
                return false;
            }

            state.HasLastMoveHoverCell = true;
            state.LastHoveredMoveCell = hoveredGrid;
            return true;
        }

        public BattleMovePathEditResult SelectMoveTargetByClick(BattleTargetingState state, Vector2Int clickedGrid)
        {
            if (state == null || state.PendingBattleCard == null || state.PendingUserUnit == null || BattleBoardSystem.Instance == null)
            {
                return BattleMovePathEditResult.None;
            }

            if (state.PendingTargetingMode == BattleCardTargetingMode.MoveThenAttack
                && clickedGrid == state.PendingUserUnit.GridPosition)
            {
                return BattleMovePathEditResult.Confirm;
            }

            if (state.PendingTargetingMode == BattleCardTargetingMode.AttackThenMove
                && state.HasConfirmedAttackTarget
                && clickedGrid == state.PendingUserUnit.GridPosition)
            {
                return BattleMovePathEditResult.Confirm;
            }

            return BattleTargetingQueryService.ApplyMoveTargetClick(
                BattleBoardSystem.Instance,
                state.PendingUserUnit,
                state.CurrentMoveBudget,
                state.SelectableMoveCells,
                state.DrawnMovePath,
                clickedGrid);
        }

        public bool DragMovePath(BattleTargetingState state, Vector2 screenPosition)
        {
            if (state == null || state.PendingBattleCard == null || state.PendingUserUnit == null || BattleBoardSystem.Instance == null)
            {
                return false;
            }

            Vector2Int hoveredGrid = BattleTargetingQueryService.ResolveTargetGridPosition(screenPosition);
            if (state.HasLastDragCell && hoveredGrid == state.LastDraggedMoveCell)
            {
                return false;
            }

            state.HasLastDragCell = true;
            state.LastDraggedMoveCell = hoveredGrid;

            return BattleTargetingQueryService.TryApplyMovePathDrag(
                BattleBoardSystem.Instance,
                state.PendingUserUnit,
                state.CurrentMoveBudget,
                state.SelectableMoveCells,
                state.DrawnMovePath,
                hoveredGrid);
        }

        public BattleMoveConfirmResult Confirm(BattleTargetingState state)
        {
            if (state == null)
            {
                return BattleMoveConfirmResult.None;
            }

            if (state.PendingTargetingMode == BattleCardTargetingMode.MoveThenAttack)
            {
                return ConfirmMoveThenAttack(state);
            }

            if (state.PendingTargetingMode == BattleCardTargetingMode.AttackThenMove)
            {
                if (!state.HasConfirmedAttackTarget)
                {
                    return BattleMoveConfirmResult.None;
                }

                return BattleMoveConfirmResult.Play(
                    state.ConfirmedAttackTargetGrid,
                    state.ConfirmedAttackTargetUnit,
                    state.SelectedAttackTargetPositions,
                    state.DrawnMovePath.Count > 0 ? state.DrawnMovePath : null,
                    skipFollowUpAttack: false,
                    skipPostAttackMove: state.DrawnMovePath.Count == 0);
            }

            if (state.DrawnMovePath.Count == 0)
            {
                return BattleMoveConfirmResult.None;
            }

            return BattleMoveConfirmResult.Play(
                state.DrawnMovePath[state.DrawnMovePath.Count - 1],
                null,
                null,
                state.DrawnMovePath,
                skipFollowUpAttack: false,
                skipPostAttackMove: false);
        }

        private static BattleMoveConfirmResult ConfirmMoveThenAttack(BattleTargetingState state)
        {
            if (BattleBoardSystem.Instance == null || state.PendingUserUnit == null || state.PendingBattleCard == null)
            {
                return BattleMoveConfirmResult.Cancel;
            }

            Vector2Int finalCell = state.DrawnMovePath.Count > 0
                ? state.DrawnMovePath[state.DrawnMovePath.Count - 1]
                : state.PendingUserUnit.GridPosition;

            HashSet<Vector2Int> attackCells = BattleTargetingQueryService.ResolveAttackSelectionCells(
                BattleBoardSystem.Instance,
                state.PendingUserUnit,
                finalCell,
                state.PendingBattleCard);

            // 이동 후 공격 카드에서 공격 대상이 없으면 카드 사용을 취소하지 않고 이동만 수행합니다.
            if (attackCells.Count == 0)
            {
                return state.DrawnMovePath.Count > 0
                    ? BattleMoveConfirmResult.Play(finalCell, null, null, state.DrawnMovePath, skipFollowUpAttack: true, skipPostAttackMove: false)
                    : BattleMoveConfirmResult.RefreshPreview;
            }

            state.ConfirmedMovePath.Clear();
            if (state.DrawnMovePath.Count > 0)
            {
                state.ConfirmedMovePath.AddRange(state.DrawnMovePath);
            }

            state.SelectableAttackCells = attackCells;
            state.HasLastAttackHoverCell = false;
            return BattleMoveConfirmResult.SwitchToAttack;
        }
    }

    internal readonly struct BattleMoveConfirmResult
    {
        private BattleMoveConfirmResult(
            BattleMoveConfirmKind kind,
            Vector2Int targetGrid,
            BattleUnit targetUnit,
            IReadOnlyList<Vector2Int> attackTargetPositions,
            IReadOnlyList<Vector2Int> plannedPath,
            bool skipFollowUpAttack,
            bool skipPostAttackMove)
        {
            Kind = kind;
            TargetGrid = targetGrid;
            TargetUnit = targetUnit;
            AttackTargetPositions = attackTargetPositions;
            PlannedPath = plannedPath;
            SkipFollowUpAttack = skipFollowUpAttack;
            SkipPostAttackMove = skipPostAttackMove;
        }

        public static BattleMoveConfirmResult None { get; } = new(BattleMoveConfirmKind.None, Vector2Int.zero, null, null, null, false, false);

        public static BattleMoveConfirmResult Cancel { get; } = new(BattleMoveConfirmKind.Cancel, Vector2Int.zero, null, null, null, false, false);

        public static BattleMoveConfirmResult RefreshPreview { get; } = new(BattleMoveConfirmKind.RefreshPreview, Vector2Int.zero, null, null, null, false, false);

        public static BattleMoveConfirmResult SwitchToAttack { get; } = new(BattleMoveConfirmKind.SwitchToAttack, Vector2Int.zero, null, null, null, false, false);

        public static BattleMoveConfirmResult Play(
            Vector2Int targetGrid,
            BattleUnit targetUnit,
            IReadOnlyList<Vector2Int> attackTargetPositions,
            IReadOnlyList<Vector2Int> plannedPath,
            bool skipFollowUpAttack,
            bool skipPostAttackMove)
        {
            return new BattleMoveConfirmResult(
                BattleMoveConfirmKind.Play,
                targetGrid,
                targetUnit,
                attackTargetPositions,
                plannedPath,
                skipFollowUpAttack,
                skipPostAttackMove);
        }

        public BattleMoveConfirmKind Kind { get; }

        public Vector2Int TargetGrid { get; }

        public BattleUnit TargetUnit { get; }

        public IReadOnlyList<Vector2Int> AttackTargetPositions { get; }

        public IReadOnlyList<Vector2Int> PlannedPath { get; }

        public bool SkipFollowUpAttack { get; }

        public bool SkipPostAttackMove { get; }
    }

    internal enum BattleMoveConfirmKind
    {
        None,
        Cancel,
        RefreshPreview,
        SwitchToAttack,
        Play,
    }
}
