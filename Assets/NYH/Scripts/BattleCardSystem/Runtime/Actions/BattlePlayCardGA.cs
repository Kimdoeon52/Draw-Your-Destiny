namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /*
     * BattlePlayCardGA
     *
     * 역할:
     * - 손패에서 전투 카드 1장을 사용하겠다는 최상위 요청 GameAction입니다.
     * - 비용 지불 결과와 후속 이동/공격 생략 여부를 기록해 실행 체인이 공유합니다.
     */
    public class BattlePlayCardGA : GameAction
    {
        public BattleCard Card { get; }
        public BattleUnit UserUnit { get; }
        public BattleUnit TargetUnit { get; }
        public Vector2Int TargetPosition { get; }
        public IReadOnlyList<Vector2Int> AttackTargetPositions { get; }
        public IReadOnlyList<Vector2Int> PlannedPath { get; }
        public int UserCurrentHealth { get; }
        public int UserUnitSpeed { get; }
        public bool SkipFollowUpAttack { get; }
        public bool SkipPostAttackMove { get; }

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
            IReadOnlyList<Vector2Int> attackTargetPositions,
            IReadOnlyList<Vector2Int> plannedPath,
            int userCurrentHealth,
            int userUnitSpeed = 0,
            bool skipFollowUpAttack = false,
            bool skipPostAttackMove = false)
        {
            Card = card;
            UserUnit = userUnit;
            TargetUnit = targetUnit;
            TargetPosition = targetPosition;
            AttackTargetPositions = attackTargetPositions;
            PlannedPath = plannedPath;
            UserCurrentHealth = userCurrentHealth;
            UserUnitSpeed = userUnitSpeed;
            SkipFollowUpAttack = skipFollowUpAttack;
            SkipPostAttackMove = skipPostAttackMove;
        }
    }
}
