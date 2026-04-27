namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 실제 공격에 맞는 칸만 계산합니다.
    /// 원거리 패턴은 기존처럼 선택한 칸을 기준으로 퍼지고,
    /// 근거리 패턴은 선택한 칸을 방향으로만 사용해 유닛 앞 1칸부터 계산합니다.
    /// </summary>
    internal static class BattleAttackImpactCellResolver
    {
        public static HashSet<Vector2Int> ResolveImpactCells(
            Vector2Int attackOrigin,
            Vector2Int targetPosition,
            int range,
            BattleAttackPattern pattern,
            AttackPatternData customPattern,
            BattleAttackPatternOriginMode originMode)
        {
            if (originMode == BattleAttackPatternOriginMode.MeleePattern)
            {
                return ResolveMeleeImpactCells(attackOrigin, targetPosition, range, pattern, customPattern);
            }

            return ResolveRangedImpactCells(attackOrigin, targetPosition, range, pattern, customPattern);
        }

        private static HashSet<Vector2Int> ResolveRangedImpactCells(
            Vector2Int attackOrigin,
            Vector2Int targetPosition,
            int range,
            BattleAttackPattern pattern,
            AttackPatternData customPattern)
        {
            HashSet<Vector2Int> result = new();
            if (customPattern != null)
            {
                return AttackPatternResolver.ResolvePatternCellsAtAnchor(
                    targetPosition,
                    attackOrigin,
                    targetPosition,
                    customPattern,
                    includeAnchorCell: true);
            }

            switch (pattern)
            {
                case BattleAttackPattern.Area:
                    AttackPatternResolver.AddDiamondCells(targetPosition, Mathf.Max(0, range), result);
                    break;

                case BattleAttackPattern.Line:
                    AddLineCellsFromTarget(attackOrigin, targetPosition, range, result);
                    break;

                case BattleAttackPattern.Adjacent4:
                    AttackPatternResolver.AddDiamondCells(targetPosition, 1, result);
                    break;

                case BattleAttackPattern.None:
                default:
                    result.Add(targetPosition);
                    break;
            }

            return result;
        }

        private static HashSet<Vector2Int> ResolveMeleeImpactCells(
            Vector2Int attackOrigin,
            Vector2Int targetPosition,
            int range,
            BattleAttackPattern pattern,
            AttackPatternData customPattern)
        {
            HashSet<Vector2Int> result = new();
            if (customPattern != null)
            {
                return AttackPatternResolver.ResolvePatternCellsAtAnchor(
                    attackOrigin,
                    attackOrigin,
                    targetPosition,
                    customPattern,
                    includeAnchorCell: false);
            }

            switch (pattern)
            {
                case BattleAttackPattern.Area:
                    AttackPatternResolver.AddDiamondCells(attackOrigin, Mathf.Max(0, range), result);
                    result.Remove(attackOrigin);
                    break;

                case BattleAttackPattern.Adjacent4:
                    AttackPatternResolver.AddDiamondCells(attackOrigin, 1, result);
                    result.Remove(attackOrigin);
                    break;

                case BattleAttackPattern.Line:
                    AddLineCellsFromOrigin(attackOrigin, targetPosition, range, result);
                    break;

                case BattleAttackPattern.None:
                default:
                    result.Add(attackOrigin + ResolveDirection(attackOrigin, targetPosition));
                    break;
            }

            return result;
        }

        private static void AddLineCellsFromOrigin(
            Vector2Int origin,
            Vector2Int target,
            int range,
            HashSet<Vector2Int> destination)
        {
            Vector2Int direction = ResolveDirection(origin, target);
            for (int i = 1; i <= Mathf.Max(1, range); i++)
            {
                destination.Add(origin + (direction * i));
            }
        }

        private static void AddLineCellsFromTarget(
            Vector2Int origin,
            Vector2Int target,
            int range,
            HashSet<Vector2Int> destination)
        {
            Vector2Int direction = ResolveDirection(origin, target);
            for (int i = 0; i < Mathf.Max(1, range); i++)
            {
                destination.Add(target + (direction * i));
            }
        }

        private static Vector2Int ResolveDirection(Vector2Int origin, Vector2Int target)
        {
            Vector2Int delta = target - origin;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) && delta.x != 0)
            {
                return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
            }

            if (delta.y != 0)
            {
                return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
            }

            return Vector2Int.up;
        }
    }
}
