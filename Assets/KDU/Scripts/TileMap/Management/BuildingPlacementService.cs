using UnityEngine;

// ============================================================
// BuildingPlacementService ??ê±´ë¬¼ ë°°ì¹˜ ë¡œì§ ì²˜ë¦¬
//
// ??ï¿½ï¿½: ë°°ì¹˜ ëª¨ë“œ ?ï¿½íƒœ ê´€ï¿?+ ?ï¿½ì œ ë°°ì¹˜ ?ï¿½í–‰
// ?ï¿½ê³„ ?ï¿½ì¹™: ?ï¿½ë ¥?ï¿?BuildingPlacementControllerê°€ ì²˜ë¦¬, ë¡œì§ï¿??ï¿½ë‹¹
//
// ë°°ì¹˜ ?ï¿½ë¦„:
//   StartPlacing(data)   ??ë°°ì¹˜ ëª¨ë“œ ì§„ì…, ?ï¿½ë¦¬ï¿??ï¿½ì‹œ ?ï¿½ì‘
//   UpdatePreview(pos)   ??ï¿??ï¿½ë ˆ???ï¿½ì¶œ, ë§ˆìš°???ï¿?ï¿½ì— ?ï¿½ë¦¬ï¿??ï¿½ì¹˜/??ê°±ì‹ 
//   TryPlaceBuilding(pos)??ì¢Œí´ï¿????ï¿½ì¶œ, ?ï¿½ì œ ë°°ì¹˜ ?ï¿½ë„
//   CancelPlacing()      ??ë°°ì¹˜ ì·¨ì†Œ, ?ï¿½ë¦¬ï¿??ï¿½ï¿½?
//
// ?ï¿½ëŠ¥ ìµœì ??
//   ë§ˆìš°?ï¿½ï¿½? ê°™ï¿½? ?ï¿???ï¿½ì— ë¨¸ë¬´???ï¿½ì•ˆ?ï¿?CanPlaceï¿??ï¿½ê³„?ï¿½í•˜ì§€ ?ï¿½ìŒ (lastTilePos ìºì‹±)
// ============================================================
public class BuildingPlacementService : MonoBehaviour
{
    private TileMapManager tileMapManager;
    private BuildingPreview buildingPreview;    // ?ï¿½ì‹ ?ï¿½ë¸Œ?ï¿½íŠ¸??ë¶™ì–´ ?ï¿½ëŠ” ?ï¿½ë¦¬ï¿?
    private Camera mainCamera;

    private BuildingData currentBuilding;       // ?ï¿½ì¬ ë°°ì¹˜ ì¤‘ì¸ ê±´ë¬¼ ?ï¿½ì´??
    private bool isPlacing = false;             // ë°°ì¹˜ ëª¨ë“œ ?ï¿½ì„± ?ï¿½ï¿½?

    // CanPlace ê²°ê³¼ ìºì‹± ??ê°™ï¿½? ?ï¿??ì¢Œí‘œ?ï¿½ì„œ ë°˜ë³µ ê³„ì‚° ë°©ï¿½?
    private Vector3Int lastTilePos = Vector3Int.zero;
    private bool lastCanPlace = false;

    // ?ï¿½ï¿½?(Controller)?ï¿½ì„œ ë°°ì¹˜ ì¤‘ì¸ì§€ ?ï¿½ì¸?????ï¿½ìš©
    public bool IsPlacing => isPlacing;

    // ?ï¿½ì¬ ?ï¿½ì¹˜ ?ï¿??(GetMouseTilePos()ï¿??ï¿½ï¿½? ?ï¿??ì¢Œí‘œ)
    // ì¹´ë“œ ?ï¿½ì • ?ï¿½ì—??ë§ˆìš°?ï¿½ï¿½? ?ï¿½ì‹œ ê³„ì‚°?ï¿½ï¿½? ë§ê³  ??ê°’ì„ ?ï¿½ë„ï¿??ï¿½ì¼
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

    // ?ï¿?ï¿?ë°°ì¹˜ ëª¨ë“œ ?ï¿½ì‘ ?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?
    // [?ï¿½ì¶œ ë°©ë²• ??NYH(ì¹´ë“œ ?ï¿½ìŠ¤?? ì°¸ê³ ]
    // ì¹´ë“œ ?ï¿½ë¦­ ??CardView ??BuildingPlacementService.StartPlacing(buildingData) ï¿??ï¿½ì¶œ.
    // ?ï¿½í›„ ?ï¿½ë ˆ?ï¿½ì–´ê°€ ?ï¿?ï¿½ì„ ?ï¿½ë¦­?ï¿½ë©´ CardSystem.TryQueuePlacementCard()ê°€ ?ï¿½í–‰?ï¿½ì–´
    // TileMapManager.PlaceBuilding()ê¹Œï¿½? ?ï¿½ë™?ï¿½ë¡œ ?ï¿½ì–´ì§„ë‹¤.
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

