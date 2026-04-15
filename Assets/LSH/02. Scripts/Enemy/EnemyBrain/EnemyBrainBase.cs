using EnemyAPool;
using PoolBase;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyAction
{
    Building, //시대에 따른 건물 짓기를 다르게 할 것
    GetGold, //골드나 식량을 동시에 얻을꺼임.
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
    [Header("병영 건물")] //0번은 석기시대 병영 1번 2번은 청동기 3,4,5번은 철기
    //[SerializeField] protected List<GameObject> BarracksPrefabs = new List<GameObject>();
    [SerializeField] protected List<BuildingData> barracksData = new List<BuildingData>();

    [Header("골드 및 식량")]
    [SerializeField] protected int gold;
    [SerializeField] protected int food;

    [Header("시대 레벨")]
    [SerializeField] protected int enemyLevel; //적의 시대를 나타낼 레벨임

    [Header("적의 번호")]
    [SerializeField] public int enemyID; //적의 번호를 나타낼 변수임

    [Header("턴 진행 여부")] //적 종류마다 다르게 설정할꺼임.
    [SerializeField] protected int countEnemyTurn; //이건 턴 진행마다 해야할꺼. 예를 들어 턴 3턴마다 영지 점령 시도 이런거.


    public event System.Action OnTurnPassed; //using System을 쓰면 Random쓰기 귀찮음. 
    protected virtual void OnEnable()
    {
        //적 행동 초기화
        InitializeActionCases();
        Debug.Log("{enemyID} 턴 시작");
    }
    //=============================임시 턴 시작 함수==============================
    protected virtual void Update()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            StartEnemyTurn();
        }
    }

    //=============================적 행동 확률====================================

    protected void InitializeActionCases()
    {
        actionCases.Clear();//일단 비워주고
        enemyLevel = 1;
        //적 행동 확률 초기화
        actionCases.Add(new ActionCases { action = EnemyAction.Building, state = EnemyState.Defend, weight = 50 });
        actionCases.Add(new ActionCases { action = EnemyAction.GetGold, state = EnemyState.Defend, weight = 50 });
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
        EnemyAction action = GetWeightedRandomAction(); //적 행동 확률에 따른 행동 선택
        bool actionCheck = CheckAction(action); //돈없으면 건물 못짓게 하기
        if (!actionCheck)
        {
            Debug.Log("<color=yellow>[돈이 없어서 강제로 골드]</color> ");
            action = EnemyAction.GetGold; //조건이 안맞으면 골드 얻는 행동으로 강제 변경
        }
        switch (action) //일단 50대 50임
        {
           case EnemyAction.Building: //건물 짓기
                Debug.Log("<color=red>[건물 짓기 시도]</color> ");
                CheckWhichBuilding(); //어떤 건물을 지을껀지 판단
                break;
           case EnemyAction.GetGold: //금과 식량 얻기
                Debug.Log("<color=red>[돈이 없어서...]</color> ");
                GetGold();
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
    }
    protected virtual void BuildLevelOne()
    {
        //적의 석기 시대 건물 짓기 행동 구현
        int buildingChoice = Random.Range(0, 3); //농장, 상점, 병영 중 하나 선택
        switch (buildingChoice)
        {
            case 0: //농장 건물
                Debug.Log("<color=yellow>[농장 건물 짓자.]</color> ");
                //SpawnBuilding(FarmPrefabs);
                SpawnBuilding(farmData);
                break;
            case 1: //상점 건물
                Debug.Log("<color=yellow>[상점 건물 짓자.]</color> ");
                //Instantiate(MarketPrefabs, GetRandomPosition(), Quaternion.identity);
                SpawnBuilding(marketData);
                break;
            case 2: //병영 건물
                Debug.Log("<color=yellow>[병영 짓자.]</color> ");
                SpawnBuilding(barracksData[0]); //석기 시대 병영
                break;
        }
    }
    protected virtual void BuildLevelTwo()
    {
        //적의 청동기 시대 건물 짓기 행동 구현
        int buildingChoice = Random.Range(0, 3); //농장, 상점, 병영 중 하나 선택
        switch (buildingChoice)
        {
            case 0: //농장 건물
                //Instantiate(FarmPrefabs, GetRandomPosition(), Quaternion.identity);
                SpawnBuilding(farmData);
                break;
            case 1: //상점 건물
                //Instantiate(MarketPrefabs, GetRandomPosition(), Quaternion.identity);
                SpawnBuilding(marketData);
                break;
            case 2: //병영 건물
                //Instantiate(BarracksPrefabs[Random.Range(1, 3)], GetRandomPosition(), Quaternion.identity); //청동기 시대 병영
                SpawnBuilding(barracksData[Random.Range(1, 3)]); //청동기 시대 병영
                break;
        }
    }
    protected virtual void BuildLevelThree()
    {
        //적의 철기 시대 건물 짓기 행동 구현
        int buildingChoice = Random.Range(0, 3); //농장, 상점, 병영 중 하나 선택
        switch (buildingChoice)
        {
            case 0: //농장 건물
                //Instantiate(FarmPrefabs, GetRandomPosition(), Quaternion.identity);
                SpawnBuilding(farmData);
                break;
            case 1: //상점 건물
                //Instantiate(MarketPrefabs, GetRandomPosition(), Quaternion.identity);
                SpawnBuilding(marketData);
                break;
            case 2: //병영 건물
                //Instantiate(BarracksPrefabs[Random.Range(3, 6)], GetRandomPosition(), Quaternion.identity); //철기 시대 병영
                SpawnBuilding(barracksData[Random.Range(3, 6)]); //철기 시대 병영
                break;
        }
    }
    protected virtual Vector3Int GetRandomCellPosition(BuildingData data) //설치 위치임.
    {
        //적이 건물을 지을 위치를 랜덤으로 결정하는 함수
        for (int i = 0; i < 20; i++) // 넉넉히 20번 시도
        {
            int x = Random.Range(0, 10);
            int y = Random.Range(0, 10);

            Vector3Int pos = new Vector3Int(x, y, 0);

            if (TileMapManager.Instance.CanPlace(pos, data)) //놓을 수 있는 곳이라면?
                return pos; //놓을 수 있는 위치 반환 
        }

        Debug.LogWarning("설치 가능한 위치 못 찾음 염병");
        return new Vector3Int(0, 0, 0); // fallback
    }

    protected void SpawnBuilding(BuildingData data)
    {
        Vector3Int cellPos = GetRandomCellPosition(data);

        TileMapManager.Instance.EnemyPlaceBuilding(cellPos, data, this, enemyID);
    }
    
    //protected GameObject SpawnBuilding(GameObject prefab)
    //{
    //    GameObject building = Instantiate(prefab, GetRandomPosition(), Quaternion.identity);

    //    var enemyBuilding = building.GetComponent<IEnemyBuilding>();
    //    if (enemyBuilding != null)
    //    {
    //        enemyBuilding.Init(this);
    //    }

    //    return building;
    //}
    //==============================골드 및 식량 얻기====================================
    protected virtual void GetGold()
    {
        //여기는 각각 Enemy마다 override하는걸로
    }
    //==============================점령 시도===========================================
    //적이 영지 점령 시도하는 행동 구현
    protected virtual bool TryToOccupy()
    {
        //적이 영지 점령 시도하는 행동 구현
        if (food > 200)
        {
            return true; //식량이 충분하면 점령 시도
        }
        return false;
    }
    //인접한 노드 탐색
    //조건1: 인접한 노드가 비어있는가. <- 비어있다면 해당 노드에 있는 Brain Active를 킨다. 키면 해당 영지는 본인 것으로 설정.
    //조건2: 인접한 노드가 플레이어의 영지인가. <- 플레이어의 영지라면 전투로 진입.
    //조건3: 인접한 노드가 본인의 영지인가. <- 본인의 영지라면 점령 시도 안함.
    //조건4: 인접한 노드가 다른적의 영지인가. <- 다른 적과 골드와 식량의 총량을 비교후 승리 판단여부.
}
