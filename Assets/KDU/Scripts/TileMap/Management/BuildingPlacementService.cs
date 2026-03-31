using UnityEngine;

// ============================================================
// BuildingPlacementService — 건물 배치 로직 처리
//
// 역할: 배치 모드 상태 관리 + 실제 배치 실행
// 설계 원칙: 입력은 BuildingPlacementController가 처리, 로직만 담당
//
// 배치 흐름:
//   StartPlacing(data)   → 배치 모드 진입, 프리뷰 표시 시작
//   UpdatePreview(pos)   → 매 프레임 호출, 마우스 타일에 프리뷰 위치/색 갱신
//   TryPlaceBuilding(pos)→ 좌클릭 시 호출, 실제 배치 시도
//   CancelPlacing()      → 배치 취소, 프리뷰 숨김
//
// 성능 최적화:
//   마우스가 같은 타일 안에 머무는 동안은 CanPlace를 재계산하지 않음 (lastTilePos 캐싱)
// ============================================================
public class BuildingPlacementService : MonoBehaviour
{
    private TileMapManager tileMapManager;
    private BuildingPreview buildingPreview;    // 자식 오브젝트에 붙어 있는 프리뷰
    private Camera mainCamera;

    private BuildingData currentBuilding;       // 현재 배치 중인 건물 데이터
    private bool isPlacing = false;             // 배치 모드 활성 여부

    // CanPlace 결과 캐싱 — 같은 타일 좌표에서 반복 계산 방지
    private Vector3Int lastTilePos = Vector3Int.zero;
    private bool lastCanPlace = false;

    // 외부(Controller)에서 배치 중인지 확인할 때 사용
    public bool IsPlacing => isPlacing;

    // 현재 위치 저장 (GetMouseTilePos()로 얻은 타일 좌표)
    // 카드 확정 시에는 마우스를 다시 계산하지 말고 이 값을 쓰도록 통일
    private Vector3Int currentPreviewTilePos = Vector3Int.zero;

    private void Awake()
    {
        buildingPreview = GetComponentInChildren<BuildingPreview>();
    }

    private void Start()
    {
        tileMapManager = TileMapManager.Instance;
        mainCamera     = Camera.main;

        if (buildingPreview != null)
            buildingPreview.HidePreview();
    }

    // ── 배치 모드 시작 ────────────────────────────────────────
    // 카드 사용 또는 GameManager.StartBuildingPlacement()에서 호출
    public void StartPlacing(BuildingData data)
    {
        if (data == null || tileMapManager == null) return;

        currentBuilding = data;
        isPlacing       = true;
        lastTilePos     = Vector3Int.zero;
        lastCanPlace    = false;
        currentPreviewTilePos = Vector3Int.zero;

        if (buildingPreview != null)
            buildingPreview.ShowPreview(currentBuilding);
    }

    // ── 배치 취소 ─────────────────────────────────────────────
    public void CancelPlacing()
    {
        isPlacing       = false;
        currentBuilding = null;
        lastTilePos     = Vector3Int.zero;
        lastCanPlace    = false;
        Debug.Log($"[PlacementService:{GetInstanceID()}] CancelPlacing 직전 preview tile={currentPreviewTilePos}");
        currentPreviewTilePos = Vector3Int.zero;

        if (buildingPreview != null)
            buildingPreview.HidePreview();
    }

    // ── 프리뷰 갱신 (매 프레임 호출) ─────────────────────────
    // tilePos: 현재 마우스가 가리키는 타일 좌표 (GetMouseTilePos()로 얻음)
    public void UpdatePreview(Vector3Int tilePos)
    {
        if (!isPlacing || buildingPreview == null || currentBuilding == null
            || tileMapManager == null || tileMapManager.groundTilemap == null) return;

        // 프리뷰가 실제로 사용 중인 좌표를 여기서 currentPreviewTilePos에 저장
        currentPreviewTilePos = tilePos;

        // 기준점(origin) 계산 → 건물 중심 월드 좌표 계산
        Vector3Int origin = tileMapManager.GetOrigin(tilePos, currentBuilding);
        float centerX = origin.x + (currentBuilding.width  - 1) * 0.5f;
        float centerY = origin.y + (currentBuilding.height - 1) * 0.5f;
        Vector3 worldPos = tileMapManager.groundTilemap.CellToWorld(
            new Vector3Int(Mathf.RoundToInt(centerX), Mathf.RoundToInt(centerY), 0));
        worldPos += tileMapManager.groundTilemap.cellSize * 0.5f; // 타일 중심으로 보정

        // 타일 좌표가 바뀔 때만 CanPlace 재계산 (캐싱)
        bool canPlace;
        if (tilePos != lastTilePos)
        {
            canPlace     = tileMapManager.CanPlace(tilePos, currentBuilding);
            lastTilePos  = tilePos;
            lastCanPlace = canPlace;
        }
        else
        {
            canPlace = lastCanPlace;
        }

        buildingPreview.UpdatePreview(worldPos, canPlace);
    }

    // ── 건물 배치 시도 ────────────────────────────────────────
    // 성공 시 CancelPlacing() 자동 호출 → true 반환
    // 실패 시 배치 모드 유지 → false 반환
    public bool TryPlaceBuilding(Vector3Int tilePos)
    {
        if (!isPlacing || currentBuilding == null || tileMapManager == null) return false;

        // 같은 타일이면 캐시 사용, 다른 타일이면 재계산
        bool canPlace = (tilePos == lastTilePos) ? lastCanPlace : tileMapManager.CanPlace(tilePos, currentBuilding);

        if (canPlace)
        {
            tileMapManager.PlaceBuilding(tilePos, currentBuilding);
            CancelPlacing();
            return true;
        }

        return false;
    }

    // HINT: CardView에서 설치 확정 시 GetMouseTilePos() 대신 사용할 getter를 추가하세요.
    // 예: public Vector3Int GetCurrentPreviewTilePos() => currentPreviewTilePos;
    public Vector3Int GetCurrentPreviewTilePos()
    {
        Debug.Log($"[PlacementService:{GetInstanceID()}] GetCurrentPreviewTilePos -> {currentPreviewTilePos}, isPlacing={isPlacing}");
        return currentPreviewTilePos;
    }

    // ── 마우스 위치 → 타일 좌표 변환 ─────────────────────────
    // Controller에서 매 프레임 호출해 UpdatePreview/TryPlaceBuilding에 전달
    public Vector3Int GetMouseTilePos()
    {
        // HINT: 이 함수는 "현재 마우스 위치를 다시 계산"합니다.
        // 프리뷰와 확정 좌표를 통일하려면 카드 확정 시에는 이 함수 대신
        // 마지막 프리뷰 좌표를 반환하는 getter를 쓰는 편이 더 안정적입니다.
        if (mainCamera == null || tileMapManager == null || tileMapManager.groundTilemap == null)
            return Vector3Int.zero;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        return tileMapManager.groundTilemap.WorldToCell(mouseWorld);
    }
}
