using UnityEngine;

// ============================================================
// BuildingPlacementService ??嫄대Ъ 諛곗튂 濡쒖쭅 泥섎━
//
// ??占쏙옙: 諛곗튂 紐⑤뱶 ?占쏀깭 愿占?+ ?占쎌젣 諛곗튂 ?占쏀뻾
// ?占쎄퀎 ?占쎌튃: ?占쎈젰?占?BuildingPlacementController媛 泥섎━, 濡쒖쭅占??占쎈떦
//
// 諛곗튂 ?占쎈쫫:
//   StartPlacing(data)   ??諛곗튂 紐⑤뱶 吏꾩엯, ?占쎈━占??占쎌떆 ?占쎌옉
//   UpdatePreview(pos)   ??占??占쎈젅???占쎌텧, 留덉슦???占?占쎌뿉 ?占쎈━占??占쎌튂/??媛깆떊
//   TryPlaceBuilding(pos)??醫뚰겢占????占쎌텧, ?占쎌젣 諛곗튂 ?占쎈룄
//   CancelPlacing()      ??諛곗튂 痍⑥냼, ?占쎈━占??占쏙옙?
//
// ?占쎈뒫 理쒖쟻??
//   留덉슦?占쏙옙? 媛숋옙? ?占???占쎌뿉 癒몃Т???占쎌븞?占?CanPlace占??占쎄퀎?占쏀븯吏 ?占쎌쓬 (lastTilePos 罹먯떛)
// ============================================================
public class BuildingPlacementService : MonoBehaviour
{
    private TileMapManager tileMapManager;
    private BuildingPreview buildingPreview;    // ?占쎌떇 ?占쎈툕?占쏀듃??遺숈뼱 ?占쎈뒗 ?占쎈━占?
    private Camera mainCamera;

    private BuildingData currentBuilding;       // ?占쎌옱 諛곗튂 以묒씤 嫄대Ъ ?占쎌씠??
    private bool isPlacing = false;             // 諛곗튂 紐⑤뱶 ?占쎌꽦 ?占쏙옙?

    // CanPlace 寃곌낵 罹먯떛 ??媛숋옙? ?占??醫뚰몴?占쎌꽌 諛섎났 怨꾩궛 諛⑼옙?
    private Vector3Int lastTilePos = Vector3Int.zero;
    private bool lastCanPlace = false;

    // ?占쏙옙?(Controller)?占쎌꽌 諛곗튂 以묒씤吏 ?占쎌씤?????占쎌슜
    public bool IsPlacing => isPlacing;

    // ?占쎌옱 ?占쎌튂 ?占??(GetMouseTilePos()占??占쏙옙? ?占??醫뚰몴)
    // 移대뱶 ?占쎌젙 ?占쎌뿉??留덉슦?占쏙옙? ?占쎌떆 怨꾩궛?占쏙옙? 留먭퀬 ??媛믪쓣 ?占쎈룄占??占쎌씪
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

    // ?占?占?諛곗튂 紐⑤뱶 ?占쎌옉 ?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?
    // [?占쎌텧 諛⑸쾿 ??NYH(移대뱶 ?占쎌뒪?? 李멸퀬]
    // 移대뱶 ?占쎈┃ ??CardView ??BuildingPlacementService.StartPlacing(buildingData) 占??占쎌텧.
    // ?占쏀썑 ?占쎈젅?占쎌뼱媛 ?占?占쎌쓣 ?占쎈┃?占쎈㈃ CardSystem.TryQueuePlacementCard()媛 ?占쏀뻾?占쎌뼱
    // TileMapManager.PlaceBuilding()源뚳옙? ?占쎈룞?占쎈줈 ?占쎌뼱吏꾨떎.
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

    // ?占?占?諛곗튂 痍⑥냼 ?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?
    public void CancelPlacing()
    {
        isPlacing       = false;
        currentBuilding = null;
        lastTilePos     = Vector3Int.zero;
        lastCanPlace    = false;
        currentPreviewTilePos = Vector3Int.zero;

        if (buildingPreview != null)
            buildingPreview.HidePreview();
    }

