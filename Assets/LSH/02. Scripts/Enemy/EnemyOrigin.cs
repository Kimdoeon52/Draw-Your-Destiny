using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyActionType
{
    Attack,
    Building,
    GetGold,
    GetHuman
}

[System.Serializable]
public class ActionCase
{
    public EnemyActionType actionType;
    [Range(0, 100)] public int weight;
}

public class EnemyOrigin : MonoBehaviour
{
    [Header("소환 위치")]
    [SerializeField] protected Transform spawnPoint;

    [Header("적 타입")]
    [SerializeField] protected EnemyType enemyType;

    [Header("적 골드 개념")]
    [SerializeField] protected int gold = 0;

    [Header("행동 확률")]
    [SerializeField] private List<ActionCase> actionCases = new List<ActionCase>();

    [SerializeField] protected int enemyID;
    [SerializeField] protected EnemyUnitPool enemyUnitPool;

    [Header("각 건물들")]
    [SerializeField] protected BuildingData houseData; //집 데이터
    [SerializeField] protected BuildingData outPostData; //소규모 영지용 데이터

    [Header("소규모 영지 배치 설정")]
    [SerializeField] protected int outpostCost = 100;
    [SerializeField] protected int outpostMinDistanceFromGoldMine = 2;
    [SerializeField] protected int outpostMaxDistanceFromGoldMine = 4;
    [SerializeField] protected int outpostTryCount = 40;

    [Header("건물 배치 설정")]
    [SerializeField] protected int houseCost = 50;
    [SerializeField] protected int minBuildRangeFromCastle = 5;
    [SerializeField] protected int maxBuildRangeFromCastle = 10;
    [SerializeField] protected int buildTryCount = 30;


    [Header("영주성")]
    [Tooltip("이 적의 영주성 스프라이트 — EnemyA/B/C Inspector에서 각각 설정")]
    public Sprite lordCastleSprite;
    [Tooltip("영주성 최대 체력")]
    public int lordCastleMaxHP = 100;


    // 같은 GameObject에 붙은 LordCastle 컴포넌트 (Start에서 자동으로 가져옴)
    protected LordCastle lordCastle;

