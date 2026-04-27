namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /*
     * BattleMoveGA
     *
     * 역할:
     * - 전투 이동 1회를 실행하기 위한 대상 위치, 경로, 이동량 정보를 담는 GameAction입니다.
     * - WasMoved는 후속 공격/이동 흐름에서 실제 이동 성공 여부를 확인할 때 사용합니다.
     */
    public class BattleMoveGA : GameAction
    {
        public BattleCard SourceCard { get; }
        public BattleUnit Unit { get; }
        public Vector2Int TargetPosition { get; }
        public IReadOnlyList<Vector2Int> PlannedPath { get; }
        public int BaseMoveAmount { get; }
        public int UnitSpeed { get; }
        public int FinalMoveAmount => BaseMoveAmount + UnitSpeed;
        public bool WasMoved { get; set; }

        public BattleMoveGA(
            BattleCard sourceCard,
            BattleUnit unit,
            Vector2Int targetPosition,
            IReadOnlyList<Vector2Int> plannedPath,
            int baseMoveAmount,
            int unitSpeed)
        {
            SourceCard = sourceCard;
            Unit = unit;
            TargetPosition = targetPosition;
            PlannedPath = plannedPath;
            BaseMoveAmount = baseMoveAmount;
            UnitSpeed = unitSpeed;
        }
    }
}
