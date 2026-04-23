namespace NYH.BattleCardSystem
{
    using UnityEngine;

    /*
     * BattleUnitSpawnService
     *
     * 역할:
     * - 전투 유닛 프리팹을 지정 그리드 셀에 생성하고 BattleBoardSystem에 등록합니다.
     * - 전투 셀 여부와 점유 여부를 검증해 잘못된 소환을 막습니다.
     */
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

        // unitPrefab을 gridPosition에 생성하고 시작 체력을 적용합니다.
        public BattleUnit SpawnUnit(BattleUnit unitPrefab, Vector2Int gridPosition, int startHealth = -1)
        {
            if (unitPrefab == null || battleBoardSystem == null)
            {
                return null;
            }

            BattleGridCoordinateService coordinateService = BattleGridCoordinateService.Instance;
            if (!coordinateService.RefreshFromTilemaps() && !coordinateService.IsCombatCell(gridPosition))
            {
                Debug.LogWarning($"[BattleUnitSpawnService] 전투 좌표 서비스를 초기화할 수 없습니다. pos={gridPosition}");
                return null;
            }

            if (!coordinateService.IsCombatCell(gridPosition))
            {
                Debug.LogWarning($"[BattleUnitSpawnService] 전투 셀이 아닌 위치에는 소환할 수 없습니다. pos={gridPosition}");
                return null;
            }

            if (battleBoardSystem.GetUnitAt(gridPosition) != null)
            {
                Debug.LogWarning($"[BattleUnitSpawnService] 이미 다른 유닛이 있는 위치입니다. pos={gridPosition}");
                return null;
            }

            Vector3 spawnPosition = BattleUnit.GetWorldPositionForGrid(gridPosition, unitPrefab.transform.position.z);
            BattleUnit spawnedUnit = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
            int resolvedHealth = startHealth >= 0 ? Mathf.Clamp(startHealth, 0, spawnedUnit.MaxHealth) : spawnedUnit.MaxHealth;
            spawnedUnit.Initialize(gridPosition, resolvedHealth);
            spawnedUnit.SnapToGridCenter();

            if (!battleBoardSystem.RegisterUnit(spawnedUnit, gridPosition))
            {
                Destroy(spawnedUnit.gameObject);
                return null;
            }

            spawnedUnit.LogGridAlignment("BattleUnitSpawnService.SpawnUnit");
            return spawnedUnit;
        }
    }
}
