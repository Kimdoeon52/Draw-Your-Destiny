namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    // 전투 그리드 프리뷰의 외부 진입점입니다.
    // 실제 생성/삭제 로직은 전용 Layer 클래스에 위임해 씬 연결과 public API를 유지합니다.
    public class BattleGridPreviewSystem : MonoBehaviour
    {
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

        private BattleGridCellPreviewLayer movePreviewLayer;
        private BattleGridCellPreviewLayer attackPreviewLayer;
        private BattleGridCellPreviewLayer pathPreviewLayer;
        private BattleGridCellPreviewLayer attackImpactPreviewLayer;
        private BattleGridUnitBorderLayer unitBorderLayer;
        private BattleGridUnitBorderLayer impactUnitBorderLayer;
        private BattleGridUnitBorderLayer hoverCellBorderLayer;
        private BattleGridSelectionOrderLayer selectionOrderLayer;
        private BattleUnitHighlightLayer unitHighlightLayer;

        // 프리뷰 Layer들을 준비해 public 메서드가 바로 위임할 수 있게 합니다.
        private void Awake()
        {
            InitializeLayers();
        }

        // 기존 ShowCells 호출을 이동 셀 프리뷰로 연결하는 호환용 래퍼입니다.
        public void ShowCells(IEnumerable<Vector2Int> cells)
        {
            ShowMoveCells(cells);
        }

        // 이동 가능한 셀들을 초록색 프리뷰로 표시합니다.
        public void ShowMoveCells(IEnumerable<Vector2Int> cells)
        {
            EnsureLayers();
            movePreviewLayer.Show(cells, previewColor, sortingOrder, "move");
        }

        // 공격 가능한 셀들을 빨간색 프리뷰로 표시합니다.
        public void ShowAttackCells(IEnumerable<Vector2Int> cells)
        {
            EnsureLayers();
            attackPreviewLayer.Show(cells, attackPreviewColor, sortingOrder, "attack");
        }

        // 플레이어가 실제로 그린 이동 경로를 별도 색상으로 표시합니다.
        public void ShowPathCells(IEnumerable<Vector2Int> cells)
        {
            EnsureLayers();
            pathPreviewLayer.Show(cells, pathPreviewColor, pathSortingOrder, "path");
        }

        // 현재 떠 있는 모든 셀/테두리/마커/유닛 하이라이트를 제거합니다.
        public void Clear()
        {
            EnsureLayers();
            movePreviewLayer.Clear();
            attackPreviewLayer.Clear();
            pathPreviewLayer.Clear();
            attackImpactPreviewLayer.Clear();
            unitBorderLayer.Clear();
            impactUnitBorderLayer.Clear();
            hoverCellBorderLayer.Clear();
            selectionOrderLayer.Clear();
            unitHighlightLayer.Clear();
        }

        // 하이라이트나 피격 플래시가 꼬였을 때 모든 유닛 색상을 즉시 초기화합니다.
        public void ResetAllUnitColorsImmediate()
        {
            EnsureLayers();
            unitHighlightLayer.ResetAllUnitColorsImmediate();
        }

        // 카드를 사용할 수 있는 유닛 후보들에 흰색 테두리를 표시합니다.
        public void ShowUnitBorders(IEnumerable<BattleUnit> units)
        {
            EnsureLayers();
            unitBorderLayer.ShowUnits(units, Color.white, pathSortingOrder + 1);
        }

        // 현재 공격 프리뷰에서 실제로 맞는 유닛들에 강조 테두리를 표시합니다.
        public void ShowImpactUnitBorders(IEnumerable<BattleUnit> units)
        {
            EnsureLayers();
            impactUnitBorderLayer.ShowUnits(units, impactBorderColor, pathSortingOrder + 2);
        }

        // 이동 중 마우스가 올라간 셀에 흰색 테두리를 표시합니다.
        public void ShowHoverCellBorder(Vector2Int? cell)
        {
            EnsureLayers();
            hoverCellBorderLayer.ShowCell(cell, hoverCellBorderColor, pathSortingOrder + 3);
        }

        // 공격 대상 선택 시 실제 영향 범위 셀을 노란색 계열로 표시합니다.
        public void ShowAttackImpactCells(IEnumerable<Vector2Int> cells)
        {
            EnsureLayers();
            attackImpactPreviewLayer.Show(cells, attackImpactCellColor, pathSortingOrder + 1, "impact");
        }

        // 다중 공격 대상 선택 순서를 숫자 마커로 표시합니다.
        public void ShowAttackSelectionOrder(IReadOnlyList<Vector2Int> cells)
        {
            EnsureLayers();
            selectionOrderLayer.Show(cells, pathSortingOrder + 4, pathSortingOrder + 5);
        }

        // 현재 조작 중인 아군 유닛을 어둡게 칠해 눈에 띄게 합니다.
        public void ShowUnitHighlights(IEnumerable<BattleUnit> units)
        {
            EnsureLayers();
            unitHighlightLayer.Show(units, unitHighlightColor);
        }

        // 아직 Layer가 없으면 생성해 Awake 이전 호출이나 런타임 생성 상황을 보호합니다.
        private void EnsureLayers()
        {
            if (movePreviewLayer == null)
            {
                InitializeLayers();
            }
        }

        // 인스펙터 설정값을 사용해 역할별 프리뷰 Layer를 생성합니다.
        private void InitializeLayers()
        {
            movePreviewLayer = new BattleGridCellPreviewLayer(transform, cellScale, previewZ);
            attackPreviewLayer = new BattleGridCellPreviewLayer(transform, cellScale, previewZ);
            pathPreviewLayer = new BattleGridCellPreviewLayer(transform, cellScale, previewZ);
            attackImpactPreviewLayer = new BattleGridCellPreviewLayer(transform, cellScale, previewZ);
            unitBorderLayer = new BattleGridUnitBorderLayer(transform, previewZ);
            impactUnitBorderLayer = new BattleGridUnitBorderLayer(transform, previewZ);
            hoverCellBorderLayer = new BattleGridUnitBorderLayer(transform, previewZ);
            selectionOrderLayer = new BattleGridSelectionOrderLayer(transform, previewZ);
            unitHighlightLayer = new BattleUnitHighlightLayer();
        }
    }
}
