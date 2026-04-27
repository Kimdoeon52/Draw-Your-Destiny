namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    // 유닛 또는 셀 위에 사각형 테두리 프리뷰를 그리는 레이어입니다.
    internal sealed class BattleGridUnitBorderLayer
    {
        private readonly Transform parent;
        private readonly float previewZ;
        private readonly List<GameObject> activeBorders = new();

        // 생성될 테두리 조각들의 부모와 Z 위치를 저장합니다.
        public BattleGridUnitBorderLayer(Transform parent, float previewZ)
        {
            this.parent = parent;
            this.previewZ = previewZ;
        }

        // 전달받은 유닛들의 현재 그리드 위치에 테두리를 다시 그립니다.
        public void ShowUnits(IEnumerable<BattleUnit> units, Color color, int sortingOrder)
        {
            Clear();
            if (units == null)
            {
                return;
            }

            foreach (BattleUnit unit in units)
            {
                if (unit == null)
                {
                    continue;
                }

                CreateBorder(unit.GridPosition, color, sortingOrder);
            }
        }

        // 하나의 셀 위치에 테두리를 표시하고 null이면 기존 테두리만 제거합니다.
        public void ShowCell(Vector2Int? cell, Color color, int sortingOrder)
        {
            Clear();
            if (!cell.HasValue)
            {
                return;
            }

            CreateBorder(cell.Value, color, sortingOrder);
        }

        // 이 레이어가 만든 모든 테두리 조각을 제거합니다.
        public void Clear()
        {
            for (int i = 0; i < activeBorders.Count; i++)
            {
                if (activeBorders[i] != null)
                {
                    Object.Destroy(activeBorders[i]);
                }
            }

            activeBorders.Clear();
        }

        // 대상 셀 주변에 위/아래/왼쪽/오른쪽 네 조각 테두리를 만듭니다.
        public void CreateBorder(Vector2Int centerCell, Color color, int sortingOrder)
        {
            CreateBorderSegment(centerCell, new Vector3(0f, 0.52f, previewZ), new Vector3(1.18f, 0.12f, 1f), color, sortingOrder);
            CreateBorderSegment(centerCell, new Vector3(0f, -0.52f, previewZ), new Vector3(1.18f, 0.12f, 1f), color, sortingOrder);
            CreateBorderSegment(centerCell, new Vector3(-0.52f, 0f, previewZ), new Vector3(0.12f, 1.18f, 1f), color, sortingOrder);
            CreateBorderSegment(centerCell, new Vector3(0.52f, 0f, previewZ), new Vector3(0.12f, 1.18f, 1f), color, sortingOrder);
        }

        // 테두리 한 변을 흰색 스프라이트 조각으로 생성합니다.
        private void CreateBorderSegment(
            Vector2Int centerCell,
            Vector3 localOffset,
            Vector3 scale,
            Color color,
            int sortingOrder)
        {
            GameObject border = new("BattleUnitBorder");
            border.transform.SetParent(parent, false);
            if (!BattleGridCoordinateService.Instance.TryGetWorldCenter(centerCell, out Vector3 borderWorld))
            {
                Object.Destroy(border);
                return;
            }

            border.transform.position = new Vector3(borderWorld.x, borderWorld.y, 0f) + localOffset;
            border.transform.localScale = scale;

            SpriteRenderer spriteRenderer = border.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = BattleGridPreviewSpriteProvider.GetWhiteSprite();
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;

            activeBorders.Add(border);
        }
    }
}
