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

            HashSet<Vector2Int> impactCells = BattleAttackImpactCellResolver.ResolveImpactCells(
                attackerPosition,
                targetPosition,
                attackGA.Range,
                attackGA.AttackPattern,
                attackGA.CustomAttackPattern,
                attackGA.PatternOriginMode);

            foreach (var pair in units)
            {
                BattleUnit unit = pair.Value;
                if (unit == null
                    || unit == attacker
                    || !unit.IsAlive
                    || !BattleUnitTargetFilterUtility.Matches(attacker, unit, attackGA.TargetFilter))
                {
                    continue;
                }

                if (impactCells.Contains(unit.GridPosition)
                    || (attackGA.AttackPattern == BattleAttackPattern.None
                        && attackGA.PrimaryTarget != null
                        && unit.GridPosition == attackGA.PrimaryTarget.GridPosition))
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
            BattleHealEffect healEffect = BattleEffectResolver.GetHealEffect(battleCard);
            if (attackEffect == null && healEffect == null)
            {
                return result;
            }

            AttackPatternData customTargetingPattern = attackEffect != null
                ? attackEffect.CustomTargetingPattern
                : healEffect.CustomHealPattern;
            BattleAttackPattern targetingPattern = attackEffect != null
                ? attackEffect.TargetingPattern
                : healEffect.HealPattern;
            int targetingRange = attackEffect != null
                ? attackEffect.TargetingRange
                : healEffect.Range;

            if (customTargetingPattern != null)
            {
                AddCustomPatternCells(attackerPosition, customTargetingPattern, result);
                result.Remove(attackerPosition);
                return result;
            }

            switch (targetingPattern)
            {
                case BattleAttackPattern.Line:
                    AttackPatternResolver.AddLineCells(attackerPosition, targetingRange, result);
                    break;

                case BattleAttackPattern.Area:
                case BattleAttackPattern.Adjacent4:
                case BattleAttackPattern.None:
                default:
                    AttackPatternResolver.AddDiamondCells(attackerPosition, targetingRange, result);
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

        private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
