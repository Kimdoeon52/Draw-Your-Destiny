namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    // 보드 위에 이동/공격/경로/유닛 선택 상태를 즉석에서 그려주는 프리뷰 시스템입니다.
    public class BattleGridPreviewSystem : MonoBehaviour
    {
        private const bool EnablePathPreviewDebug = true;

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
        private readonly Dictionary<SpriteRenderer, Color> highlightedUnitColors = new();
        private static Sprite cachedPreviewSprite;

        // 기존 호출부 호환용 래퍼입니다.
        public void ShowCells(IEnumerable<Vector2Int> cells)
        {
            ShowMoveCells(cells);
        }

        // 이동 가능한 칸을 초록색 셀로 표시합니다.
        public void ShowMoveCells(IEnumerable<Vector2Int> cells)
        {
            ShowCellsInternal(cells, previewColor, activeMovePreviewCells, sortingOrder, "move", false);
        }

        // 공격 가능한 칸을 빨간색 셀로 표시합니다.
        public void ShowAttackCells(IEnumerable<Vector2Int> cells)
        {
            ShowCellsInternal(cells, attackPreviewColor, activeAttackPreviewCells, sortingOrder, "attack", false);
        }

        // 플레이어가 실제로 그린 경로를 별도 색/정렬 순서로 강조합니다.
        public void ShowPathCells(IEnumerable<Vector2Int> cells)
        {
            ShowCellsInternal(cells, pathPreviewColor, activePathPreviewCells, pathSortingOrder, "path", true);
        }

        // 현재 표시 중인 모든 프리뷰를 한 번에 정리합니다.
        public void Clear()
        {
            ClearAllPreviewCells();
            ClearUnitBorders();
            ClearImpactUnitBorders();
            ClearHoverCellBorders();
            ClearUnitHighlights();
        }

        // 선택 가능한 유닛 후보를 테두리로 감쌉니다.
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

        // 현재 공격에 실제로 피격될 유닛만 별도 테두리로 강조합니다.
        public void ShowImpactUnitBorders(IEnumerable<BattleUnit> units)
        {
            ClearImpactUnitBorders();
            if (units == null)
            {
                return;
            }

            // 공격 범위 전체와 별도로,
            // 현재 마우스가 가리키는 타일로 실제 타격되는 적만 강조합니다.
            foreach (BattleUnit unit in units)
            {
                if (unit == null)
                {
                    continue;
                }

                CreateBorder(unit.GridPosition, impactBorderColor, activeImpactUnitBorders, pathSortingOrder + 2);
            }
        }

        // 이동 목표로 가리키는 칸 하나를 흰색 테두리로 강조합니다.
        public void ShowHoverCellBorder(Vector2Int? cell)
        {
            ClearHoverCellBorders();
            if (!cell.HasValue)
            {
                return;
            }

            CreateBorder(cell.Value, hoverCellBorderColor, activeHoverCellBorders, pathSortingOrder + 3);
        }

        // 마우스가 올라간 공격 타일 기준 실제 피격 패턴 셀을 반투명하게 보여줍니다.
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

        // 현재 조작 중인 아군 유닛을 틴트 처리해 시선을 모읍니다.
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

        // 같은 종류의 이전 프리뷰를 지우고, 전달받은 셀 목록을 화면에 다시 생성합니다.
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

        // 이동/공격/경로 셀 프리뷰를 모두 제거합니다.
        private void ClearAllPreviewCells()
        {
            ClearPreviewList(activeMovePreviewCells);
            ClearPreviewList(activeAttackPreviewCells);
            ClearPreviewList(activePathPreviewCells);
            ClearPreviewList(activeAttackImpactPreviewCells);
        }

        // 프리뷰 오브젝트 목록을 안전하게 제거하고 비웁니다.
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

        // 강조 처리에 사용했던 원래 색상을 복원합니다.
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

        // 선택 가능 유닛 테두리를 제거합니다.
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

        // 실제 피격 유닛 테두리를 제거합니다.
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

        // 기본 유닛 선택 테두리를 생성합니다.
        private void CreateBorder(Vector2Int centerCell, Color color)
        {
            CreateBorder(centerCell, color, activeUnitBorders, pathSortingOrder + 1);
        }

        // 사각형 외곽선을 4개의 얇은 스프라이트로 만들어 한 칸을 감쌉니다.
        private void CreateBorder(Vector2Int centerCell, Color color, List<GameObject> targetList, int borderSortingOrder)
        {
            CreateBorderSegment(centerCell, new Vector3(0f, 0.52f, previewZ), new Vector3(1.18f, 0.12f, 1f), color, targetList, borderSortingOrder);
            CreateBorderSegment(centerCell, new Vector3(0f, -0.52f, previewZ), new Vector3(1.18f, 0.12f, 1f), color, targetList, borderSortingOrder);
            CreateBorderSegment(centerCell, new Vector3(-0.52f, 0f, previewZ), new Vector3(0.12f, 1.18f, 1f), color, targetList, borderSortingOrder);
            CreateBorderSegment(centerCell, new Vector3(0.52f, 0f, previewZ), new Vector3(0.12f, 1.18f, 1f), color, targetList, borderSortingOrder);
        }

        // 외곽선 한 변을 구성하는 단일 스프라이트 조각입니다.
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

        // 경로 프리뷰가 끊기거나 정렬 순서가 꼬일 때 확인하는 진단용 로그입니다.
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

        // 미리보기용 흰색 스프라이트를 한 번만 생성해 재사용합니다.
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
