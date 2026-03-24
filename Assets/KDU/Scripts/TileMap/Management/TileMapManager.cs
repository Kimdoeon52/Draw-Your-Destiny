using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 타일맵 관리 싱글톤
public class TileMapManager : Singleton<TileMapManager>
{
    [Header("지형 레이어 (Terrain Layers)")]
    public Tilemap groundTilemap;       // 평지 (Plain, 건설 가능)
    public Tilemap farmlandTilemap;     // 농경지 (Farmland, 농장 전용)
    public Tilemap riverTilemap;        // 강 (River, 이동/건설 불가)
    public Tilemap goldMineTilemap;     // 금광 (Resource)

    [Header("게임플레이 레이어")]
    public Tilemap cityTilemap;         // 도시 위치 (일반 건물 배치 불가)
    public Tilemap buildingTilemap;     // 건물 레이어 (코드 미사용, 레이어 순서용)

    [Header("오버레이 레이어")]
    public Tilemap fogTilemap;          // 안개 전쟁 오버레이
    public Tilemap territoryTilemap;    // 영토 오버레이
    private Tile territoryTile;

    [Header("Building Management")]
    private Dictionary<Vector3Int, BuildingData> buildingMap = new Dictionary<Vector3Int, BuildingData>();
    private Dictionary<Vector3Int, GameObject> buildingObjects = new Dictionary<Vector3Int, GameObject>();
    private Transform buildingContainer;

    // BuildingInstance 추적
    private List<BuildingInstance> allBuildings = new List<BuildingInstance>();
    private Dictionary<Vector3Int, BuildingInstance> buildingInstanceMap = new Dictionary<Vector3Int, BuildingInstance>();

    // 타일별 데이터 (소유권, 안개 등)
    private Dictionary<Vector3Int, TileData> tileDataMap = new Dictionary<Vector3Int, TileData>();

    // civID별 영토 색상: 0=플레이어(파랑), 1=AI1(빨강), 2=AI2(초록), 3=AI3(노랑)
    private static readonly Color[] CivColors =
    {
        new Color(0.2f, 0.5f, 1f,   0.35f),
        new Color(1f,   0.2f, 0.2f, 0.35f),
        new Color(0.2f, 1f,   0.2f, 0.35f),
        new Color(1f,   0.8f, 0f,   0.35f),
    };

    // 하위 호환성
    public Tilemap tilemap => groundTilemap;

    protected override void Awake()
    {
        base.Awake();

        GameObject container = new GameObject("Buildings");
        container.transform.SetParent(transform);
        buildingContainer = container.transform;

        territoryTile = ScriptableObject.CreateInstance<Tile>();
        territoryTile.sprite = FogManager.CreateSolidSprite();
        territoryTile.color = Color.white;

        InitializeTileDataMap();
    }

    // 모든 지형 레이어를 수집 후 TileData 초기화
    private void InitializeTileDataMap()
    {
        HashSet<Vector3Int> allPositions = new HashSet<Vector3Int>();

        CollectPositions(groundTilemap,    allPositions);
        CollectPositions(farmlandTilemap,  allPositions);
        CollectPositions(riverTilemap,     allPositions);
        CollectPositions(goldMineTilemap,  allPositions);
        CollectPositions(cityTilemap,      allPositions);

        foreach (Vector3Int pos in allPositions)
            tileDataMap[pos] = new TileData(GetTileType(pos), FogState.Hidden, -1);
    }

