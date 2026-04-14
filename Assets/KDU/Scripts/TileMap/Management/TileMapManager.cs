using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// ============================================================
// TileMapManager — 타일맵 전체 관리 싱글톤
//
// 담당 범위:
//   1. 타일 데이터 (tileDataMap) — 모든 타일의 타입/소유권을 런타임에 관리
//   2. 건물 배치/제거 — PlaceBuilding, RemoveBuilding
//   3. 영토 소유권 — SetOwner, GetOwner
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
    public Tilemap cityTilemap;         // Tilemap_City     — City 타일 (일반 건물 배치 가능)
    public Tilemap farmlandTilemap;     // Tilemap_Farmland — Farmland 지형 (Farm 전용)

    [Header("전투 지형 레이어 — CombatArea Tilemap 연결")]
    public Tilemap groundTilemap;       // Tilemap_Ground   — 평지 (이동 1칸)
    public Tilemap forestTilemap;       // Tilemap_Forest   — 숲 (이동 불가)
    public Tilemap riverTilemap;        // Tilemap_River    — 강 (이동 2칸 소비)

    // ── 건물 관련 컬렉션 ──────────────────────────────────────
    // buildingMap: footprint의 모든 타일 좌표를 키로 BuildingData를 저장
    //              CanPlace에서 충돌 검사 시 사용
    private Dictionary<Vector3Int, BuildingData> buildingMap = new Dictionary<Vector3Int, BuildingData>();

    // buildingObjects: origin 좌표   → 화면에 표시되는 GameObject (SpriteRenderer)
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

    protected override void Awake()
    {
        base.Awake();

        // 씬에 Buildings 빈 오브젝트를 만들어 배치된 건물 GameObject를 그 아래로 넣음
        GameObject container = new GameObject("Buildings");
        container.transform.SetParent(transform);
        buildingContainer = container.transform;

        InitializeTileDataMap();
    }

    // 씬의 모든 지형 Tilemap을 순회해 tileDataMap을 초기화
    private void InitializeTileDataMap()
    {
        HashSet<Vector3Int> allPositions = new HashSet<Vector3Int>();

        CollectPositions(cityTilemap,     allPositions);
        CollectPositions(farmlandTilemap, allPositions);

        foreach (Vector3Int pos in allPositions)
            tileDataMap[pos] = new TileData(GetTileType(pos), -1);
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
        bool exists = (cityTilemap     != null && cityTilemap.HasTile(pos))
                   || (farmlandTilemap != null && farmlandTilemap.HasTile(pos));

        if (exists)
        {
            if (tileDataMap.ContainsKey(pos))
                tileDataMap[pos].type = GetTileType(pos);
            else
                tileDataMap[pos] = new TileData(GetTileType(pos), -1);
        }
        else
        {
            tileDataMap.Remove(pos);
        }
    }

    // 타일맵 전체 좌표 반환 (순회 용도)
    public IEnumerable<Vector3Int> GetAllTilePositions()
    {
        return tileDataMap.Keys;
    }

    // ── 타일 타입 조회 ────────────────────────────────────────
    // 우선순위: Farmland > City
    // 타일이 없는 위치는 River(이동/건설 불가)로 처리
    public TileType GetTileType(Vector3Int pos)
    {
        if (farmlandTilemap != null && farmlandTilemap.HasTile(pos)) return TileType.Farmland;
        if (cityTilemap     != null && cityTilemap.HasTile(pos))     return TileType.City;
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
    public void SetOwner(Vector3Int pos, int civID)
    {
        if (!tileDataMap.ContainsKey(pos)) return;
        tileDataMap[pos].ownerCivID = civID;
    }

    // ── 특정 타일에 있는 BuildingInstance 반환 (없으면 null) ────
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
    public void RegisterBuildingInstance(BuildingInstance instance)
    {
        if (instance == null) return;
        allBuildings.Add(instance);
        foreach (Vector3Int pos in instance.footprint)
        {
            buildingMap[pos] = instance.data; // data가 null이면 null 등록 (충돌 방지용)
            buildingInstanceMap[pos] = instance;
        }
        //BuildingRegistry.Instance?.Register(instance);
    }

    // ── 건물 배치 가능 여부 확인 ─────────────────────────────────
    // clickPos 기준으로 origin을 계산한 뒤 footprint 전체를 검사
    // 조건: 맵 안 + 강 없음 + 건물 없음 + allowedTiles 타입 일치
    //       + maxPerTerritory 영지 내 제한 + isUniqueGlobal 게임 전체 제한
    public bool CanPlace(Vector3Int clickPos, BuildingData building)
    {
        // 영지 내 최대 설치 수 검사
        if (building.maxPerTerritory >= 0)
        {
            int count = 0;
            foreach (BuildingInstance b in allBuildings)
            {
                if (b.data != null && b.data.buildingType == building.buildingType)
                    count++;
            }
            if (count >= building.maxPerTerritory) return false;
        }

        // 게임 전체 유일성 검사 (Lab 등)
        if (building.isUniqueGlobal && IsAlreadyBuiltGlobally(building))
            return false;

        Vector3Int origin = GetOrigin(clickPos, building);

        for (int x = 0; x < building.width; x++)
        {
            for (int y = 0; y < building.height; y++)
            {
                Vector3Int checkPos = origin + new Vector3Int(x, y, 0);

                if (!IsValidPosition(checkPos)) return false;
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

    // 게임 전체 노드에서 동일 buildingType 건물이 이미 존재하는지 검사
    // 현재 노드(allBuildings, 라이브)와 저장된 전 노드(WorldMapManager 경유)를 모두 확인
    private bool IsAlreadyBuiltGlobally(BuildingData building)
    {
        // 현재 진입 중인 노드 (라이브 상태 — NodeData.buildings보다 최신)
        foreach (BuildingInstance b in allBuildings)
        {
            if (b.data != null && b.data.buildingType == building.buildingType)
                return true;
        }

        // 저장된 모든 플레이어 노드 (마지막 이탈 시 스냅샷)
        if (WorldMapManager.Instance != null)
            return WorldMapManager.Instance.HasUniqueGlobalBuilding(building.buildingType);

        return false;
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
            visual = visual
        };

        // Behaviour 캐싱 & 초기화
        instance.behaviour = visual.GetComponent<BuildingBehaviour>();
        if (instance.behaviour != null)
        {
            instance.behaviour.instance = instance;
            instance.behaviour.OnPlaced();
        }

        allBuildings.Add(instance);
        foreach (Vector3Int pos in footprint)
            buildingInstanceMap[pos] = instance;

        //BuildingRegistry.Instance?.Register(instance);

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
    // BuildingData.visualPrefab이 있으면 프리팹 인스턴스화, 없으면 빈 GO 방식 fallback
    private GameObject CreateBuildingVisual(Vector3Int origin, BuildingData building)
    {
        GameObject buildingObj;

        if (building.visualPrefab != null)
        {
            buildingObj = Instantiate(building.visualPrefab, buildingContainer);
            buildingObj.name = $"{building.buildingName}_{origin.x}_{origin.y}";

            // 프리팹의 SpriteRenderer에 SO 스프라이트 적용
            // 프리팹은 구조(컴포넌트/Behaviour)만 담고, 스프라이트는 SO가 관리
            SpriteRenderer sr = buildingObj.GetComponent<SpriteRenderer>();
            if (sr != null && building.sprite != null)
                sr.sprite = building.sprite;
        }
        else
        {
            buildingObj = new GameObject($"{building.buildingName}_{origin.x}_{origin.y}");
            buildingObj.transform.SetParent(buildingContainer);
            SpriteRenderer sr = buildingObj.AddComponent<SpriteRenderer>();
            sr.sprite = building.sprite;
            sr.sortingOrder = 100;
        }

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
        //BuildingRegistry.Instance?.Remove(instance);

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
        return cityTilemap.GetCellCenterWorld(pos);
    }

    // 건물 프리뷰와 실제 설치가 항상 같은 좌표를 사용하도록 공용 계산식으로 통일한다.
    public Vector3 GetBuildingWorldCenter(Vector3Int origin, BuildingData building)
    {
        if (cityTilemap == null || building == null)
            return Vector3.zero;

        Vector3 originWorld = cityTilemap.CellToWorld(origin);
        Vector3 cellSize = cityTilemap.cellSize;

        return originWorld + new Vector3(
            building.width * cellSize.x * 0.5f,
            building.height * cellSize.y * 0.5f,
            0f);
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
            visual = visual
        };

        // Behaviour 캐싱 & 초기화
        instance.behaviour = visual.GetComponent<BuildingBehaviour>();
        if (instance.behaviour != null)
        {
            instance.behaviour.instance = instance;
            instance.behaviour.OnPlaced();
        }

        allBuildings.Add(instance);

        foreach (Vector3Int pos in footprint)
        {
            buildingInstanceMap[pos] = instance;
        }

        //BuildingRegistry.Instance?.Register(instance);

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

    ////--------------------도라지 코드 건들지 마셈 진짜 혼난다^_^------------------
    //private Dictionary<Vector3Int, HumanUnit> occupiedCells
    //= new Dictionary<Vector3Int, HumanUnit>(); //각 셀에 유닛있는지 확인하려는 용도임
    //public bool IsCellOccupied(Vector3Int cell) //셀이 점유되어있는지 확인하는 함수
    //{
    //    return occupiedCells.ContainsKey(cell); //점유된 셀이면 true, 아니면 false 반환
    //}

    //public bool RegisterUnitCell(Vector3Int cell, HumanUnit unit)
    //{
    //    if (occupiedCells.ContainsKey(cell)) //이미 점유된 칸이면 안댐
    //        return false;

    //    occupiedCells[cell] = unit; //새로운 유닛 등록
    //    return true;
    //}

    //public void UnregisterUnitCell(Vector3Int cell, HumanUnit unit)
    //{
    //    if (occupiedCells.TryGetValue(cell, out var existing)) //점유된 칸이 맞는지 확인
    //    {
    //        if (existing == unit) //등록된 유닛이 맞으면 해제
    //            occupiedCells.Remove(cell);//점유 해제
    //    }
    //}

    // ── NodeDataManager 연동 — 노드 진입/이탈 시 호출 ─────────────

    // 씬의 지형 Tilemap을 모두 비움 (노드 이탈 시)
    public void ClearAllTerrainTiles()
    {
        if (cityTilemap     != null) cityTilemap.ClearAllTiles();
        if (farmlandTilemap != null) farmlandTilemap.ClearAllTiles();
        if (groundTilemap   != null) groundTilemap.ClearAllTiles();
        if (forestTilemap   != null) forestTilemap.ClearAllTiles();
        if (riverTilemap    != null) riverTilemap.ClearAllTiles();
        tileDataMap.Clear();
    }

    // 배치된 건물 시각화를 모두 Destroy하고 관련 컬렉션을 초기화 (노드 이탈 시)
    public void ClearAllBuildings()
    {
        foreach (GameObject obj in buildingObjects.Values)
        {
            if (obj != null) Destroy(obj);
        }
        buildingObjects.Clear();
        buildingMap.Clear();
        buildingInstanceMap.Clear();
        allBuildings.Clear();
    }

    // tileDataMap을 지형 Tilemap 기준으로 재초기화 (지형 복사 후 호출)
    public void ReinitializeTileDataMap()
    {
        tileDataMap.Clear();
        InitializeTileDataMap();
    }

    // 저장된 BuildingInstance의 시각화를 재생성하고 컬렉션에 등록 (노드 진입 시)
    public void RestoreBuildingInstance(BuildingInstance instance)
    {
        if (instance == null || instance.data == null) return;

        // footprint 재계산 (저장 시 유지된 origin 기반)
        List<Vector3Int> footprint = new List<Vector3Int>();
        for (int x = 0; x < instance.data.width; x++)
        for (int y = 0; y < instance.data.height; y++)
        {
            Vector3Int pos = instance.origin + new Vector3Int(x, y, 0);
            footprint.Add(pos);
            buildingMap[pos] = instance.data;
        }
        instance.footprint = footprint;

        // 시각화 재생성
        instance.visual = CreateBuildingVisual(instance.origin, instance.data);

        // Behaviour 캐싱 & 저장된 상태 복원
        instance.behaviour = instance.visual != null ? instance.visual.GetComponent<BuildingBehaviour>() : null;
        if (instance.behaviour != null)
        {
            instance.behaviour.instance = instance;
            instance.behaviour.LoadState(instance.savedState);
        }

        // 컬렉션 등록
        allBuildings.Add(instance);
        foreach (Vector3Int pos in footprint)
            buildingInstanceMap[pos] = instance;

        //BuildingRegistry.Instance?.Register(instance);
    }

    // 특정 타입의 건물이 있는지 모든 건물 인스턴스를 조회해 찾는 함수.
    public static bool HasBuildingOfType(BuildingType targetBuildingType)
    {
        List<BuildingInstance> allBuildings = TileMapManager.Instance.GetAllBuildings();

        if (allBuildings.Count == 0) return false;
        foreach (var building in allBuildings)
        {
            if (building.data.buildingType == targetBuildingType)
            {
                return true;
            }
        }

        return false;
    }
}
