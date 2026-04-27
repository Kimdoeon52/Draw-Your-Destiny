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
                Debug.LogWarning($"[BattleUnitSpawnService] 전투 셀이 아닌 위치에 소환 시도 중입니다. (임시 허용 강제 스폰) pos={gridPosition}");
                // return null;
            }

            if (battleBoardSystem.GetUnitAt(gridPosition) != null)
            {
                Debug.LogWarning($"[BattleUnitSpawnService] 이미 다른 유닛이 있는 위치입니다. pos={gridPosition}");
                return null;
            }

            Vector3 spawnPosition = BattleUnit.GetWorldPositionForGrid(gridPosition, unitPrefab.transform.position.z);
            BattleUnit spawnedUnit = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
            
            // [수정] 문명 모드 이동 스크립트 비활성화 (전투 중 맘대로 돌아다니는 현상 방지)
            MonoBehaviour worldScript = spawnedUnit.GetComponent("EnemyUnitBase") as MonoBehaviour;
            if (worldScript != null) worldScript.enabled = false;
            // [수정] 프리팹의 렌더러가 꺼져있을 수 있으므로 강제로 켭니다 (유령 유닛 방지)
            foreach (var sr in spawnedUnit.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = true;
            foreach (var r in spawnedUnit.GetComponentsInChildren<Renderer>(true)) r.enabled = true;

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

        // 유닛 수량(count)을 스탯 배율로 적용하여 1기만 스폰합니다.
        // 종류별 1기만 소환하되, count만큼 HP/공격력이 증가합니다.
        public BattleUnit SpawnUnitWithCount(BattleUnit unitPrefab, Vector2Int gridPosition, int baseHealth, int baseAttack, int unitCount)
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
                Debug.LogWarning($"[BattleUnitSpawnService] 전투 셀이 아닌 위치에 소환 시도 중입니다. (임시 허용 강제 스폰) pos={gridPosition}");
                // return null;
            }

            if (battleBoardSystem.GetUnitAt(gridPosition) != null)
            {
                Debug.LogWarning($"[BattleUnitSpawnService] 이미 다른 유닛이 있는 위치입니다. pos={gridPosition}");
                return null;
            }

            Vector3 spawnPosition = BattleUnit.GetWorldPositionForGrid(gridPosition, unitPrefab.transform.position.z);
            BattleUnit spawnedUnit = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);

            // [수정] 문명 모드 이동 스크립트 비활성화 (전투 중 맘대로 돌아다니는 현상 방지)
            MonoBehaviour worldScript = spawnedUnit.GetComponent("EnemyUnitBase") as MonoBehaviour;
            if (worldScript != null) worldScript.enabled = false;
            // [수정] 프리팹의 렌더러가 꺼져있을 수 있으므로 강제로 켭니다 (유령 유닛 방지)
            foreach (var sr in spawnedUnit.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = true;
            foreach (var r in spawnedUnit.GetComponentsInChildren<Renderer>(true)) r.enabled = true;

            spawnedUnit.InitializeWithCount(gridPosition, baseHealth, baseAttack, unitCount);
            spawnedUnit.SnapToGridCenter();

            if (!battleBoardSystem.RegisterUnit(spawnedUnit, gridPosition))
            {
                Destroy(spawnedUnit.gameObject);
                return null;
            }

            Debug.Log($"[BattleUnitSpawnService] 유닛 스폰 완료 (count 적용): type={spawnedUnit.UnitType}, count={unitCount}, HP={spawnedUnit.MaxHealth}, ATK={spawnedUnit.CurrentAttackPower}, pos={gridPosition}");
            spawnedUnit.LogGridAlignment("BattleUnitSpawnService.SpawnUnitWithCount");
            return spawnedUnit;
        }
    }
}
