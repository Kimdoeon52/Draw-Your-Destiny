namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /*
     * BattleUnitPrefabRegistry
     *
     * 역할:
     * - 병종(UnitType) → BattleUnit 프리팹 매핑을 Inspector에서 관리합니다.
     * - 런타임에 O(1) 조회를 위해 초기화 시 딕셔너리로 캐싱합니다.
     *
     * 사용법:
     * - Assets 폴더에서 우클릭 → Create → Data → Battle Unit Prefab Registry로 생성
     * - 병종마다 Player/Enemy 프리팹을 드래그 앤 드롭으로 할당
     * - BattleUnitRosterBuilder 또는 BattleStartSpawner에서 참조
     */
    [CreateAssetMenu(menuName = "Data/Battle Unit Prefab Registry")]
    public class BattleUnitPrefabRegistry : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            [Tooltip("유닛 병종")]
            public UnitType UnitType;

            [Tooltip("플레이어 팀 프리팹")]
            public BattleUnit PlayerPrefab;

            [Tooltip("적 팀 프리팹")]
            public BattleUnit EnemyPrefab;
        }

        [SerializeField] private List<Entry> entries = new();

        private Dictionary<UnitType, Entry> lookupCache;

        // 지정한 병종과 팀에 맞는 프리팹을 반환합니다.
        // 등록되지 않은 병종이면 null을 반환합니다.
        public BattleUnit GetPrefab(UnitType unitType, BattleTeam team)
        {
            EnsureCache();
            if (!lookupCache.TryGetValue(unitType, out Entry entry))
            {
                return null;
            }

            return team == BattleTeam.Player ? entry.PlayerPrefab : entry.EnemyPrefab;
        }

        // entries 목록에 등록된 병종 수를 반환합니다.
        public int Count
        {
            get
            {
                EnsureCache();
                return lookupCache.Count;
            }
        }

        private void EnsureCache()
        {
            if (lookupCache != null)
            {
                return;
            }

            lookupCache = new Dictionary<UnitType, Entry>();
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                lookupCache[entry.UnitType] = entry;
            }
        }

        private void OnValidate()
        {
            // Inspector에서 값 변경 시 캐시 무효화
            lookupCache = null;
        }
    }
}
