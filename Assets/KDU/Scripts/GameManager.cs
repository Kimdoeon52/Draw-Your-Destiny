using DG.Tweening;
using NYH.CoreCardSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.IK;
using System;

// ============================================================
// GameManager — 게임 전체 상태 관리 (PersistentSingleton)
//
// 씬 전환 후에도 파괴되지 않는다.
// 담당:
//   - 현재 턴 카운트
//   - 플레이어 재화 (금, 연구 포인트, 인구/인구 한도)
//   - 건물 배치 모드 진입 요청 (UI → GameManager → BuildingPlacementService)
//
// 재화 흐름:
//   건물 배치 → TileMapManager.PlaceBuilding() → GameManager.SpendGold()
//   턴 생산   → (미구현) 건물 효과를 순회해 AddGold/AddResearch 호출 예정
// ============================================================
public class GameManager : PersistentSingleton<GameManager>
{
    [Header("Managers")]
    private TileMapManager tileMapManager;
    private BuildingPlacementService placementService;

    [Header("Game State")]
    public int currentTurn = 0;


 
    // 턴 상태 플래그 (다른 시스템에서 턴 전환 시점을 감지할 때 사용)
    public bool endTurn = false;
    public bool startTurn = false;

    public Era playerEra = Era.Stone;
    public event Action<Era> OnEraChanged; // 시대 변경 이벤트 추가

    Camera cam;
    [SerializeField] int lordCastleSize = 4; // 영주성 크기

    [Header("플레이어 영주성")]
    [SerializeField] PlayerLordCastle playerLordCastle;

    private CitySpawnManager citySpawnManager;

    protected override void Awake()
    {
        base.Awake();
        InitializeManagers();
        cam = Camera.main;
    }

    private void Start()
    {
        StartCoroutine(InitPlayerLordCastleNextFrame());
    }

