namespace NYH.BattleCardSystem
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 이동 가능 칸과 자동 이동 경로를 계산합니다.
    /// 보드 상태를 직접 소유하지 않고, BattleBoardSystem에서 받은 규칙 함수만 사용합니다.
    /// </summary>
    internal static class BattleMovementQueryService
    {
        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left,
        };

        public static bool TryBuildMovePath(
            BattleUnit unit,
            Vector2Int startPosition,
            Vector2Int targetPosition,
            int moveBudget,
            Func<BattleUnit, Vector2Int, Vector2Int, bool> canStepTo,
            Func<Vector2Int, int> getStepCost,
            out List<Vector2Int> path)
        {
            path = new List<Vector2Int>();
            if (unit == null || !unit.IsAlive || moveBudget < 0 || canStepTo == null || getStepCost == null)
            {
                return false;
            }

            if (startPosition == targetPosition)
            {
                return true;
            }

            Queue<Vector2Int> frontier = new();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new();
            Dictionary<Vector2Int, int> costSoFar = new();
            frontier.Enqueue(startPosition);
            costSoFar[startPosition] = 0;

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
                if (current == targetPosition)
                {
                    break;
                }

                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int next = current + direction;
                    if (!canStepTo(unit, current, next))
                    {
                        continue;
                    }

                    int newCost = costSoFar[current] + getStepCost(next);
                    if (newCost > moveBudget)
                    {
                        continue;
                    }

                    if (costSoFar.TryGetValue(next, out int existingCost) && existingCost <= newCost)
                    {
                        continue;
                    }

                    costSoFar[next] = newCost;
                    cameFrom[next] = current;
                    frontier.Enqueue(next);
                }
            }

            if (!costSoFar.ContainsKey(targetPosition))
            {
                return false;
            }

            Vector2Int trace = targetPosition;
            List<Vector2Int> reversePath = new();
            while (trace != startPosition)
            {
                reversePath.Add(trace);
                trace = cameFrom[trace];
            }

            reversePath.Reverse();
            path = reversePath;
            return true;
        }

        public static HashSet<Vector2Int> GetSelectableMoveCells(
            BattleUnit unit,
            Vector2Int startPosition,
            int moveBudget,
            Func<BattleUnit, Vector2Int, Vector2Int, bool> canStepTo,
            Func<Vector2Int, int> getStepCost)
        {
            HashSet<Vector2Int> result = new();
            if (unit == null || !unit.IsAlive || moveBudget < 0 || canStepTo == null || getStepCost == null)
            {
                return result;
            }

            Queue<Vector2Int> frontier = new();
            Dictionary<Vector2Int, int> costSoFar = new();
            frontier.Enqueue(startPosition);
            costSoFar[startPosition] = 0;

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();

                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int next = current + direction;
                    if (!canStepTo(unit, current, next))
                    {
                        continue;
                    }

                    int newCost = costSoFar[current] + getStepCost(next);
                    if (newCost > moveBudget)
                    {
                        continue;
                    }

                    if (costSoFar.TryGetValue(next, out int existingCost) && existingCost <= newCost)
                    {
                        continue;
                    }

                    costSoFar[next] = newCost;
                    frontier.Enqueue(next);
                    result.Add(next);
                }
            }

            return result;
        }
    }
}
