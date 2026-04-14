using UnityEngine;
using EnemyAPool;

public class EnemyA : EnemyBrainBase
{
    protected override void OnEnable()
    {
        base.OnEnable();
        enemyID = 1;
    }
    //==============================================================================================
    protected override void GetGold() //골드와 식량을 얻는 행동 구현
    {
        gold += 100;
        food += 50;
    }
    protected override void StartEnemyTurn()
    {
        base.StartEnemyTurn();
    }
}