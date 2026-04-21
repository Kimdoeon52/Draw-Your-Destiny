namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /// <summary>
    /// 전투 카드 타겟팅 중 필요한 임시 상태를 보관합니다.
    /// 입력 처리, 프리뷰 렌더링, 카드 실행은 담당하지 않습니다.
    /// </summary>
    internal sealed class BattleTargetingState
    {
        public BattleCard PendingBattleCard { get; private set; }

        public BattleCardTargetingMode PendingTargetingMode { get; private set; } = BattleCardTargetingMode.Auto;

        public CardView PendingCardView { get; private set; }

        public BattleUnit PendingUserUnit { get; set; }

        public List<BattleUnit> SelectableUnits { get; } = new();

        public HashSet<Vector2Int> SelectableMoveCells { get; set; } = new();

        public HashSet<Vector2Int> SelectableAttackCells { get; set; } = new();

        public List<Vector2Int> DrawnMovePath { get; } = new();

        public List<Vector2Int> ConfirmedMovePath { get; } = new();

        public List<Vector2Int> SelectedAttackTargetPositions { get; } = new();

        public BattleUnit ConfirmedAttackTargetUnit { get; set; }

        public bool HasConfirmedAttackTarget { get; set; }

        public Vector2Int ConfirmedAttackTargetGrid { get; set; }

        public int CurrentMoveBudget { get; set; }

        public bool HasLastDragCell { get; set; }

        public Vector2Int LastDraggedMoveCell { get; set; }

        public bool HasLastMoveHoverCell { get; set; }

        public Vector2Int LastHoveredMoveCell { get; set; }

        public bool HasLastAttackHoverCell { get; set; }

        public bool WasLastAttackHoverValid { get; set; }

        public Vector2Int LastHoveredAttackCell { get; set; }

        public void Begin(BattleCard battleCard, CardView cardView)
        {
            Reset();
            PendingBattleCard = battleCard;
            PendingTargetingMode = BattleCardTargetingUtility.ResolveTargetingMode(battleCard);
            PendingCardView = cardView;
        }

        public void Reset()
        {
            PendingBattleCard = null;
            PendingTargetingMode = BattleCardTargetingMode.Auto;
            PendingCardView = null;
            PendingUserUnit = null;
            SelectableUnits.Clear();
            SelectableMoveCells.Clear();
            SelectableAttackCells.Clear();
            DrawnMovePath.Clear();
            ConfirmedMovePath.Clear();
            SelectedAttackTargetPositions.Clear();
            ConfirmedAttackTargetUnit = null;
            HasConfirmedAttackTarget = false;
            ConfirmedAttackTargetGrid = Vector2Int.zero;
            CurrentMoveBudget = 0;
            HasLastDragCell = false;
            LastDraggedMoveCell = Vector2Int.zero;
            HasLastMoveHoverCell = false;
            LastHoveredMoveCell = Vector2Int.zero;
            HasLastAttackHoverCell = false;
            WasLastAttackHoverValid = false;
            LastHoveredAttackCell = Vector2Int.zero;
        }
    }
}
