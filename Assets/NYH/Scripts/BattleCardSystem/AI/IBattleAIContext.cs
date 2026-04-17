namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

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
