namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    // 하나의 셀 프리뷰 레이어를 생성하고 제거합니다.
    internal sealed class BattleGridCellPreviewLayer
    {
        private readonly Transform parent;
        private readonly Vector3 cellScale;
        private readonly float previewZ;
        private readonly List<GameObject> activeCells = new();

        // 생성될 셀 프리뷰의 부모, 크기, Z 위치를 저장합니다.
        public BattleGridCellPreviewLayer(Transform parent, Vector3 cellScale, float previewZ)
        {
            this.parent = parent;
            this.cellScale = cellScale;
            this.previewZ = previewZ;
        }

        // 전달받은 그리드 셀들을 지정 색상과 정렬 순서의 SpriteRenderer로 다시 그립니다.
        public void Show(IEnumerable<Vector2Int> cells, Color color, int sortingOrder, string label)
        {
            Clear();
            if (cells == null)
            {
                return;
            }

            foreach (Vector2Int cell in cells)
            {
                CreateCell(cell, color, sortingOrder, label);
            }
        }

        // 이 레이어가 만든 모든 프리뷰 셀 오브젝트를 제거합니다.
        public void Clear()
        {
            for (int i = 0; i < activeCells.Count; i++)
            {
                if (activeCells[i] != null)
                {
                    Object.Destroy(activeCells[i]);
                }
            }

            activeCells.Clear();
        }

        // 단일 그리드 셀을 월드 좌표로 변환한 뒤 흰색 스프라이트 기반 프리뷰를 생성합니다.
        private void CreateCell(Vector2Int cell, Color color, int sortingOrder, string label)
        {
            if (!BattleGridCoordinateService.Instance.TryGetWorldCenter(cell, out Vector3 previewWorld))
            {
                return;
            }

            GameObject previewCell = new($"BattlePreview_{label}_{cell.x}_{cell.y}");
            previewCell.transform.SetParent(parent, false);
            previewCell.transform.position = new Vector3(previewWorld.x, previewWorld.y, previewZ);
            previewCell.transform.localScale = cellScale;

            SpriteRenderer spriteRenderer = previewCell.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = BattleGridPreviewSpriteProvider.GetWhiteSprite();
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;

            activeCells.Add(previewCell);
        }
    }
}
