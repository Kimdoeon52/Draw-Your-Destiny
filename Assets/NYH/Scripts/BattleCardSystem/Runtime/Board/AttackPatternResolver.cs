namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 공격 패턴 데이터를 실제 그리드 칸 목록으로 변환합니다.
    /// 유닛 피격 판정이나 보드 상태 조회는 담당하지 않습니다.
    /// </summary>
    internal static class AttackPatternResolver
    {
        public static HashSet<Vector2Int> ResolvePatternCells(
            Vector2Int attackerPosition,
            Vector2Int targetPosition,
            AttackPatternData patternData)
        {
            return ResolvePatternCellsAtAnchor(
                attackerPosition,
                attackerPosition,
                targetPosition,
                patternData);
        }

        public static HashSet<Vector2Int> ResolvePatternCellsAtAnchor(
            Vector2Int anchorPosition,
            Vector2Int facingSourcePosition,
            Vector2Int facingTargetPosition,
            AttackPatternData patternData,
            bool includeAnchorCell = false)
        {
            HashSet<Vector2Int> resolved = new();
            if (patternData == null || patternData.Cells == null)
            {
                return resolved;
            }

            if (includeAnchorCell)
            {
                resolved.Add(anchorPosition);
            }

            FacingDirection facing = ResolveFacingDirection(facingSourcePosition, facingTargetPosition);
            foreach (Vector2Int cell in patternData.Cells)
            {
                Vector2Int rotated = patternData.RotateToFacing ? RotateOffset(cell, facing) : cell;
                resolved.Add(anchorPosition + rotated);
            }

            return resolved;
        }

        public static void AddDiamondCells(Vector2Int center, int range, HashSet<Vector2Int> destination)
        {
            if (destination == null)
            {
                return;
            }

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

        public static void AddLineCells(Vector2Int center, int range, HashSet<Vector2Int> destination)
        {
            if (destination == null)
            {
                return;
            }

            for (int i = 1; i <= range; i++)
            {
                destination.Add(center + new Vector2Int(i, 0));
                destination.Add(center + new Vector2Int(-i, 0));
                destination.Add(center + new Vector2Int(0, i));
                destination.Add(center + new Vector2Int(0, -i));
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

        private enum FacingDirection
        {
            Up,
            Right,
            Down,
            Left,
        }
    }
}