    private void CollectPositions(Tilemap target, HashSet<Vector3Int> result)
    {
        if (target == null) return;

        BoundsInt bounds = target.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (target.HasTile(pos))
                result.Add(pos);
        }
    }

    // 특정 타일맵의 타일 제거 + tileDataMap 동기화
    public void EraseTile(Tilemap target, Vector3Int pos)
    {
        if (target == null) return;
        target.SetTile(pos, null);
        RefreshTileData(pos);
    }

    // 타일 제거 후 tileDataMap 재평가
    private void RefreshTileData(Vector3Int pos)
    {
        bool exists = (groundTilemap   != null && groundTilemap.HasTile(pos))
                   || (farmlandTilemap != null && farmlandTilemap.HasTile(pos))
                   || (riverTilemap    != null && riverTilemap.HasTile(pos))
                   || (goldMineTilemap != null && goldMineTilemap.HasTile(pos))
                   || (cityTilemap     != null && cityTilemap.HasTile(pos));

        if (exists)
        {
            if (tileDataMap.ContainsKey(pos))
                tileDataMap[pos].type = GetTileType(pos);
            else
                tileDataMap[pos] = new TileData(GetTileType(pos), FogState.Hidden, -1);
        }
        else
        {
            tileDataMap.Remove(pos);
        }
    }

    // FogManager에서 전체 타일 순회 시 사용
    public IEnumerable<Vector3Int> GetAllTilePositions()
    {
        return tileDataMap.Keys;
    }

    // ── 타일 타입 조회 (우선순위: 강 > 금광 > 농경지 > 도시 > 평지) ──
    public TileType GetTileType(Vector3Int pos)
    {
        if (riverTilemap    != null && riverTilemap.HasTile(pos))    return TileType.River;
        if (goldMineTilemap != null && goldMineTilemap.HasTile(pos)) return TileType.Resource;
        if (farmlandTilemap != null && farmlandTilemap.HasTile(pos)) return TileType.Farmland;
        if (cityTilemap     != null && cityTilemap.HasTile(pos))     return TileType.City;
        if (groundTilemap   != null && groundTilemap.HasTile(pos))   return TileType.Plain;
        return TileType.River; // 타일 없음 = 이동 불가
    }

    // ── 위치 유효성 확인 ──────────────────────────────────────────
    public bool IsValidPosition(Vector3Int pos)
    {
        return tileDataMap.ContainsKey(pos);
    }

    // ── 소유권 조회 ───────────────────────────────────────────────
    public int GetOwner(Vector3Int pos)
    {
        return tileDataMap.TryGetValue(pos, out TileData data) ? data.ownerCivID : -1;
    }

    // ── 단일 타일 소유권 설정 ────────────────────────────────────
    public void SetOwner(Vector3Int pos, int civID)
    {
        if (!tileDataMap.ContainsKey(pos)) return;
        tileDataMap[pos].ownerCivID = civID;
        RefreshTerritoryVisual(pos, civID);
    }

    // ── BFS로 반경 내 타일 일괄 점령 ────────────────────────────
    public void ClaimTerritory(Vector3Int center, int civID, int radius)
    {
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        queue.Enqueue(center);
        visited.Add(center);

        Vector3Int[] dirs = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };

        while (queue.Count > 0)
        {
            Vector3Int cur = queue.Dequeue();
            int dist = Mathf.Abs(cur.x - center.x) + Mathf.Abs(cur.y - center.y);
            if (dist > radius) continue;

            SetOwner(cur, civID);

            foreach (Vector3Int dir in dirs)
            {
                Vector3Int next = cur + dir;
                if (!visited.Contains(next) && tileDataMap.ContainsKey(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }
    }

    // ── 문명 영토 1칸 확장 ──────────────────────────────────────
    public void ExpandTerritory(int civID)
    {
        List<Vector3Int> toAdd = new List<Vector3Int>();
        Vector3Int[] dirs = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };

        foreach (KeyValuePair<Vector3Int, TileData> pair in tileDataMap)
        {
            if (pair.Value.ownerCivID != civID) continue;

            foreach (Vector3Int dir in dirs)
            {
                Vector3Int neighbor = pair.Key + dir;
                if (tileDataMap.TryGetValue(neighbor, out TileData neighborData) && neighborData.ownerCivID == -1)
                    toAdd.Add(neighbor);
            }
        }

        foreach (Vector3Int pos in toAdd)
            SetOwner(pos, civID);
    }

    // ── 문명 영토 전체 이전 (도시 점령 시) ──────────────────────
    public void TransferTerritory(int fromCivID, int toCivID)
    {
        List<Vector3Int> targets = new List<Vector3Int>();

        foreach (KeyValuePair<Vector3Int, TileData> pair in tileDataMap)
        {
            if (pair.Value.ownerCivID == fromCivID)
                targets.Add(pair.Key);
        }

        foreach (Vector3Int pos in targets)
            SetOwner(pos, toCivID);
    }

    // ── 영토 오버레이 색상 갱신 ─────────────────────────────────
    private void RefreshTerritoryVisual(Vector3Int pos, int civID)
    {
        if (territoryTilemap == null || territoryTile == null) return;

        if (civID == -1)
        {
            territoryTilemap.SetTile(pos, null);
            return;
        }

        territoryTilemap.SetTile(pos, territoryTile);
        territoryTilemap.SetTileFlags(pos, TileFlags.None);
        territoryTilemap.SetColor(pos, CivColors[civID]);
    }

    // ── 건물 조회 ────────────────────────────────────────────────
    public BuildingInstance GetBuildingAt(Vector3Int pos)
    {
        return buildingInstanceMap.TryGetValue(pos, out BuildingInstance b) ? b : null;
    }

    public List<BuildingInstance> GetAllBuildings()
    {
        return allBuildings;
    }

    // ── 건물 배치 가능 확인 ──────────────────────────────────────
    public bool CanPlace(Vector3Int clickPos, BuildingData building)
    {
        Vector3Int origin = GetOrigin(clickPos, building);

        for (int x = 0; x < building.width; x++)
        {
            for (int y = 0; y < building.height; y++)
            {
                Vector3Int checkPos = origin + new Vector3Int(x, y, 0);

                if (!IsValidPosition(checkPos)) return false;

                if (riverTilemap    != null && riverTilemap.HasTile(checkPos))    return false;
                if (cityTilemap     != null && cityTilemap.HasTile(checkPos))     return false;
                if (buildingMap.ContainsKey(checkPos)) return false;

                TileType tileType = GetTileType(checkPos);
                bool allowed = false;
                foreach (TileType t in building.allowedTiles)
                {
                    if (tileType == t) { allowed = true; break; }
                }
                if (!allowed) return false;
            }
        }

        return true;
    }

    // ── 건물 배치 ───────────────────────────────────────────────
    public void PlaceBuilding(Vector3Int clickPos, BuildingData building, int civID = 0)
    {
        if (!CanPlace(clickPos, building)) return;

        Vector3Int origin = GetOrigin(clickPos, building);
        List<Vector3Int> footprint = new List<Vector3Int>();

        for (int x = 0; x < building.width; x++)
        {
            for (int y = 0; y < building.height; y++)
            {
                Vector3Int pos = origin + new Vector3Int(x, y, 0);
                buildingMap[pos] = building;
                footprint.Add(pos);
            }
        }

        GameObject visual = CreateBuildingVisual(origin, building);

        BuildingInstance instance = new BuildingInstance
        {
            data        = building,
            origin      = origin,
            footprint   = footprint,
            ownerCivID  = civID,
            wasEverSeen = (civID == 0), // 내 건물은 항상 표시
            visual      = visual
        };

        allBuildings.Add(instance);
        foreach (Vector3Int pos in footprint)
            buildingInstanceMap[pos] = instance;

        FogManager.Instance?.OnBuildingPlaced(instance);

        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.SpendGold(building.goldCost);
            gameManager.IncreasePopulationCap(building.populationCapBonus);
        }
    }

    // ── 건물 시각화 생성 ─────────────────────────────────────────
    private GameObject CreateBuildingVisual(Vector3Int origin, BuildingData building)
    {
        GameObject buildingObj = new GameObject($"{building.buildingName}_{origin.x}_{origin.y}");
        buildingObj.transform.SetParent(buildingContainer);

        SpriteRenderer spriteRenderer = buildingObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = building.sprite;
        spriteRenderer.sortingOrder = 1;

        float centerX = origin.x + (building.width - 1) * 0.5f;
        float centerY = origin.y + (building.height - 1) * 0.5f;
        Vector3 worldPos = groundTilemap.CellToWorld(new Vector3Int(Mathf.RoundToInt(centerX), Mathf.RoundToInt(centerY), 0));

        buildingObj.transform.position = worldPos + groundTilemap.cellSize * 0.5f;
        buildingObj.transform.localScale = new Vector3(building.width, building.height, 1);

        buildingObjects[origin] = buildingObj;
        return buildingObj;
    }

    // ── 건물 제거 ────────────────────────────────────────────────
    public void RemoveBuilding(Vector3Int position)
    {
        if (!buildingMap.ContainsKey(position)) return;

        // BuildingInstance 정리
        if (buildingInstanceMap.TryGetValue(position, out BuildingInstance instance))
        {
            FogManager.Instance?.OnBuildingDestroyed(instance);
            allBuildings.Remove(instance);
            foreach (Vector3Int pos in instance.footprint)
                buildingInstanceMap.Remove(pos);
        }

        // buildingMap 정리 (footprint 전체)
        if (instance != null)
        {
            foreach (Vector3Int pos in instance.footprint)
                buildingMap.Remove(pos);
        }
        else
        {
            buildingMap.Remove(position);
        }

        // 시각화 제거
        if (buildingObjects.TryGetValue(position, out GameObject obj))
        {
            Destroy(obj);
            buildingObjects.Remove(position);
        }
        // origin이 다른 경우도 처리
        else if (instance != null && buildingObjects.TryGetValue(instance.origin, out GameObject originObj))
        {
            Destroy(originObj);
            buildingObjects.Remove(instance.origin);
        }
    }

    // ── 건물 기준점 계산 (StarCraft 방식) ────────────────────────
    public Vector3Int GetOrigin(Vector3Int clickPos, BuildingData building)
    {
        int offsetX = building.width  % 2 == 0 ? building.width  / 2 - 1 : building.width  / 2;
        int offsetY = building.height % 2 == 0 ? building.height / 2 - 1 : building.height / 2;
        return clickPos - new Vector3Int(offsetX, offsetY, 0);
    }

    // ── 건물 점유 타일 목록 ──────────────────────────────────────
    public List<Vector3Int> GetBuildingFootprint(Vector3Int clickPos, BuildingData building)
    {
        List<Vector3Int> positions = new List<Vector3Int>();
        Vector3Int origin = GetOrigin(clickPos, building);

        for (int x = 0; x < building.width; x++)
            for (int y = 0; y < building.height; y++)
                positions.Add(origin + new Vector3Int(x, y, 0));

        return positions;
    }
}