    private IEnumerator InitPlayerLordCastleNextFrame()
    {
        yield return null;

        if (playerLordCastle == null)
        {
            Debug.LogWarning("[GameManager] playerLordCastle이 연결되지 않았습니다.");
            yield break;
        }

        if (citySpawnManager == null)
            citySpawnManager = FindFirstObjectByType<CitySpawnManager>();

        if (tileMapManager == null)
            tileMapManager = TileMapManager.Instance;

        if (tileMapManager == null || tileMapManager.buildingTilemap == null)
        {
            Debug.LogWarning("[GameManager] 플레이어 영주성 배치에 필요한 TileMapManager를 찾을 수 없습니다.");
            yield break;
        }

        if (citySpawnManager.TryGetSpawnedCityBounds(0, out BoundsInt cityBounds))
        {
            playerLordCastle.Initialize(tileMapManager.buildingTilemap, cityBounds);

            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                Vector3 castlePos = playerLordCastle.transform.position;
                cam.transform.position = new Vector3(castlePos.x, castlePos.y, cam.transform.position.z);
            }

            Debug.Log("[GameManager] 플레이어 영주성 배치 완료.");
        }
        else
        {
            Debug.LogWarning("[GameManager] 플레이어 도시 bounds를 찾을 수 없습니다.");
        }
    }

    public void GameOver()
    {
        Debug.Log("[GameManager] 게임 오버 — 플레이어 영주성 함락.");
        // TODO: 게임 오버 UI 연결
    }

    // 씬 시작 시 필요한 매니저 참조 수집
    private void InitializeManagers()
    {
        tileMapManager = TileMapManager.Instance;
        placementService = FindFirstObjectByType<BuildingPlacementService>();
        citySpawnManager = FindFirstObjectByType<CitySpawnManager>();
    }

    // ── 건물 배치 ─────────────────────────────────────────────
    // UI 버튼 등 외부에서 건물 배치 모드 진입 요청 시 호출
    // 금이 부족하면 진입하지 않는다
    public void StartBuildingPlacement(BuildingData buildingData)
    {
        if (placementService == null) return;
        if (ResourceManager.Instance.Gold < buildingData.goldCost) return;

        placementService.StartPlacing(buildingData);
    }

    // ── 턴 관리 ───────────────────────────────────────────────
    public void StartTurn()
    {
        startTurn = true;
        endTurn = false;
        currentTurn++;
       /* OngoingEffectSystem.Instance.OnTurnStartOrEnd();*/
    }

    public void EndTurn()
    {
        endTurn = true;
        startTurn = false;
        checkResearch();
    }

    // ── 재화 관리 ─────────────────────────────────────────────
    public void AddGold(int amount) => ResourceManager.Instance.AddGold(amount);

    // 금 소모 — 부족하면 false 반환 (TileMapManager.PlaceBuilding에서 호출)
    public bool SpendGold(int amount)
    {
        if (ResourceManager.Instance.Gold < amount)
        {
            Debug.Log($"골드가 부족하여 사용할 수 없습니다.");
            return false;
        }
        ResourceManager.Instance.AddGold(-amount);
        Debug.Log($"골드 사용:{amount} / 남은 골드:{ResourceManager.Instance.Gold}");
        return true;
    }

    public void AddResearch(int amount) => ResourceManager.Instance.AddResearch(amount);
    // 식량 획득 (CardSystem에서 호출)
    public void AddFood(int amount) => ResourceManager.Instance.AddFood(amount);

    // 민가(House) 배치 시 TileMapManager에서 호출
    public void IncreasePopulationCap(int amount)
    {
        ResourceManager.Instance.AddMaxPopulation(amount);
        Debug.Log($"인구 한도 증가:{amount} / 현재 인구:{ResourceManager.Instance.Population}");
    }

    // 인간 생성 카드 효과 처리 후 CardSystem에서 호출
    public void GenerateHumans(int amount, PlayerUnitInfoByJob unitInfo)
    {
        List<Vector3Int> spawnTiles = GetManorOuterTiles();

        // Fisher-Yates 셔플
        for (int i = spawnTiles.Count - 1; i > 0; i--)
        {
            int rand = UnityEngine.Random.Range(0, i + 1);
            (spawnTiles[i], spawnTiles[rand]) = (spawnTiles[rand], spawnTiles[i]);
        }

        for (int i = 0; i < amount; i++)
        {
            GameObject human = HumanPool.Instance.GetHuman(0);
            if (human == null) break;

            Vector3Int tile = spawnTiles[i % spawnTiles.Count];
            human.transform.position = tileMapManager.groundTilemap.GetCellCenterWorld(tile);

            HumanUnit humanUnit = human.GetComponent<HumanUnit>();
            humanUnit.SetUnitInfo(unitInfo);
        }
    }

    // 플레이어 영주성(lordCastleSize×lordCastleSize) 바깥 1칸 링 타일 목록
    private List<Vector3Int> GetManorOuterTiles()
    {
        Vector3Int center = citySpawnManager.SpawnedCityCenters[0];

        // GetOrigin과 동일한 오프셋: 짝수 크기 → offset = size/2 - 1
        int offset = lordCastleSize / 2 - 1;
        int xMin = center.x - offset;
        int xMax = xMin + lordCastleSize - 1;
        int yMin = center.y - offset;
        int yMax = yMin + lordCastleSize - 1;

        List<Vector3Int> outerTiles = new List<Vector3Int>();
        for (int x = xMin - 1; x <= xMax + 1; x++)
        for (int y = yMin - 1; y <= yMax + 1; y++)
        {
            bool isInterior = (x >= xMin && x <= xMax && y >= yMin && y <= yMax);
            if (!isInterior)
                outerTiles.Add(new Vector3Int(x, y, 0));
        }
        return outerTiles;
    }


    private void checkResearch()
    {
        if (ResourceManager.Instance.Research >= 100)
        {
            if (playerEra == Era.Stone)
            {
                playerEra = Era.Bronze;
                ResourceManager.Instance.AddResearch(-100);
                OnEraChanged?.Invoke(playerEra); // 이벤트 호출
                TileMapManager.Instance?.UpgradeBuildingsForEra(Era.Bronze);
            }
            else if (playerEra == Era.Bronze)
            {
                playerEra = Era.Iron;
                ResourceManager.Instance.AddResearch(-100);
                OnEraChanged?.Invoke(playerEra); // 이벤트 호출
                TileMapManager.Instance?.UpgradeBuildingsForEra(Era.Iron);
            }
        }
    }

    // 금
    public void ConvertGoldToFood(int percent)
    {
        int amount = (ResourceManager.Instance.Gold * percent) / 100;
        ResourceManager.Instance.AddFood(amount);
    }
}
