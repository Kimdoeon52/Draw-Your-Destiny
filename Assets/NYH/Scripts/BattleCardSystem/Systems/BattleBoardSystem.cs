namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    public enum BattleTileType
    {
        Plain,
        Forest,
        River,
        Rock,
    }

    /*
     * BattleBoardSystem
     *
     * 역할:
     * - 전투 보드의 타일 타입과 전투 유닛 위치를 관리합니다.
     * - 이동 가능 여부, 이동 비용, 공격 범위 내 대상 탐색을 처리합니다.
     *
     * 인스펙터에서 넣는 것:
     * - 현재는 없음
     *
     * 사용하는 법:
     * - 전투 씬에 1개만 둡니다.
     * - BattleUnit이 OnEnable에서 자동 등록됩니다.
     * - 필요 시 SetTile()로 평지/강/숲/바위 타일을 코드에서 설정합니다.
     */
    public class BattleBoardSystem : Singleton<BattleBoardSystem>
    {
        private readonly Dictionary<Vector2Int, BattleTileType> tileMap = new();
        private readonly Dictionary<Vector2Int, BattleUnit> unitMap = new();
        private readonly Dictionary<BattleUnit, Vector2Int> reverseUnitMap = new();

        public void SetTile(Vector2Int position, BattleTileType tileType)
        {
            tileMap[position] = tileType;
        }

        public BattleTileType GetTile(Vector2Int position)
        {
            return tileMap.TryGetValue(position, out var tileType) ? tileType : BattleTileType.Plain;
        }

        public bool RegisterUnit(BattleUnit unit, Vector2Int position)
        {
            if (unit == null)
            {
                return false;
            }

            if (unitMap.TryGetValue(position, out var occupant) && occupant != unit)
            {
                return false;
            }

            unitMap[position] = unit;
            reverseUnitMap[unit] = position;
            unit.SetGridPosition(position);
            return true;
        }

        public void UnregisterUnit(BattleUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            if (reverseUnitMap.TryGetValue(unit, out var position))
            {
                reverseUnitMap.Remove(unit);
                if (unitMap.TryGetValue(position, out var occupant) && occupant == unit)
                {
                    unitMap.Remove(position);
                }
            }
        }

        public BattleUnit GetUnitAt(Vector2Int position)
        {
            return unitMap.TryGetValue(position, out var unit) ? unit : null;
        }

        public bool TryMoveUnit(
            BattleUnit unit,
            Vector2Int targetPosition,
            int moveBudget,
            bool syncTransform = true,
            IReadOnlyList<Vector2Int> plannedPath = null)
        {
            if (unit == null || !unit.IsAlive || moveBudget < 0)
            {
                return false;
            }

            if (!reverseUnitMap.TryGetValue(unit, out var startPosition))
            {
                startPosition = unit.GridPosition;
            }

            if (startPosition == targetPosition)
            {
                return true;
            }

            if (!CanEnter(targetPosition))
            {
                return false;
            }

            // 드래그로 그린 실제 경로가 있으면 그 경로 비용으로 검증해야
            // "목적지는 같지만 중간에 강/우회 때문에 비용이 더 드는" 경우를 막을 수 있습니다.
            int requiredCost = plannedPath != null && plannedPath.Count > 0
                ? CalculatePathCost(startPosition, targetPosition, plannedPath)
                : CalculateMoveCost(startPosition, targetPosition);
            if (requiredCost > moveBudget)
            {
                return false;
            }

            unitMap.Remove(startPosition);
            unitMap[targetPosition] = unit;
            reverseUnitMap[unit] = targetPosition;
            unit.SetGridPosition(targetPosition);

            if (syncTransform)
            {
                unit.transform.position = new Vector3(targetPosition.x, targetPosition.y, unit.transform.position.z);
            }

            return true;
        }

        public bool CanStepTo(BattleUnit unit, Vector2Int fromPosition, Vector2Int targetPosition)
        {
            if (unit == null || !unit.IsAlive)
            {
                return false;
            }

            if (ManhattanDistance(fromPosition, targetPosition) != 1)
            {
                return false;
            }

            if (unitMap.TryGetValue(targetPosition, out var occupant) && occupant != null && occupant != unit)
            {
                return false;
            }

            return CanEnter(targetPosition);
        }

        public int GetStepCost(Vector2Int targetPosition)
        {
            BattleTileType targetTile = GetTile(targetPosition);
            return targetTile == BattleTileType.River ? 2 : 1;
        }

        public bool TryBuildMovePath(
            BattleUnit unit,
            Vector2Int startPosition,
            Vector2Int targetPosition,
            int moveBudget,
            out List<Vector2Int> path)
        {
            path = new List<Vector2Int>();
            if (unit == null || !unit.IsAlive || moveBudget < 0)
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

            Vector2Int[] directions =
            {
                Vector2Int.up,
                Vector2Int.right,
                Vector2Int.down,
                Vector2Int.left,
            };

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
                if (current == targetPosition)
                {
                    break;
                }

                foreach (Vector2Int direction in directions)
                {
                    Vector2Int next = current + direction;
                    if (!CanStepTo(unit, current, next))
                    {
                        continue;
                    }

                    int newCost = costSoFar[current] + GetStepCost(next);
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

        public HashSet<Vector2Int> GetSelectableMoveCells(BattleUnit unit, int moveBudget)
        {
            HashSet<Vector2Int> result = new();
            if (unit == null || !unit.IsAlive || moveBudget < 0)
            {
                return result;
            }

            Vector2Int startPosition = reverseUnitMap.TryGetValue(unit, out var registeredPosition)
                ? registeredPosition
                : unit.GridPosition;

            Queue<Vector2Int> frontier = new();
            Dictionary<Vector2Int, int> costSoFar = new();
            frontier.Enqueue(startPosition);
            costSoFar[startPosition] = 0;

            Vector2Int[] directions =
            {
                Vector2Int.up,
                Vector2Int.right,
                Vector2Int.down,
                Vector2Int.left,
            };

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();

                foreach (Vector2Int direction in directions)
                {
                    Vector2Int next = current + direction;
                    if (!CanStepTo(unit, current, next))
                    {
                        continue;
                    }

                    int newCost = costSoFar[current] + GetStepCost(next);
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

        public List<BattleUnit> GetUnitsInAttackArea(BattleUnit attacker, Vector2Int targetPosition, BattleAttackGA attackGA)
        {
            Vector2Int attackerPosition = attacker != null ? attacker.GridPosition : Vector2Int.zero;
            return GetUnitsInAttackArea(attacker, attackerPosition, targetPosition, attackGA);
        }

        public List<BattleUnit> GetUnitsInAttackArea(
            BattleUnit attacker,
            Vector2Int attackerPosition,
            Vector2Int targetPosition,
            BattleAttackGA attackGA)
        {
            List<BattleUnit> result = new();
            if (attacker == null || attackGA == null)
            {
                return result;
            }

            // 이동 후 공격 미리보기에서는 "아직 보드에 적용되지 않은 가상 위치"에서
            // 공격 범위를 계산해야 하므로 attackerPosition을 별도로 받습니다.
            BattleTeam targetTeam = attacker.Team == BattleTeam.Player ? BattleTeam.Enemy : BattleTeam.Player;
            HashSet<Vector2Int> customPatternCells = null;
            if (attackGA.CustomAttackPattern != null)
            {
                customPatternCells = ResolvePatternCells(attackerPosition, targetPosition, attackGA.CustomAttackPattern);
            }

            foreach (var pair in unitMap)
            {
                BattleUnit unit = pair.Value;
                if (unit == null || !unit.IsAlive || unit.Team != targetTeam)
                {
                    continue;
                }

                if (customPatternCells != null)
                {
                    if (customPatternCells.Contains(unit.GridPosition))
                    {
                        result.Add(unit);
                    }
                }
                else if (IsInAttackArea(attackerPosition, targetPosition, unit.GridPosition, attackGA))
                {
                    result.Add(unit);
                }
            }

            result.Sort((a, b) =>
                ManhattanDistance(a.GridPosition, attackerPosition).CompareTo(
                    ManhattanDistance(b.GridPosition, attackerPosition)));

            if (!attackGA.HitsAllTargetsInRange && attackGA.TargetCount > 0 && result.Count > attackGA.TargetCount)
            {
                result.RemoveRange(attackGA.TargetCount, result.Count - attackGA.TargetCount);
            }

            return result;
        }

        public HashSet<Vector2Int> GetSelectableAttackCells(BattleUnit attacker, BattleCard battleCard)
        {
            Vector2Int attackerPosition = attacker != null ? attacker.GridPosition : Vector2Int.zero;
            return GetSelectableAttackCells(attacker, attackerPosition, battleCard);
        }

        public HashSet<Vector2Int> GetSelectableAttackCells(
            BattleUnit attacker,
            Vector2Int attackerPosition,
            BattleCard battleCard)
        {
            HashSet<Vector2Int> result = new();
            if (attacker == null || battleCard == null)
            {
                return result;
            }

            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(battleCard);
            if (attackEffect == null)
            {
                return result;
            }

            // 공격 가능 셀 후보만 계산하는 단계입니다.
            // 실제로 적을 맞출 수 있는지 여부는 UI 쪽에서 한 번 더 필터링합니다.
            if (attackEffect.CustomAttackPattern != null)
            {
                AddCustomPatternCells(attackerPosition, attackEffect.CustomAttackPattern, result);
                return result;
            }

            switch (attackEffect.AttackPattern)
            {
                case BattleAttackPattern.Line:
                    AddLineCells(attackerPosition, attackEffect.Range, result);
                    break;

                case BattleAttackPattern.Area:
                case BattleAttackPattern.Adjacent4:
                case BattleAttackPattern.None:
                default:
                    AddDiamondCells(attackerPosition, attackEffect.Range, result);
                    break;
            }

            result.Remove(attackerPosition);
            return result;
        }

        public HashSet<Vector2Int> ResolvePatternCells(
            Vector2Int attackerPosition,
            Vector2Int targetPosition,
            AttackPatternData patternData)
        {
            HashSet<Vector2Int> resolved = new();
            if (patternData == null || patternData.Cells == null)
            {
                return resolved;
            }

            FacingDirection facing = ResolveFacingDirection(attackerPosition, targetPosition);
            foreach (var cell in patternData.Cells)
            {
                Vector2Int rotated = patternData.RotateToFacing ? RotateOffset(cell, facing) : cell;
                resolved.Add(attackerPosition + rotated);
            }

            return resolved;
        }

        private static void AddDiamondCells(Vector2Int center, int range, HashSet<Vector2Int> destination)
        {
            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    Vector2Int offset = new(x, y);
                    if (Mathf.Abs(offset.x) + Mathf.Abs(offset.y) <= range)
                    {
                        destination.Add(center + offset);
                    }
                }
            }
        }

        private static void AddLineCells(Vector2Int center, int range, HashSet<Vector2Int> destination)
        {
            for (int i = 1; i <= range; i++)
            {
                destination.Add(center + new Vector2Int(i, 0));
                destination.Add(center + new Vector2Int(-i, 0));
                destination.Add(center + new Vector2Int(0, i));
                destination.Add(center + new Vector2Int(0, -i));
            }
        }

        private void AddCustomPatternCells(Vector2Int attackerPosition, AttackPatternData patternData, HashSet<Vector2Int> destination)
        {
            if (patternData == null)
            {
                return;
            }

            if (!patternData.RotateToFacing)
            {
                foreach (var cell in ResolvePatternCells(attackerPosition, attackerPosition, patternData))
                {
                    destination.Add(cell);
                }

                return;
            }

            Vector2Int[] facingTargets =
            {
                attackerPosition + Vector2Int.up,
                attackerPosition + Vector2Int.right,
                attackerPosition + Vector2Int.down,
                attackerPosition + Vector2Int.left,
            };

            foreach (var targetPosition in facingTargets)
            {
                foreach (var cell in ResolvePatternCells(attackerPosition, targetPosition, patternData))
                {
                    destination.Add(cell);
                }
            }
        }

        private bool CanEnter(Vector2Int position)
        {
            if (unitMap.ContainsKey(position))
            {
                return false;
            }

            BattleTileType tileType = GetTile(position);
            return tileType != BattleTileType.Forest && tileType != BattleTileType.Rock;
        }

        private int CalculateMoveCost(Vector2Int start, Vector2Int target)
        {
            int steps = ManhattanDistance(start, target);
            if (steps == 0)
            {
                return 0;
            }

            BattleTileType targetTile = GetTile(target);
            int tileCost = targetTile == BattleTileType.River ? 2 : 1;
            return steps * tileCost;
        }

        private int CalculatePathCost(
            Vector2Int start,
            Vector2Int target,
            IReadOnlyList<Vector2Int> plannedPath)
        {
            if (plannedPath == null || plannedPath.Count == 0)
            {
                return CalculateMoveCost(start, target);
            }

            int totalCost = 0;
            Vector2Int current = start;
            for (int i = 0; i < plannedPath.Count; i++)
            {
                Vector2Int next = plannedPath[i];
                if (ManhattanDistance(current, next) != 1)
                {
                    return int.MaxValue;
                }

                totalCost += GetStepCost(next);
                current = next;
            }

            return current == target ? totalCost : int.MaxValue;
        }

        private bool IsInAttackArea(Vector2Int attackerPos, Vector2Int targetPos, Vector2Int unitPos, BattleAttackGA attackGA)
        {
            switch (attackGA.AttackPattern)
            {
                case BattleAttackPattern.Adjacent4:
                    return ManhattanDistance(targetPos, unitPos) <= 1;

                case BattleAttackPattern.Line:
                    if (attackerPos.x != targetPos.x && attackerPos.y != targetPos.y)
                    {
                        return false;
                    }

                    if (attackerPos.x == targetPos.x && unitPos.x == attackerPos.x)
                    {
                        return IsBetween(attackerPos.y, targetPos.y, unitPos.y);
                    }

                    if (attackerPos.y == targetPos.y && unitPos.y == attackerPos.y)
                    {
                        return IsBetween(attackerPos.x, targetPos.x, unitPos.x);
                    }

                    return false;

                case BattleAttackPattern.Area:
                    return ManhattanDistance(targetPos, unitPos) <= Mathf.Max(0, attackGA.Range);

                case BattleAttackPattern.None:
                default:
                    return unitPos == targetPos || (attackGA.PrimaryTarget != null && unitPos == attackGA.PrimaryTarget.GridPosition);
            }
        }

        private static FacingDirection ResolveFacingDirection(Vector2Int attackerPosition, Vector2Int targetPosition)
        {
            Vector2Int delta = targetPosition - attackerPosition;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                return delta.x >= 0 ? FacingDirection.Right : FacingDirection.Left;
            }

            if (delta.y != 0)
            {
                return delta.y >= 0 ? FacingDirection.Up : FacingDirection.Down;
            }

            return FacingDirection.Up;
        }

        private static Vector2Int RotateOffset(Vector2Int offset, FacingDirection facing)
        {
            return facing switch
            {
                FacingDirection.Up => offset,
                FacingDirection.Right => new Vector2Int(offset.y, -offset.x),
                FacingDirection.Down => new Vector2Int(-offset.x, -offset.y),
                FacingDirection.Left => new Vector2Int(-offset.y, offset.x),
                _ => offset,
            };
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private static bool IsBetween(int start, int end, int value)
        {
            int min = Mathf.Min(start, end);
            int max = Mathf.Max(start, end);
            return value >= min && value <= max;
        }

        private enum FacingDirection
        {
            Up,
            Right,
            Down,
            Left,
        }
    }
}
