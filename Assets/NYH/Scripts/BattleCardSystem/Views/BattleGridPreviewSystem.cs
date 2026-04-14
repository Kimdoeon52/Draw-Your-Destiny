namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    public class BattleGridPreviewSystem : MonoBehaviour
    {
        private const bool EnablePathPreviewDebug = true;

        [SerializeField] private Color previewColor = new(0f, 1f, 0f, 0.95f);
        [SerializeField] private Color attackPreviewColor = new(1f, 0f, 0f, 0.95f);
        [SerializeField] private Color pathPreviewColor = new(0f, 0.75f, 1f, 1f);
        [SerializeField] private Color unitHighlightColor = new(0.15f, 0.15f, 0.15f, 1f);
        [SerializeField] private Vector3 cellScale = new(1.25f, 1.25f, 1f);
        [SerializeField] private int sortingOrder = 2000;
        [SerializeField] private int pathSortingOrder = 2100;
        [SerializeField] private float previewZ = -0.25f;

        private readonly List<GameObject> activeMovePreviewCells = new();
        private readonly List<GameObject> activeAttackPreviewCells = new();
        private readonly List<GameObject> activePathPreviewCells = new();
        private readonly List<GameObject> activeUnitBorders = new();
        private readonly Dictionary<SpriteRenderer, Color> highlightedUnitColors = new();
        private static Sprite cachedPreviewSprite;

        public void ShowCells(IEnumerable<Vector2Int> cells)
        {
            ShowMoveCells(cells);
        }

        public void ShowMoveCells(IEnumerable<Vector2Int> cells)
        {
            ShowCellsInternal(cells, previewColor, activeMovePreviewCells, sortingOrder, "move", false);
        }

        public void ShowAttackCells(IEnumerable<Vector2Int> cells)
        {
            ShowCellsInternal(cells, attackPreviewColor, activeAttackPreviewCells, sortingOrder, "attack", false);
        }

        public void ShowPathCells(IEnumerable<Vector2Int> cells)
        {
            ShowCellsInternal(cells, pathPreviewColor, activePathPreviewCells, pathSortingOrder, "path", true);
        }

        public void Clear()
        {
            ClearAllPreviewCells();
            ClearUnitBorders();
            ClearUnitHighlights();
        }

        public void ShowUnitBorders(IEnumerable<BattleUnit> units)
        {
            ClearUnitBorders();
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

                CreateBorder(unit.GridPosition, Color.white);
            }
        }

        public void ShowUnitHighlights(IEnumerable<BattleUnit> units)
        {
            ClearUnitHighlights();
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

                SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>();
                foreach (SpriteRenderer renderer in renderers)
                {
                    if (renderer == null || highlightedUnitColors.ContainsKey(renderer))
                    {
                        continue;
                    }

                    highlightedUnitColors.Add(renderer, renderer.color);
                    renderer.color = unitHighlightColor;
                }
            }
        }

        private void ShowCellsInternal(
            IEnumerable<Vector2Int> cells,
            Color color,
            List<GameObject> targetList,
            int rendererSortingOrder,
            string debugLabel,
            bool enableDetailedDebug)
        {
            ClearPreviewList(targetList);
            if (cells == null)
            {
                return;
            }

            List<Vector2Int> createdCells = new();
            foreach (Vector2Int cell in cells)
            {
                GameObject previewCell = new($"BattlePreview_{debugLabel}_{cell.x}_{cell.y}");
                previewCell.transform.SetParent(transform, false);
                previewCell.transform.position = new Vector3(cell.x, cell.y, previewZ);
                previewCell.transform.localScale = cellScale;

                SpriteRenderer spriteRenderer = previewCell.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = GetPreviewSprite();
                spriteRenderer.color = color;
                spriteRenderer.sortingOrder = rendererSortingOrder;

                targetList.Add(previewCell);
                createdCells.Add(cell);
            }

            if (EnablePathPreviewDebug && enableDetailedDebug)
            {
                LogPathPreviewDiagnostics(createdCells, targetList, rendererSortingOrder);
            }
        }

        private void ClearAllPreviewCells()
        {
            ClearPreviewList(activeMovePreviewCells);
            ClearPreviewList(activeAttackPreviewCells);
            ClearPreviewList(activePathPreviewCells);
        }

        private static void ClearPreviewList(List<GameObject> previewList)
        {
            for (int i = 0; i < previewList.Count; i++)
            {
                if (previewList[i] != null)
                {
                    Destroy(previewList[i]);
                }
            }

            previewList.Clear();
        }

        private void ClearUnitHighlights()
        {
            foreach (var pair in highlightedUnitColors)
            {
                if (pair.Key != null)
                {
                    pair.Key.color = pair.Value;
                }
            }

            highlightedUnitColors.Clear();
        }

        private void ClearUnitBorders()
        {
            for (int i = 0; i < activeUnitBorders.Count; i++)
            {
                if (activeUnitBorders[i] != null)
                {
                    Destroy(activeUnitBorders[i]);
                }
            }

            activeUnitBorders.Clear();
        }

        private void CreateBorder(Vector2Int centerCell, Color color)
        {
            CreateBorderSegment(centerCell, new Vector3(0f, 0.52f, previewZ), new Vector3(1.18f, 0.12f, 1f), color);
            CreateBorderSegment(centerCell, new Vector3(0f, -0.52f, previewZ), new Vector3(1.18f, 0.12f, 1f), color);
            CreateBorderSegment(centerCell, new Vector3(-0.52f, 0f, previewZ), new Vector3(0.12f, 1.18f, 1f), color);
            CreateBorderSegment(centerCell, new Vector3(0.52f, 0f, previewZ), new Vector3(0.12f, 1.18f, 1f), color);
        }

        private void CreateBorderSegment(Vector2Int centerCell, Vector3 localOffset, Vector3 scale, Color color)
        {
            GameObject border = new("BattleUnitBorder");
            border.transform.SetParent(transform, false);
            border.transform.position = new Vector3(centerCell.x, centerCell.y, 0f) + localOffset;
            border.transform.localScale = scale;

            SpriteRenderer spriteRenderer = border.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetPreviewSprite();
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = pathSortingOrder + 1;

            activeUnitBorders.Add(border);
        }

        private void LogPathPreviewDiagnostics(
            IReadOnlyList<Vector2Int> cells,
            IReadOnlyList<GameObject> previewObjects,
            int rendererSortingOrder)
        {
            int brokenSegments = 0;
            StringBuilder details = new();

            for (int i = 0; i < cells.Count; i++)
            {
                if (i > 0)
                {
                    Vector2Int previous = cells[i - 1];
                    Vector2Int current = cells[i];
                    int manhattan = Mathf.Abs(previous.x - current.x) + Mathf.Abs(previous.y - current.y);
                    if (manhattan != 1)
                    {
                        brokenSegments++;
                    }
                }

                GameObject previewObject = i < previewObjects.Count ? previewObjects[i] : null;
                if (details.Length > 0)
                {
                    details.Append(" | ");
                }

                details.Append(
                    $"#{i}:{cells[i]},obj={(previewObject != null ? previewObject.name : "null")},world={(previewObject != null ? previewObject.transform.position.ToString() : "null")},sorting={rendererSortingOrder},scale={(previewObject != null ? previewObject.transform.localScale.ToString() : "null")}");
            }

            Debug.Log(
                $"[BattlePathDebug] previewCount={cells.Count}, brokenSegments={brokenSegments}, previewZ={previewZ}, moveSorting={sortingOrder}, pathSorting={pathSortingOrder}, details={details}");
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
                Texture2D.whiteTexture.width);

            return cachedPreviewSprite;
        }
    }
}
