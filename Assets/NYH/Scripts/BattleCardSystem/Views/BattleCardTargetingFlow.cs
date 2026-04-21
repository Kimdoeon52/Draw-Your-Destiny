namespace NYH.BattleCardSystem
{
    using System;
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /// <summary>
    /// 전투 카드 타겟팅의 시작, 취소, 이동/공격 단계 전환을 조정합니다.
    /// 씬 참조 연결, 손패 UI 복구, 프리뷰 계산, 실제 카드 실행은 전용 객체로 위임합니다.
    /// </summary>
    internal sealed class BattleCardTargetingFlow
    {
        private readonly BattleTargetingState state = new();
        private readonly BattleCardTargetingController inputRouter = new();
        private readonly BattleMoveTargetingFlow moveFlow = new();
        private readonly BattleUnitTargetingFlow unitFlow;
        private readonly BattleAttackTargetingFlow attackFlow;
        private readonly BattleAttackFollowUpFlow attackFollowUpFlow = new();
        private readonly BattleTargetingPreviewCoordinator previewCoordinator;
        private readonly Func<bool> canUseBoardTargeting;
        private readonly Action<BattleCardPlayRequest> onPlayRequested;

        public BattleCardTargetingFlow(
            BattleTargetingPreviewPresenter previewPresenter,
            BattleAttackTargetingFlow attackFlow,
            Func<bool> canUseBoardTargeting,
            Action<BattleCardPlayRequest> onPlayRequested)
        {
            previewCoordinator = new BattleTargetingPreviewCoordinator(previewPresenter);
            unitFlow = new BattleUnitTargetingFlow(moveFlow);
            this.attackFlow = attackFlow ?? new BattleAttackTargetingFlow();
            this.canUseBoardTargeting = canUseBoardTargeting;
            this.onPlayRequested = onPlayRequested;
        }

        public bool IsIdle => inputRouter.IsIdle;

        public void Tick(BattleCardTargetingInput input)
        {
            inputRouter.Tick(
                input,
                HasAttackTargetSelectionForUndo,
                UndoLastAttackTargetSelection,
                Cancel,
                ConfirmMovePath,
                HandleMoveTargetHover,
                HandleMovePathDrag,
                HandleAttackTargetHover,
                HandleBoardTargetingClick,
                ClearMoveDragState);
        }

        public void Begin(BattleCard battleCard, CardView cardView)
        {
            if (battleCard == null)
            {
                return;
            }

            Clear(returnCardToHand: false);
            state.Begin(battleCard, cardView);
            state.SelectableUnits.AddRange(BattleTargetingQueryService.FindUsablePlayerUnits(battleCard));
            inputRouter.Phase = BattleCardTargetingPhase.SelectUnit;
            previewCoordinator.ShowSelectableUnits(state.SelectableUnits);

            CardViewHoverSystem.Instance?.Hide();
            state.PendingCardView?.BeginExternalSelection();
        }

        public void Cancel()
        {
            Clear(returnCardToHand: true);
        }

        public void Clear(bool returnCardToHand)
        {
            CardViewHoverSystem.Instance?.Hide();

            if (returnCardToHand && state.PendingCardView != null)
            {
                state.PendingCardView.CancelExternalSelection();
            }

            inputRouter.Reset();
            state.Reset();
            previewCoordinator.Clear();
        }

        private bool HasAttackTargetSelectionForUndo()
        {
            return state.SelectedAttackTargetPositions.Count > 0;
        }

        private void UndoLastAttackTargetSelection()
        {
            if (state.SelectedAttackTargetPositions.Count == 0)
            {
                return;
            }

            state.SelectedAttackTargetPositions.RemoveAt(state.SelectedAttackTargetPositions.Count - 1);
            state.ConfirmedAttackTargetGrid = state.SelectedAttackTargetPositions.Count > 0
                ? state.SelectedAttackTargetPositions[state.SelectedAttackTargetPositions.Count - 1]
                : Vector2Int.zero;
            state.HasConfirmedAttackTarget = state.SelectedAttackTargetPositions.Count > 0;
            state.ConfirmedAttackTargetUnit = state.HasConfirmedAttackTarget
                ? BattleBoardSystem.Instance?.GetUnitAt(state.ConfirmedAttackTargetGrid)
                : null;
            previewCoordinator.RefreshAttackPreview(state, state.HasLastAttackHoverCell && state.WasLastAttackHoverValid
                ? state.LastHoveredAttackCell
                : (Vector2Int?)null);
        }

        private void ClearMoveDragState()
        {
            state.HasLastDragCell = false;
        }

        private void HandleBoardTargetingClick(Vector2 screenPosition)
        {
            if (canUseBoardTargeting != null && !canUseBoardTargeting.Invoke())
            {
                Cancel();
                return;
            }

            Vector2Int clickedGrid = BattleTargetingQueryService.ResolveTargetGridPosition(screenPosition);
            BattleUnit clickedUnit = BattleBoardSystem.Instance != null
                ? BattleBoardSystem.Instance.GetUnitAt(clickedGrid)
                : null;

            if (inputRouter.Phase == BattleCardTargetingPhase.SelectUnit)
            {
                TrySelectUserUnit(clickedUnit);
                return;
            }

            if (inputRouter.Phase == BattleCardTargetingPhase.SelectMoveTarget)
            {
                TrySelectMoveTargetByClick(clickedGrid);
                return;
            }

            if (inputRouter.Phase == BattleCardTargetingPhase.SelectAttackTarget)
            {
                TrySelectAttackTarget(clickedGrid, clickedUnit);
            }
        }

        private void TrySelectUserUnit(BattleUnit clickedUnit)
        {
            BattleUnitTargetingResult result = unitFlow.SelectUnit(state, clickedUnit);
            switch (result.Kind)
            {
                case BattleUnitTargetingResultKind.Cancel:
                    Cancel();
                    break;

                case BattleUnitTargetingResultKind.SelectMoveTarget:
                    inputRouter.Phase = BattleCardTargetingPhase.SelectMoveTarget;
                    previewCoordinator.RefreshMovePreview(state);
                    break;

                case BattleUnitTargetingResultKind.SelectAttackTarget:
                    inputRouter.Phase = BattleCardTargetingPhase.SelectAttackTarget;
                    previewCoordinator.RefreshAttackPreview(state, null);
                    break;

                case BattleUnitTargetingResultKind.Play:
                    RequestPlay(result.TargetGrid, result.TargetUnit, null, null);
                    break;
            }
        }

        private void HandleMoveTargetHover(Vector2 screenPosition)
        {
            if (moveFlow.UpdateHover(state, screenPosition))
            {
                previewCoordinator.RefreshMovePreview(state);
            }
        }

        private void TrySelectMoveTargetByClick(Vector2Int clickedGrid)
        {
            BattleMovePathEditResult editResult = moveFlow.SelectMoveTargetByClick(state, clickedGrid);
            if (editResult == BattleMovePathEditResult.Confirm)
            {
                ConfirmMovePath();
                return;
            }

            if (editResult == BattleMovePathEditResult.Changed)
            {
                previewCoordinator.RefreshMovePreview(state);
            }
        }

        private void HandleMovePathDrag(Vector2 screenPosition)
        {
            if (moveFlow.DragMovePath(state, screenPosition))
            {
                previewCoordinator.RefreshMovePreview(state);
            }
        }

        private void ConfirmMovePath()
        {
            BattleMoveConfirmResult result = moveFlow.Confirm(state);
            switch (result.Kind)
            {
                case BattleMoveConfirmKind.Cancel:
                    Cancel();
                    break;

                case BattleMoveConfirmKind.RefreshPreview:
                    previewCoordinator.RefreshMovePreview(state);
                    break;

                case BattleMoveConfirmKind.SwitchToAttack:
                    inputRouter.Phase = BattleCardTargetingPhase.SelectAttackTarget;
                    previewCoordinator.RefreshAttackPreview(state, null);
                    break;

                case BattleMoveConfirmKind.Play:
                    RequestPlay(
                        result.TargetGrid,
                        result.TargetUnit,
                        result.AttackTargetPositions,
                        result.PlannedPath,
                        result.SkipFollowUpAttack,
                        result.SkipPostAttackMove);
                    break;
            }
        }

        private void HandleAttackTargetHover(Vector2 screenPosition)
        {
            if (attackFlow.UpdateHover(state, screenPosition, out Vector2Int? previewGrid))
            {
                previewCoordinator.RefreshAttackPreview(state, previewGrid);
            }
        }

        private void TrySelectAttackTarget(Vector2Int clickedGrid, BattleUnit clickedUnit)
        {
            if (state.PendingBattleCard == null || state.PendingUserUnit == null)
            {
                Cancel();
                return;
            }

            BattleAttackTargetSelection selection = attackFlow.SelectTarget(
                BattleBoardSystem.Instance,
                state.PendingBattleCard,
                state.PendingUserUnit,
                state.ConfirmedMovePath,
                state.SelectableAttackCells,
                state.SelectedAttackTargetPositions,
                clickedGrid,
                clickedUnit);

            if (!selection.IsValid)
            {
                return;
            }

            BattleAttackFollowUpResult followUp = attackFollowUpFlow.Resolve(state, selection);
            switch (followUp.Kind)
            {
                case BattleAttackFollowUpKind.Cancel:
                    Cancel();
                    break;

                case BattleAttackFollowUpKind.RefreshAttackPreview:
                    previewCoordinator.RefreshAttackPreview(state, followUp.TargetGrid);
                    break;

                case BattleAttackFollowUpKind.SelectMoveTarget:
                    inputRouter.Phase = BattleCardTargetingPhase.SelectMoveTarget;
                    previewCoordinator.RefreshMovePreview(state);
                    break;

                case BattleAttackFollowUpKind.Play:
                    RequestPlay(
                        followUp.TargetGrid,
                        followUp.TargetUnit,
                        followUp.AttackTargetPositions,
                        followUp.PlannedPath,
                        followUp.SkipFollowUpAttack,
                        followUp.SkipPostAttackMove);
                    break;
            }
        }

        private void RequestPlay(
            Vector2Int targetGrid,
            BattleUnit targetUnit,
            IReadOnlyList<Vector2Int> attackTargetPositions,
            IReadOnlyList<Vector2Int> plannedPath,
            bool skipFollowUpAttack = false,
            bool skipPostAttackMove = false)
        {
            BattleCardPlayRequest request = BattleCardPlayRequestFactory.Create(
                state,
                targetGrid,
                targetUnit,
                attackTargetPositions,
                plannedPath,
                skipFollowUpAttack,
                skipPostAttackMove);

            if (!request.IsValid)
            {
                Cancel();
                return;
            }

            CardViewHoverSystem.Instance?.Hide();
            Clear(returnCardToHand: false);
            onPlayRequested?.Invoke(request);
        }
    }
}
