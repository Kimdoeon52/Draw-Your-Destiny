namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    /*
     * IBattleAIContext
     *
     * 역할:
     * - AI 전략 ScriptableObject가 보드와 유닛을 직접 찾지 않고 필요한 정보만 요청하게 하는 인터페이스입니다.
     * - 구현체는 BattleEnemyAIController입니다.
     */
    public interface IBattleAIContext
    {
        BattleUnit GetNearestPlayerUnit(BattleUnit requester);
        BattleUnit GetNearestEnemyUnit(BattleUnit requester);
        Vector2Int GetGridPosition(BattleUnit unit);
        int GetMoveBudget(BattleUnit unit);
        int GetAttackRange(BattleUnit unit);
        List<Vector2Int> FindPathTowards(BattleUnit unit, Vector2Int targetCell, int moveBudget);
        UniTask MoveUnitAlongPathAsync(BattleUnit unit, IReadOnlyList<Vector2Int> path);
        bool TryAttackPlayerInRange(BattleUnit unit, int attackRange);
        bool IsCellWalkable(Vector2Int cell);
    }
}
