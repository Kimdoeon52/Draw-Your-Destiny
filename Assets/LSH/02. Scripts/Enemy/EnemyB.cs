using System.Collections.Generic;
using UnityEngine;


public class EnemyB : EnemyOrigin
{

    protected override void Start()
    {
        enemyID = 2;
        base.Start();
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.E)) 
        {
            ExecuteRandomAction();
        }
    }


    protected override void ExecuteRandomAction()
    {
        base.ExecuteRandomAction();
        Debug.Log("적 B의 행동 실행");

        //foreach (var actionCase in actionCases) //가중치 확률로 행동 선택하는 부분
        //{
        //    currentWeight += actionCase.weight;

        //    if (randomValue < currentWeight)
        //    {
        //        return actionCase.actionType;
        //    }
        //}
    }

    protected override void DoGetEnemyHuman()
    {
        base.DoGetEnemyHuman();
    }
}
