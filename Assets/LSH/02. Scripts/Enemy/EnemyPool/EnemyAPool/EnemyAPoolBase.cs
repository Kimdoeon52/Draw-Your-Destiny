using System.Collections.Generic;
using UnityEngine;
using System;
namespace EnemyAPool
{
    public class EnemyAPoolBase : MonoBehaviour, IEnemyPool,IEnemyBuilding
    {
        [Header("적유닛")]
        public GameObject enemyUnitPrefab;
        [Header("최대 생성")]
        public int poolSize = 20;
        [Header("생성위치")]
        public Transform poolParent;
        [Header("현재 건물 턴")]
        public int currentTurn = 0;
        private Queue<GameObject> pool = new Queue<GameObject>();

        [SerializeField] private int SpawnTurn = 5; //일단 5턴 마다.
        [SerializeField] private int enemyID = 1;

        protected EnemyBrainBase brain;


        //=============================이벤트 등록===============================

        //public void Init(EnemyBrainBase brain) //EnemyBrainBase에서 건물 생성시 처리
        //{
        //    enemyBrainbase = brain;
        //}
        public void Init(EnemyBrainBase brainRef)
        {
            brain = brainRef;
            brain.OnTurnPassed += HandleTurn;
        }
        private void OnDisable()
        {
            if (brain != null)
                brain.OnTurnPassed -= HandleTurn;
        }

        //============================턴 이벤트========================================
        private void HandleTurn()
        {
            currentTurn++;

            if (currentTurn % SpawnTurn == 0)
            {
                Debug.Log("<color=green>[Pool에서 유닛 생산]</color>");
                GetEnemy(enemyID); //소환
            }
        }
        //==============================================================================
        void Start()
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject human = Instantiate(enemyUnitPrefab, poolParent);
                human.SetActive(false);
                pool.Enqueue(human);
            }
        }
        ////===========================임시 코드================================
        //private void Update()
        //{
        //    if (Input.GetKeyDown(KeyCode.A))
        //    {
        //        GetEnemy(1);
        //    }
        //}
        //============================유닛 소환 및 리턴===============================
        public GameObject GetEnemy(int enemyCivID) //이거 쓰셈 소환할때.(카드 만드는 사람은 이걸 읽도록)
        {
            if (pool.Count == 0)
            {
                Debug.Log("전부 소환됐음요");
                return null;
            }

            GameObject enemy = pool.Dequeue(); //풀에서 꺼냄
            enemy.SetActive(true); 
            EnemyUnitBase enemyAUnit = enemy.GetComponent<EnemyUnitBase>();
            enemyAUnit.enemyUnitID = enemyCivID; //아이디 등록
            enemyAUnit.SetOwnerPool(this); //유닛이 어디서 왓는지 pool 등록(재자리로 돌아가기 위함)
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
    interface IEnemyBuilding
    {
        void Init(EnemyBrainBase brainRef);
    }
}
