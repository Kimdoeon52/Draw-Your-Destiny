using System.Collections.Generic;
using UnityEngine;
using Base;

namespace PoolBase
{
    public class HumanPool : Singleton<HumanPool>, IHumanPool
    {
        [Header("유닛")]
        [SerializeField] private GameObject humanPrefab;
        [Header("최대 생성")]
        [SerializeField] protected int poolSize = 20;
        [SerializeField] protected Transform poolParent;
        protected Queue<GameObject> pool = new Queue<GameObject>();

        private void Awake()
        {
            poolParent = transform;
        }
        protected virtual void Start()
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
        public virtual GameObject GetHuman(int ownerCivID)
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