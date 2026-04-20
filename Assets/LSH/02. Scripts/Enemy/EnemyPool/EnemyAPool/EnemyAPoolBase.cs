using System.Collections.Generic;
using UnityEngine;

namespace EnemyAPool
{
    public class EnemyAPoolBase : MonoBehaviour, IEnemyPool, IEnemyBuilding
    {
        [SerializeField] private int SpawnTurn = 5;
        [SerializeField] private int enemyID = 1;
        [SerializeField] private UnitType unitType;

        protected EnemyBrainBase brain;
        private List<System.Action> producerHandlers = new List<System.Action>();

        //=============================이벤트 등록===============================
        public void Init(EnemyBrainBase brainRef, int nodeID)
        {
            if (brainRef == null)
            {
                Debug.LogError("EnemyAPoolBase: 전달된 brainRef가 null입니다!");
                return;
            }

            brain = brainRef;
            Debug.Log($"<color=orange>[Pool Init] {gameObject.name}이 {brain.name}에 등록됨 (노드 {nodeID})</color>");

            System.Action handler = null;
            handler = () =>
            {
                if (brain.countEnemyTurn > 0 && brain.countEnemyTurn % SpawnTurn == 0)
                {
                    Debug.Log($"<color=green>[Pool] {gameObject.name} 유닛 생산! 노드 {nodeID}</color>");
                    GetEnemy(enemyID, nodeID);
                }
            };

            brain.OnTurnPassed += handler;
            producerHandlers.Add(handler);
        }

        public void ClearAllProducers()
        {
            if (brain != null)
                foreach (var h in producerHandlers)
                    brain.OnTurnPassed -= h;
            producerHandlers.Clear();
        }

        private void OnDisable()
        {
            ClearAllProducers();
        }

        //============================유닛 수 증감 (비주얼 없음 — NodeData.units로 추적)===============================
        public GameObject GetEnemy(int enemyCivID, int homeNodeID)
        {
            if (brain != null)
                brain.OnUnitSpawned(unitType, homeNodeID);
            return null;
        }

        public void ReturnHuman(GameObject enemy)
        {
            // 비주얼 유닛 없음 — Dead() 호출 경로에서 올 경우 대비
            // homeNodeID를 알 수 없으므로 Brain의 OnUnitReturned는 Dead()에서 직접 호출해야 함
        }
    }

    public interface IEnemyPool
    {
        GameObject GetEnemy(int enemyCivID, int homeNodeID);
        void ReturnHuman(GameObject enemy);
    }

    interface IEnemyBuilding
    {
        void Init(EnemyBrainBase brainRef, int nodeID);
    }
}