    // ?ï¿?ï¿?ë°°ì¹˜ ì·¨ì†Œ ?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?
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

    // ?ï¿?ï¿??ï¿½ë¦¬ï¿?ê°±ì‹  (ï¿??ï¿½ë ˆ???ï¿½ì¶œ) ?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?
    // tilePos: ?ï¿½ì¬ ë§ˆìš°?ï¿½ï¿½? ê°€ë¦¬í‚¤???ï¿??ì¢Œí‘œ (GetMouseTilePos()ï¿??ï¿½ìŒ)
    public void UpdatePreview(Vector3Int tilePos)
    {
        /*if (!isPlacing || buildingPreview == null || currentBuilding == null
            || tileMapManager == null || tileMapManager.groundTilemap == null) return;*/

        // ?ï¿½ë¦¬ë·°ï¿½? ?ï¿½ì œï¿??ï¿½ìš© ì¤‘ì¸ ì¢Œí‘œï¿??ï¿½ê¸°??currentPreviewTilePos???ï¿??
        currentPreviewTilePos = tilePos;

        // ê¸°ï¿½???origin) ê³„ì‚° ??ê±´ë¬¼ ì¤‘ì‹¬ ?ï¿½ë“œ ì¢Œí‘œ ê³„ì‚°
        Vector3Int origin = tileMapManager.GetOrigin(tilePos, currentBuilding);
        Vector3 worldPos = tileMapManager.GetBuildingWorldCenter(origin, currentBuilding);

        // ?ï¿??ì¢Œí‘œê°€ ë°”ï¿½??ï¿½ë§Œ CanPlace ?ï¿½ê³„??(ìºì‹±)
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

    // ?ï¿?ï¿?ê±´ë¬¼ ë°°ì¹˜ ?ï¿½ë„ ?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?
    // ?ï¿½ê³µ ??CancelPlacing() ?ï¿½ë™ ?ï¿½ì¶œ ??true ë°˜í™˜
    // ?ï¿½íŒ¨ ??ë°°ì¹˜ ëª¨ë“œ ?ï¿½ï¿½? ??false ë°˜í™˜
    public bool TryPlaceBuilding(Vector3Int tilePos)
    {
        if (!isPlacing || currentBuilding == null || tileMapManager == null) return false;

        // ê°™ï¿½? ?ï¿?ï¿½ì´ï¿?ìºì‹œ ?ï¿½ìš©, ?ï¿½ë¥¸ ?ï¿?ï¿½ì´ï¿??ï¿½ê³„??
        bool canPlace = (tilePos == lastTilePos) ? lastCanPlace : tileMapManager.CanPlace(tilePos, currentBuilding);

        if (canPlace)
        {
            tileMapManager.PlaceBuilding(tilePos, currentBuilding);
            CancelPlacing();
            return true;
        }

        return false;
    }

    // HINT: CardView?ï¿½ì„œ ?ï¿½ì¹˜ ?ï¿½ì • ??GetMouseTilePos() ?ï¿???ï¿½ìš©??getterï¿?ì¶”ï¿½??ï¿½ì„¸??
    // ?? public Vector3Int GetCurrentPreviewTilePos() => currentPreviewTilePos;
    public Vector3Int GetCurrentPreviewTilePos()
    {
        return currentPreviewTilePos;
    }

    // ?ï¿?ï¿?ë§ˆìš°???ï¿½ì¹˜ ???ï¿??ì¢Œí‘œ ë³€???ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?ï¿?
    // Controller?ï¿½ì„œ ï¿??ï¿½ë ˆ???ï¿½ì¶œ??UpdatePreview/TryPlaceBuilding???ï¿½ë‹¬
    public Vector3Int GetMouseTilePos()
    {
        // HINT: ???ï¿½ìˆ˜??"?ï¿½ì¬ ë§ˆìš°???ï¿½ì¹˜ï¿??ï¿½ì‹œ ê³„ì‚°"?ï¿½ë‹ˆ??
        // ?ï¿½ë¦¬ë·°ï¿½? ?ï¿½ì • ì¢Œí‘œï¿??ï¿½ì¼?ï¿½ë ¤ï¿?ì¹´ë“œ ?ï¿½ì • ?ï¿½ì—?????ï¿½ìˆ˜ ?ï¿??
        // ë§ˆï¿½?ï¿??ï¿½ë¦¬ï¿?ì¢Œí‘œï¿?ë°˜í™˜?ï¿½ëŠ” getterï¿??ï¿½ëŠ” ?ï¿½ì´ ???ï¿½ì •?ï¿½ì…?ï¿½ë‹¤.
        if (mainCamera == null || tileMapManager == null || tileMapManager.cityTilemap == null)
            return Vector3Int.zero;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        return tileMapManager.cityTilemap.WorldToCell(mouseWorld);
    }
}
