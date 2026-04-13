using UnityEngine;
using EnemyAPool;

public class EnemyA : EnemyBrainBase
{
    //==============================================================================================
    protected override void GetGold() //골드와 식량을 얻는 행동 구현
    {
        faction.AddGold(100);
        faction.AddFood(50);
    }
}