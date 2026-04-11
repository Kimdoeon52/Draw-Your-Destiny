using System.Collections.Generic;
using UnityEngine;
using Base;

namespace PoolBase
{
    public class HumanPool : MonoBehaviour, IHumanPool
    {
        public GameObject humanPrefab;
        public int poolSize = 20;
        public Transform poolParent;
        private Queue<GameObject> pool = new Queue<GameObject>();

        void Start()
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject human = Instantiate(humanPrefab, poolParent);
                human.SetActive(false);
                pool.Enqueue(human);
            }
        }
        //===========================임시 코드================================
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                GetHuman(0);
            }
        }
        //====================================================================
        public GameObject GetHuman(int ownerCivID) //이거 쓰셈 소환할때.(카드 만드는 사람은 이걸 읽도록)
        {
            if (pool.Count == 0)
            {
                Debug.Log("전부 소환됐음요");
                return null;
            }

            GameObject human = pool.Dequeue();
            human.SetActive(true);
            HumanBase humanUnit = human.GetComponent<HumanBase>();
            humanUnit.ownerCivID = ownerCivID;
            humanUnit.SetOwnerPool(this);
            return human;
        }

        public void ReturnHuman(GameObject human)
        {
            human.SetActive(false);
            pool.Enqueue(human);
        }
    }
    public interface IHumanPool
    {
        GameObject GetHuman(int ownerCivID);
        void ReturnHuman(GameObject human);
    }
}