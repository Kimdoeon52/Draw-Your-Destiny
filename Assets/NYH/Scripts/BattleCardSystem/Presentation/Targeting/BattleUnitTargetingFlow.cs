namespace NYH.BattleCardSystem
{
    using UnityEngine;

    /// <summary>
    /// 카드 타겟팅에서 처음 선택한 아군 유닛을 이동/공격/즉시 실행 단계로 분류합니다.
    /// 입력 라우팅, 프리뷰 표시, 카드 실행 요청 전달은 담당하지 않고 첫 단계 상태 구성만 담당합니다.
    /// </summary>
    internal sealed class BattleUnitTargetingFlow
    {
        private readonly BattleMoveTargetingFlow moveFlow;

        public BattleUnitTargetingFlow(BattleMoveTargetingFlow moveFlow)
        {
            this.moveFlow = moveFlow;
        }

        public BattleUnitTargetingResult SelectUnit(BattleTargetingState state, BattleUnit clickedUnit)
        {
            if (state == null || clickedUnit == null || clickedUnit.Team != BattleTeam.Player || !clickedUnit.IsAlive)
            {
                return BattleUnitTargetingResult.None;
            }

            if (state.SelectableUnits.Count > 0 && !state.SelectableUnits.Contains(clickedUnit))
            {
                return BattleUnitTargetingResult.None;
            }

            if (BattleBoardSystem.Instance == null || state.PendingBattleCard == null)
            {
                return BattleUnitTargetingResult.Cancel;
            }

            if (!BattleCardUnitTypeRestriction.CanUserUnitPlay(state.PendingBattleCard, clickedUnit))
            {
                return BattleUnitTargetingResult.None;
            }

            if (state.PendingTargetingMode == BattleCardTargetingMode.MoveOnly
                || state.PendingTargetingMode == BattleCardTargetingMode.MoveThenAttack)
            {
                return moveFlow != null && moveFlow.BeginMoveSelection(state, clickedUnit)
                    ? BattleUnitTargetingResult.SelectMoveTarget
                    : BattleUnitTargetingResult.Cancel;
            }

            if (state.PendingTargetingMode == BattleCardTargetingMode.AttackOnly
                || state.PendingTargetingMode == BattleCardTargetingMode.AttackThenMove)
            {
                BeginAttackSelection(state, clickedUnit);
                return BattleUnitTargetingResult.SelectAttackTarget;
            }

            return BattleUnitTargetingResult.Play(clickedUnit.GridPosition, clickedUnit);
        }

        private static void BeginAttackSelection(BattleTargetingState state, BattleUnit clickedUnit)
        {
            state.PendingUserUnit = clickedUnit;
            state.SelectableAttackCells = BattleTargetingQueryService.ResolveAttackSelectionCells(
                BattleBoardSystem.Instance,
                clickedUnit,
                clickedUnit.GridPosition,
                state.PendingBattleCard);
            state.CurrentMoveBudget = state.PendingTargetingMode == BattleCardTargetingMode.AttackThenMove
                ? BattleTargetingQueryService.ResolveMoveBudget(state.PendingBattleCard, clickedUnit)
                : 0;
            state.ConfirmedMovePath.Clear();
            state.SelectedAttackTargetPositions.Clear();
            state.ConfirmedAttackTargetUnit = null;
            state.HasConfirmedAttackTarget = false;
            state.HasLastMoveHoverCell = false;
            state.HasLastAttackHoverCell = false;
        }
    }

    internal readonly struct BattleUnitTargetingResult
    {
        private BattleUnitTargetingResult(BattleUnitTargetingResultKind kind, Vector2Int targetGrid, BattleUnit targetUnit)
        {
            Kind = kind;
            TargetGrid = targetGrid;
            TargetUnit = targetUnit;
        }

        public static BattleUnitTargetingResult None { get; } = new(BattleUnitTargetingResultKind.None, Vector2Int.zero, null);

        public static BattleUnitTargetingResult Cancel { get; } = new(BattleUnitTargetingResultKind.Cancel, Vector2Int.zero, null);

        public static BattleUnitTargetingResult SelectMoveTarget { get; } = new(BattleUnitTargetingResultKind.SelectMoveTarget, Vector2Int.zero, null);

        public static BattleUnitTargetingResult SelectAttackTarget { get; } = new(BattleUnitTargetingResultKind.SelectAttackTarget, Vector2Int.zero, null);

        public static BattleUnitTargetingResult Play(Vector2Int targetGrid, BattleUnit targetUnit)
        {
            return new BattleUnitTargetingResult(BattleUnitTargetingResultKind.Play, targetGrid, targetUnit);
        }

        public BattleUnitTargetingResultKind Kind { get; }

        public Vector2Int TargetGrid { get; }

        public BattleUnit TargetUnit { get; }
    }

    internal enum BattleUnitTargetingResultKind
    {
        None,
        Cancel,
        SelectMoveTarget,
        SelectAttackTarget,
        Play,
    }
}
