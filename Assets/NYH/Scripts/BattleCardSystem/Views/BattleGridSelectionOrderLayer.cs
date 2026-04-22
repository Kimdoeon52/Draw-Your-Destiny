namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    // 다중 공격 선택 순서를 테두리와 숫자로 표시하는 레이어입니다.
    internal sealed class BattleGridSelectionOrderLayer
    {
        private static readonly Color MarkerBorderColor = new(1f, 0.92f, 0.35f, 1f);
        private static readonly Color MarkerTextColor = new(0.15f, 0.05f, 0.05f, 1f);

        private readonly Transform parent;
        private readonly float previewZ;
        private readonly List<GameObject> activeMarkers = new();

        // 생성될 순서 마커들의 부모와 Z 위치를 저장합니다.
        public BattleGridSelectionOrderLayer(Transform parent, float previewZ)
        {
            this.parent = parent;
            this.previewZ = previewZ;
        }

        // 선택된 셀 목록을 순서대로 숫자 마커와 테두리로 다시 그립니다.
        public void Show(IReadOnlyList<Vector2Int> cells, int borderSortingOrder, int textSortingOrder)
        {
            Clear();
            if (cells == null)
            {
                return;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                CreateSelectionOrderMarker(cells[i], i + 1, borderSortingOrder, textSortingOrder);
            }
        }

        // 이 레이어가 만든 모든 순서 마커 오브젝트를 제거합니다.
        public void Clear()
        {
            for (int i = 0; i < activeMarkers.Count; i++)
            {
                if (activeMarkers[i] != null)
                {
                    Object.Destroy(activeMarkers[i]);
                }
            }

            activeMarkers.Clear();
        }

        // 단일 셀 위에 선택 순서 테두리와 숫자 라벨을 생성합니다.
        private void CreateSelectionOrderMarker(Vector2Int centerCell, int order, int borderSortingOrder, int textSortingOrder)
        {
            CreateBorder(centerCell, borderSortingOrder);

            GameObject label = new($"BattleAttackOrder_{order}");
            label.transform.SetParent(parent, false);
            if (!BattleGridCoordinateService.Instance.TryGetWorldCenter(centerCell, out Vector3 labelWorld))
            {
                Object.Destroy(label);
                return;
            }

            label.transform.position = new Vector3(labelWorld.x, labelWorld.y, previewZ - 0.01f);

            TextMesh textMesh = label.AddComponent<TextMesh>();
            textMesh.text = order.ToString();
            textMesh.fontSize = 64;
            textMesh.characterSize = 0.09f;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = MarkerTextColor;

            MeshRenderer renderer = label.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = textSortingOrder;
            }

            activeMarkers.Add(label);
        }

        // 순서 마커의 강조 테두리를 네 조각으로 생성합니다.
        private void CreateBorder(Vector2Int centerCell, int sortingOrder)
        {
            CreateBorderSegment(centerCell, new Vector3(0f, 0.52f, previewZ), new Vector3(1.18f, 0.12f, 1f), sortingOrder);
            CreateBorderSegment(centerCell, new Vector3(0f, -0.52f, previewZ), new Vector3(1.18f, 0.12f, 1f), sortingOrder);
            CreateBorderSegment(centerCell, new Vector3(-0.52f, 0f, previewZ), new Vector3(0.12f, 1.18f, 1f), sortingOrder);
            CreateBorderSegment(centerCell, new Vector3(0.52f, 0f, previewZ), new Vector3(0.12f, 1.18f, 1f), sortingOrder);
        }

        // 순서 마커 테두리의 한 변을 생성합니다.
        private void CreateBorderSegment(Vector2Int centerCell, Vector3 localOffset, Vector3 scale, int sortingOrder)
        {
            GameObject border = new("BattleAttackOrderBorder");
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
            spriteRenderer.color = MarkerBorderColor;
            spriteRenderer.sortingOrder = sortingOrder;

            activeMarkers.Add(border);
        }
    }
}
