namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 공격 가능한 칸과 실제 피격 유닛을 계산합니다.
    /// 보드의 유닛/타일 저장소는 소유하지 않고, 전달받은 현재 보드 상태만 읽습니다.
    /// </summary>
    internal static class BattleAttackQueryService
    {
        public static List<BattleUnit> GetUnitsInAttackArea(
            BattleUnit attacker,
            Vector2Int attackerPosition,
            Vector2Int targetPosition,
            BattleAttackGA attackGA,
            IEnumerable<KeyValuePair<Vector2Int, BattleUnit>> units)
        {
            List<BattleUnit> result = new();
            if (attacker == null || attackGA == null || units == null)
            {
                return result;
            }

            BattleTeam targetTeam = attacker.Team == BattleTeam.Player ? BattleTeam.Enemy : BattleTeam.Player;
            HashSet<Vector2Int> customPatternCells = null;
            if (attackGA.CustomAttackPattern != null)
            {
                customPatternCells = AttackPatternResolver.ResolvePatternCellsAtAnchor(
                    targetPosition,
                    attackerPosition,
                    targetPosition,
                    attackGA.CustomAttackPattern,
                    includeAnchorCell: true);
            }

            foreach (var pair in units)
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

        public static HashSet<Vector2Int> GetSelectableAttackCells(
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

            if (attackEffect.CustomTargetingPattern != null)
            {
                AddCustomPatternCells(attackerPosition, attackEffect.CustomTargetingPattern, result);
                return result;
            }

            switch (attackEffect.TargetingPattern)
            {
                case BattleAttackPattern.Line:
                    AttackPatternResolver.AddLineCells(attackerPosition, attackEffect.TargetingRange, result);
                    break;

                case BattleAttackPattern.Area:
                case BattleAttackPattern.Adjacent4:
                case BattleAttackPattern.None:
                default:
                    AttackPatternResolver.AddDiamondCells(attackerPosition, attackEffect.TargetingRange, result);
                    break;
            }

            result.Remove(attackerPosition);
            return result;
        }

        private static void AddCustomPatternCells(Vector2Int attackerPosition, AttackPatternData patternData, HashSet<Vector2Int> destination)
        {
            if (patternData == null || destination == null)
            {
                return;
            }

            if (!patternData.RotateToFacing)
            {
                foreach (Vector2Int cell in AttackPatternResolver.ResolvePatternCells(attackerPosition, attackerPosition, patternData))
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

            foreach (Vector2Int targetPosition in facingTargets)
            {
                foreach (Vector2Int cell in AttackPatternResolver.ResolvePatternCells(attackerPosition, targetPosition, patternData))
                {
                    destination.Add(cell);
                }
            }
        }

        private static bool IsInAttackArea(Vector2Int attackerPos, Vector2Int targetPos, Vector2Int unitPos, BattleAttackGA attackGA)
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

        private static bool IsBetween(int start, int end, int value)
        {
            int min = Mathf.Min(start, end);
            int max = Mathf.Max(start, end);
            return value >= min && value <= max;
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
