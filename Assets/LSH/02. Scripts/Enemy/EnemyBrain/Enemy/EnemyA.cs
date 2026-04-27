using UnityEngine;
using EnemyAPool;
using Unity.VisualScripting;

public class EnemyA : EnemyBrainBase //1. 골드가 많음 2. 카드 갯수 많음 3. 시대발전 느림. 4. 전투 유지 5. 사실상 가장 강한 적. 6. 후반캐리형
{ //전투트리 Enemy
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
    protected override void AddFightAction() //초기 행동 확률 조정
    {
        AddActionCase(EnemyAction.Building, 40); //건물40프로
        AddActionCase(EnemyAction.GetGold, 40); //골드 40프로
        AddActionCase(EnemyAction.TryOccupy, 10); //영지 10프로
        AddActionCase(EnemyAction.Rest, 10); //휴식 10프로
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
            actionCases[0].weight = 30; //건물 짓기 확률 60%
            actionCases[1].weight = 0; //건물 위주로 하기 위해 골드 얻기 x
            actionCases[2].weight = 60; //영지 점령 시도 확률 40%
            actionCases[3].weight = 10; 
            cardCount = 5;
        }
    }
    //================================레벨업 조건==========================================
    protected override void CheckLevelUp() //적 레벨업 조건 구현
    {
        //적의 레벨업 조건 구현
        switch (enemyLevel)
        {
            case 1:
                if (countEnemyTurn >= 25) //10턴마다 레벨업
                {
                    enemyLevel = 2;
                    Debug.Log($"<color=green>[레벨업!!] 적 레벨이 {enemyLevel}로 상승했습니다.</color>");
                }
                break;
            case 2:
                if (countEnemyTurn >= 40) //20턴마다 레벨업
                {
                    enemyLevel = 3;
                    Debug.Log($"<color=green>[레벨업!!] 적 레벨이 {enemyLevel}로 상승했습니다.</color>");
                }
                break;
        }
        return;
    }
    //==============================================================================================
    protected override void GetGold() //골드와 시대점수를 얻는 행동 구현
    {
        switch (enemyLevel) //시대발전은 느리게 후반 골드는 많이
        {
            case 1:
                gold += 1000;
                science += 50;
                food += 10;
                break;
            case 2:
                gold += 5000;
                science += 100;
                food += 15;
                break;
            case 3:
                gold += 1000;
                food += 20;
                break;
            default:
                gold += 1000;
                science += 50;
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
