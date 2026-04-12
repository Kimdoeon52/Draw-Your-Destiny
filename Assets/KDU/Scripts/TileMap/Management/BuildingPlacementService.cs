using UnityEngine;

// ============================================================
// BuildingPlacementService ??건물 배치 로직 처리
//
// ??��: 배치 모드 ?�태 관�?+ ?�제 배치 ?�행
// ?�계 ?�칙: ?�력?� BuildingPlacementController가 처리, 로직�??�당
//
// 배치 ?�름:
//   StartPlacing(data)   ??배치 모드 진입, ?�리�??�시 ?�작
//   UpdatePreview(pos)   ??�??�레???�출, 마우???�?�에 ?�리�??�치/??갱신
//   TryPlaceBuilding(pos)??좌클�????�출, ?�제 배치 ?�도
//   CancelPlacing()      ??배치 취소, ?�리�??��?
//
// ?�능 최적??
//   마우?��? 같�? ?�???�에 머무???�안?� CanPlace�??�계?�하지 ?�음 (lastTilePos 캐싱)
// ============================================================
public class BuildingPlacementService : MonoBehaviour
{
    private TileMapManager tileMapManager;
    private BuildingPreview buildingPreview;    // ?�식 ?�브?�트??붙어 ?�는 ?�리�?
    private Camera mainCamera;

    private BuildingData currentBuilding;       // ?�재 배치 중인 건물 ?�이??
    private bool isPlacing = false;             // 배치 모드 ?�성 ?��?

    // CanPlace 결과 캐싱 ??같�? ?�??좌표?�서 반복 계산 방�?
    private Vector3Int lastTilePos = Vector3Int.zero;
    private bool lastCanPlace = false;

    // ?��?(Controller)?�서 배치 중인지 ?�인?????�용
    public bool IsPlacing => isPlacing;

    // ?�재 ?�치 ?�??(GetMouseTilePos()�??��? ?�??좌표)
    // 카드 ?�정 ?�에??마우?��? ?�시 계산?��? 말고 ??값을 ?�도�??�일
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

    // ?�?� 배치 모드 ?�작 ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
    // [?�출 방법 ??NYH(카드 ?�스?? 참고]
    // 카드 ?�릭 ??CardView ??BuildingPlacementService.StartPlacing(buildingData) �??�출.
    // ?�후 ?�레?�어가 ?�?�을 ?�릭?�면 CardSystem.TryQueuePlacementCard()가 ?�행?�어
    // TileMapManager.PlaceBuilding()까�? ?�동?�로 ?�어진다.
    //
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

    // ?�?� 배치 취소 ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
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

    // ?�?� ?�리�?갱신 (�??�레???�출) ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
    // tilePos: ?�재 마우?��? 가리키???�??좌표 (GetMouseTilePos()�??�음)
    public void UpdatePreview(Vector3Int tilePos)
    {
        if (!isPlacing || buildingPreview == null || currentBuilding == null
            || tileMapManager == null || tileMapManager.groundTilemap == null) return;

        // ?�리뷰�? ?�제�??�용 중인 좌표�??�기??currentPreviewTilePos???�??
        currentPreviewTilePos = tilePos;

        // 기�???origin) 계산 ??건물 중심 ?�드 좌표 계산
        Vector3Int origin = tileMapManager.GetOrigin(tilePos, currentBuilding);
        Vector3 worldPos = tileMapManager.GetBuildingWorldCenter(origin, currentBuilding);

        // ?�??좌표가 바�??�만 CanPlace ?�계??(캐싱)
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

    // ?�?� 건물 배치 ?�도 ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
    // ?�공 ??CancelPlacing() ?�동 ?�출 ??true 반환
    // ?�패 ??배치 모드 ?��? ??false 반환
    public bool TryPlaceBuilding(Vector3Int tilePos)
    {
        if (!isPlacing || currentBuilding == null || tileMapManager == null) return false;

        // 같�? ?�?�이�?캐시 ?�용, ?�른 ?�?�이�??�계??
        bool canPlace = (tilePos == lastTilePos) ? lastCanPlace : tileMapManager.CanPlace(tilePos, currentBuilding);

        if (canPlace)
        {
            tileMapManager.PlaceBuilding(tilePos, currentBuilding);
            CancelPlacing();
            return true;
        }

        return false;
    }

    // HINT: CardView?�서 ?�치 ?�정 ??GetMouseTilePos() ?�???�용??getter�?추�??�세??
    // ?? public Vector3Int GetCurrentPreviewTilePos() => currentPreviewTilePos;
    public Vector3Int GetCurrentPreviewTilePos()
    {
        Debug.Log($"[PlacementService:{GetInstanceID()}] GetCurrentPreviewTilePos -> {currentPreviewTilePos}, isPlacing={isPlacing}");
        return currentPreviewTilePos;
    }

    // ?�?� 마우???�치 ???�??좌표 변???�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
    // Controller?�서 �??�레???�출??UpdatePreview/TryPlaceBuilding???�달
    public Vector3Int GetMouseTilePos()
    {
        // HINT: ???�수??"?�재 마우???�치�??�시 계산"?�니??
        // ?�리뷰�? ?�정 좌표�??�일?�려�?카드 ?�정 ?�에?????�수 ?�??
        // 마�?�??�리�?좌표�?반환?�는 getter�??�는 ?�이 ???�정?�입?�다.
        if (mainCamera == null || tileMapManager == null || tileMapManager.groundTilemap == null)
            return Vector3Int.zero;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        return tileMapManager.groundTilemap.WorldToCell(mouseWorld);
    }
}

