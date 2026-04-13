using System.Collections.Generic;
using UnityEngine;
using System;

namespace EnemyAPool
{
    public class EnemyAPoolBase : MonoBehaviour, IEnemyPool
    {
        [Header("적유닛")]
        public GameObject enemyUnitPrefab;
        [Header("최대 생성")]
        public int poolSize = 20;
        [Header("생성위치")]
        public Transform poolParent;
        private Queue<GameObject> pool = new Queue<GameObject>();

        void Start()
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject human = Instantiate(enemyUnitPrefab, poolParent);
                human.SetActive(false);
                pool.Enqueue(human);
            }
        }
        //===========================임시 코드================================
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                GetEnemy(1);
            }
        }
        //====================================================================
        public GameObject GetEnemy(int enemyCivID) //이거 쓰셈 소환할때.(카드 만드는 사람은 이걸 읽도록)
        {
            if (pool.Count == 0)
            {
                Debug.Log("전부 소환됐음요");
                return null;
            }

            GameObject enemy = pool.Dequeue();
            enemy.SetActive(true);
            EnemyUnitBase enemyAUnit = enemy.GetComponent<EnemyUnitBase>();
            enemyAUnit.enemyUnitID = enemyCivID;
            enemyAUnit.SetOwnerPool(this);
            return enemy;
        }

        public void ReturnHuman(GameObject enemy)
        {
            enemy.SetActive(false);
            pool.Enqueue(enemy);
        }
    }
    public interface IEnemyPool
    {
        GameObject GetEnemy(int enemyCivID);
        void ReturnHuman(GameObject enemy);
    }
}
