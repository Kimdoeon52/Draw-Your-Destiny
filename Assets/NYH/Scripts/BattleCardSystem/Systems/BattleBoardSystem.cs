namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    public enum BattleTileType
    {
        Plain,
        Forest,
        River,
        Rock,
    }

    /// <summary>
    /// 전투 보드의 현재 타일/유닛 배치 상태를 소유합니다.
    /// 이동 경로 계산, 공격 범위 계산, 패턴 회전은 전용 query/resolver 서비스로 위임합니다.
    /// </summary>
    public class BattleBoardSystem : Singleton<BattleBoardSystem>
    {
        private readonly Dictionary<Vector2Int, BattleTileType> tileMap = new();
        private readonly Dictionary<Vector2Int, BattleUnit> unitMap = new();
        private readonly Dictionary<BattleUnit, Vector2Int> reverseUnitMap = new();

        public void SetTile(Vector2Int position, BattleTileType tileType)
        {
            if (!EnsureCombatTilesLoaded() || !BattleGridCoordinateService.Instance.IsCombatCell(position))
            {
                return;
            }

            tileMap[position] = tileType;
        }

        public BattleTileType GetTile(Vector2Int position)
        {
            EnsureCombatTilesLoaded();
            return tileMap.TryGetValue(position, out BattleTileType tileType) ? tileType : BattleTileType.Rock;
        }

        public bool RegisterUnit(BattleUnit unit, Vector2Int position)
        {
            if (unit == null)
            {
                return false;
            }

            if (!EnsureCombatTilesLoaded())
            {
                Debug.LogWarning($"[BattleBoardSystem] 전투 타일 정보가 없어 유닛을 등록할 수 없습니다. unit={unit.name}, pos={position}");
                return false;
            }

            if (!BattleGridCoordinateService.Instance.IsCombatCell(position))
            {
                Debug.LogWarning($"[BattleBoardSystem] 전투 셀이 아닌 위치에는 유닛을 등록할 수 없습니다. unit={unit.name}, pos={position}");
                return false;
            }

            if (unitMap.TryGetValue(position, out BattleUnit occupant) && occupant != unit)
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

            if (reverseUnitMap.TryGetValue(unit, out Vector2Int position))
            {
                reverseUnitMap.Remove(unit);
                if (unitMap.TryGetValue(position, out BattleUnit occupant) && occupant == unit)
                {
                    unitMap.Remove(position);
                }
            }
        }

        public BattleUnit GetUnitAt(Vector2Int position)
        {
            return unitMap.TryGetValue(position, out BattleUnit unit) ? unit : null;
        }

        public List<BattleUnit> GetUnitsInCells(
            BattleUnit sourceUnit,
            IEnumerable<Vector2Int> cells,
            BattleUnitTargetFilter targetFilter)
        {
            List<BattleUnit> result = new();
            if (sourceUnit == null || cells == null)
            {
                return result;
            }

            HashSet<Vector2Int> cellSet = cells as HashSet<Vector2Int> ?? new HashSet<Vector2Int>(cells);
            foreach (var pair in unitMap)
            {
                BattleUnit unit = pair.Value;
                if (unit == null
                    || !unit.IsAlive
                    || !cellSet.Contains(pair.Key)
                    || !BattleUnitTargetFilterUtility.Matches(sourceUnit, unit, targetFilter))
                {
                    continue;
                }

                result.Add(unit);
            }

            return result;
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

            if (!EnsureCombatTilesLoaded())
            {
                return false;
            }

            if (!reverseUnitMap.TryGetValue(unit, out Vector2Int startPosition))
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
                unit.transform.position = BattleUnit.GetWorldPositionForGrid(targetPosition, unit.transform.position.z);
            }

            return true;
        }

        public bool CanStepTo(BattleUnit unit, Vector2Int fromPosition, Vector2Int targetPosition)
        {
            if (unit == null || !unit.IsAlive)
            {
                return false;
            }

            if (!EnsureCombatTilesLoaded())
            {
                return false;
            }

            if (ManhattanDistance(fromPosition, targetPosition) != 1)
            {
                return false;
            }

            if (unitMap.TryGetValue(targetPosition, out BattleUnit occupant) && occupant != null && occupant != unit)
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

        public bool ReloadCombatTilesFromCoordinateService()
        {
            tileMap.Clear();
            if (BattleGridCoordinateService.Instance == null || !BattleGridCoordinateService.Instance.RefreshFromTilemaps())
            {
                return false;
            }

            foreach (Vector2Int cell in BattleGridCoordinateService.Instance.GetAllCombatCells())
            {
                tileMap[cell] = BattleGridCoordinateService.Instance.GetBattleTileType(cell);
            }

            return tileMap.Count > 0;
        }

        public bool TryBuildMovePath(
            BattleUnit unit,
            Vector2Int startPosition,
            Vector2Int targetPosition,
            int moveBudget,
            out List<Vector2Int> path)
        {
            if (!EnsureCombatTilesLoaded())
            {
                path = new List<Vector2Int>();
                return false;
            }

            return BattleMovementQueryService.TryBuildMovePath(
                unit,
                startPosition,
                targetPosition,
                moveBudget,
                CanStepTo,
                GetStepCost,
                out path);
        }

        public HashSet<Vector2Int> GetSelectableMoveCells(BattleUnit unit, int moveBudget)
        {
            HashSet<Vector2Int> empty = new();
            if (unit == null || !unit.IsAlive || moveBudget < 0 || !EnsureCombatTilesLoaded())
            {
                return empty;
            }

            Vector2Int startPosition = reverseUnitMap.TryGetValue(unit, out Vector2Int registeredPosition)
                ? registeredPosition
                : unit.GridPosition;

            return BattleMovementQueryService.GetSelectableMoveCells(
                unit,
                startPosition,
                moveBudget,
                CanStepTo,
                GetStepCost);
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
            return BattleAttackQueryService.GetUnitsInAttackArea(
                attacker,
                attackerPosition,
                targetPosition,
                attackGA,
                unitMap);
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
            return BattleAttackQueryService.GetSelectableAttackCells(attacker, attackerPosition, battleCard);
        }

        public HashSet<Vector2Int> ResolvePatternCells(
            Vector2Int attackerPosition,
            Vector2Int targetPosition,
            AttackPatternData patternData)
        {
            return AttackPatternResolver.ResolvePatternCells(attackerPosition, targetPosition, patternData);
        }

        public HashSet<Vector2Int> ResolvePatternCellsAtAnchor(
            Vector2Int anchorPosition,
            Vector2Int facingSourcePosition,
            Vector2Int facingTargetPosition,
            AttackPatternData patternData,
            bool includeAnchorCell = false)
        {
            return AttackPatternResolver.ResolvePatternCellsAtAnchor(
                anchorPosition,
                facingSourcePosition,
                facingTargetPosition,
                patternData,
                includeAnchorCell);
        }

        private bool CanEnter(Vector2Int position)
        {
            if (!tileMap.ContainsKey(position))
            {
                return false;
            }

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

        private bool EnsureCombatTilesLoaded()
        {
            return tileMap.Count > 0 || ReloadCombatTilesFromCoordinateService();
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
