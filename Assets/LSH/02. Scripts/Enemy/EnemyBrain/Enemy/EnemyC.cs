using UnityEngine;

public class EnemyC : EnemyBrainBase //1. 무난 
{//전투트리Enemy
    protected override void Awake()
    {
        enemyID = 3;
        enemyLevel = 1;
        cardCount = 2;
        base.Awake();
    }
    protected override void OnEnable()
    {
        base.OnEnable();
    }
    protected override void GetGold() //골드와 시대점수를 얻는 행동 구현
    {
        switch (enemyLevel)
        {
            case 1:
                gold += 500;
                science += 100;
                break;
            case 2:
                gold += 750;
                science += 300;
                break;
            case 3:
                gold += 1000;
                break;
            default:
                gold += 1000;
                science += 50;
                break;
        }
    }
    //===================================Enemy별 행동 확률 조정==========================================
    protected override void AddFightAction() //초기 행동 확률 조정
    {
        AddActionCase(EnemyAction.Building, 40); //건물40프로
        AddActionCase(EnemyAction.GetGold, 30); //골드 30프로
        AddActionCase(EnemyAction.TryOccupy, 10); //영지 10프로
        AddActionCase(EnemyAction.Rest, 20); //휴식 20프로
    }
    protected override void UpdateActionCases() //적 행동 확률 업데이트하는 함수임. 예를 들어 레벨업하면 건물 짓는 행동 확률이 올라가는 식으로.
    {
        //적 행동 확률 업데이트 구현
        if (enemyLevel == 2)
        {
            actionCases[0].weight = 40; //건물 짓기 확률 40%
            actionCases[1].weight = 20; //골드 얻기 확률 20%
            actionCases[2].weight = 30; //영지 점령 시도 확률 30%
            actionCases[3].weight = 10;
        }
        else if (enemyLevel == 3) 
        {
            actionCases[0].weight = 50; //건물 짓기 확률 50%
            actionCases[1].weight = 10; //건물 위주로 하기 위해 골드 얻기 x
            actionCases[2].weight = 30; //영지 점령 시도 확률 30%
            actionCases[3].weight = 10;
            cardCount = 5;
        }
    }
    //==============================================레벨업 조건==========================================
    protected override void CheckLevelUp() //적 레벨업 조건 구현
    {
        //적의 레벨업 조건 구현
        switch (enemyLevel)
        {
            case 1:
                if (countEnemyTurn >= 20) //10턴마다 레벨업
                {
                    enemyLevel = 2;
                    Debug.Log($"<color=green>[레벨업!!] 적 레벨이 {enemyLevel}로 상승했습니다.</color>");
                }
                break;
            case 2:
                if (countEnemyTurn >= 35) //20턴마다 레벨업
                {
                    enemyLevel = 3;
                    Debug.Log($"<color=green>[레벨업!!] 적 레벨이 {enemyLevel}로 상승했습니다.</color>");
                }
                break;
        }
        return;
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
