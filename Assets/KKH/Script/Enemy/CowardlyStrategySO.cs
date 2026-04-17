using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NYH.BattleCardSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "CowardlyStrategy", menuName = "AI/Strategy/Cowardly Strategy")]
public class CowardlyStrategySO : AIBehaviorStrategySO
{
    [Header("거리 설정")]
    [SerializeField] private float idealDistance = 5f;
    [SerializeField] private int moveStep = 3;

    public override async UniTask ExecuteBehaviorAsync(EnemyAIManager context, Transform unit)
    {
        Vector3Int unitCell = context.grid.WorldToCell(unit.position);
        Vector3Int playerCell = context.grid.WorldToCell(context.playerUnit.position);
        float distToPlayer = Vector3.Distance(unit.position, context.playerUnit.position);

        Vector3Int targetCell = unitCell;
        if (distToPlayer > idealDistance + 1f)
        {
            targetCell = playerCell;
        }
        else if (distToPlayer < idealDistance - 1f)
        {
            Vector3 fleeDir = (unit.position - context.playerUnit.position).normalized;
            targetCell = unitCell + new Vector3Int(
                Mathf.RoundToInt(fleeDir.x * moveStep),
                Mathf.RoundToInt(fleeDir.y * moveStep),
                0);
        }

        if (targetCell != unitCell)
        {
            if (!context.IsCellWalkable(targetCell))
            {
                targetCell = GetBestAdjacentCell(context, unitCell, playerCell, distToPlayer < idealDistance);
            }

            List<Vector3Int> path = context.FindPathToPlayer(unitCell, targetCell);
            if (path != null && path.Count > 0)
            {
                await context.MoveUnitAlongPathAsync(unit, path);
            }
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

        BattleUnit player = context.GetNearestPlayerUnit(unit);
        if (player == null)
        {
            return;
        }

        Vector2Int unitCell = context.GetGridPosition(unit);
        Vector2Int playerCell = context.GetGridPosition(player);
        float distance = Vector2Int.Distance(unitCell, playerCell);
        Vector2Int desiredCell = unitCell;

        if (distance > idealDistance + 1f)
        {
            desiredCell = playerCell;
        }
        else if (distance < idealDistance - 1f)
        {
            Vector2 direction = ((Vector2)(unitCell - playerCell)).normalized;
            desiredCell = unitCell + new Vector2Int(
                Mathf.RoundToInt(direction.x * moveStep),
                Mathf.RoundToInt(direction.y * moveStep));
        }

        List<Vector2Int> path = context.FindPathTowards(unit, desiredCell, context.GetMoveBudget(unit));
        if (path != null && path.Count > 0)
        {
            await context.MoveUnitAlongPathAsync(unit, path);
        }

        context.TryAttackPlayerInRange(unit, context.GetAttackRange(unit));
        await UniTask.Delay(200);
    }

    private Vector3Int GetBestAdjacentCell(EnemyAIManager context, Vector3Int unitCell, Vector3Int playerCell, bool shouldFlee)
    {
        Vector3Int bestCell = unitCell;
        float bestDist = shouldFlee ? -1f : float.MaxValue;
        Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        foreach (var dir in dirs)
        {
            Vector3Int neighbor = unitCell + dir;
            if (context.IsCellWalkable(neighbor))
            {
                float d = Vector3.Distance(neighbor, playerCell);
                if (shouldFlee ? (d > bestDist) : (d < bestDist))
                {
                    bestDist = d;
                    bestCell = neighbor;
                }
            }
        }
        return bestCell;
    }
}
