namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 공격 타겟 선택 이후 다중 선택 유지, 즉시 실행, 공격 후 이동 전환을 결정합니다.
    /// 공격 클릭 판정, 프리뷰 표시, 실제 카드 실행 호출은 담당하지 않고 선택 완료 뒤의 흐름만 담당합니다.
    /// </summary>
    internal sealed class BattleAttackFollowUpFlow
    {
        public BattleAttackFollowUpResult Resolve(BattleTargetingState state, BattleAttackTargetSelection selection)
        {
            if (state == null || !selection.IsValid)
            {
                return BattleAttackFollowUpResult.None;
            }

            state.ConfirmedAttackTargetGrid = selection.TargetGrid;
            state.ConfirmedAttackTargetUnit = selection.ResolvedTarget;
            state.HasConfirmedAttackTarget = true;

            if (!selection.IsComplete)
            {
                return BattleAttackFollowUpResult.RefreshAttackPreview(selection.TargetGrid);
            }

            if (state.PendingTargetingMode != BattleCardTargetingMode.AttackThenMove)
            {
                return BattleAttackFollowUpResult.Play(
                    selection.TargetGrid,
                    selection.ResolvedTarget,
                    state.SelectedAttackTargetPositions,
                    state.ConfirmedMovePath.Count > 0 ? state.ConfirmedMovePath : null,
                    skipFollowUpAttack: false,
                    skipPostAttackMove: false);
            }

            if (BattleBoardSystem.Instance == null)
            {
                return BattleAttackFollowUpResult.Cancel;
            }

            state.DrawnMovePath.Clear();
            state.ConfirmedMovePath.Clear();
            state.HasLastDragCell = false;
            state.SelectableMoveCells = BattleBoardSystem.Instance.GetSelectableMoveCells(state.PendingUserUnit, state.CurrentMoveBudget);

            // 공격 후 이동 카드에서 이동할 칸이 없으면 공격만 실행되도록 fallback합니다.
            if (state.SelectableMoveCells.Count == 0)
            {
                return BattleAttackFollowUpResult.Play(
                    state.ConfirmedAttackTargetGrid,
                    state.ConfirmedAttackTargetUnit,
                    state.SelectedAttackTargetPositions,
                    null,
                    skipFollowUpAttack: false,
                    skipPostAttackMove: true);
            }

            return BattleAttackFollowUpResult.SelectMoveTarget;
        }
    }

    internal readonly struct BattleAttackFollowUpResult
    {
        private BattleAttackFollowUpResult(
            BattleAttackFollowUpKind kind,
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

        public static BattleAttackFollowUpResult None { get; } = new(
            BattleAttackFollowUpKind.None,
            Vector2Int.zero,
            null,
            null,
            null,
            skipFollowUpAttack: false,
            skipPostAttackMove: false);

        public static BattleAttackFollowUpResult Cancel { get; } = new(
            BattleAttackFollowUpKind.Cancel,
            Vector2Int.zero,
            null,
            null,
            null,
            skipFollowUpAttack: false,
            skipPostAttackMove: false);

        public static BattleAttackFollowUpResult SelectMoveTarget { get; } = new(
            BattleAttackFollowUpKind.SelectMoveTarget,
            Vector2Int.zero,
            null,
            null,
            null,
            skipFollowUpAttack: false,
            skipPostAttackMove: false);

        public static BattleAttackFollowUpResult RefreshAttackPreview(Vector2Int previewGrid)
        {
            return new BattleAttackFollowUpResult(
                BattleAttackFollowUpKind.RefreshAttackPreview,
                previewGrid,
                null,
                null,
                null,
                skipFollowUpAttack: false,
                skipPostAttackMove: false);
        }

        public static BattleAttackFollowUpResult Play(
            Vector2Int targetGrid,
            BattleUnit targetUnit,
            IReadOnlyList<Vector2Int> attackTargetPositions,
            IReadOnlyList<Vector2Int> plannedPath,
            bool skipFollowUpAttack,
            bool skipPostAttackMove)
        {
            return new BattleAttackFollowUpResult(
                BattleAttackFollowUpKind.Play,
                targetGrid,
                targetUnit,
                attackTargetPositions,
                plannedPath,
                skipFollowUpAttack,
                skipPostAttackMove);
        }

        public BattleAttackFollowUpKind Kind { get; }

        public Vector2Int TargetGrid { get; }

        public BattleUnit TargetUnit { get; }

        public IReadOnlyList<Vector2Int> AttackTargetPositions { get; }

        public IReadOnlyList<Vector2Int> PlannedPath { get; }

        public bool SkipFollowUpAttack { get; }

        public bool SkipPostAttackMove { get; }
    }

    internal enum BattleAttackFollowUpKind
    {
        None,
        Cancel,
        RefreshAttackPreview,
        SelectMoveTarget,
        Play,
    }
}