    // ?占?占??占쎈━占?媛깆떊 (占??占쎈젅???占쎌텧) ?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?
    // tilePos: ?占쎌옱 留덉슦?占쏙옙? 媛由ы궎???占??醫뚰몴 (GetMouseTilePos()占??占쎌쓬)
    public void UpdatePreview(Vector3Int tilePos)
    {
        /*if (!isPlacing || buildingPreview == null || currentBuilding == null
            || tileMapManager == null || tileMapManager.groundTilemap == null) return;*/

        // ?占쎈━酉곤옙? ?占쎌젣占??占쎌슜 以묒씤 醫뚰몴占??占쎄린??currentPreviewTilePos???占??
        currentPreviewTilePos = tilePos;

        // 湲곤옙???origin) 怨꾩궛 ??嫄대Ъ 以묒떖 ?占쎈뱶 醫뚰몴 怨꾩궛
        Vector3Int origin = tileMapManager.GetOrigin(tilePos, currentBuilding);
        Vector3 worldPos = tileMapManager.GetBuildingWorldCenter(origin, currentBuilding);

        // ?占??醫뚰몴媛 諛뷂옙??占쎈쭔 CanPlace ?占쎄퀎??(罹먯떛)
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

    // ?占?占?嫄대Ъ 諛곗튂 ?占쎈룄 ?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?
    // ?占쎄났 ??CancelPlacing() ?占쎈룞 ?占쎌텧 ??true 諛섑솚
    // ?占쏀뙣 ??諛곗튂 紐⑤뱶 ?占쏙옙? ??false 諛섑솚
    public bool TryPlaceBuilding(Vector3Int tilePos)
    {
        if (!isPlacing || currentBuilding == null || tileMapManager == null) return false;

        // 媛숋옙? ?占?占쎌씠占?罹먯떆 ?占쎌슜, ?占쎈Ⅸ ?占?占쎌씠占??占쎄퀎??
        bool canPlace = (tilePos == lastTilePos) ? lastCanPlace : tileMapManager.CanPlace(tilePos, currentBuilding);

        if (canPlace)
        {
            tileMapManager.PlaceBuilding(tilePos, currentBuilding);
            CancelPlacing();
            return true;
        }

        return false;
    }

    // HINT: CardView?占쎌꽌 ?占쎌튂 ?占쎌젙 ??GetMouseTilePos() ?占???占쎌슜??getter占?異뷂옙??占쎌꽭??
    // ?? public Vector3Int GetCurrentPreviewTilePos() => currentPreviewTilePos;
    public Vector3Int GetCurrentPreviewTilePos()
    {
        return currentPreviewTilePos;
    }

    // ?占?占?留덉슦???占쎌튂 ???占??醫뚰몴 蹂???占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?占?
    // Controller?占쎌꽌 占??占쎈젅???占쎌텧??UpdatePreview/TryPlaceBuilding???占쎈떖
    public Vector3Int GetMouseTilePos()
    {
        // HINT: ???占쎌닔??"?占쎌옱 留덉슦???占쎌튂占??占쎌떆 怨꾩궛"?占쎈땲??
        // ?占쎈━酉곤옙? ?占쎌젙 醫뚰몴占??占쎌씪?占쎈젮占?移대뱶 ?占쎌젙 ?占쎌뿉?????占쎌닔 ?占??
        // 留덌옙?占??占쎈━占?醫뚰몴占?諛섑솚?占쎈뒗 getter占??占쎈뒗 ?占쎌씠 ???占쎌젙?占쎌엯?占쎈떎.
        if (mainCamera == null || tileMapManager == null || tileMapManager.cityTilemap == null)
            return Vector3Int.zero;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        return tileMapManager.cityTilemap.WorldToCell(mouseWorld);
    }
}
