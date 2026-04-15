namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    // Draws move, attack, path, and unit selection previews over the battle grid.
    public class BattleGridPreviewSystem : MonoBehaviour
    {
        private const bool EnablePathPreviewDebug = false;

        [SerializeField] private Color previewColor = new(0f, 1f, 0f, 0.95f);
        [SerializeField] private Color attackPreviewColor = new(1f, 0f, 0f, 0.95f);
        [SerializeField] private Color pathPreviewColor = new(0f, 0.75f, 1f, 1f);
        [SerializeField] private Color unitHighlightColor = new(0.15f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color impactBorderColor = new(1f, 0.35f, 0.2f, 1f);
        [SerializeField] private Color hoverCellBorderColor = new(1f, 1f, 1f, 1f);
        [SerializeField] private Color attackImpactCellColor = new(1f, 0.9f, 0.35f, 0.45f);
        [SerializeField] private Vector3 cellScale = new(1.25f, 1.25f, 1f);
        [SerializeField] private int sortingOrder = 2000;
        [SerializeField] private int pathSortingOrder = 2100;
        [SerializeField] private float previewZ = -0.25f;

        private readonly List<GameObject> activeMovePreviewCells = new();
        private readonly List<GameObject> activeAttackPreviewCells = new();
        private readonly List<GameObject> activePathPreviewCells = new();
        private readonly List<GameObject> activeUnitBorders = new();
        private readonly List<GameObject> activeImpactUnitBorders = new();
        private readonly List<GameObject> activeHoverCellBorders = new();
        private readonly List<GameObject> activeAttackImpactPreviewCells = new();
        private readonly List<GameObject> activeSelectionOrderMarkers = new();
        private readonly Dictionary<SpriteRenderer, Color> highlightedUnitColors = new();
        private static Sprite cachedPreviewSprite;

        // Backward-compatible wrapper for older callers.
        public void ShowCells(IEnumerable<Vector2Int> cells)
        {
            ShowMoveCells(cells);
        }

        // Show selectable move cells in green.
        public void ShowMoveCells(IEnumerable<Vector2Int> cells)
        {
            ShowCellsInternal(cells, previewColor, activeMovePreviewCells, sortingOrder, "move", false);
        }

        // Show selectable attack cells in red.
        public void ShowAttackCells(IEnumerable<Vector2Int> cells)
        {
            ShowCellsInternal(cells, attackPreviewColor, activeAttackPreviewCells, sortingOrder, "attack", false);
        }

        // Highlight the actual drawn move path separately from selectable cells.
        public void ShowPathCells(IEnumerable<Vector2Int> cells)
        {
            ShowCellsInternal(cells, pathPreviewColor, activePathPreviewCells, pathSortingOrder, "path", true);
        }

        // Clear every active preview in one pass.
        public void Clear()
        {
            ClearAllPreviewCells();
            ClearUnitBorders();
            ClearImpactUnitBorders();
            ClearHoverCellBorders();
            ClearSelectionOrderMarkers();
            ClearUnitHighlights();
        }

        // Outline selectable unit candidates.
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

        // Outline only the units that would actually be hit by the current attack.
        public void ShowImpactUnitBorders(IEnumerable<BattleUnit> units)
        {
            ClearImpactUnitBorders();
            if (units == null)
            {
                return;
            }

            // This is separate from the full attack range preview.
            // It highlights only the enemies hit by the hovered target tile.
            foreach (BattleUnit unit in units)
            {
                if (unit == null)
                {
                    continue;
                }

                CreateBorder(unit.GridPosition, impactBorderColor, activeImpactUnitBorders, pathSortingOrder + 2);
            }
        }

        // Outline the currently hovered move destination in white.
        public void ShowHoverCellBorder(Vector2Int? cell)
        {
            ClearHoverCellBorders();
            if (!cell.HasValue)
            {
                return;
            }

            CreateBorder(cell.Value, hoverCellBorderColor, activeHoverCellBorders, pathSortingOrder + 3);
        }

        // Show the actual impacted attack pattern cells for the hovered attack tile.
        public void ShowAttackImpactCells(IEnumerable<Vector2Int> cells)
        {
            ShowCellsInternal(
                cells,
                attackImpactCellColor,
                activeAttackImpactPreviewCells,
                pathSortingOrder + 1,
                "impact",
                false);
        }

        public void ShowAttackSelectionOrder(IReadOnlyList<Vector2Int> cells)
        {
            ClearSelectionOrderMarkers();
            if (cells == null)
            {
                return;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                CreateSelectionOrderMarker(cells[i], i + 1);
            }
        }

        // Tint the currently controlled allied unit so it stands out.
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

        // Rebuild one preview layer from the supplied cell list.
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

        // Remove all move, attack, path, and impact preview cells.
        private void ClearAllPreviewCells()
        {
            ClearPreviewList(activeMovePreviewCells);
            ClearPreviewList(activeAttackPreviewCells);
            ClearPreviewList(activePathPreviewCells);
            ClearPreviewList(activeAttackImpactPreviewCells);
        }

        // Safely destroy and clear a preview object list.
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

        // Restore original colors after unit highlight preview ends.
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

        // Clear candidate unit borders.
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

        // Clear impact target borders.
        private void ClearImpactUnitBorders()
        {
            for (int i = 0; i < activeImpactUnitBorders.Count; i++)
            {
                if (activeImpactUnitBorders[i] != null)
                {
                    Destroy(activeImpactUnitBorders[i]);
                }
            }

            activeImpactUnitBorders.Clear();
        }

        private void ClearHoverCellBorders()
        {
            for (int i = 0; i < activeHoverCellBorders.Count; i++)
            {
                if (activeHoverCellBorders[i] != null)
                {
                    Destroy(activeHoverCellBorders[i]);
                }
            }

            activeHoverCellBorders.Clear();
        }

        private void ClearSelectionOrderMarkers()
        {
            for (int i = 0; i < activeSelectionOrderMarkers.Count; i++)
            {
                if (activeSelectionOrderMarkers[i] != null)
                {
                    Destroy(activeSelectionOrderMarkers[i]);
                }
            }

            activeSelectionOrderMarkers.Clear();
        }

        // Create the default unit-selection border.
        private void CreateBorder(Vector2Int centerCell, Color color)
        {
            CreateBorder(centerCell, color, activeUnitBorders, pathSortingOrder + 1);
        }

        // Build a rectangular outline from four thin sprite pieces.
        private void CreateBorder(Vector2Int centerCell, Color color, List<GameObject> targetList, int borderSortingOrder)
        {
            CreateBorderSegment(centerCell, new Vector3(0f, 0.52f, previewZ), new Vector3(1.18f, 0.12f, 1f), color, targetList, borderSortingOrder);
            CreateBorderSegment(centerCell, new Vector3(0f, -0.52f, previewZ), new Vector3(1.18f, 0.12f, 1f), color, targetList, borderSortingOrder);
            CreateBorderSegment(centerCell, new Vector3(-0.52f, 0f, previewZ), new Vector3(0.12f, 1.18f, 1f), color, targetList, borderSortingOrder);
            CreateBorderSegment(centerCell, new Vector3(0.52f, 0f, previewZ), new Vector3(0.12f, 1.18f, 1f), color, targetList, borderSortingOrder);
        }

        // Create one edge segment for the border outline.
        private void CreateBorderSegment(
            Vector2Int centerCell,
            Vector3 localOffset,
            Vector3 scale,
            Color color,
            List<GameObject> targetList,
            int borderSortingOrder)
        {
            GameObject border = new("BattleUnitBorder");
            border.transform.SetParent(transform, false);
            border.transform.position = new Vector3(centerCell.x, centerCell.y, 0f) + localOffset;
            border.transform.localScale = scale;

            SpriteRenderer spriteRenderer = border.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetPreviewSprite();
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = borderSortingOrder;

            targetList.Add(border);
        }

        private void CreateSelectionOrderMarker(Vector2Int centerCell, int order)
        {
            CreateBorder(centerCell, new Color(1f, 0.92f, 0.35f, 1f), activeSelectionOrderMarkers, pathSortingOrder + 4);

            GameObject label = new($"BattleAttackOrder_{order}");
            label.transform.SetParent(transform, false);
            label.transform.position = new Vector3(centerCell.x, centerCell.y, previewZ - 0.01f);

            TextMesh textMesh = label.AddComponent<TextMesh>();
            textMesh.text = order.ToString();
            textMesh.fontSize = 64;
            textMesh.characterSize = 0.09f;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = new Color(0.15f, 0.05f, 0.05f, 1f);

            MeshRenderer renderer = label.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = pathSortingOrder + 5;
            }

            activeSelectionOrderMarkers.Add(label);
        }

        // Diagnostic log for broken or mis-sorted path previews.
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
                    $"#{i}:{cells[i]},obj={(previewObject != null ? previewObject.name : "null")}," +
                    $"world={(previewObject != null ? previewObject.transform.position.ToString() : "null")}," +
                    $"sorting={rendererSortingOrder},scale={(previewObject != null ? previewObject.transform.localScale.ToString() : "null")}");
            }

        /*    Debug.Log(
                $"[BattlePathDebug] previewCount={cells.Count}, brokenSegments={brokenSegments}, previewZ={previewZ}, " +
                $"moveSorting={sortingOrder}, pathSorting={pathSortingOrder}, details={details}");*/
        }

        // Lazily create and reuse the white sprite used by preview cells.
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
