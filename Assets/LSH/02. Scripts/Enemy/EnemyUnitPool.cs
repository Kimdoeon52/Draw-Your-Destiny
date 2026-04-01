using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    A,
    B,
    C
}

public class EnemyUnitPool : MonoBehaviour
{
    [Header("0:A, 1:B, 2:C")]
    public GameObject[] enemyUnitPrefabs;

    public Transform poolParent;
    public int poolSize = 20;

    private Dictionary<EnemyType, Queue<GameObject>> pools = new Dictionary<EnemyType, Queue<GameObject>>();

    void Start()
    {
        for (int i = 0; i < enemyUnitPrefabs.Length; i++)
        {
            EnemyType type = (EnemyType)i;
            pools[type] = new Queue<GameObject>();

            for (int j = 0; j < poolSize; j++)
            {
                GameObject enemy = Instantiate(enemyUnitPrefabs[i], poolParent);
                enemy.SetActive(false);

                EnemyUnit enemyUnit = enemy.GetComponent<EnemyUnit>();
                if (enemyUnit == null)
                {
                    Debug.LogError($"{enemy.name} 에 EnemyUnit 스크립트가 없습니다.");
                    continue;
                }

                enemyUnit.enemyType = type;
                enemyUnit.enemyPool = this;

                pools[type].Enqueue(enemy);
            }
        }
    }

    public GameObject GetEnemyUnit(EnemyType type, int ownerCivID)
    {
        if (!pools.ContainsKey(type) || pools[type].Count == 0)
        {
            Debug.LogWarning($"{type} 풀에 남은 적이 없습니다.");
            return null;
        }

        GameObject enemy = pools[type].Dequeue();
        enemy.SetActive(true);
        EnemyUnit enemyUnit = enemy.GetComponent<EnemyUnit>();
        if (enemyUnit != null)
        {
            enemyUnit.ownerCivID = ownerCivID;
            enemyUnit.enemyType = type;
            enemyUnit.enemyPool = this;
            enemyUnit.UnitAppear();
        }

        return enemy;
    }

    public void ReturnEnemy(GameObject enemy)
    {
        EnemyUnit enemyUnit = enemy.GetComponent<EnemyUnit>();
        if (enemyUnit == null)
        {
            Debug.LogWarning("EnemyUnit 정보가 없습니다.");
            return;
        }

        enemy.SetActive(false);
        pools[enemyUnit.enemyType].Enqueue(enemy);
    }
}