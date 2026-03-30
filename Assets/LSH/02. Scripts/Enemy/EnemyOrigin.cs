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
    [Header("적 골드 개념")]
    int gold = 0;

    [Header("행동 확률")]
    [SerializeField] private List<ActionCase> actionCases = new List<ActionCase>();

    [SerializeField] protected int enemyID;
    [SerializeField] protected EnemyUnitPool enemyUnitPool;
    protected virtual void Start()
    {
        AddTurnList();
        GameStartEnemyInfo();
        enemyUnitPool=transform.parent.transform.GetComponentInChildren<EnemyUnitPool>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) //실험용
        {
            StartEnemyTurn();
        }
    }

    protected virtual void GameStartEnemyInfo()//게임 시작시 적 정보들.
    {

    }
    protected virtual void AddTurnList() //나중에 추가할꺼 있으면 여기
    {
        actionCases.Add(new ActionCase { actionType = EnemyActionType.Attack, weight = 5 });
        actionCases.Add(new ActionCase { actionType = EnemyActionType.Building, weight = 20 });
        actionCases.Add(new ActionCase { actionType = EnemyActionType.GetGold, weight = 40 });
        actionCases.Add(new ActionCase { actionType = EnemyActionType.GetHuman, weight = 35 });
    }
    //--------------------턴 시작과 종료----------------------
    public virtual void StartEnemyTurn()
    {
        Debug.Log("적 턴 시작");
        ExecuteRandomAction();
    }
    protected virtual void EndEnemyTurn() //턴종료 관련된건 여기에 때려박기
    {
        Debug.Log("적 턴 종료");
    }

    //---------------------행동 실행----------------------
    protected virtual void ExecuteRandomAction()
    {
        EnemyActionType enemyAction = GetWeightedRandomAction();
        bool actionCheck = CheckAction(enemyAction); //조건 검사하는거임
        if (!actionCheck)
        {
            enemyAction = EnemyActionType.GetGold;
            Debug.Log($"<color=yellow>[적은 지금 거지에요]</color> 골드 부족으로 인해 강제적으로 골드 턴.");
        }
        Debug.Log($"<color=yellow>[Enemy Turn]</color> 행동: {enemyAction}");
        switch (enemyAction) // 해당 액션 실행 나중에 조전 필요하면 각 함수를 수정할것 아니면 위에꺼로
        {
            case EnemyActionType.Attack:
                DoAttack();
                break;

            case EnemyActionType.Building:
                DoBuilding();
                break;

            case EnemyActionType.GetGold:
                DoGetGold();
                break;

            case EnemyActionType.GetHuman:
                DoGetEnemyHuman();
                break;
        }
        EndEnemyTurn();
    }

    protected virtual EnemyActionType GetWeightedRandomAction()
    {
        int totalWeight = 0;

        foreach (var actionCase in actionCases)
        {
            totalWeight += actionCase.weight;
        }

        int randomValue = Random.Range(0, totalWeight); //0부터 99까지의 랜덤 값임 이게
        int currentWeight = 0;

        return EnemyActionType.GetHuman;
    }

    //--------------------각 기본 행동들-------------------

    protected virtual void DoAttack()
    {
        Debug.Log($"적이 공격을 선택했다!");
    }

    //-------------------행동들-------------------
    protected virtual void DoBuilding()
    {
        //메인 건물 근처에 건물을 생성
        //TileMapManager.Instance.PlaceBuilding(transform.gameObject, ,enemyID);
        Debug.Log($"적이 건물을 짓는다람쥐");
    }

    protected virtual void DoGetGold()
    {
        gold += 10;
        Debug.Log($"<color=blue>[골드 얻음]</color> 현재 골드: {gold}");
    }
    protected virtual void DoWait()
    {
        Debug.Log("한조 대기중.");
    }
    protected virtual void DoGetEnemyHuman(/*EnemyType enemyType*/)
    {
        enemyUnitPool.GetEnemyUnit();
        Debug.Log($"인간 뽑는중");
    }

    //-------------------특수 행동--------------------------------
    protected virtual void BuildSmallTown() //영지 건설용도
    {
        //순서0 제일 가까운 광산의 위치를 탐색.
        //순서1 유닛을 광산 위치로 움직인다.
        //순서2 영지을 설치한다.
        //TileMapManager.Instance.PlaceBuilding(위치 pos, House 같은 빌딩 타입, 적 id 이거는 1로 할꺼임)
    }

    //----------------행동검사------------------
    protected virtual bool CheckAction(EnemyActionType action) //행동 검사 조건 넣기
    {
        switch (action)
        {
            case EnemyActionType.Attack:
                return true;

            case EnemyActionType.Building:
                return gold >= 50;

            case EnemyActionType.GetGold:
                return true;

            case EnemyActionType.GetHuman:
                return gold >= 10;
        }

        return false;
    }
}
