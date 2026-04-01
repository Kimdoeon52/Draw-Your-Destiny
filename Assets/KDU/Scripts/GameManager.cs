using DG.Tweening;
using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.IK;

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

    // 재화
    public int playerGold = 10;
    public int playerResearch = 0;
    public int Food = 0;

    // 인구 — 현재 인구 수 / 최대 인구 한도
    // 인구 한도는 민가(House) 배치 시 populationCapBonus만큼 증가
    public int playerPopulation = 0;
    public int playerPopulationCap = 10;

    // 턴 상태 플래그 (다른 시스템에서 턴 전환 시점을 감지할 때 사용)
    public bool endTurn = false;
    public bool startTurn = false;

    public Era playerEra = Era.Stone;

    Camera cam;
    [SerializeField] Transform[] humanGenPoints;

    protected override void Awake()
    {
        base.Awake();
        InitializeManagers();
        cam = Camera.main;
    }

    // 씬 시작 시 필요한 매니저 참조 수집
    private void InitializeManagers()
    {
        tileMapManager = TileMapManager.Instance;
        placementService = FindFirstObjectByType<BuildingPlacementService>();
    }

    // ── 건물 배치 ─────────────────────────────────────────────
    // UI 버튼 등 외부에서 건물 배치 모드 진입 요청 시 호출
    // 금이 부족하면 진입하지 않는다
    public void StartBuildingPlacement(BuildingData buildingData)
    {
        if (placementService == null) return;
        if (playerGold < buildingData.goldCost) return;

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
    public void AddGold(int amount)
    {
        playerGold += amount;
        Debug.Log($"골드 획득 현재 골드:{playerGold}");
    }

    // 금 소모 — 부족하면 false 반환 (TileMapManager.PlaceBuilding에서 호출)
    public bool SpendGold(int amount)
    {
        if (playerGold < amount)
        {
            Debug.Log($"골드가 부족하여 사용할 수 없습니다.");
            return false;
        }
        playerGold -= amount;
        Debug.Log($"골드 사용:{amount} / 남은 골드:{playerGold}");
        return true;
    }

    public void AddResearch(int amount)
    {
        playerResearch += amount;
        Debug.Log($"연구 포인트 획득:{amount} / 현재:{playerResearch}");
    }

    // 민가(House) 배치 시 TileMapManager에서 호출
    public void IncreasePopulationCap(int amount)
    {
        playerPopulationCap += amount;
        Debug.Log($"인구 한도 증가:{amount} / 현재 인구:{playerPopulation}");
    }

    // 인간 생성 카드 효과 처리 후 CardSystem에서 호출
    public void GenerateHumans(int amount, PlayerUnitInfoByJob unitInfo)
    {
        GameObject[] humans = new GameObject[amount];

        List<int> indices = new List<int>();
        for (int i = 0; i < humanGenPoints.Length; i++)
        {
            indices.Add(i);
        }

        // 섞기 (Fisher-Yates Shuffle)
        for (int i = 0; i < indices.Count; i++)
        {
            int rand = Random.Range(i, indices.Count);
            int temp = indices[i];
            indices[i] = indices[rand];
            indices[rand] = temp;
        }

        for (int i = 0; i < amount; i++)
        {
            humans[i] = HumanPool.Instance.GetHuman(0);
            int index = indices[i];
            humans[i].transform.position = humanGenPoints[index].position;
            Debug.Log($"{humans[i].transform.position}");
            HumanUnit humanUnit = humans[i].GetComponent<HumanUnit>();
            humanUnit.SetUnitInfo(unitInfo);
        }
    }

    // 식량 획득 (CardSystem에서 호출)
    public void AddFood(int amount)
    {
        Food += amount;
        Debug.Log($"식량 획득:{amount}");
    }
    private void checkResearch()
    {
        if (playerResearch >= 100)
        {
            if (playerEra == Era.Stone)
            {
                playerEra = Era.Bronze;
                playerResearch -= 100;
                TileMapManager.Instance?.UpgradeBuildingsForEra(Era.Bronze);
            }
            else if (playerEra == Era.Bronze)
            {
                playerEra = Era.Iron;
                playerResearch -= 100;
                TileMapManager.Instance?.UpgradeBuildingsForEra(Era.Iron);
            }
        }
    }
}
