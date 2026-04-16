using EnemyAPool;
using PoolBase;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum EnemyAction
{
    Building, //시대에 따른 건물 짓기를 다르게 할 것
    GetGold, //골드나 식량을 동시에 얻을꺼임.
    TryOccupy //영지 점령 시도
}

public enum EnemyState
{
    Attack, //공격 <- 전투 전용 행동 타입
    Defend //방어 <- 일반적인 문명 행동 타입
}

[System.Serializable]
public class ActionCases
{
    public EnemyAction action;
    public EnemyState state;
    [Range(0, 100)] public int weight; //행동 확률
}


public class EnemyBrainBase : MonoBehaviour
{
    [Header("적 행동 확률")]
    [SerializeField] protected List<ActionCases> actionCases = new List<ActionCases>();

    [Header("농장 건물")]
    //[SerializeField] protected GameObject FarmPrefabs;
    [SerializeField] protected BuildingData farmData;
    [Header("상점 건물")]
    //[SerializeField] protected GameObject MarketPrefabs;
    [SerializeField] protected BuildingData marketData;
    [Header("민가 건물")]
    [SerializeField] protected BuildingData houseData;
    [Header("병영 건물")] //0번은 석기시대 병영 1번 2번은 청동기 3,4,5번은 철기
    //[SerializeField] protected List<GameObject> BarracksPrefabs = new List<GameObject>();
    [SerializeField] protected List<BuildingData> barracksData = new List<BuildingData>();

    [Header("골드 및 과학")]
    [SerializeField] protected int gold;
    [SerializeField] protected int science;

    [Header("시대 레벨")]
    [SerializeField] protected int enemyLevel; //적의 시대를 나타낼 레벨임

    [Header("적의 번호")]
    [SerializeField] public int enemyID; //적의 번호를 나타낼 변수임

    [Header("턴 진행 여부")] //적 종류마다 다르게 설정할꺼임.
    [SerializeField] protected int countEnemyTurn; //이건 턴 진행마다 해야할꺼. 예를 들어 턴 3턴마다 영지 점령 시도 이런거.

    [Header("지금 위치해있는 노드")] //현재 위치한 노드 Id 예를 들면 지금 노드 번호 101이런거임
    [SerializeField] protected int currentNodeID; //현재 위치한 노드 Id 예를 들면 지금 노드 번호 101이런거임
    public event System.Action OnTurnPassed; //using System을 쓰면 Random쓰기 귀찮음. 

