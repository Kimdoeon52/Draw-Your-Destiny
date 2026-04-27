namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /// <summary>
    /// 타겟팅 상태와 선택 결과를 BattleCardSystem.PlayCard 호출용 요청으로 변환합니다.
    /// 실제 카드 실행, 비용 지불, 카드뷰 애니메이션은 담당하지 않습니다.
    /// </summary>
    internal static class BattleCardPlayRequestFactory
    {
        public static BattleCardPlayRequest Create(
            BattleTargetingState state,
            Vector2Int targetGrid,
            BattleUnit targetUnit,
            IReadOnlyList<Vector2Int> attackTargetPositions,
            IReadOnlyList<Vector2Int> plannedPath,
            bool skipFollowUpAttack,
            bool skipPostAttackMove)
        {
            if (state == null || state.PendingBattleCard == null || state.PendingUserUnit == null)
            {
                return BattleCardPlayRequest.Invalid;
            }

            List<Vector2Int> attackTargetSnapshot = attackTargetPositions != null
                ? new List<Vector2Int>(attackTargetPositions)
                : (state.SelectedAttackTargetPositions.Count > 0 ? new List<Vector2Int>(state.SelectedAttackTargetPositions) : null);
            List<Vector2Int> plannedPathSnapshot = plannedPath != null
                ? new List<Vector2Int>(plannedPath)
                : (state.ConfirmedMovePath.Count > 0 ? new List<Vector2Int>(state.ConfirmedMovePath) : null);

            return new BattleCardPlayRequest(
                isValid: true,
                card: state.PendingBattleCard,
                userUnit: state.PendingUserUnit,
                playedCardView: state.PendingCardView,
                targetGrid: targetGrid,
                targetUnit: targetUnit,
                attackTargetPositions: attackTargetSnapshot,
                plannedPath: plannedPathSnapshot,
                skipFollowUpAttack: skipFollowUpAttack,
                skipPostAttackMove: skipPostAttackMove);
        }
    }

    internal readonly struct BattleCardPlayRequest
    {
        public static BattleCardPlayRequest Invalid { get; } = new(
            isValid: false,
            card: null,
            userUnit: null,
            playedCardView: null,
            targetGrid: Vector2Int.zero,
            targetUnit: null,
            attackTargetPositions: null,
            plannedPath: null,
            skipFollowUpAttack: false,
            skipPostAttackMove: false);

        public BattleCardPlayRequest(
            bool isValid,
            BattleCard card,
            BattleUnit userUnit,
            CardView playedCardView,
            Vector2Int targetGrid,
            BattleUnit targetUnit,
            IReadOnlyList<Vector2Int> attackTargetPositions,
            IReadOnlyList<Vector2Int> plannedPath,
            bool skipFollowUpAttack,
            bool skipPostAttackMove)
        {
            IsValid = isValid;
            Card = card;
            UserUnit = userUnit;
            PlayedCardView = playedCardView;
            TargetGrid = targetGrid;
            TargetUnit = targetUnit;
            AttackTargetPositions = attackTargetPositions;
            PlannedPath = plannedPath;
            SkipFollowUpAttack = skipFollowUpAttack;
            SkipPostAttackMove = skipPostAttackMove;
        }

        public bool IsValid { get; }

        public BattleCard Card { get; }

        public BattleUnit UserUnit { get; }

        public CardView PlayedCardView { get; }

        public Vector2Int TargetGrid { get; }

        public BattleUnit TargetUnit { get; }

        public IReadOnlyList<Vector2Int> AttackTargetPositions { get; }

        public IReadOnlyList<Vector2Int> PlannedPath { get; }

        public bool SkipFollowUpAttack { get; }

        public bool SkipPostAttackMove { get; }
    }
}
