using UnityEngine;

public class EnemyB : EnemyBrainBase //1. 골드가 많음 2. 시대발전 빠름 3. 방어위주 점령 시도 잘안함 4. 건물 많이 지어서 유닛 갯수가 많아짐 //5. 초반 러너.
{//경제트리 Enemy
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
    //===================================Enemy별 행동 확률 조정==========================================
    protected override void AddMoneyAction() //초기 행동 확률 조정
    {
        AddActionCase(EnemyAction.Building, 35); //건물35프로
        AddActionCase(EnemyAction.GetGold, 40); //골드 40프로
        AddActionCase(EnemyAction.TryOccupy, 5); //영지 5프로
        AddActionCase(EnemyAction.Rest, 20); //휴식 20프로
    }
    protected override void UpdateActionCases() //적 행동 확률 업데이트하는 함수임. 예를 들어 레벨업하면 건물 짓는 행동 확률이 올라가는 식으로.
    {
        //적 행동 확률 업데이트 구현
        if (enemyLevel == 2)
        {
            actionCases[0].weight = 60; //건물 짓기 확률 40%
            actionCases[1].weight = 30; //골드 얻기 확률 40%
            actionCases[2].weight = 10; //영지 점령 시도 확률 10%
        }
        else if (enemyLevel == 3) //공격적으로 변함
        {
            actionCases[0].weight = 80; //건물 짓기 확률 60%
            actionCases[1].weight = 10; //건물 위주로 하기 위해 골드 얻기 x
            actionCases[2].weight = 10; //영지 점령 시도 확률 10%
        }
    }
    //=================================================================================================
    protected override void GetGold() //골드와 시대점수를 얻는 행동 구현
    {
        switch (enemyLevel) //시대 발전은 빠르게 골드도 많이
        {
            case 1:
                gold += 50;
                science += 100;
                break;
            case 2:
                gold += 75;
                science += 300;
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
    //=============================================레벨업 조건========================================== 

    protected override void CheckLevelUp() //적 레벨업 조건 구현
    {
        //적의 레벨업 조건 구현
        switch (enemyLevel)
        {
            case 1:
                if (countEnemyTurn >= 15) //10턴마다 레벨업
                {
                    enemyLevel = 2;
                    Debug.Log($"<color=green>[레벨업!!] 적 레벨이 {enemyLevel}로 상승했습니다.</color>");
                }
                break;
            case 2:
                if (countEnemyTurn >= 30) //20턴마다 레벨업
                {
                    enemyLevel = 3;
                    Debug.Log($"<color=green>[레벨업!!] 적 레벨이 {enemyLevel}로 상승했습니다.</color>");
                }
                break;
        }
        return;
    }
    //=================================================================================================
    protected override void DefineEnemyState()
    {
        enemyState = EnemyState.Money; //경제 트리로 설정
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
