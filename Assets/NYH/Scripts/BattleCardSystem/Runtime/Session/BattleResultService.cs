namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;

    /// <summary>
    /// 전투 종료 여부와 결과 수치를 계산합니다.
    /// BattleManager의 phase 전환이나 이벤트 발행은 담당하지 않습니다.
    /// </summary>
    internal static class BattleResultService
    {
        public static BattleResult BuildResult(
            bool isVictory,
            int turnCount,
            IReadOnlyList<BattleUnit> playerUnits,
            IReadOnlyList<BattleUnit> enemyUnits)
        {
            return new BattleResult
            {
                IsVictory = isVictory,
                TurnCount = turnCount,
                SurvivingPlayerUnits = CountAliveUnits(playerUnits),
                SurvivingEnemyUnits = CountAliveUnits(enemyUnits),
            };
        }

        public static bool HasAliveUnits(IReadOnlyList<BattleUnit> units)
        {
            if (units == null)
            {
                return false;
            }

            foreach (BattleUnit unit in units)
            {
                if (unit != null && unit.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountAliveUnits(IReadOnlyList<BattleUnit> units)
        {
            int count = 0;
            if (units == null)
            {
                return count;
            }

            foreach (BattleUnit unit in units)
            {
                if (unit != null && unit.IsAlive)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