    [Header("인구최대치")]
    [SerializeField] protected int maxHuman; //민가설치에 따라서 달라짐.
    [Header("병력 수")]
    [SerializeField] protected int rockWarriorCount;
    [SerializeField] protected int archerCount;
    [SerializeField] protected int healerCount;
    [SerializeField] protected int wizzardCount;
    [SerializeField] protected int horseWarriorCount; 
    [SerializeField] protected int superUnitCount;
    [Header("농장 수")]
    [SerializeField] protected int farmCount;
    protected virtual void OnEnable()
    {
        //적 행동 초기화
        InitializeActionCases();
        Debug.Log("{enemyID} 턴 시작");
        Setting();
    }
    //=============================임시 턴 시작 함수==============================
    protected virtual void Update()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            StartEnemyTurn();
        }
    }
    //=============================초기화 함수====================================
    protected virtual void Setting()
    {
        maxHuman = 10;
        rockWarriorCount = 0;
        archerCount = 0;
        healerCount = 0;
        wizzardCount = 0;
        horseWarriorCount = 0;
        superUnitCount = 0;
    }
    //=============================적 행동 확률====================================

    protected virtual void InitializeActionCases()
    {
        actionCases.Clear();//일단 비워주고
        enemyLevel = 1;
        //적 행동 확률 초기화
        actionCases.Add(new ActionCases { action = EnemyAction.Building, state = EnemyState.Defend, weight = 50 });
        actionCases.Add(new ActionCases { action = EnemyAction.GetGold, state = EnemyState.Defend, weight = 40 });
        actionCases.Add(new ActionCases { action = EnemyAction.TryOccupy, state = EnemyState.Attack, weight = 10 });
    }

    protected virtual void UpdateActionCases() //적 행동 확률 업데이트하는 함수임. 예를 들어 레벨업하면 건물 짓는 행동 확률이 올라가는 식으로.
    {
        //적 행동 확률 업데이트 구현
        if (enemyLevel == 2)
        {
            actionCases[0].weight = 50; //건물 짓기 확률 40%
            actionCases[1].weight = 30; //골드 얻기 확률 40%
            actionCases[2].weight = 20; //영지 점령 시도 확률 20%
        }
        else if (enemyLevel == 3) //공격적으로 변함.
        {
            actionCases[0].weight = 60; //건물 짓기 확률 60%
            actionCases[1].weight = 0; //건물 위주로 하기 위해 골드 얻기 x
            actionCases[2].weight = 40; //영지 점령 시도 확률 40%
        }
    }
    protected virtual EnemyAction GetWeightedRandomAction()
    {
        int totalWeight = 0; //전체적인 가중치 계산용도
        foreach (var actionCase in actionCases) //각 행동 케이스
            totalWeight += actionCase.weight; //전체 가중치 계산 일단 100이겠죠?

        if (totalWeight <= 0) //그냥 안전장치로 골드나 얻으라는 뜻
            return EnemyAction.GetGold;

        int randomValue = Random.Range(0, totalWeight); //0부터 100까지 생각
        int currentWeight = 0; //현재 가중치

        foreach (var actionCase in actionCases)
        {
            currentWeight += actionCase.weight; //첫번째 가중치 50 두번째 가중치 100이 되겠죠?
            if (randomValue < currentWeight) //랜덤값이 현재 가중치 보다 낮으면 액션
                return actionCase.action; //첫번째 케이스면 0~49 두번째 케이스면 50~99
        }

        return EnemyAction.GetGold;
    }
    
    
    //==============================적 행동 실행====================================
    protected virtual void StartEnemyTurn()
    {
        CheckLevelUp();
        UpdateActionCases();
        EnemyAction action = GetWeightedRandomAction(); //적 행동 확률에 따른 행동 선택
        bool actionCheck = CheckAction(action); //돈없으면 건물 못짓게 하기
        if (!actionCheck)
        {
            Debug.Log("<color=yellow>[돈이 없어서 강제로 골드]</color> ");
            action = EnemyAction.GetGold; //조건이 안맞으면 골드 얻는 행동으로 강제 변경
        }
        switch (action) 
        {
           case EnemyAction.Building: //건물 짓기
                Debug.Log("<color=red>[건물 짓기 시도]</color> ");
                CheckWhichBuilding(); //어떤 건물을 지을껀지 판단
                break;
           case EnemyAction.GetGold: //금과 식량 얻기
                Debug.Log("<color=red>[돈이 없어서...]</color> ");
                GetGold();
                break;
            case EnemyAction.TryOccupy: //영지 점령 시도
                Debug.Log("<color=red>[영지 점령 시도]</color> ");
                TryToOccupyAdjacentNode();
                break;
        }
        countEnemyTurn++;
        Debug.Log("<color=cyan>[턴 이벤트 발생]</color>");
        OnTurnPassed?.Invoke();
    }
    
    protected virtual bool CheckAction(EnemyAction action) //행동 조건 검사
    {
        Debug.Log("<color=red>[행동 체크 들어감...]</color> ");
        switch (action)
        {
            case EnemyAction.Building:
                Debug.Log("<color=red>[돈이 있어]</color> ");
                return gold >= 300;//골드가 없으면 건물 못 지음
            case EnemyAction.GetGold:
                return true;
            case EnemyAction.TryOccupy:
                return gold >= 1000; //영지 점령은 1000골드 이상으로 자주 못하게 설정.
        }
        return false;
    }
    //==============================건물 짓기====================================
    protected virtual void CheckWhichBuilding()
    {
        switch(enemyLevel) //적의 시대에 따른 건물 짓기
        {
            case 1: //석기 시대
                Debug.Log("<color=yellow>[건물 짓자.]</color> ");
                BuildLevelOne();
                break;
            case 2: //청동기 시대
                BuildLevelTwo();
                break;
            case 3: //철기 시대
                BuildLevelThree();
                break;
        }
        gold -= 300;
    }
    protected virtual void BuildLevelOne()
    {
        //적의 석기 시대 건물 짓기 행동 구현
        int buildingChoice = Random.Range(0, 3); //농장, 상점, 병영 중 하나 선택
        if(maxHuman <= 100)
        {
            buildingChoice = Random.Range(0, 4); //농장, 상점, 병영, 민가 중 하나 선택
        }
        switch (buildingChoice)
        {
            case 0: //농장 건물
                Debug.Log("<color=yellow>[농장 건물 짓자.]</color> ");
                if(farmCount >= 3) //농장 3개 이상이면 농장 안짓게 하기
                {
                    Debug.Log("<color=yellow>[농장 3개 이상이라 농장 안짓자.]</color> ");
                    CheckWhichBuilding(); //다시 건물 선택
                    return;
                }
                //SpawnBuilding(FarmPrefabs);
                farmCount += 1;
                SpawnBuilding(farmData);
                break;
            case 1: //상점 건물
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [상점 건물 짓자.]</color> ");
                //Instantiate(MarketPrefabs, GetRandomPosition(), Quaternion.identity);
                SpawnBuilding(marketData);
                break;
            case 2: //병영 건물
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [병영 짓자.]</color> ");
                SpawnBuilding(barracksData[0]); //석기 시대 병영
                rockWarriorCount += 5; //석기 시대 병영은 돌전사 5명 생산
                break;
            case 3:
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [민가 짓자.]</color> ");
                SpawnBuilding(houseData);
                maxHuman += 10;
                break;
        }
    }
    protected virtual void BuildLevelTwo()
    {
        //적의 청동기 시대 건물 짓기 행동 구현
        int buildingChoice = Random.Range(0, 5); //농장, 상점, 병영 중 하나 선택
        if (maxHuman <= 100)
        {
            buildingChoice = Random.Range(0, 6); //농장, 상점, 병영, 민가 중 하나 선택
        }
        switch (buildingChoice)
        {
            case 0: //농장 건물
                //Instantiate(FarmPrefabs, GetRandomPosition(), Quaternion.identity);
                if (farmCount >= 3) //농장 3개 이상이면 농장 안짓게 하기
                {
                    Debug.Log("<color=yellow>[농장 3개 이상이라 농장 안짓자.]</color> ");
                    CheckWhichBuilding(); //다시 건물 선택
                    return;
                }
                farmCount += 1;
                SpawnBuilding(farmData);
                break;
            case 1: //상점 건물
                    //Instantiate(MarketPrefabs, GetRandomPosition(), Quaternion.identity);
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [상점 건물 짓자.]</color> ");
                SpawnBuilding(marketData);
                break;
            case 2: //병영 건물
                //Instantiate(BarracksPrefabs[Random.Range(1, 3)], GetRandomPosition(), Quaternion.identity); //청동기 시대 병영
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [아처 짓자.]</color> ");
                SpawnBuilding(barracksData[1]); //청동기 시대 병영
                archerCount += 5;
                break;
            case 3: //병영 건물
                //Instantiate(BarracksPrefabs[Random.Range(1, 3)], GetRandomPosition(), Quaternion.identity); //청동기 시대 병영
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [힐러 짓자.]</color> ");
                SpawnBuilding(barracksData[2]); //청동기 시대 병영
                healerCount += 5;
                break;
            case 4: //병영 건물
                //Instantiate(BarracksPrefabs[Random.Range(1, 3)], GetRandomPosition(), Quaternion.identity); //청동기 시대 병영
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [우가우가 짓자.]</color> ");
                SpawnBuilding(barracksData[0]); //청동기 시대 병영
                rockWarriorCount += 5;
                break;
            case 5:
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [민가 짓자.]</color> ");
                SpawnBuilding(houseData);
                maxHuman += 10;
                break;
        }
    }
    protected virtual void BuildLevelThree()
    {
        //적의 철기 시대 건물 짓기 행동 구현
        int buildingChoice = Random.Range(0, 8); //농장, 상점, 병영 중 하나 선택
        if (maxHuman <= 100)
        {
            buildingChoice = Random.Range(0, 9); //농장, 상점, 병영, 민가 중 하나 선택
        }
        switch (buildingChoice)
        {
            case 0: //농장 건물
                //Instantiate(FarmPrefabs, GetRandomPosition(), Quaternion.identity);
                if (farmCount >= 3) //농장 3개 이상이면 농장 안짓게 하기
                {
                    Debug.Log("<color=yellow>[농장 3개 이상이라 농장 안짓자.]</color> ");
                    CheckWhichBuilding(); //다시 건물 선택
                    return;
                }
                farmCount += 1;
                SpawnBuilding(farmData);
                break;
            case 1: //상점 건물
                //Instantiate(MarketPrefabs, GetRandomPosition(), Quaternion.identity);
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [상점 짓자.]</color> ");
                SpawnBuilding(marketData);
                break;
            case 2: //병영 건물
                //Instantiate(BarracksPrefabs[Random.Range(1, 3)], GetRandomPosition(), Quaternion.identity); //청동기 시대 병영
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [아처 짓자.]</color> ");
                SpawnBuilding(barracksData[1]); //청동기 시대 병영
                archerCount += 2;
                break;
            case 3: //병영 건물
                //Instantiate(BarracksPrefabs[Random.Range(1, 3)], GetRandomPosition(), Quaternion.identity); //청동기 시대 병영
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [힐러 짓자.]</color> ");
                SpawnBuilding(barracksData[2]); //청동기 시대 병영
                healerCount += 5;
                break;
            case 4: //병영 건물
                //Instantiate(BarracksPrefabs[Random.Range(1, 3)], GetRandomPosition(), Quaternion.identity); //청동기 시대 병영
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [우가우가 짓자.]</color> ");
                SpawnBuilding(barracksData[0]); //청동기 시대 병영
                rockWarriorCount += 5;
                break;
            case 5:
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [기마병 짓자.]</color> ");
                SpawnBuilding(barracksData[3]); //철기 시대 병영
                horseWarriorCount += 3;
                break;
            case 6:
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [마법사 짓자.]</color> ");
                SpawnBuilding(barracksData[4]); //철기 시대 병영
                wizzardCount += 4;
                break;
            case 7:
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [슈퍼유닛 짓자.]</color> ");
                SpawnBuilding(barracksData[5]);
                superUnitCount += 1;
                break;
            case 8:
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [민가 짓자.]</color> ");
                SpawnBuilding(houseData);
                maxHuman += 10;
                break;

        }
    }
    

    protected void SpawnBuilding(BuildingData data) //건물의 정보를 가지고 노드에 추가할 때 쓰는 거임.
    {
        AddBuildingToNode(currentNodeID, data); //현재 노드에 건물 추가
    }

    protected virtual bool AddBuildingToNode(int nodeID, BuildingData buildingData) //건물을 노드에 추가하는 함수임
    {
        Debug.Log("건물 짓는중...");
        if (buildingData == null) return false;

        NodeData node = WorldMapManager.Instance.GetNode(nodeID); //노드 정보 가져오기
        if (node == null) return false;

        Vector3Int pos = FindRandomPlace(buildingData); //랜덤 위치에 생성
        BuildingInstance instance = new BuildingInstance
        {
            data = buildingData, // 건물 데이터 할당
            origin = pos, // 건물의 위치는 나중에 타일맵에서 결정하므로 일단 (0,0,0)으로 초기화
            footprint = new List<Vector3Int>(), 
            ownerCivID = node.ownerCivID, // 건물의 소유자는 노드의 소유자와 동일하게 설정
            visual = null // 시각적 표현은 나중에 타일맵에서 처리하므로 일단 null로 초기화
        };

        node.buildings.Add(instance); // 노드의 건물 리스트에 새 건물 인스턴스 추가

        Debug.Log($"[AI] 노드 {nodeID}에 {buildingData.buildingName} 추가됨");
        return true;
    }

    protected virtual Vector3Int FindRandomPlace(BuildingData data)
    {
        Tilemap cityTilemap = null;
        // 적 자기 노드의 city 타일맵을 소스에서 가져옴
        if (data.buildingType == BuildingType.Farm)
        {
            if(!NodeDataManager.Instance.TryGetFarmlandTilemap(currentNodeID, out cityTilemap))
                return Vector3Int.zero;
        }
        else
        {
            if (!NodeDataManager.Instance.TryGetCityTilemap(currentNodeID, out cityTilemap))
                return Vector3Int.zero;
        }
        cityTilemap.CompressBounds(); // 타일맵의 실제 타일이 있는 영역으로 Bounds를 압축
        BoundsInt bounds = cityTilemap.cellBounds; // 타일맵의 셀 범위를 나타내는 BoundsInt 가져오기

        NodeData node = WorldMapManager.Instance.GetNode(currentNodeID);

        for (int i = 0; i < 50; i++)
        {
            int x = Random.Range(bounds.xMin, bounds.xMax); // Bounds의 xMin과 xMax 사이에서 랜덤한 x 좌표 생성
            int y = Random.Range(bounds.yMin, bounds.yMax); // Bounds의 yMin과 yMax 사이에서 랜덤한 y 좌표 생성
            Vector3Int pos = new Vector3Int(x, y, 0);

            // 1) 이 셀에 city 타일이 실제로 있는가
            if (cityTilemap.GetTile(pos) == null) continue;

            // 2) 건물 footprint 전체가 타일 안에 들어가는가
            bool ok = true;
            for (int dx = 0; dx < data.width && ok; dx++)
                for (int dy = 0; dy < data.height && ok; dy++)
                    if (cityTilemap.GetTile(pos + new Vector3Int(dx, dy, 0)) == null)
                        ok = false;
            if (!ok) continue;

            // 3) 이 노드에 이미 있는 다른 건물과 겹치는지 검사
            if (OverlapsExisting(node, pos, data)) continue;

            return pos;
        }
        return Vector3Int.zero;
    }

    private bool OverlapsExisting(NodeData node, Vector3Int origin, BuildingData data) //건물의 footprint가 노드에 이미 있는 다른 건물과 겹치는지 확인
    {
        foreach (var b in node.buildings) //노드에 있는 건물
        {
            if (b?.data == null) continue; //데이터가 없으면 패스
            for (int x = 0; x < data.width; x++) //새로 지으려는 건물의 폭과 높이만큼 반복
                for (int y = 0; y < data.height; y++) //새로 지으려는 건물의 폭과 높이만큼 반복
                {
                    Vector3Int p = origin + new Vector3Int(x, y, 0); 
                    for (int bx = 0; bx < b.data.width; bx++) //노드에 이미 있는 건물의 폭과 높이만큼 반복
                        for (int by = 0; by < b.data.height; by++) //노드에 이미 있는 건물의 폭과 높이만큼 반복
                            if (p == b.origin + new Vector3Int(bx, by, 0)) //새로 지으려는 건물의 각 타일이 노드에 이미 있는 건물의 각 타일과 겹치는지 검사
                                return true;//겹치는 타일이 하나라도 있으면 true 반환
                }
        }
        return false; //겹치는 타일이 하나도 없으면 false 반환
    }

    //protected void SpawnBuilding(BuildingData data)
    //{
    //    Vector3Int cellPos = GetRandomCellPosition(data);

    //    TileMapManager.Instance.EnemyPlaceBuilding(cellPos, data, this, enemyID);
    //}

    //protected virtual Vector3Int GetRandomCellPosition(BuildingData data) //설치 위치임.
    //{
    //    //적이 건물을 지을 위치를 랜덤으로 결정하는 함수
    //    for (int i = 0; i < 20; i++) // 넉넉히 20번 시도
    //    {
    //        int x = Random.Range(0, 10);
    //        int y = Random.Range(0, 10);

    //        Vector3Int pos = new Vector3Int(x, y, 0);

    //        if (TileMapManager.Instance.CanPlace(pos, data)) //놓을 수 있는 곳이라면?
    //            return pos; //놓을 수 있는 위치 반환 
    //    }

    //    Debug.LogWarning("설치 가능한 위치 못 찾음 염병");
    //    return new Vector3Int(0, 0, 0); // fallback
    //}

    //==============================골드 및 식량 얻기====================================
    protected virtual void GetGold()
    {
        //여기는 각각 Enemy마다 override하는걸로
    }
    //==============================레벨업 조건==========================================
    protected virtual void CheckLevelUp()
    {
        //적의 레벨업 조건 구현
        if (science >= 1000)
        {
            enemyLevel++;
            science = 0; //레벨업 후 과학 초기화
            Debug.Log("<color=green>[레벨업!]</color> ");
        }
    }
    //==============================점령 시도===========================================
    //적이 영지 점령 시도하는 행동 구현
    protected virtual bool TryToOccupy()
    {
        //적이 영지 점령 시도하는 행동 구현
        if (gold > 500)
        {
            return true; //골드가 충분하면 점령 시도
        }
        return false;
    }
    //인접한 노드 탐색
    //조건1: 인접한 노드가 비어있는가. <- 비어있다면 해당 노드에 있는 Brain Active를 킨다. 키면 해당 영지는 본인 것으로 설정.
    //조건2: 인접한 노드가 플레이어의 영지인가. <- 플레이어의 영지라면 전투로 진입.
    //조건3: 인접한 노드가 본인의 영지인가. <- 본인의 영지라면 점령 시도 안함.
    //조건4: 인접한 노드가 다른적의 영지인가. <- 다른 적과 골드와 식량의 총량을 비교후 승리 판단여부.
    //============================================점령 조건==============================================
    protected virtual void TryToOccupyAdjacentNode()
    {
        //인접한 노드 탐색
        List<NodeData> adjacentNodes = WorldMapManager.Instance.GetAdjacentNodes(currentNodeID); //현재 노드의 인접한 노드 리스트 가져오기
        foreach (NodeData adjacentNode in adjacentNodes) //각 인접한 노드에 대해서
        {
            if (adjacentNode.ownerCivID == -1) //조건1: 인접한 노드가 누구의 것도 아니라면
            {
                Debug.Log($"<color=green>[노드 {adjacentNode} 점령 시도]</color> ");
                adjacentNode.ownerCivID = enemyID; //해당 노드를 본인의 영지로 설정
                return;
            }
            else if (adjacentNode.ownerCivID == 0) //조건2: 인접한 노드가 플레이어의 영지인가
            {
                Debug.Log($"<color=red>[노드 {adjacentNode} 공격 시도]</color> ");
                //전투로 진입하는 행동 구현
                return;
            }
            else if (adjacentNode.ownerCivID == enemyID) //조건3: 인접한 노드가 본인의 영지인가
            {
                Debug.Log($"<color=yellow>[노드 {adjacentNode}는 이미 내 영지]</color> ");
                continue; //점령 시도 안함
            }
            else //조건4: 인접한 노드가 다른적의 영지인가
            {
                Debug.Log($"<color=red>[노드 {adjacentNode} 다른 적과 전투 시도]</color> ");
                //다른 적과 골드와 식량의 총량을 비교후 승리 판단여부 행동 구현
                return;
            }
        }
    }


}
