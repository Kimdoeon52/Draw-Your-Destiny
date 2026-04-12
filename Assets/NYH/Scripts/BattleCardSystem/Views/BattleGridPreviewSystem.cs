namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /*
     * BattleGridPreviewSystem
     *
     * 역할:
     * - 전투 타겟팅 중 선택 가능한 셀을 월드에 간단한 컬러 타일로 표시합니다.
     * - 공격 카드의 타겟 범위를 시각화하는 용도입니다.
     */
    public class BattleGridPreviewSystem : MonoBehaviour
    {
        [SerializeField] private Color previewColor = new(0.2f, 1f, 0.2f, 0.35f);
        [SerializeField] private Vector3 cellScale = new(0.95f, 0.95f, 1f);
        [SerializeField] private int sortingOrder = 50;

        private readonly List<GameObject> activePreviewCells = new();
        private static Sprite cachedPreviewSprite;

        public void ShowCells(IEnumerable<Vector2Int> cells)
        {
            Clear();

            if (cells == null)
            {
                return;
            }

            foreach (var cell in cells)
            {
                GameObject previewCell = new($"BattlePreview_{cell.x}_{cell.y}");
                previewCell.transform.SetParent(transform, false);
                previewCell.transform.position = new Vector3(cell.x, cell.y, 0f);
                previewCell.transform.localScale = cellScale;

                SpriteRenderer spriteRenderer = previewCell.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = GetPreviewSprite();
                spriteRenderer.color = previewColor;
                spriteRenderer.sortingOrder = sortingOrder;

                activePreviewCells.Add(previewCell);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < activePreviewCells.Count; i++)
            {
                if (activePreviewCells[i] != null)
                {
                    Destroy(activePreviewCells[i]);
                }
            }

            activePreviewCells.Clear();
        }

        private static Sprite GetPreviewSprite()
        {
            if (cachedPreviewSprite != null)
            {
                return cachedPreviewSprite;
            }

            cachedPreviewSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);

            return cachedPreviewSprite;
        }
    }
}
