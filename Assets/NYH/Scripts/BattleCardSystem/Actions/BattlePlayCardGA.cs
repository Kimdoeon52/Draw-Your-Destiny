namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    public class BattlePlayCardGA : GameAction
    {
        public BattleCard Card { get; }
        public BattleUnit UserUnit { get; }
        public BattleUnit TargetUnit { get; }
        public Vector2Int TargetPosition { get; }
        public IReadOnlyList<Vector2Int> PlannedPath { get; }
        public int UserCurrentHealth { get; }
        public int UserUnitSpeed { get; }

        public bool WasPlayed { get; set; }
        public bool PaidByActionPoints { get; set; }
        public bool UsedHealthPenalty { get; set; }
        public int ActionPointsSpent { get; set; }
        public int HealthPenaltyAmount { get; set; }

        public BattlePlayCardGA(
            BattleCard card,
            BattleUnit userUnit,
            BattleUnit targetUnit,
            Vector2Int targetPosition,
            IReadOnlyList<Vector2Int> plannedPath,
            int userCurrentHealth,
            int userUnitSpeed = 0)
        {
            Card = card;
            UserUnit = userUnit;
            TargetUnit = targetUnit;
            TargetPosition = targetPosition;
            PlannedPath = plannedPath;
            UserCurrentHealth = userCurrentHealth;
            UserUnitSpeed = userUnitSpeed;
        }
    }
}
