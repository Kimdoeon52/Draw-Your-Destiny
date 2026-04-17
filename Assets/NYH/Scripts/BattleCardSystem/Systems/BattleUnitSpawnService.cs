namespace NYH.BattleCardSystem
{
    using UnityEngine;

    public class BattleUnitSpawnService : MonoBehaviour
    {
        [SerializeField] private BattleBoardSystem battleBoardSystem;

        private void Awake()
        {
            if (battleBoardSystem == null)
            {
                battleBoardSystem = BattleBoardSystem.Instance;
            }
        }

        public BattleUnit SpawnUnit(BattleUnit unitPrefab, Vector2Int gridPosition, int startHealth = -1)
        {
            if (unitPrefab == null || battleBoardSystem == null)
            {
                return null;
            }

            if (battleBoardSystem.GetUnitAt(gridPosition) != null)
            {
                return null;
            }

            Vector3 spawnPosition = new(gridPosition.x, gridPosition.y, unitPrefab.transform.position.z);
            BattleUnit spawnedUnit = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
            int resolvedHealth = startHealth >= 0 ? Mathf.Clamp(startHealth, 0, spawnedUnit.MaxHealth) : spawnedUnit.MaxHealth;
            spawnedUnit.Initialize(gridPosition, resolvedHealth);

            if (!battleBoardSystem.RegisterUnit(spawnedUnit, gridPosition))
            {
                Destroy(spawnedUnit.gameObject);
                return null;
            }

            return spawnedUnit;
        }
    }
}
