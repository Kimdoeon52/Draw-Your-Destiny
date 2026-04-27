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
    /// 이 클래스는 "현재 보드에 무엇이 어디에 있는가"를 보장하는 중심 저장소입니다.
    /// </summary>
    public class BattleBoardSystem : Singleton<BattleBoardSystem>
    {
        private readonly Dictionary<Vector2Int, BattleTileType> tileMap = new();
        private readonly Dictionary<Vector2Int, BattleUnit> unitMap = new();
        private readonly Dictionary<BattleUnit, Vector2Int> reverseUnitMap = new();

        // 특정 전투 셀의 지형 타입을 수동으로 지정합니다.
        public void SetTile(Vector2Int position, BattleTileType tileType)
        {
            if (!EnsureCombatTilesLoaded() || !BattleGridCoordinateService.Instance.IsCombatCell(position))
            {
                return;
            }

            tileMap[position] = tileType;
        }

        // 지정 셀의 지형 타입을 반환하며, 전투 셀이 아니면 Rock으로 취급합니다.
        public BattleTileType GetTile(Vector2Int position)
        {
            EnsureCombatTilesLoaded();
            return tileMap.TryGetValue(position, out BattleTileType tileType) ? tileType : BattleTileType.Rock;
        }

        // 유닛을 보드 점유 맵에 등록하고 그리드 위치를 동기화합니다.
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

        // 비활성화/사망 등으로 보드에서 빠지는 유닛의 점유 정보를 제거합니다.
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

        // 특정 셀을 점유 중인 유닛을 조회합니다.
        public BattleUnit GetUnitAt(Vector2Int position)
        {
            return unitMap.TryGetValue(position, out BattleUnit unit) ? unit : null;
        }

        // 주어진 셀 목록 안의 유닛 중 필터 조건에 맞는 유닛만 반환합니다.
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

        // 유닛을 targetPosition으로 이동시키고 보드 점유 맵과 Transform을 함께 갱신합니다.
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

        // fromPosition에서 targetPosition으로 한 칸 이동할 수 있는지 확인합니다.
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

        // 지형 타입에 따른 이동 비용을 반환합니다.
        public int GetStepCost(Vector2Int targetPosition)
        {
            BattleTileType targetTile = GetTile(targetPosition);
            return targetTile == BattleTileType.River ? 2 : 1;
        }

        // 좌표 서비스의 타일맵 캐시를 다시 읽어 보드 지형 맵을 갱신합니다.
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

        // 이동 예산 안에서 시작 셀부터 목표 셀까지의 경로를 계산합니다.
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

        // 유닛이 현재 위치에서 선택할 수 있는 이동 후보 셀을 계산합니다.
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

        // BattleAttackGA 설정을 기준으로 실제 공격 범위 안의 유닛을 계산합니다.
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

        // 카드 설정을 기준으로 공격 조준 가능한 셀을 계산합니다.
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
