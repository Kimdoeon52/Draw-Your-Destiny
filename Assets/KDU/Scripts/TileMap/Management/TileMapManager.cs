using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// ============================================================
// TileMapManager — 타일맵 전체 관리 싱글톤
//
// 담당 범위:
//   1. 타일 데이터 (tileDataMap) — 모든 타일의 타입/소유권을 런타임에 관리
//   2. 건물 배치/제거 — PlaceBuilding, RemoveBuilding
//   3. 영토 소유권 — ClaimTerritory, ExpandTerritory, TransferTerritory
//   4. Outpost 확장 — ExpandOutpostArea (City/Farmland 타일 동적 생성)
//
// 주요 데이터 구조:
//   tileDataMap         : Vector3Int → TileData  (타일 상태, 소유권)
//   buildingMap         : Vector3Int → BuildingData  (해당 위치 건물 타입, footprint 전체)
//   buildingInstanceMap : Vector3Int → BuildingInstance  (런타임 건물 인스턴스, footprint 전체)
//   allBuildings        : BuildingInstance 전체 리스트
//
// 씬의 Tilemap 레이어 참조를 Inspector에서 연결해야 한다.
// ============================================================
public class TileMapManager : Singleton<TileMapManager>
{
    [Header("지형 레이어 — Inspector에서 씬 Tilemap 연결 필요")]
    public Tilemap groundTilemap;       // Tilemap_Ground   — Plain 지형
    public Tilemap farmlandTilemap;     // Tilemap_Farmland — Farmland 지형 (Farm 전용)
    public Tilemap riverTilemap;        // Tilemap_River    — 이동/건설 불가
    public Tilemap goldMineTilemap;     // Tilemap_Gold     — 금광 자원

    [Header("게임플레이 레이어")]
    public Tilemap cityTilemap;         // Tilemap_City     — City 타일 (일반 건물 배치 가능)
    public Tilemap buildingTilemap;     // 코드 미사용, 레이어 렌더링 순서 확보용

    [Header("오버레이 레이어")]
    public Tilemap fogTilemap;          // Tilemap_Fog      — 안개 오버레이 (FogManager 제어)
    public Tilemap territoryTilemap;    // Tilemap_Territory — 영토 색상 오버레이
    private Tile territoryTile;         // 코드에서 생성하는 단색 영토 타일 (1×1 흰색)

    [Header("소규모 영지 타일 에셋 — Outpost 건설 시 동적 타일 생성에 사용")]
    public Tile cityTileAsset;          // Outpost 영역(8×8)에 깔릴 City 타일 에셋
    public Tile farmlandTileAsset;      // Outpost 테두리(2칸)에 깔릴 Farmland 타일 에셋

    [Header("소규모 영지 영주성 — LordCastle 시스템 연동")]
    [Tooltip("LordCastle 컴포넌트가 붙은 프리팹. 소규모 영지 배치 시 8×8 중앙에 자동 생성됨.")]
    public GameObject outpostManorPrefab; // LordCastle 컴포넌트 포함 프리팹 (문명 공용)
    [Tooltip("문명별 영주성 스프라이트. 인덱스 = civID (0=플레이어, 1=AI-A, 2=AI-B, 3=AI-C)")]
    public Sprite[] outpostManorSprites = new Sprite[4]; // civID별 스프라이트
    public int outpostManorHP = 50;       // 영주성 초기 체력

    [Header("농장 인접 스프라이트 (18종)")]
    [Tooltip(
        "가로 줄 계열 (N·S 없음):\n" +
        "  0: 왼쪽 끝   (E만 연결, 풀이 왼쪽)\n" +
        "  1: 오른쪽 끝  (W만 연결, 풀이 오른쪽)\n" +
        "  2: 가운데    (E+W 연결, 세로 줄)\n" +
        "세로 줄 계열 (E·W 없음):\n" +
        "  3: 위쪽 끝   (S만 연결, 풀이 위)\n" +
        "  4: 아래쪽 끝  (N만 연결, 풀이 아래)\n" +
        "  5: 가운데    (N+S 연결, 가로 줄)\n" +
        "NE 구석: 6=바깥(대각없음) / 7=바깥(대각있음) / 8=안쪽꺾임\n" +
        "NW 구석: 9=바깥(대각없음) / 10=바깥(대각있음) / 11=안쪽꺾임\n" +
        "SE 구석: 12=바깥(대각없음) / 13=바깥(대각있음) / 14=안쪽꺾임\n" +
        "SW 구석: 15=바깥(대각없음) / 16=바깥(대각있음) / 17=안쪽꺾임\n" +
        "해당 없음(단독·복잡) → BuildingData.sprite 사용")]
    public Sprite[] farmSprites = new Sprite[18];

    // ── 건물 관련 컬렉션 ──────────────────────────────────────
    // buildingMap: footprint의 모든 타일 좌표를 키로 BuildingData를 저장
    //              CanPlace에서 충돌 검사 시 사용
    private Dictionary<Vector3Int, BuildingData> buildingMap = new Dictionary<Vector3Int, BuildingData>();

    // buildingObjects: origin 좌표 → 화면에 표시되는 GameObject (SpriteRenderer)
    private Dictionary<Vector3Int, GameObject> buildingObjects = new Dictionary<Vector3Int, GameObject>();

