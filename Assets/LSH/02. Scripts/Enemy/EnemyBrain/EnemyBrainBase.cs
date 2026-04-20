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
    [SerializeField] protected BuildingData farmData;
    [Header("상점 건물")]
    [SerializeField] protected BuildingData marketData;
    [Header("민가 건물")]
    [SerializeField] protected BuildingData houseData;
    [Header("병영 건물")] //0번은 석기시대 병영 1번 2번은 청동기 3,4,5번은 철기
    [SerializeField] protected List<BuildingData> barracksData = new List<BuildingData>();
    [Header("병영별 풀 (barracksData와 인덱스 일치)")]
    [SerializeField] protected List<EnemyAPool.EnemyAPoolBase> barracksPools = new List<EnemyAPool.EnemyAPoolBase>();

    [Header("골드 및 과학")]
    [SerializeField] protected int gold;
    [SerializeField] protected int science;

    [Header("시대 레벨")]
    [SerializeField] protected int enemyLevel; //적의 시대를 나타낼 레벨임

    [Header("적의 번호")]
    [SerializeField] public int enemyID; //적의 번호를 나타낼 변수임

    [Header("턴 진행 여부")] //적 종류마다 다르게 설정할꺼임.
    [SerializeField] public int countEnemyTurn; //이건 턴 진행마다 해야할꺼. 예를 들어 턴 3턴마다 영지 점령 시도 이런거.

    public event System.Action OnTurnPassed; //using System을 쓰면 Random쓰기 귀찮음.

    //==============================병력 수 증감 (NodeData.units에 노드별로 기록)====================================
    public virtual void OnUnitSpawned(UnitType type, int nodeID) //유닛 등록하는 용도 카운트 증가
    {
        Debug.Log("<color=cyan>[유닛 카운트 증가!!]</color>");
        NodeData node = WorldMapManager.Instance.GetNode(nodeID); //노드 ID에 있는 노드
        if (node == null) return;
        NodeUnit existing = node.units.Find(u => u.unitType == type);
        if (existing != null)
            existing.count++; //안에 유닛 타입맞는거 증가.
        else
            node.units.Add(new NodeUnit { unitType = type, count = 1 }); 
    }

    public virtual void OnUnitReturned(UnitType type, int nodeID) //유닛 죽었을때
    {
        NodeData node = WorldMapManager.Instance.GetNode(nodeID);
        if (node == null) return;
        NodeUnit existing = node.units.Find(u => u.unitType == type);
        if (existing != null && existing.count > 0)
            existing.count--;
    }
    //====================================================================================
    protected virtual void Awake()
    {
        EnemyBrainManager.Instance.Register(enemyID, this);
    }

    protected virtual void OnEnable()
    {
        //적 행동 초기화
        InitializeActionCases();
        //Debug.Log("{enemyID} 턴 시작");
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
    public virtual void StartEnemyTurn()
    {
        CheckLevelUp();
        UpdateActionCases();

        List<NodeData> owned = WorldMapManager.Instance.GetNodesByCivID(enemyID);
        if (owned.Count == 0)
        {
            //Debug.Log($"<color=gray>[{enemyID}] 소유 노드 없음 — 턴 패스</color>");
            return;
        }

        for (int i = 0; i < 5; i++) //한 턴에 5번 행동
        {
            NodeData target = owned[Random.Range(0, owned.Count)]; //소유 노드 중 랜덤 1개
            int nodeID = target.nodeID;

            EnemyAction action = GetWeightedRandomAction(); //적 행동 확률에 따른 행동 선택
            bool actionCheck = CheckAction(action); //돈없으면 건물 못짓게 하기
            if (!actionCheck)
            {
                //Debug.Log("<color=yellow>[돈이 없어서 강제로 골드]</color> ");
                action = EnemyAction.GetGold; //조건이 안맞으면 골드 얻는 행동으로 강제 변경
            }
            switch (action)
            {
               case EnemyAction.Building: //건물 짓기
                    Debug.Log($"<color=red>[행동 {i+1}] 노드 {nodeID} 건물 짓기 시도</color> ");
                    CheckWhichBuilding(nodeID); //어떤 건물을 지을껀지 판단
                    break;
               case EnemyAction.GetGold: //금과 식량 얻기
                    Debug.Log($"<color=red>[행동 {i+1}] 노드 {nodeID} 골드 얻기</color> ");
                    GetGold();
                    break;
                case EnemyAction.TryOccupy: //영지 점령 시도
                    Debug.Log($"<color=pink>[행동 {i+1}] 노드 {nodeID} 점령 시도</color> ");
                    TryToOccupyAdjacentNode(nodeID);
                    break;
            }
        }
        countEnemyTurn++;
        //Debug.Log("<color=cyan>[턴 이벤트 발생]</color>");
        if (OnTurnPassed != null)
        {
            OnTurnPassed.Invoke();
        }
        else
        {
            // 구독자가 아무도 없으면 이게 찍힙니다.
            Debug.LogWarning("<color=red>[턴 이벤트] 구독 중인 Pool이 없습니다! Init이 되었는지 확인하세요.</color>");
        }
    }

    protected virtual bool CheckAction(EnemyAction action) //행동 조건 검사
    {
        switch (action)
        {
            case EnemyAction.Building:
                return gold >= 300;//골드가 없으면 건물 못 지음
            case EnemyAction.GetGold:
                return true;
            case EnemyAction.TryOccupy:
                return gold >= 300; //영지 점령은 1000골드 이상으로 자주 못하게 설정.
        }
        return false;
    }
    //==============================건물 짓기====================================
    protected virtual void CheckWhichBuilding(int nodeID)
    {
        switch(enemyLevel) //적의 시대에 따른 건물 짓기
        {
            case 1: //석기 시대
                Debug.Log("<color=yellow>[건물 짓자.]</color> ");
                BuildLevelOne(nodeID);
                break;
            case 2: //청동기 시대
                BuildLevelTwo(nodeID);
                break;
            case 3: //철기 시대
                BuildLevelThree(nodeID);
                break;
        }
        gold -= 300;
    }
    protected virtual void BuildLevelOne(int nodeID)
    {
        NodeData node = WorldMapManager.Instance.GetNode(nodeID);
        //적의 석기 시대 건물 짓기 행동 구현
        int buildingChoice = Random.Range(0, 3); //농장, 상점, 병영 중 하나 선택
        if(node.maxHuman <= 100)
        {
            buildingChoice = Random.Range(0, 4); //농장, 상점, 병영, 민가 중 하나 선택
        }
        switch (buildingChoice)
        {
            case 0: //농장 건물
                Debug.Log("<color=yellow>[농장 건물 짓자.]</color> ");
                if(node.farmCount >= 3) //농장 3개 이상이면 농장 안짓게 하기
                {
                    Debug.Log("<color=yellow>[농장 3개 이상이라 농장 안짓자.]</color> ");
                    CheckWhichBuilding(nodeID); //다시 건물 선택
                    return;
                }
                node.farmCount += 1;
                SpawnBuilding(nodeID, farmData);
                break;
            case 1: //상점 건물
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [상점 건물 짓자.]</color> ");
                SpawnBuilding(nodeID, marketData);
                break;
            case 2: //병영 건물
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [병영 짓자.]</color> ");
                SpawnBuilding(nodeID, barracksData[0]); //석기 시대 병영
                if (barracksPools.Count > 0 && barracksPools[0] != null) barracksPools[0].Init(this, nodeID);
                break;
            case 3:
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [민가 짓자.]</color> ");
                SpawnBuilding(nodeID, houseData);
                node.maxHuman += 10;
                break;
        }
    }
    protected virtual void BuildLevelTwo(int nodeID)
    {
        NodeData node = WorldMapManager.Instance.GetNode(nodeID);
        //적의 청동기 시대 건물 짓기 행동 구현
        int buildingChoice = Random.Range(0, 5); //농장, 상점, 병영 중 하나 선택
        if (node.maxHuman <= 100)
        {
            buildingChoice = Random.Range(0, 6); //농장, 상점, 병영, 민가 중 하나 선택
        }
        switch (buildingChoice)
        {
            case 0: //농장 건물
                if (node.farmCount >= 3) //농장 3개 이상이면 농장 안짓게 하기
                {
                    Debug.Log("<color=yellow>[농장 3개 이상이라 농장 안짓자.]</color> ");
                    CheckWhichBuilding(nodeID); //다시 건물 선택
                    return;
                }
                node.farmCount += 1;
                SpawnBuilding(nodeID, farmData);
                break;
            case 1: //상점 건물
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [상점 건물 짓자.]</color> ");
                SpawnBuilding(nodeID, marketData);
                break;
            case 2: //병영 건물
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [아처 짓자.]</color> ");
                SpawnBuilding(nodeID, barracksData[1]); //청동기 시대 병영
                if (barracksPools.Count > 1 && barracksPools[1] != null) barracksPools[1].Init(this, nodeID);
                break;
            case 3: //병영 건물
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [힐러 짓자.]</color> ");
                SpawnBuilding(nodeID, barracksData[2]); //청동기 시대 병영
                if (barracksPools.Count > 2 && barracksPools[2] != null) barracksPools[2].Init(this, nodeID);
                break;
            case 4: //병영 건물
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [우가우가 짓자.]</color> ");
                SpawnBuilding(nodeID, barracksData[0]); //청동기 시대 병영
                if (barracksPools.Count > 0 && barracksPools[0] != null) barracksPools[0].Init(this, nodeID);
                break;
            case 5:
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [민가 짓자.]</color> ");
                SpawnBuilding(nodeID, houseData);
                node.maxHuman += 10;
                break;
        }
    }
    protected virtual void BuildLevelThree(int nodeID)
    {
        NodeData node = WorldMapManager.Instance.GetNode(nodeID);
        //적의 철기 시대 건물 짓기 행동 구현
        int buildingChoice = Random.Range(0, 8); //농장, 상점, 병영 중 하나 선택
        if (node.maxHuman <= 100)
        {
            buildingChoice = Random.Range(0, 9); //농장, 상점, 병영, 민가 중 하나 선택
        }
        switch (buildingChoice)
        {
            case 0: //농장 건물
                if (node.farmCount >= 3) //농장 3개 이상이면 농장 안짓게 하기
                {
                    Debug.Log("<color=yellow>[농장 3개 이상이라 농장 안짓자.]</color> ");
                    CheckWhichBuilding(nodeID); //다시 건물 선택
                    return;
                }
                node.farmCount += 1;
                SpawnBuilding(nodeID, farmData);
                break;
            case 1: //상점 건물
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [상점 짓자.]</color> ");
                SpawnBuilding(nodeID, marketData);
                break;
            case 2: //병영 건물
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [아처 짓자.]</color> ");
                SpawnBuilding(nodeID, barracksData[1]); //청동기 시대 병영
                if (barracksPools.Count > 1 && barracksPools[1] != null) barracksPools[1].Init(this, nodeID);
                break;
            case 3: //병영 건물
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [힐러 짓자.]</color> ");
                SpawnBuilding(nodeID, barracksData[2]); //청동기 시대 병영
                if (barracksPools.Count > 2 && barracksPools[2] != null) barracksPools[2].Init(this, nodeID);
                break;
            case 4: //병영 건물
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [우가우가 짓자.]</color> ");
                SpawnBuilding(nodeID, barracksData[0]); //청동기 시대 병영
                if (barracksPools.Count > 0 && barracksPools[0] != null) barracksPools[0].Init(this, nodeID);
                break;
            case 5:
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [기마병 짓자.]</color> ");
                SpawnBuilding(nodeID, barracksData[3]); //철기 시대 병영
                if (barracksPools.Count > 3 && barracksPools[3] != null) barracksPools[3].Init(this, nodeID);
                break;
            case 6:
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [마법사 짓자.]</color> ");
                SpawnBuilding(nodeID, barracksData[4]); //철기 시대 병영
                if (barracksPools.Count > 4 && barracksPools[4] != null) barracksPools[4].Init(this, nodeID);
                break;
            case 7:
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [슈퍼유닛 짓자.]</color> ");
                SpawnBuilding(nodeID, barracksData[5]);
                if (barracksPools.Count > 5 && barracksPools[5] != null) barracksPools[5].Init(this, nodeID);
                break;
            case 8:
                Debug.Log($"<color=yellow>{enemyLevel}<-적 레벨 [민가 짓자.]</color> ");
                SpawnBuilding(nodeID, houseData);
                node.maxHuman += 10;
                break;
        }
    }


    protected void SpawnBuilding(int nodeID, BuildingData data) //건물의 정보를 가지고 노드에 추가할 때 쓰는 거임.
    {
        AddBuildingToNode(nodeID, data);
    }

    protected virtual bool AddBuildingToNode(int nodeID, BuildingData buildingData) //건물을 노드에 추가하는 함수임
    {
        Debug.Log("건물 짓는중...");
        if (buildingData == null) return false;

        NodeData node = WorldMapManager.Instance.GetNode(nodeID); //노드 정보 가져오기
        if (node == null) return false;

        Vector3Int pos = FindRandomPlace(nodeID, buildingData); //랜덤 위치에 생성
        if (pos == Vector3Int.zero) //유효한 자리 못 찾으면 배치 취소 (맵 원점 배치 방지)
        {
            Debug.Log($"<color=orange>[AI] 노드 {nodeID}에 {buildingData.buildingName} 배치 실패 — 유효 위치 없음</color>");
            return false;
        }
        BuildingInstance instance = new BuildingInstance
        {
            data = buildingData,
            origin = pos,
            footprint = new List<Vector3Int>(),
            ownerCivID = node.ownerCivID,
            visual = null
        };
        if (buildingData.visualPrefab == null)
        {
            Debug.LogWarning($"<color=orange>[AI] {buildingData.buildingName}의 visualPrefab이 null — 인스턴스화 스킵</color>");
            node.buildings.Add(instance);
            return true;
        }
        GameObject build = Instantiate(buildingData.visualPrefab, Vector3.zero, Quaternion.identity);
        // (위치는 pos를 월드 좌표로 변환해서 세팅해야 함)
        instance.visual = build;

        var poolBase = build.GetComponent<EnemyAPool.EnemyAPoolBase>();
        if (poolBase != null)
        {
            poolBase.Init(this, nodeID);
            Debug.Log($"<color=cyan>[신규 병영] {buildingData.buildingName} 구독 완료</color>");
        }
        node.buildings.Add(instance); // 노드의 건물 리스트에 새 건물 인스턴스 추가

        Debug.Log($"[AI] 노드 {nodeID}에 {buildingData.buildingName} 추가됨 (cell {pos.x},{pos.y})");
        return true;
    }

    protected virtual Vector3Int FindRandomPlace(int nodeID, BuildingData data)
    {
        Tilemap cityTilemap = null;
        // 적 자기 노드의 city 타일맵을 소스에서 가져옴
        if (data.buildingType == BuildingType.Farm)
        {
            if(!NodeDataManager.Instance.TryGetFarmlandTilemap(nodeID, out cityTilemap))
                return Vector3Int.zero;
        }
        else
        {
            if (!NodeDataManager.Instance.TryGetCityTilemap(nodeID, out cityTilemap))
                return Vector3Int.zero;
        }
        cityTilemap.CompressBounds(); // 타일맵의 실제 타일이 있는 영역으로 Bounds를 압축
        BoundsInt bounds = cityTilemap.cellBounds; // 타일맵의 셀 범위를 나타내는 BoundsInt 가져오기

        NodeData node = WorldMapManager.Instance.GetNode(nodeID);

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
    protected virtual void TryToOccupyAdjacentNode(int nodeID)
    {
        //인접한 노드 탐색
        List<NodeData> adjacentNodes = WorldMapManager.Instance.GetAdjacentNodes(nodeID); //현재 노드의 인접한 노드 리스트 가져오기
        foreach (NodeData adjacentNode in adjacentNodes) //각 인접한 노드에 대해서
        {
            if (adjacentNode.ownerCivID == -1) //조건1: 인접한 노드가 누구의 것도 아니라면
            {
                adjacentNode.ownerCivID = enemyID; //해당 노드를 본인의 영지로 설정
                return;
            }
            else if (adjacentNode.ownerCivID == 0) //조건2: 인접한 노드가 플레이어의 영지인가
            {
                //전투로 진입하는 행동 구현
                return;
            }
            else if (adjacentNode.ownerCivID == enemyID) //조건3: 인접한 노드가 본인의 영지인가
            {
                continue; //점령 시도 안함
            }
            else //조건4: 인접한 노드가 다른적의 영지인가
            {
                //다른 적과 골드와 식량의 총량을 비교후 승리 판단여부 행동 구현
                return;
            }
        }
    }
}
