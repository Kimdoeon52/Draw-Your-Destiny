using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Base;

namespace PoolBase
{
    public class HumanPool : MonoBehaviour, IHumanPool
    {
        [Header("유닛")]
        [SerializeField] private GameObject humanPrefab;
        [Header("최대 생성")]
        [SerializeField] protected int poolSize = 20;
        [SerializeField] protected Transform poolParent;
        protected Queue<GameObject> pool = new Queue<GameObject>();

        protected virtual void Awake()
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

            //유효한 city 타일 중 랜덤 1곳에 배치 (타일맵 밖에 있으면 이동 루프가 갈 곳을 못찾음)
            PlaceOnRandomCityTile(human);

            ActivateHuman(human, ownerCivID);

            return human;
        }

        // 서브클래스 공용: UnitsRoot로 parent 이동 → 활성화 → homeNodeID 기록 → UnitRegistry 등록.
        // 건물 Destroy와 독립적으로 유닛이 살아남고, 노드별로 토글 가능해진다.
        protected void ActivateHuman(GameObject human, int ownerCivID)
        {
            UnitRegistry registry = UnitRegistry.Instance;
            if (registry != null)
                human.transform.SetParent(registry.UnitsRoot, true);

            human.SetActive(true);

            HumanBase humanUnit = human.GetComponent<HumanBase>();
            humanUnit.ownerCivID = ownerCivID;
            humanUnit.SetOwnerPool(this);

            int nodeID = WorldMapManager.Instance != null ? WorldMapManager.Instance.CurrentNodeID : -1;
            humanUnit.homeNodeID = nodeID;

            if (registry != null) registry.Register(humanUnit);
        }

        private void PlaceOnRandomCityTile(GameObject human)
        {
            Tilemap cityMap = TileMapManager.Instance != null ? TileMapManager.Instance.cityTilemap : null;
            if (cityMap == null)
            {
                Debug.LogWarning("HumanPool: cityTilemap이 없어서 유닛을 배치할 수 없습니다.");
                return;
            }

            cityMap.CompressBounds();
            BoundsInt b = cityMap.cellBounds;

            for (int i = 0; i < 30; i++)
            {
                int x = Random.Range(b.xMin, b.xMax);
                int y = Random.Range(b.yMin, b.yMax);
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (cityMap.GetTile(cell) != null)
                {
                    Vector3 world = cityMap.GetCellCenterWorld(cell);
                    world.z = human.transform.position.z;
                    human.transform.position = world;
                    return;
                }
            }

            Debug.LogWarning("HumanPool: 유효한 city 타일을 못 찾아서 유닛이 기본 위치에 배치됨.");
        }

        public void ReturnHuman(GameObject human)
        {
            HumanBase humanUnit = human.GetComponent<HumanBase>();
            if (humanUnit != null)
            {
                UnitRegistry.Instance?.Unregister(humanUnit);
                humanUnit.homeNodeID = -1;
            }
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