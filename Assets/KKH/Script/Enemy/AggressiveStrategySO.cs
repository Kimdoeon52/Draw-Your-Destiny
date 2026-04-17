using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NYH.BattleCardSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "AggressiveStrategy", menuName = "AI/Strategy/Aggressive Strategy")]
public class AggressiveStrategySO : AIBehaviorStrategySO
{
    public override async UniTask ExecuteBehaviorAsync(EnemyAIManager context, Transform unit)
    {
        Vector3Int unitCell = context.grid.WorldToCell(unit.position);
        Vector3Int targetCell = context.grid.WorldToCell(context.playerUnit.position);
        List<Vector3Int> path = context.FindPathToPlayer(unitCell, targetCell);

        if (path != null && path.Count > 0)
        {
            await context.MoveUnitAlongPathAsync(unit, path);
        }

        Vector3Int currentCell = context.grid.WorldToCell(unit.position);
        context.TryAttackNearbyPlayer(unit, currentCell);
        await UniTask.Delay(200);
    }

    public override async UniTask ExecuteBehaviorAsync(IBattleAIContext context, BattleUnit unit)
    {
        if (context == null || unit == null)
        {
            return;
        }

        BattleUnit target = context.GetNearestPlayerUnit(unit);
        if (target == null)
        {
            return;
        }

        List<Vector2Int> path = context.FindPathTowards(unit, context.GetGridPosition(target), context.GetMoveBudget(unit));
        if (path != null && path.Count > 0)
        {
            await context.MoveUnitAlongPathAsync(unit, path);
        }

        context.TryAttackPlayerInRange(unit, context.GetAttackRange(unit));
        await UniTask.Delay(200);
    }
}
