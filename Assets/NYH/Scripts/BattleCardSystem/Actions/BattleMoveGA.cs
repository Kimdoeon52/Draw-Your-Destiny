namespace NYH.BattleCardSystem
{
    using NYH.CoreCardSystem;
    using UnityEngine;

    public class BattleMoveGA : GameAction
    {
        public BattleCard SourceCard { get; }
        public BattleUnit Unit { get; }
        public Vector2Int TargetPosition { get; }
        public int BaseMoveAmount { get; }
        public int UnitSpeed { get; }
        public int FinalMoveAmount => BaseMoveAmount + UnitSpeed;
        public bool WasMoved { get; set; }

        public BattleMoveGA(BattleCard sourceCard, BattleUnit unit, Vector2Int targetPosition, int baseMoveAmount, int unitSpeed)
        {
            SourceCard = sourceCard;
            Unit = unit;
            TargetPosition = targetPosition;
            BaseMoveAmount = baseMoveAmount;
            UnitSpeed = unitSpeed;
        }
    }
}
