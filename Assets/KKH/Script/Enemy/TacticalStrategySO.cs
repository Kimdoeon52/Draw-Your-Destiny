using Cysharp.Threading.Tasks;
using NYH.BattleCardSystem;
using UnityEngine;

namespace KKH.Script.Enemy
{
    [CreateAssetMenu(fileName = "TacticalStrategy", menuName = "AI/Strategy/Tactical Strategy")]
    public class TacticalStrategySO : AIBehaviorStrategySO
    {
        private enum State
        {
            Analyze,
            HitAndRun,
            Positionning,
            FinalAttack,
            EndTurn,
        }

        public override async UniTask ExecuteBehaviorAsync(EnemyAIManager context, Transform unit)
        {
            State currentState = State.Analyze;
            int attackRange = 2;

            while (currentState != State.EndTurn)
            {
                Vector3Int unitCell = context.grid.WorldToCell(unit.position);
                Vector3Int playerCell = context.grid.WorldToCell(context.playerUnit.position);

                switch (currentState)
                {
                    case State.Analyze:
                        currentState = DetermineNextState(unitCell, playerCell, attackRange);
                        break;
                    case State.HitAndRun:
                        await HandleHitAndRun(context, unit, unitCell, playerCell);
                        currentState = State.EndTurn;
                        break;
                    case State.Positionning:
                        await HandlePositionning(context, unit, unitCell, playerCell, attackRange);
                        currentState = State.FinalAttack;
                        break;
                    case State.FinalAttack:
                        context.TryAttackNearbyPlayer(unit, context.grid.WorldToCell(unit.position));
                        currentState = State.EndTurn;
                        break;
                }

                await UniTask.Yield();
            }
        }

        public override async UniTask ExecuteBehaviorAsync(IBattleAIContext context, BattleUnit unit)
        {
            if (context == null || unit == null)
            {
                return;
            }

            State currentState = State.Analyze;
            int attackRange = context.GetAttackRange(unit);

            while (currentState != State.EndTurn)
            {
                BattleUnit player = context.GetNearestPlayerUnit(unit);
                if (player == null)
                {
                    return;
                }

                Vector2Int unitCell = context.GetGridPosition(unit);
                Vector2Int playerCell = context.GetGridPosition(player);

                switch (currentState)
                {
                    case State.Analyze:
                        currentState = DetermineNextState(unitCell, playerCell, attackRange);
                        break;
                    case State.HitAndRun:
                        context.TryAttackPlayerInRange(unit, attackRange);
                        Vector2Int fleeTarget = unitCell + new Vector2Int(
                            Mathf.Clamp(unitCell.x - playerCell.x, -1, 1),
                            Mathf.Clamp(unitCell.y - playerCell.y, -1, 1)) * context.GetMoveBudget(unit);
                        var fleePath = context.FindPathTowards(unit, fleeTarget, context.GetMoveBudget(unit));
                        if (fleePath.Count > 0)
                        {
                            await context.MoveUnitAlongPathAsync(unit, fleePath);
                        }
                        currentState = State.EndTurn;
                        break;
                    case State.Positionning:
                        Vector2Int bestCell = FindBestTacticalCell(context, unit, playerCell, attackRange);
                        var path = context.FindPathTowards(unit, bestCell, context.GetMoveBudget(unit));
                        if (path.Count > 0)
                        {
                            await context.MoveUnitAlongPathAsync(unit, path);
                        }
                        currentState = State.FinalAttack;
                        break;
                    case State.FinalAttack:
                        context.TryAttackPlayerInRange(unit, attackRange);
                        currentState = State.EndTurn;
                        break;
                }

                await UniTask.Yield();
            }
        }

        private State DetermineNextState(Vector2Int unitCell, Vector2Int playerCell, int attackRange)
        {
            int dist = Mathf.Abs(unitCell.x - playerCell.x) + Mathf.Abs(unitCell.y - playerCell.y);
            bool isSameAxis = unitCell.x == playerCell.x || unitCell.y == playerCell.y;
            return (isSameAxis && dist <= attackRange) ? State.HitAndRun : State.Positionning;
        }

        private State DetermineNextState(Vector3Int unitCell, Vector3Int playerCell, int attackRange)
        {
            int dist = Mathf.Abs(unitCell.x - playerCell.x) + Mathf.Abs(unitCell.y - playerCell.y);
            bool isSameAxis = unitCell.x == playerCell.x || unitCell.y == playerCell.y;
            return (isSameAxis && dist <= attackRange) ? State.HitAndRun : State.Positionning;
        }

        private async UniTask HandleHitAndRun(EnemyAIManager context, Transform unit, Vector3Int unitCell, Vector3Int playerCell)
        {
            context.TryAttackNearbyPlayer(unit, unitCell);
            Vector3Int fleeDir = new(
                Mathf.Clamp(unitCell.x - playerCell.x, -1, 1),
                Mathf.Clamp(unitCell.y - playerCell.y, -1, 1),
                0);
            Vector3Int target = unitCell + fleeDir * 5;
            var path = context.FindPathToPlayer(unitCell, target);
            if (path.Count > 0)
            {
                await context.MoveUnitAlongPathAsync(unit, path);
            }
        }

        private async UniTask HandlePositionning(EnemyAIManager context, Transform unit, Vector3Int unitCell, Vector3Int playerCell, int attackRange)
        {
            Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
            Vector3Int bestCell = playerCell;
            int minDistance = int.MaxValue;
            foreach (var dir in dirs)
            {
                Vector3Int candidate = playerCell + (dir * attackRange);
                if (context.IsCellWalkable(candidate))
                {
                    int dist = Mathf.Abs(candidate.x - unitCell.x) + Mathf.Abs(candidate.y - unitCell.y);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestCell = candidate;
                    }
                }
            }

            var path = context.FindPathToPlayer(unitCell, bestCell);
            if (path.Count > 0)
            {
                await context.MoveUnitAlongPathAsync(unit, path);
            }
        }

        private Vector2Int FindBestTacticalCell(IBattleAIContext context, BattleUnit unit, Vector2Int playerCell, int attackRange)
        {
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            Vector2Int bestCell = context.GetGridPosition(unit);
            int bestDistance = int.MaxValue;

            foreach (Vector2Int dir in dirs)
            {
                Vector2Int candidate = playerCell + (dir * attackRange);
                if (!context.IsCellWalkable(candidate))
                {
                    continue;
                }

                int distance = Mathf.Abs(candidate.x - unit.GridPosition.x) + Mathf.Abs(candidate.y - unit.GridPosition.y);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCell = candidate;
                }
            }

            return bestCell;
        }
    }
}
