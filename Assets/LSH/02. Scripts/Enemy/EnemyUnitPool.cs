using System;
using System.Collections.Generic;
using UnityEngine;

// Dictionary로 적 생성 및 A,B,C 각각 스폰
// 건물 생성
// 가능하면 이동
public class EnemyUnitPool : MonoBehaviour
{
    public GameObject[] EnemyUnitPrefeb;
    public Dictionary<int, GameObject[]> EnemyUnitPostfeb;
    public Transform poolParent;
    public int poolSize = 20;

    private Queue<GameObject> pool = new Queue<GameObject>();

    void Start()
    {
        for (int a = 0; a < EnemyUnitPrefeb.Length; a++)
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject human = Instantiate(EnemyUnitPrefeb[a], poolParent);
                human.SetActive(false);

                pool.Enqueue(human);
            }
        }
    }

    void UnitInitialize(/*인트값 받아서 a,b,c 판단*/)
    {

    }

    public GameObject GetEnemyUnit() //이거 쓰셈 소환할때.(카드 만드는 사람은 이걸 읽도록)
    {
        if (pool.Count == 0)
        {
            Debug.Log("전부 소환됐음요");
            return null;
        }

        GameObject enemy = pool.Dequeue();
        enemy.SetActive(true);
        EnemyUnit enemyUnit = enemy.GetComponent<EnemyUnit>();
        if (enemyUnit != null)
        {
            enemyUnit.UnitAppear();
        }

        return enemy;
    }

    public void ReturnEnemy(GameObject enemy)
    {
        enemy.SetActive(false);
        pool.Enqueue(enemy);
    }
}