    protected virtual void Start()
    {
        enemyUnitPool = transform.parent.GetComponentInChildren<EnemyUnitPool>();

        // EnemyA/B/C 자체가 영주성이므로 같은 오브젝트에서 가져옴
        lordCastle = GetComponent<LordCastle>();

        AddTurnList();
        GameStartEnemyInfo();

        // CitySpawnManager가 DefaultExecutionOrder(10)으로 나중에 실행되므로
        // 한 프레임 후에 영주성 위치를 잡아야 도시 중심 좌표가 확정됨
        StartCoroutine(InitLordCastleNextFrame());
    }
    protected virtual void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            StartEnemyTurn();
        }
    }

    // 영주성 위치 + 스프라이트 초기화 (1프레임 대기 후 실행)
    private IEnumerator InitLordCastleNextFrame()
    {
        yield return null;

        if (lordCastle == null)
        {
            Debug.LogWarning($"[EnemyOrigin] enemyID={enemyID}: LordCastle 컴포넌트가 없습니다.");
            yield break;
        }

        // CitySpawnManager에서 도시 중심 좌표 가져오기
        // SpawnedCityCenters 순서: [0]=플레이어, [1]=AI-A, [2]=AI-B, [3]=AI-C
        CitySpawnManager citySpawnManager = FindFirstObjectByType<CitySpawnManager>();
        TileMapManager tileMapManager = TileMapManager.Instance;
        if (citySpawnManager == null || tileMapManager == null || tileMapManager.groundTilemap == null)
        {
            Debug.LogWarning($"[EnemyOrigin] enemyID={enemyID}: 영주성 배치에 필요한 매니저를 찾을 수 없습니다.");
            yield break;
        }

        // 시작 도시 bounds 기준으로 4x4 영주성의 실제 중심을 계산
        // 나중에 소규모 영지(예: 8x8)에서도 해당 영지 bounds와 tileSize=2만 넘기면 같은 방식 사용 가능
        if (citySpawnManager.TryGetSpawnedCityBounds(enemyID, out BoundsInt cityBounds))
        {
            lordCastle.Initialize(lordCastleSprite, tileMapManager.groundTilemap, cityBounds, lordCastleMaxHP);
            Debug.Log($"[EnemyOrigin] enemyID={enemyID}: 영주성을 bounds 중심에 배치 완료.");
            yield break;
        }
        // bounds 정보가 없으면 기존 중심 타일 기준으로 한 번 더 시도
        if (citySpawnManager == null || citySpawnManager.SpawnedCityCenters.Count <= enemyID)
        {
            Debug.LogWarning($"[EnemyOrigin] enemyID={enemyID}: 도시 중심 좌표를 찾을 수 없습니다.");
            yield break;
        }

        Vector3Int cityCenter = citySpawnManager.SpawnedCityCenters[enemyID];

        // 타일 중앙 월드 좌표로 변환
        Vector3 worldPos = tileMapManager.groundTilemap.GetCellCenterWorld(cityCenter);

        // 영주성 초기화 (스프라이트 + 위치 + 체력)
        lordCastle.Initialize(lordCastleSprite, worldPos, lordCastleMaxHP);

        Debug.Log($"[EnemyOrigin] enemyID={enemyID}: 영주성을 {cityCenter} 위에 배치 완료.");
    }

    protected virtual void GameStartEnemyInfo()
    {
    }

    protected virtual void AddTurnList()
    {
        actionCases.Clear();

        actionCases.Add(new ActionCase { actionType = EnemyActionType.Attack, weight = 5 });
        actionCases.Add(new ActionCase { actionType = EnemyActionType.Building, weight = 20 });
        actionCases.Add(new ActionCase { actionType = EnemyActionType.GetGold, weight = 40 });
        actionCases.Add(new ActionCase { actionType = EnemyActionType.GetHuman, weight = 35 });
    }

    //--------------------턴 시작과 종료----------------------
    protected virtual void StartEnemyTurn()
    {
        Debug.Log("적 턴 시작");
        ExecuteRandomAction();
    }

    protected virtual void EndEnemyTurn()
    {
        Debug.Log("적 턴 종료");
    }

    //---------------------행동 실행----------------------
    protected virtual void ExecuteRandomAction()
    {
        EnemyActionType enemyAction = GetWeightedRandomAction();
        bool actionCheck = CheckAction(enemyAction);

        if (!actionCheck)
        {
            enemyAction = EnemyActionType.GetGold;
            Debug.Log($"<color=yellow>[적은 지금 거지에요]</color> 골드 부족으로 인해 강제적으로 골드 턴.");
        }

        Debug.Log($"<color=yellow>[Enemy Turn]</color> 행동: {enemyAction}");

        switch (enemyAction)
        {
            case EnemyActionType.Attack: DoAttack(); break;
            case EnemyActionType.Building: DoBuilding(); break;
            case EnemyActionType.GetGold: DoGetGold(); break;
            case EnemyActionType.GetHuman: DoGetEnemyHuman(); break;
        }

        EndEnemyTurn();
    }

    protected virtual EnemyActionType GetWeightedRandomAction()
    {
        int totalWeight = 0;
        foreach (var actionCase in actionCases)
            totalWeight += actionCase.weight;

        if (totalWeight <= 0)
            return EnemyActionType.GetGold;

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var actionCase in actionCases)
        {
            currentWeight += actionCase.weight;
            if (randomValue < currentWeight)
                return actionCase.actionType;
        }

        return EnemyActionType.GetGold;
    }

    //--------------------각 기본 행동들-------------------
    protected virtual void DoAttack()
    {
        Debug.Log("적이 공격을 선택했다!");
    }

    protected virtual void DoBuilding()
    {
        int randomValue = Random.Range(0, 100);

        if (randomValue < 50)
        {
            BuildSmallTown();
            return;
        }

        if (houseData == null)
        {
            Debug.LogWarning("houseData가 연결되지 않았습니다.");
            return;
        }

        if (gold < houseCost)
        {
            Debug.LogWarning($"집 건설 골드 부족. 현재 골드: {gold}");
            return;
        }

        TileMapManager tileMapManager = TileMapManager.Instance;
        if (tileMapManager == null || tileMapManager.groundTilemap == null)
        {
            Debug.LogWarning("TileMapManager 또는 groundTilemap이 없습니다.");
            return;
        }

        Vector3Int castleCell = tileMapManager.groundTilemap.WorldToCell(transform.position);

        if (TryFindBuildPositionNearCastle(castleCell, out Vector3Int buildPos))
        {
            bool success = tileMapManager.PlaceBuildingForAI(buildPos, houseData, enemyID);

            if (success)
            {
                gold -= houseCost;
                Debug.Log($"<color=green>[건물 건설]</color> {enemyType}가 {buildPos} 에 집을 설치했습니다. 현재 골드: {gold}");
            }
            else
            {
                Debug.LogWarning("건설 위치를 찾았지만 실제 배치에는 실패했습니다.");
            }
        }
        else
        {
            Debug.Log("영주성 주변에 집을 지을 수 있는 위치를 찾지 못했습니다.");
        }
    }

    protected bool TryFindBuildPositionNearCastle(Vector3Int castleCell, out Vector3Int result)
    {
        TileMapManager tileMapManager = TileMapManager.Instance;
        result = castleCell;

        for (int i = 0; i < buildTryCount; i++)
        {
            int offsetX = Random.Range(-maxBuildRangeFromCastle, maxBuildRangeFromCastle + 1);
            int offsetY = Random.Range(-maxBuildRangeFromCastle, maxBuildRangeFromCastle + 1);

            // 너무 가까운 칸은 제외
            if (Mathf.Abs(offsetX) < minBuildRangeFromCastle &&
                Mathf.Abs(offsetY) < minBuildRangeFromCastle)
            {
                continue;
            }

            Vector3Int candidate = castleCell + new Vector3Int(offsetX, offsetY, 0);

            // 자기 영토 위만 짓고 싶으면 이 조건 추가
            if (tileMapManager.GetOwner(candidate) != enemyID)
                continue;

            if (tileMapManager.CanPlace(candidate, houseData))
            {
                result = candidate;
                return true;
            }
        }

        return false;
    }


    protected virtual void DoGetGold()
    {
        gold += 100;
        Debug.Log($"{enemyType}가 골드를 얻었어용. <color=green>현재 골드: {gold}</color>");
    }

    protected virtual void DoWait()
    {
        Debug.Log("한조 대기중.");
    }

    protected virtual void DoGetEnemyHuman()
    {
        if (enemyUnitPool == null)
        {
            Debug.LogWarning("EnemyUnitPool이 연결되지 않았습니다.");
            return;
        }

        if (gold < 10)
        {
            Debug.LogWarning("골드 부족으로 유닛을 소환할 수 없습니다.");
            return;
        }

        GameObject spawnedEnemy = enemyUnitPool.GetEnemyUnit(enemyType, enemyID);

        if (spawnedEnemy == null)
        {
            Debug.LogWarning($"{enemyType} 타입 유닛 소환 실패");
            return;
        }

        gold -= 10;

        if (spawnPoint == null)
            spawnedEnemy.transform.position = transform.position;
        else
            spawnedEnemy.transform.position = spawnPoint.position;

        EnemyUnit enemyUnit = spawnedEnemy.GetComponent<EnemyUnit>();
        if (enemyUnit != null)
        {
            enemyUnit.enemyType = enemyType;
            enemyUnit.enemyPool = enemyUnitPool;
        }

        Debug.Log($"<color=red>[유닛 소환]</color> {enemyType} 적 소환 완료 / 현재 골드: {gold}");
    }

    //-------------------특수 행동--------------------------------

    //영지 기준으로 가까운 금광찾기
    protected bool TryFindNearestGoldMine(out Vector3Int nearestGoldMineCell)
    {
        nearestGoldMineCell = Vector3Int.zero;

        TileMapManager tileMapManager = TileMapManager.Instance;
        if (tileMapManager == null || tileMapManager.groundTilemap == null)
            return false;

        List<Vector3Int> goldMineCells = tileMapManager.GetAllGoldMineCells();
        if (goldMineCells == null || goldMineCells.Count == 0)
            return false;

        Vector3Int castleCell = tileMapManager.groundTilemap.WorldToCell(transform.position);

        float minDist = float.MaxValue;
        bool found = false;

        foreach (Vector3Int goldCell in goldMineCells)
        {
            float dist = Vector3Int.Distance(castleCell, goldCell);

            if (dist < minDist)
            {
                minDist = dist;
                nearestGoldMineCell = goldCell;
                found = true;
            }
        }

        return found;
    }

    //금광 중심 설치 시도
    protected bool TryFindOutpostPositionNearGoldMine(Vector3Int goldMineCell, out Vector3Int result)
    {
        result = goldMineCell;

        TileMapManager tileMapManager = TileMapManager.Instance;
        if (tileMapManager == null)
            return false;

        for (int i = 0; i < outpostTryCount; i++)
        {
            int offsetX = Random.Range(-outpostMaxDistanceFromGoldMine, outpostMaxDistanceFromGoldMine + 1);
            int offsetY = Random.Range(-outpostMaxDistanceFromGoldMine, outpostMaxDistanceFromGoldMine + 1);

            int manhattan = Mathf.Abs(offsetX) + Mathf.Abs(offsetY);

            // 너무 가까운 건 제외, 너무 먼 것도 제외
            if (manhattan < outpostMinDistanceFromGoldMine || manhattan > outpostMaxDistanceFromGoldMine)
                continue;

            Vector3Int candidate = goldMineCell + new Vector3Int(offsetX, offsetY, 0);

            // 설치 가능 여부
            if (!tileMapManager.CanPlace(candidate, outPostData))
                continue;

            result = candidate;
            return true;
        }

        return false;
    }
    protected virtual void BuildSmallTown()
    {

    }

    //----------------행동검사------------------
    protected virtual bool CheckAction(EnemyActionType action)
    {
        switch (action)
        {
            case EnemyActionType.Attack: return true;
            case EnemyActionType.Building: return gold >= 50;
            case EnemyActionType.GetGold: return true;
            case EnemyActionType.GetHuman: return gold >= 10;
        }
        return false;
    }
}