    // 건물 오브젝트를 묶는 빈 부모 GameObject (Hierarchy 정리용)
    private Transform buildingContainer;

    // allBuildings: 씬 전체 BuildingInstance 리스트 (순회, 턴 처리에 사용)
    private List<BuildingInstance> allBuildings = new List<BuildingInstance>();

    // buildingInstanceMap: footprint 모든 타일 → BuildingInstance
    //                      GetBuildingAt()로 특정 타일의 건물을 O(1) 조회
    private Dictionary<Vector3Int, BuildingInstance> buildingInstanceMap = new Dictionary<Vector3Int, BuildingInstance>();

    // tileDataMap: 맵의 모든 타일 좌표 → TileData (타입, 소유권)
    //             씬 시작 시 지형 Tilemap을 순회해 자동 초기화됨
    private Dictionary<Vector3Int, TileData> tileDataMap = new Dictionary<Vector3Int, TileData>();

    // civID 0~3에 대응하는 영토 오버레이 색상 (반투명)
    // 0=플레이어(파랑), 1=AI1(빨강), 2=AI2(초록), 3=AI3(노랑)
    private static readonly Color[] CivColors =
    {
        new Color(0.2f, 0.5f, 1f,   0.35f),
        new Color(1f,   0.2f, 0.2f, 0.35f),
        new Color(0.2f, 1f,   0.2f, 0.35f),
        new Color(1f,   0.8f, 0f,   0.35f),
    };

    // groundTilemap 하위 호환성 alias (이전 코드에서 tilemap으로 접근하던 곳 대응)
    public Tilemap tilemap => groundTilemap;

    protected override void Awake()
    {
        base.Awake();

        // 씬에 Buildings 빈 오브젝트를 만들어 배치된 건물 GameObject를 그 아래로 넣음
        GameObject container = new GameObject("Buildings");
        container.transform.SetParent(transform);
        buildingContainer = container.transform;

        // 영토 오버레이에 쓸 단색 흰 타일을 코드로 생성 (에셋 불필요)
        territoryTile = ScriptableObject.CreateInstance<Tile>();
        territoryTile.sprite = FogManager.CreateSolidSprite();
        territoryTile.color = Color.white;

        InitializeTileDataMap();
    }

    // 씬의 모든 지형 Tilemap을 순회해 tileDataMap을 초기화
    // 이후 GetAllTilePositions()로 FogManager가 전체 타일에 안개를 깐다
    private void InitializeTileDataMap()
    {
        HashSet<Vector3Int> allPositions = new HashSet<Vector3Int>();

        CollectPositions(groundTilemap,   allPositions);
        CollectPositions(farmlandTilemap, allPositions);
        CollectPositions(riverTilemap,    allPositions);
        CollectPositions(goldMineTilemap, allPositions);
        CollectPositions(cityTilemap,     allPositions);

        foreach (Vector3Int pos in allPositions)
            tileDataMap[pos] = new TileData(GetTileType(pos), FogState.Explored, -1);
    }

    // Tilemap에서 타일이 있는 좌표만 result에 추가 (null 타일맵 안전 처리)
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

    // 특정 Tilemap의 타일을 제거하고 tileDataMap을 최신 상태로 갱신
    public void EraseTile(Tilemap target, Vector3Int pos)
    {
        if (target == null) return;
        target.SetTile(pos, null);
        RefreshTileData(pos);
    }

