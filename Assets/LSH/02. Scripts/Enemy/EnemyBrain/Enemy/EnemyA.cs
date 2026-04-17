using UnityEngine;
using EnemyAPool;
using Unity.VisualScripting;

public class EnemyA : EnemyBrainBase
{
    protected override void Awake()
    {
        enemyID = 1;
        enemyLevel = 1;
        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }
    //==============================================================================================
    protected override void GetGold() //골드와 식량을 얻는 행동 구현
    {
        switch (enemyLevel)
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
    protected override void StartEnemyTurn()
    {
        base.StartEnemyTurn();
    }
    protected override Vector3Int FindRandomPlace(int nodeID, BuildingData data)
    {
        return base.FindRandomPlace(nodeID, data);
    }
}
