using UnityEngine;
using EnemyAPool;
using Unity.VisualScripting;

public class EnemyA : EnemyBrainBase //1. 골드가 많음 2. 카드 갯수 많음 3. 시대발전 느림. 4. 전투 유지 5. 사실상 가장 강한 적. 6. 후반캐리형
{
    protected override void Awake()
    {
        enemyID = 1;
        enemyLevel = 1;
        cardCount = 2;
        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }
    //===================================Enemy별 행동 확률 조정==========================================
    protected override void InitializeActionCases()
    {
        actionCases.Clear();//일단 비워주고
        enemyLevel = 1;
        //적 행동 확률 초기화
        actionCases.Add(new ActionCases { action = EnemyAction.Building, state = EnemyState.Defend, weight = 35 });
        actionCases.Add(new ActionCases { action = EnemyAction.GetGold, state = EnemyState.Defend, weight = 40 });
        actionCases.Add(new ActionCases { action = EnemyAction.TryOccupy, state = EnemyState.Attack, weight = 5 });
        actionCases.Add(new ActionCases { action = EnemyAction.Rest, state = EnemyState.Defend, weight = 20 });
    }
    protected override void UpdateActionCases() //적 행동 확률 업데이트하는 함수임. 예를 들어 레벨업하면 건물 짓는 행동 확률이 올라가는 식으로.
    {
        //적 행동 확률 업데이트 구현
        if (enemyLevel == 2)
        {
            actionCases[0].weight = 60; //건물 짓기 확률 40%
            actionCases[1].weight = 10; //골드 얻기 확률 40%
            actionCases[2].weight = 10; //영지 점령 시도 확률 30%
            actionCases[3].weight = 20;
            cardCount = 3;
        }
        else if (enemyLevel == 3) //공격적으로 변함.
        {
            actionCases[0].weight = 60; //건물 짓기 확률 60%
            actionCases[1].weight = 0; //건물 위주로 하기 위해 골드 얻기 x
            actionCases[2].weight = 30; //영지 점령 시도 확률 40%
            actionCases[3].weight = 10;
            cardCount = 5;
        }
    }
    //=================================================================================================

    //==============================================================================================
    protected override void GetGold() //골드와 시대점수를 얻는 행동 구현
    {
        switch (enemyLevel) //시대발전은 느리게 후반 골드는 많이
        {
            case 1:
                gold += 30;
                science += 50;
                break;
            case 2:
                gold += 50;
                science += 100;
                break;
            case 3:
                gold += 100;
                break;
            default:
                gold += 100;
                science += 50;
                break;
        }
    }
    protected override void CheckLevelUp()
    {
        switch (enemyLevel)
        {
            case 1:
                if (science >= 1000)
                {
                    enemyLevel = 2;
                    Debug.Log("EnemyA가 레벨업했습니다! 현재 레벨: " + enemyLevel);
                }
                break;
            case 2:
                if (science >= 5000)
                {
                    enemyLevel = 3;
                    Debug.Log("EnemyA가 레벨업했습니다! 현재 레벨: " + enemyLevel);
                }
                break;
        }
    }
    public override void StartEnemyTurn()
    {
        base.StartEnemyTurn();
    }
    protected override Vector3Int FindRandomPlace(int nodeID, BuildingData data)
    {
        return base.FindRandomPlace(nodeID, data);
    }
}
