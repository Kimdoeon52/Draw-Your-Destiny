namespace NYH.BattleCardSystem
{
    using NYH.CoreCardSystem;
    using UnityEngine;

    public class BattlePlayCardGA : GameAction
    {
        public BattleCard Card { get; }
        public BattleUnit UserUnit { get; }
        public BattleUnit TargetUnit { get; }
        public Vector2Int TargetPosition { get; }
        public int UserCurrentHealth { get; }
        public int UserUnitSpeed { get; }

        public bool WasPlayed { get; set; }
        public bool PaidByFood { get; set; }
        public bool UsedHealthPenalty { get; set; }
        public int FoodSpent { get; set; }
        public int HealthPenaltyAmount { get; set; }

        public BattlePlayCardGA(
            BattleCard card,
            BattleUnit userUnit,
            BattleUnit targetUnit,
            Vector2Int targetPosition,
            int userCurrentHealth,
            int userUnitSpeed = 0)
        {
            Card = card;
            UserUnit = userUnit;
            TargetUnit = targetUnit;
            TargetPosition = targetPosition;
            UserCurrentHealth = userCurrentHealth;
            UserUnitSpeed = userUnitSpeed;
        }
    }
}