    // 타일 제거 후 해당 좌표의 tileDataMap 재평가
    // 모든 지형 레이어에서 타일이 사라졌으면 tileDataMap에서도 제거
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
                tileDataMap[pos] = new TileData(GetTileType(pos), FogState.Explored, -1);
        }
        else
        {
            tileDataMap.Remove(pos);
        }
    }

    // FogManager.InitializeFog()에서 전체 타일에 안개를 깔 때 사용
    public IEnumerable<Vector3Int> GetAllTilePositions()
    {
        return tileDataMap.Keys;
    }

    // ── 타일 타입 조회 ────────────────────────────────────────
    // 우선순위: River > Resource > Farmland > City > Plain
    // 여러 Tilemap 레이어가 겹쳐 있어도 가장 높은 우선순위 타입 하나만 반환
    // 타일이 없는 위치는 River(이동 불가)로 처리
    public TileType GetTileType(Vector3Int pos)
    {
        if (riverTilemap    != null && riverTilemap.HasTile(pos))    return TileType.River;
        if (goldMineTilemap != null && goldMineTilemap.HasTile(pos)) return TileType.Resource;
        if (farmlandTilemap != null && farmlandTilemap.HasTile(pos)) return TileType.Farmland;
        if (cityTilemap     != null && cityTilemap.HasTile(pos))     return TileType.City;
        if (groundTilemap   != null && groundTilemap.HasTile(pos))   return TileType.Plain;
        return TileType.River; // 타일 없음 = 이동/건설 불가
    }

    // ── 위치 유효성 확인 ──────────────────────────────────────────
    // tileDataMap에 등록된 좌표만 유효 (맵 밖/타일 없는 곳은 false)
    public bool IsValidPosition(Vector3Int pos)
    {
        return tileDataMap.ContainsKey(pos);
    }

    // ── 소유권 조회 ───────────────────────────────────────────────
    // 반환값: 0~3(문명ID) 또는 -1(미점령)
    public int GetOwner(Vector3Int pos)
    {
        return tileDataMap.TryGetValue(pos, out TileData data) ? data.ownerCivID : -1;
    }

    // ── 단일 타일 소유권 설정 ────────────────────────────────────
    // civID = -1이면 영토 오버레이 타일 제거(미점령 표시)
    public void SetOwner(Vector3Int pos, int civID)
    {
        if (!tileDataMap.ContainsKey(pos)) return;
        tileDataMap[pos].ownerCivID = civID;
        RefreshTerritoryVisual(pos, civID);
    }

    // ── BFS로 center 기준 반경 내 타일을 civID로 일괄 점령 ──────
    // 도시 건설 시 초기 영토 확보, Outpost 설치 후 영토 등록 등에 사용
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

    // ── civID 영토의 모든 경계 타일에서 미점령 인접 타일 1칸씩 확장 ──
    // 매 턴 자동 영토 확장 처리에 사용 (미구현)
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

    // ── 도시 점령 시 패배 문명의 모든 영토를 승리 문명으로 이전 ──
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
    // SetOwner() 호출 시 자동으로 불림 — 외부에서 직접 호출 불필요
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

    // ── 특정 타일에 있는 BuildingInstance 반환 (없으면 null) ────
    // FogManager에서 건물 렌더링 시 사용
    public BuildingInstance GetBuildingAt(Vector3Int pos)
    {
        return buildingInstanceMap.TryGetValue(pos, out BuildingInstance b) ? b : null;
    }

    // 씬 전체 건물 리스트 반환 (턴 처리 시 goldPerTurn 등 일괄 적용에 사용 예정)
    public List<BuildingInstance> GetAllBuildings()
    {
        return allBuildings;
    }

    // 외부에서 직접 생성한 BuildingInstance를 등록
    // 영주성처럼 BuildingData 없이 코드로 생성하는 특수 건물에 사용
    // FogManager 연동(OnBuildingPlaced)은 호출부에서 직접 처리
    public void RegisterBuildingInstance(BuildingInstance instance)
    {
        if (instance == null) return;
        allBuildings.Add(instance);
        foreach (Vector3Int pos in instance.footprint)
        {
            buildingMap[pos] = instance.data; // data가 null이면 null 등록 (충돌 방지용)
            buildingInstanceMap[pos] = instance;
        }
        BuildingRegistry.Instance?.Register(instance);
    }

    // ── 건물 배치 가능 여부 확인 ─────────────────────────────────
    // clickPos 기준으로 origin을 계산한 뒤 footprint 전체를 검사
    // 조건: 맵 안 + 강 없음 + 건물 없음 + allowedTiles 타입 일치
    public bool CanPlace(Vector3Int clickPos, BuildingData building)
    {
        Vector3Int origin = GetOrigin(clickPos, building);

        // 버려진 영지 위 소규모 영지 배치 특수 케이스
        // 점령 후 잔해 origin과 정확히 일치하면 allowedTiles 체크를 건너뜀
        bool isRuinsPlacement = false;
        if (building.buildingType == BuildingType.Outpost)
        {
            var ruins = AbandonedTerritoryManager.Instance;
            if (ruins != null && ruins.CanPlaceOutpostHere(origin, 0))
                isRuinsPlacement = true;
        }

        for (int x = 0; x < building.width; x++)
        {
            for (int y = 0; y < building.height; y++)
            {
                Vector3Int checkPos = origin + new Vector3Int(x, y, 0);

                if (!IsValidPosition(checkPos)) return false;
                if (riverTilemap != null && riverTilemap.HasTile(checkPos)) return false;
                if (buildingMap.ContainsKey(checkPos)) return false;

                // 잔해 위 배치는 타일 타입 무관하게 허용 (Plain이 아닌 잔해 위에도 배치 가능)
                if (!isRuinsPlacement)
                {
                    TileType tileType = GetTileType(checkPos);
                    bool allowed = false;
                    foreach (TileType t in building.allowedTiles)
                    {
                        if (tileType == t) { allowed = true; break; }
                    }
                    if (!allowed) return false;
                }
            }
        }

        // [Outpost 전용 추가 검사]
        // 소규모 영지는 설치 시 주변에 8×8 City 영역 + 3칸 Farmland 테두리가 생성되므로
        // 1×1 클릭 타일만 검사하면 부족함. 아래 범위를 추가로 검사한다.
        //
        //   ┌──────────────────────────────┐  ← dy = +7
        //   │  [Farmland 테두리 3칸]        │
        //   │  ┌──────────────────────┐    │  ← dy = +4
        //   │  │   [8×8 City 영역]   │    │
        //   │  │        [X]          │    │  ← origin (클릭 지점)
        //   │  │   8×8 내부 전체     │    │
        //   │  └──────────────────────┘    │  ← dy = -3
        //   │  [Farmland 테두리 3칸]        │
        //   └──────────────────────────────┘  ← dy = -6
        //   dx: -6 ~ +7  (총 14칸)
        //
        // 8×8 내부(dx -3~+4, dy -3~+4): 강 또는 기존 건물 있으면 배치 불가
        // Farmland 테두리(그 외):        기존 건물 있으면 배치 불가 (다른 영지 침범 방지)
        if (building.buildingType == BuildingType.Outpost && !isRuinsPlacement)
        {
            for (int dx = -6; dx <= 7; dx++)
            for (int dy = -6; dy <= 7; dy++)
            {
                Vector3Int checkPos = origin + new Vector3Int(dx, dy, 0);
                bool isInCityZone = dx >= -3 && dx <= 4 && dy >= -3 && dy <= 4;

                if (isInCityZone)
                {
                    // 8×8 내부: 강 또는 기존 건물 있으면 불가
                    if (riverTilemap != null && riverTilemap.HasTile(checkPos)) return false;
                    if (buildingMap.ContainsKey(checkPos)) return false;
                }
                else
                {
                    // Farmland 테두리: 기존 건물과 겹치면 불가 (다른 영지 침범 방지)
                    if (buildingMap.ContainsKey(checkPos)) return false;
                }
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
            data = building,
            origin = origin,
            footprint = footprint,
            ownerCivID = civID,
            wasEverSeen = (civID == 0), // 내 건물은 항상 표시
            visual = visual
        };

        allBuildings.Add(instance);
        foreach (Vector3Int pos in footprint)
            buildingInstanceMap[pos] = instance;

        // [Outpost 전용 처리 — 직접 호출 불필요, PlaceBuilding이 자동으로 수행]
        // 소규모 영지 카드를 사용하면 아래 순서로 자동 실행됨:
        //   1. ExpandOutpostArea  : origin 기준 8×8 City + 2칸 Farmland 타일 생성
        //   2. FogManager         : 8×8 영역 안개 영구 해제
        //   3. AbandonedTerritory : 버려진 영지 위 배치라면 완전 점령 처리
        //   4. PlaceOutpostManor  : origin 자리에 2×2 영주성 자동 등록 (비용 없음)
        if (building.buildingType == BuildingType.Outpost)
        {
            // 버려진 영지 위 배치 여부 확인
            var ruins = AbandonedTerritoryManager.Instance;
            bool isRuinsPlacement = ruins != null && ruins.CanPlaceOutpostHere(origin, civID);

            ExpandOutpostArea(origin);
            FogManager.Instance?.OnOutpostBuilt(new Vector3Int(origin.x - 3, origin.y - 3, 0), 8);

            // 잔해 위라면 버려진 영지 완전 점령 처리 (외벽 설치 등)
            if (isRuinsPlacement)
                ruins.OnRuinsOutpostBuilt(civID);

            // Outpost 1×1 자리에 2×2 영주성 자동 배치 (비용 없음)
            PlaceOutpostManor(origin, civID);
        }

        FogManager.Instance?.OnBuildingPlaced(instance);
        BuildingRegistry.Instance?.Register(instance);

        //// 농장이면 자신 + 인접 농장 스프라이트 갱신
        //if (building.buildingType == BuildingType.Farm)
        //    RefreshFarmAndNeighbors(origin);

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
        spriteRenderer.sortingOrder = 100; //건물 오더레이어

        buildingObj.transform.position = GetBuildingWorldCenter(origin, building);
        buildingObj.transform.localScale = new Vector3(building.width, building.height, 1);

        buildingObjects[origin] = buildingObj;
        return buildingObj;
    }

    // ── 건물 제거 ────────────────────────────────────────────────
    public void RemoveBuilding(Vector3Int position)
    {
        if (!buildingMap.ContainsKey(position)) return;

        //// 농장이면 제거 전에 origin 기록 (제거 후 인접 농장 갱신에 사용)
        //Vector3Int? farmOriginToRefresh = null;
        //if (buildingInstanceMap.TryGetValue(position, out BuildingInstance checkFarm)
        //    && checkFarm?.data?.buildingType == BuildingType.Farm)
        //    farmOriginToRefresh = checkFarm.origin;

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
        BuildingRegistry.Instance?.Remove(instance);

        //// 농장 제거 후 인접 농장 스프라이트 갱신 (자신은 이미 map에서 제거됨)
        //if (farmOriginToRefresh.HasValue)
        //    RefreshFarmAndNeighbors(farmOriginToRefresh.Value);
    }

    // ── 건물 기준점 계산 (StarCraft 방식) ────────────────────────
    public Vector3Int GetOrigin(Vector3Int clickPos, BuildingData building)
    {
        int offsetX = building.width % 2 == 0 ? building.width / 2 - 1 : building.width / 2;
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

    // --------건들ㄴ--------------------------------
    // -------------------------------------------------
    // 이 타일이 "이동 가능한 영지 타일"인지 검사하는 함수
    // -------------------------------------------------
    public bool IsWalkableTerritory(Vector3Int pos)
    {
        // -------------------------------------------------
        // 1. tileDataMap 안에 이 좌표의 타일 데이터가 있는지 검사
        // -------------------------------------------------
        // TryGetValue:
        // - pos 키가 존재하면 true
        // - data 변수에 해당 타일 정보가 들어감
        // - 없으면 false
        //
        // 여기서 false라는 뜻은:
        // "그 좌표에는 관리 중인 타일 데이터가 없다"
        // 즉, 없는 타일 / 빈 칸 / 맵 바깥일 가능성이 크다
        if (!tileDataMap.TryGetValue(pos, out TileData data))
            return false;


        // -------------------------------------------------
        // 2. ownerCivID 가 -1이 아니면 영지로 점령된 타일
        // -------------------------------------------------
        // 지금 규칙:
        // -1  = 미점령
        // 그 외 = 누군가의 영지
        //
        // 따라서 점령된 영지면 true,
        // 미점령이면 false 반환
        return data.ownerCivID != -1;
    }


    // -------------------------------------------------
    // 현재 타일에서 이동 가능한 주변 타일들을 구하는 함수
    // -------------------------------------------------
    public List<Vector3Int> GetNeighbors(Vector3Int current)
    {
        // -------------------------------------------------
        // 1. 결과를 담을 리스트 생성
        // -------------------------------------------------
        // 여기에 "현재 칸에서 실제로 이동 가능한 이웃 칸"들을 넣을 것
        List<Vector3Int> neighbors = new List<Vector3Int>();


        // -------------------------------------------------
        // 2. 현재 타일 기준으로 검사할 방향 정의
        // -------------------------------------------------
        // 상하좌우 + 대각선 4개
        // 총 8방향 탐색
        //
        // 예를 들어 current가 (5,5,0) 이라면
        // (1,0,0)  -> (6,5,0)  오른쪽
        // (-1,0,0) -> (4,5,0)  왼쪽
        // (0,1,0)  -> (5,6,0)  위
        // (0,-1,0) -> (5,4,0)  아래
        // (1,1,0)  -> (6,6,0)  오른쪽 위
        // ...
        Vector3Int[] directions =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(-1, -1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, 1, 0)
        };


        // -------------------------------------------------
        // 3. 각 방향을 하나씩 검사
        // -------------------------------------------------
        foreach (var dir in directions)
        {
            // -------------------------------------------------
            // 4. 현재 타일 + 방향 = 다음 후보 타일 계산
            // -------------------------------------------------
            // 예:
            // current = (5,5,0)
            // dir     = (1,0,0)
            // next    = (6,5,0)
            Vector3Int next = current + dir;


            // -------------------------------------------------
            // 5. 그 후보 타일이 실제로 이동 가능한지 검사
            // -------------------------------------------------
            // IsWalkableTerritory(next)가 true면
            // "점령된 영지 타일"이라는 뜻
            if (IsWalkableTerritory(next))
                neighbors.Add(next); // 이동 가능하므로 리스트에 추가
        }


        // -------------------------------------------------
        // 6. 최종적으로 이동 가능한 주변 칸 목록 반환
        // -------------------------------------------------
        return neighbors;
    }


    // -------------------------------------------------
    // 타일 좌표를 월드 좌표(타일 중앙)로 바꾸는 함수
    // -------------------------------------------------
    public Vector3 GetCellCenterWorld(Vector3Int pos)
    {
        return groundTilemap.GetCellCenterWorld(pos);
    }

    // 건물 프리뷰와 실제 설치가 항상 같은 좌표를 사용하도록 공용 계산식으로 통일한다.
    public Vector3 GetBuildingWorldCenter(Vector3Int origin, BuildingData building)
    {
        if (groundTilemap == null || building == null)
            return Vector3.zero;

        Vector3 originWorld = groundTilemap.CellToWorld(origin);
        Vector3 cellSize = groundTilemap.cellSize;

        return originWorld + new Vector3(
            building.width * cellSize.x * 0.5f,
            building.height * cellSize.y * 0.5f,
            0f);
    }

    // ── 소규모 영지 확장 ─────────────────────────────────────────
    // origin 기준 8×8 City 타일 생성 + 3칸 Farmland 테두리 생성
    private void ExpandOutpostArea(Vector3Int origin)
    {
        // 8×8 City 영역 (origin 기준 -3 ~ +4)
        for (int dx = -3; dx <= 4; dx++)
        for (int dy = -3; dy <= 4; dy++)
        {
            SetCityTile(origin + new Vector3Int(dx, dy, 0));
        }

        // 3칸 Farmland 테두리 (14×14 외곽 - 8×8 내부)
        for (int dx = -6; dx <= 7; dx++)
        for (int dy = -6; dy <= 7; dy++)
        {
            if (dx >= -3 && dx <= 4 && dy >= -3 && dy <= 4) continue;
            SetFarmlandTile(origin + new Vector3Int(dx, dy, 0));
        }
    }

    // Outpost 1×1 자리를 정리하고 중앙에 LordCastle(2×2) 영주성을 생성
    //
    // [호출 불필요 — PlaceBuilding이 자동으로 처리]
    // LordCastle.tileSize = 2로 8×8 영지 중앙에 정확히 배치됨.
    // buildingMap에는 null로 등록 (CanPlace 충돌 검사용, BuildingData 불필요)
    // BuildingInstance.data = null, visual = LordCastle GameObject (FogManager 연동)
    private void PlaceOutpostManor(Vector3Int origin, int civID)
    {
        if (outpostManorPrefab == null)
        {
            Debug.LogWarning("[TileMapManager] outpostManorPrefab이 연결되지 않았습니다. 인스펙터에서 LordCastle 프리팹을 연결하세요.");
            return;
        }

        // 1×1 Outpost가 점유한 origin 타일 정리
        if (buildingInstanceMap.TryGetValue(origin, out BuildingInstance outpostInst))
        {
            allBuildings.Remove(outpostInst);
            buildingInstanceMap.Remove(origin);
        }
        buildingMap.Remove(origin);

        if (buildingObjects.TryGetValue(origin, out GameObject outpostObj))
        {
            Destroy(outpostObj);
            buildingObjects.Remove(origin);
        }

        // LordCastle 프리팹 생성 및 초기화
        // ExpandOutpostArea가 생성한 8×8 영역의 BoundsInt를 넘겨 2×2 중앙 배치
        GameObject manorObj = Instantiate(outpostManorPrefab, Vector3.zero, Quaternion.identity, buildingContainer);
        manorObj.name = $"OutpostManor_{origin.x}_{origin.y}";

        LordCastle lordCastle = manorObj.GetComponent<LordCastle>();
        if (lordCastle != null)
        {
            lordCastle.tileSize = 2;
            BoundsInt areaBounds = new BoundsInt(origin.x - 3, origin.y - 3, 0, 8, 8, 1);
            Sprite sprite = (outpostManorSprites != null && civID < outpostManorSprites.Length)
                ? outpostManorSprites[civID]
                : null;
            lordCastle.Initialize(sprite, groundTilemap, areaBounds, outpostManorHP);
        }

        // 2×2 footprint를 buildingMap에 null로 등록 (CanPlace 충돌 차단용)
        List<Vector3Int> footprint = new List<Vector3Int>();
        for (int x = 0; x < 2; x++)
        for (int y = 0; y < 2; y++)
        {
            Vector3Int pos = origin + new Vector3Int(x, y, 0);
            buildingMap[pos] = null;
            footprint.Add(pos);
        }

        // BuildingInstance 등록 (data=null, FogManager 연동용)
        BuildingInstance manor = new BuildingInstance
        {
            data        = null,
            origin      = origin,
            footprint   = footprint,
            ownerCivID  = civID,
            wasEverSeen = (civID == 0),
            visual      = manorObj
        };

        allBuildings.Add(manor);
        foreach (Vector3Int pos in footprint)
            buildingInstanceMap[pos] = manor;

        buildingObjects[origin] = manorObj;
        FogManager.Instance?.OnBuildingPlaced(manor);
    }

    // City 타일 1칸 설정 (강 위 / 맵 밖 스킵)
    private void SetCityTile(Vector3Int pos)
    {
        if (cityTileAsset == null) return;
        if (!tileDataMap.ContainsKey(pos)) return;
        if (riverTilemap != null && riverTilemap.HasTile(pos)) return;

        cityTilemap.SetTile(pos, cityTileAsset);
        cityTilemap.SetTileFlags(pos, TileFlags.None);
        tileDataMap[pos].type = TileType.City;
    }

    // Farmland 타일 1칸 설정 (강 / City / 맵 밖 스킵, 중복 배치 방지)
    private void SetFarmlandTile(Vector3Int pos)
    {
        if (farmlandTileAsset == null) return;
        if (!tileDataMap.ContainsKey(pos)) return;
        if (riverTilemap != null && riverTilemap.HasTile(pos)) return;
        if (cityTilemap != null && cityTilemap.HasTile(pos)) return;
        if (farmlandTilemap != null && farmlandTilemap.HasTile(pos)) return;

        farmlandTilemap.SetTile(pos, farmlandTileAsset);
        farmlandTilemap.SetTileFlags(pos, TileFlags.None);
        tileDataMap[pos].type = TileType.Farmland;
    }

    //----------------------------적 전용 건물 설치하는거임 건들 ㄴㄴㄴㄴㄴ-------------------------------------------------
    public bool PlaceBuildingForAI(Vector3Int clickPos, BuildingData building, int civID)
    {
        if (building == null)
        {
            Debug.LogWarning("building이 null임.");
            return false;
        }

        if (!CanPlace(clickPos, building))
        {
            return false;
        }

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
            data = building,
            origin = origin,
            footprint = footprint,
            ownerCivID = civID,
            wasEverSeen = true, // AI 건물은 처음엔 플레이어 시야 기준 미확인 처리 가능 일단 테스트 용으로 true로 했음
            visual = visual
        };

        allBuildings.Add(instance);

        foreach (Vector3Int pos in footprint)
        {
            buildingInstanceMap[pos] = instance;
        }

        // Outpost 특수 처리도 동일하게 유지
        if (building.buildingType == BuildingType.Outpost)
        {
            var ruins = AbandonedTerritoryManager.Instance;
            bool isRuinsPlacement = ruins != null && ruins.CanPlaceOutpostHere(origin, civID);

            ExpandOutpostArea(origin);
            FogManager.Instance?.OnOutpostBuilt(new Vector3Int(origin.x - 3, origin.y - 3, 0), 8);

            if (isRuinsPlacement)
                ruins.OnRuinsOutpostBuilt(civID);

            PlaceOutpostManor(origin, civID);
        }

        FogManager.Instance?.OnBuildingPlaced(instance);
        BuildingRegistry.Instance?.Register(instance);

        //// 농장이면 자신 + 인접 농장 스프라이트 갱신
        //if (building.buildingType == BuildingType.Farm)
        //    RefreshFarmAndNeighbors(origin);

        // 플레이어용 GameManager 자원 처리 없음
        return true;
    }

    // ── 시대 전환 시 자동 업그레이드 ────────────────────────────────
    // GameManager.checkResearch()에서 시대가 바뀔 때 호출
    // isAutoUpgrade=true이고 upgradesTo의 requiredEra가 현재 시대 이하인 건물을 자동 교체
    public void UpgradeBuildingsForEra(Era newEra)
    {
        foreach (BuildingInstance instance in allBuildings)
        {
            if (!instance.data.isAutoUpgrade) continue;
            if (instance.data.upgradesTo == null) continue;
            if (instance.data.upgradesTo.requiredEra > newEra) continue;

            BuildingData next = instance.data.upgradesTo;

            // 데이터 교체
            instance.data = next;

            // buildingMap도 새 데이터로 갱신
            foreach (Vector3Int pos in instance.footprint)
                buildingMap[pos] = next;

            // 스프라이트 교체
            if (instance.visual != null)
            {
                SpriteRenderer sr = instance.visual.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = next.sprite;
            }

            Debug.Log($"[TileMapManager] 건물 업그레이드: {instance.origin} → {next.buildingName}");
        }
    }

    //// ── 농장 8방향 스프라이트 갱신 (18종) ───────────────────────────────
    //// 연결 조건: 해당 방향 바깥에 농경지 타일 OR Farm 건물 중 하나라도 존재
    ////
    //// 직선 4방향: footprint 외곽 행/열 전체를 검사 (하나라도 있으면 연결)
    //// 대각 4방향: footprint 모서리 바깥 타일 1칸만 검사
    ////            단, 양쪽 직선방향이 모두 연결됐을 때만 유효

    //// 해당 위치에 Farm 건물이 있으면 true
    //// (농경지 타일은 Farm 아래 항상 존재하므로 제외 — 건물 기준으로만 연결 판정)
    //private bool IsFarmOrFarmland(Vector3Int pos)
    //{
    //    if (buildingInstanceMap.TryGetValue(pos, out BuildingInstance inst)
    //        && inst?.data?.buildingType == BuildingType.Farm) return true;
    //    return false;
    //}

    //// footprint 경계 바깥 지정 방향 연결 여부
    //// dx/dy: 직선은 (±1,0)(0,±1) / 대각은 (±1,±1)
    //private bool IsConnectedTo(Vector3Int origin, int w, int h, int dx, int dy)
    //{
    //    if (dx != 0 && dy != 0) // 대각선: 모서리 타일 1칸
    //    {
    //        Vector3Int pos = origin + new Vector3Int(dx > 0 ? w : -1, dy > 0 ? h : -1, 0);
    //        return IsFarmOrFarmland(pos);
    //    }
    //    if (dx != 0) // 좌우: 해당 열 전체
    //    {
    //        int x = dx > 0 ? origin.x + w : origin.x - 1;
    //        for (int j = 0; j < h; j++)
    //            if (IsFarmOrFarmland(new Vector3Int(x, origin.y + j, 0))) return true;
    //    }
    //    else // 상하: 해당 행 전체
    //    {
    //        int y = dy > 0 ? origin.y + h : origin.y - 1;
    //        for (int i = 0; i < w; i++)
    //            if (IsFarmOrFarmland(new Vector3Int(origin.x + i, y, 0))) return true;
    //    }
    //    return false;
    //}

    //// 18종 스프라이트 인덱스 분류
    //// 반환 -1 → BuildingData 기본 스프라이트 (단독·해당 없음)
    ////
    ////  가로 줄 계열 (N·S 없음):
    ////   0=왼쪽끝(E만)  1=오른쪽끝(W만)  2=가운데(E+W, 세로줄)
    ////  세로 줄 계열 (E·W 없음):
    ////   3=위쪽끝(S만)  4=아래쪽끝(N만)  5=가운데(N+S, 가로줄)
    ////  구석 계열 (+0=바깥대각없음 / +1=바깥대각있음 / +2=안쪽꺾임):
    ////   NE: 6~8   NW: 9~11   SE: 12~14   SW: 15~17
    //private int GetFarmSpriteIndex(bool N, bool NE, bool E, bool SE, bool S, bool SW, bool W, bool NW)
    //{
    //    bool anyNS = N || S;
    //    bool anyEW = E || W;

    //    // 가로 줄 계열 (N도 S도 없음)
    //    if (!anyNS && anyEW)
    //    {
    //        if ( E && !W) return 0;
    //        if (!E &&  W) return 1;
    //        if ( E &&  W) return 2;
    //    }

    //    // 세로 줄 계열 (E도 W도 없음)
    //    if (!anyEW && anyNS)
    //    {
    //        if ( S && !N) return 3;
    //        if (!S &&  N) return 4;
    //        if ( N &&  S) return 5;
    //    }

    //    // 구석 계열 — 인접 두 직선방향이 모두 연결된 경우
    //    // S 또는 W도 연결됐으면 안쪽 꺾임, 아니면 대각선 유무로 바깥 구석 구분
    //    if (N && E) { if (S || W) return 8;  return NE ? 7 : 6;  }
    //    if (N && W) { if (S || E) return 11; return NW ? 10 : 9; }
    //    if (S && E) { if (N || W) return 14; return SE ? 13 : 12; }
    //    if (S && W) { if (N || E) return 17; return SW ? 16 : 15; }

    //    return -1; // 단독 또는 해당 없음 → 기본 스프라이트
    //}

    //// 특정 농장 인스턴스의 8방향을 검사해 스프라이트 교체
    //private void RefreshFarmSprite(BuildingInstance farm)
    //{
    //    if (farm == null || farm.data?.buildingType != BuildingType.Farm) return;
    //    if (farm.visual == null) return;

    //    Vector3Int o = farm.origin;
    //    int w = farm.data.width;
    //    int h = farm.data.height;

    //    bool N = IsConnectedTo(o, w, h,  0,  1);
    //    bool S = IsConnectedTo(o, w, h,  0, -1);
    //    bool E = IsConnectedTo(o, w, h,  1,  0);
    //    bool W = IsConnectedTo(o, w, h, -1,  0);
    //    // 대각선은 양쪽 직선방향 둘 다 연결됐을 때만 확인
    //    bool NE = N && E && IsConnectedTo(o, w, h,  1,  1);
    //    bool NW = N && W && IsConnectedTo(o, w, h, -1,  1);
    //    bool SE = S && E && IsConnectedTo(o, w, h,  1, -1);
    //    bool SW = S && W && IsConnectedTo(o, w, h, -1, -1);

    //    int idx = GetFarmSpriteIndex(N, NE, E, SE, S, SW, W, NW);

    //    Sprite newSprite = (idx < 0 || farmSprites == null || idx >= farmSprites.Length)
    //        ? farm.data.sprite
    //        : farmSprites[idx] ?? farm.data.sprite;

    //    SpriteRenderer sr = farm.visual.GetComponent<SpriteRenderer>();
    //    if (sr != null) sr.sprite = newSprite;
    //}

    //// 배치/제거된 농장의 자신 + 8방향 인접 농장 모두 갱신
    //// (대각선 연결 여부도 스프라이트에 영향을 주므로 대각 4방향 포함)
    //private void RefreshFarmAndNeighbors(Vector3Int farmOrigin)
    //{
    //    // 자신 갱신 (제거 직후 호출 시에는 map에서 이미 없으므로 무시됨)
    //    if (buildingInstanceMap.TryGetValue(farmOrigin, out BuildingInstance self))
    //        RefreshFarmSprite(self);

    //    // 8방향 인접 Farm 갱신 (직선 4 + 대각 4, 농장 크기 3칸 간격)
    //    Vector3Int[] dirs =
    //    {
    //        new Vector3Int( 3,  0, 0), new Vector3Int(-3,  0, 0),
    //        new Vector3Int( 0,  3, 0), new Vector3Int( 0, -3, 0),
    //        new Vector3Int( 3,  3, 0), new Vector3Int(-3,  3, 0),
    //        new Vector3Int( 3, -3, 0), new Vector3Int(-3, -3, 0),
    //    };
    //    foreach (Vector3Int dir in dirs)
    //    {
    //        if (buildingInstanceMap.TryGetValue(farmOrigin + dir, out BuildingInstance neighbor)
    //            && neighbor?.data?.buildingType == BuildingType.Farm)
    //            RefreshFarmSprite(neighbor);
    //    }
    //}

    //---------------------------금광타일 찾는거임 건들 ㄴㄴㄴㄴㄴㄴ-----------------------
    public List<Vector3Int> GetAllGoldMineCells()
    {
        List<Vector3Int> result = new List<Vector3Int>();

        if (goldMineTilemap == null)
            return result;

        BoundsInt bounds = goldMineTilemap.cellBounds;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (goldMineTilemap.HasTile(pos))
                result.Add(pos);
        }

        return result;
    }
    //--------------------도라지 코드 건들지 마셈 진짜 혼난다^_^------------------
    private Dictionary<Vector3Int, HumanUnit> occupiedCells
    = new Dictionary<Vector3Int, HumanUnit>(); //각 셀에 유닛있는지 확인하려는 용도임
    public bool IsCellOccupied(Vector3Int cell) //셀이 점유되어있는지 확인하는 함수
    {
        return occupiedCells.ContainsKey(cell); //점유된 셀이면 true, 아니면 false 반환
    }

    public bool RegisterUnitCell(Vector3Int cell, HumanUnit unit)
    {
        if (occupiedCells.ContainsKey(cell)) //이미 점유된 칸이면 안댐
            return false;

        occupiedCells[cell] = unit; //새로운 유닛 등록
        return true;
    }

    public void UnregisterUnitCell(Vector3Int cell, HumanUnit unit)
    {
        if (occupiedCells.TryGetValue(cell, out var existing)) //점유된 칸이 맞는지 확인
        {
            if (existing == unit) //등록된 유닛이 맞으면 해제
                occupiedCells.Remove(cell);//점유 해제
        }
    }
}
