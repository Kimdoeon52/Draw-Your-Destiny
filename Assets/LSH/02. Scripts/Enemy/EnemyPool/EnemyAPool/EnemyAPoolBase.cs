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
        public int poolSize = 10;
        [Header("생성위치")]
        public Transform poolParent;
        private Queue<GameObject> pool = new Queue<GameObject>();

        [SerializeField] private int SpawnTurn = 5; //일단 5턴 마다.
        [SerializeField] private int enemyID = 1;
        [SerializeField] private EnemyUnitType unitType; //이 풀이 생산하는 유닛 종류

        protected EnemyBrainBase brain;
        private List<System.Action> producerHandlers = new List<System.Action>(); //건물별 생산 핸들러 (건물마다 독립 카운터)


        //=============================이벤트 등록===============================

        //건물 하나 지을때마다 호출 — 각 호출마다 독립된 localTurn을 가진 핸들러를 OnTurnPassed에 구독
        public void Init(EnemyBrainBase brainRef)
        {
            brain = brainRef;
            int localTurn = 0;
            System.Action handler = null;
            handler = () =>
            {
                localTurn++;
                if (localTurn % SpawnTurn == 0)
                {
                    Debug.Log("<color=green>[Pool에서 유닛 생산]</color>");
                    GetEnemy(enemyID);
                }
            };
            brain.OnTurnPassed += handler;
            producerHandlers.Add(handler);
        }

        //건물 일괄 철거 등으로 이 풀의 모든 생산 핸들러를 해지할 때 호출
        public void ClearAllProducers()
        {
            if (brain != null)
            {
                foreach (var h in producerHandlers)
                    brain.OnTurnPassed -= h;
            }
            producerHandlers.Clear();
        }

        private void OnDisable()
        {
            ClearAllProducers();
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
            if (brain != null) brain.OnUnitSpawned(unitType); //brain에 소환 알림
            return enemy;
        }

        public void ReturnHuman(GameObject enemy)
        {
            enemy.SetActive(false);
            pool.Enqueue(enemy);
            if (brain != null) brain.OnUnitReturned(unitType); //brain에 반환 알림
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
