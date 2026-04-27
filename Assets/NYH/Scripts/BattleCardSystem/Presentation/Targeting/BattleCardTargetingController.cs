namespace NYH.BattleCardSystem
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 전투 카드 타겟팅 단계와 입력 라우팅을 담당합니다.
    /// 이동/공격 가능 여부 계산, 프리뷰 렌더링, 카드 실행은 담당하지 않습니다.
    /// </summary>
    internal sealed class BattleCardTargetingController
    {
        public BattleCardTargetingPhase Phase { get; set; } = BattleCardTargetingPhase.None;

        public bool IsIdle => Phase == BattleCardTargetingPhase.None;

        public void Reset()
        {
            Phase = BattleCardTargetingPhase.None;
        }

        public void Tick(
            BattleCardTargetingInput input,
            Func<bool> canUndoAttackTarget,
            Action undoAttackTarget,
            Action cancelTargeting,
            Action confirmMovePath,
            Action<Vector2> handleMoveTargetHover,
            Action<Vector2> handleMovePathDrag,
            Action<Vector2> handleAttackTargetHover,
            Action<Vector2> handleUtilityTargetHover,
            Action<Vector2> handleBoardTargetingClick,
            Action clearMoveDragState)
        {
            if (IsIdle)
            {
                return;
            }

            if (input.CancelPressed)
            {
                if ((Phase == BattleCardTargetingPhase.SelectAttackTarget
                    || Phase == BattleCardTargetingPhase.SelectUtilityGrid)
                    && canUndoAttackTarget != null
                    && canUndoAttackTarget.Invoke())
                {
                    undoAttackTarget?.Invoke();
                    return;
                }

                cancelTargeting?.Invoke();
                return;
            }

            if (Phase == BattleCardTargetingPhase.SelectMoveTarget && input.ConfirmMovePressed)
            {
                confirmMovePath?.Invoke();
                return;
            }

            if (Phase == BattleCardTargetingPhase.SelectMoveTarget)
            {
                handleMoveTargetHover?.Invoke(input.MousePosition);
            }

            if (Phase == BattleCardTargetingPhase.SelectMoveTarget && input.PrimaryHeld)
            {
                handleMovePathDrag?.Invoke(input.MousePosition);
            }

            if (Phase == BattleCardTargetingPhase.SelectAttackTarget)
            {
                handleAttackTargetHover?.Invoke(input.MousePosition);
            }

            if (Phase == BattleCardTargetingPhase.SelectUtilityGrid)
            {
                handleUtilityTargetHover?.Invoke(input.MousePosition);
            }

            if (input.PrimaryReleased)
            {
                clearMoveDragState?.Invoke();
            }

            if (input.PrimaryPressed)
            {
                handleBoardTargetingClick?.Invoke(input.MousePosition);
            }
        }
    }

    internal enum BattleCardTargetingPhase
    {
        None,
        SelectUnit,
        SelectMoveTarget,
        SelectAttackTarget,
        SelectUtilityGrid,
    }

    internal readonly struct BattleCardTargetingInput
    {
        public BattleCardTargetingInput(
            Vector2 mousePosition,
            bool cancelPressed,
            bool confirmMovePressed,
            bool primaryHeld,
            bool primaryReleased,
            bool primaryPressed)
        {
            MousePosition = mousePosition;
            CancelPressed = cancelPressed;
            ConfirmMovePressed = confirmMovePressed;
            PrimaryHeld = primaryHeld;
            PrimaryReleased = primaryReleased;
            PrimaryPressed = primaryPressed;
        }

        public Vector2 MousePosition { get; }

        public bool CancelPressed { get; }

        public bool ConfirmMovePressed { get; }

        public bool PrimaryHeld { get; }

        public bool PrimaryReleased { get; }

        public bool PrimaryPressed { get; }
    }
}
