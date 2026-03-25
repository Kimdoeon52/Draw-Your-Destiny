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
public class EnemyA : MonoBehaviour
{
    [Header("적 골드 개념")]
    int gold = 0;

    [Header("행동 확률")]
    [SerializeField] private List<ActionCase> actionCases = new List<ActionCase>();

    bool isMyTurn;

    private void Start()
    {
        isMyTurn = false;
        AddTurnList();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) //실험용
        {
            StartEnemyTurn();
        }
    }
    void AddTurnList() //나중에 추가할꺼 있으면 여기
    {
        actionCases.Add(new ActionCase { actionType = EnemyActionType.Attack, weight = 5 });
        actionCases.Add(new ActionCase { actionType = EnemyActionType.Building, weight = 20 });
        actionCases.Add(new ActionCase { actionType = EnemyActionType.GetGold, weight = 40 });
        actionCases.Add(new ActionCase { actionType = EnemyActionType.GetHuman, weight = 35 }); 
    }
    public void StartEnemyTurn()
    {
        isMyTurn = true;
        Debug.Log("적 턴 시작");

        ExecuteRandomAction();
    }

    private void ExecuteRandomAction()
    {
        EnemyActionType enemyAction = GetWeightedRandomAction();
        CheckAction(enemyAction); //조건 검사하는거임
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
                DoGetHuman();
                break;
        }
        EndEnemyTurn();
    }

    private EnemyActionType GetWeightedRandomAction()
    {
        int totalWeight = 0;

        foreach (var actionCase in actionCases)
        {
            totalWeight += actionCase.weight;
        }

        int randomValue = Random.Range(0, totalWeight); //0부터 99까지의 랜덤 값임 이게
        int currentWeight = 0;

        foreach (var actionCase in actionCases) //가중치 확률로 행동 선택하는 부분
        {
            currentWeight += actionCase.weight;

            if (randomValue < currentWeight)
            {
                return actionCase.actionType;
            }
        }

        return EnemyActionType.GetHuman;
    }

    private void DoAttack()
    {
        Debug.Log($"적이 공격을 선택했다!");
    }


    private void DoBuilding()
    {
        Debug.Log($"적이 건물을 짓는다람쥐");
    }
    private void DoGetGold()
    {
        gold += 10;
        Debug.Log($"<color=blue>[골드 얻음]</color> 현재 골드: {gold}");
    }
    private void DoWait()
    {
        Debug.Log("한조 대기중.");
    }
    private void DoGetHuman()
    {
        Debug.Log($"인간 뽑는중");
    }
    private void EndEnemyTurn() //턴종료 관련된건 여기에 때려박기
    {
        isMyTurn = false;
        Debug.Log("적 턴 종료");
    }

    private bool CheckAction(EnemyActionType action) //행동 검사 조건 넣기
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
