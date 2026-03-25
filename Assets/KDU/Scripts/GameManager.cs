using UnityEngine;

// 전체 게임 관리 싱글톤
public class GameManager : PersistentSingleton<GameManager>
{
    [Header("Managers")]
    private TileMapManager tileMapManager;
    private BuildingPlacementService placementService;

    [Header("Game State")]
    public int currentTurn = 0;
    public int playerGold = 10;
    public int playerResearch = 0;
    public int playerPopulation = 0;
    public int playerPopulationCap = 10;
    public bool endTurn = false;
    public bool startTurn = false;

    protected override void Awake()
    {
        base.Awake();
        InitializeManagers();
    }

    // 매니저 초기화
    private void InitializeManagers()
    {
        tileMapManager = TileMapManager.Instance;
        placementService = FindFirstObjectByType<BuildingPlacementService>();
    }

    // 건물 배치 모드 시작
    public void StartBuildingPlacement(BuildingData buildingData)
    {
        if (placementService == null)
            return;

        if (playerGold < buildingData.goldCost)
            return;

        placementService.StartPlacing(buildingData);
    }

    //턴 시작
    public void StartTurn()
    {
        startTurn = true;
        endTurn = false;
        currentTurn = currentTurn + 1;
    }

    // 턴 종료
    public void EndTurn()
    {
        endTurn = true;
        startTurn = false;
    }

    // 금 추가
    public void AddGold(int amount)
    {
        playerGold += amount;
        Debug.Log($"골드 획득 현재 골드:{playerGold}");
    }

    // 금 소모
    public bool SpendGold(int amount)
    {
        if (playerGold < amount)
        {
            Debug.Log($"골드가 부족하여 사용할 수 없습니다.");
            return false;
        }

        playerGold -= amount;
        {
            Debug.Log($"골드 사용 골드:{amount}");
            Debug.Log($"남은 골드: {playerGold}");
            return true;
        }
    }

    // 연구 포인트 추가
    public void AddResearch(int amount)
    {
        playerResearch += amount;
        Debug.Log($"연구 포인트 획득:{amount}");
        Debug.Log($"현재 연구 포인트: {playerResearch}");
    }

    // 인구 한도 증가
    public void IncreasePopulationCap(int amount)
    {
        playerPopulationCap += amount;
        Debug.Log($"인구 한도 증가:{amount}");
        Debug.Log($"현재 인구 수:{playerPopulation}");
    }
}
