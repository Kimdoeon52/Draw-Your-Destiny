using NYH.BattleCardSystem;
using UnityEngine;

// 임시 테스트용 전투 모드 전환 스크립트.
// 버튼 OnClick에서 EnterBattleMode / ExitBattleMode를 연결하거나,
// enableKeyboardShortcut을 켜서 키 입력으로도 빠르게 테스트할 수 있습니다.
public class TemporaryBattleModeSwitcher : MonoBehaviour
{
    [System.Serializable]
    private class UnitSpawnRequest
    {
        public BattleUnit unitPrefab;
        public Vector2Int gridPosition;
        public int startHealth = 10;
    }

    [Header("Optional Reference")]
    [SerializeField] private BattleSessionController battleSessionController;

    [Header("Optional Spawn")]
    [SerializeField] private BattleBoardSystem battleBoardSystem;
    [SerializeField] private UnitSpawnRequest[] unitsToSpawn;

    [Header("Keyboard Test")]
    [SerializeField] private bool enableKeyboardShortcut = true;
    [SerializeField] private KeyCode enterBattleKey = KeyCode.F7;
    [SerializeField] private KeyCode exitBattleKey = KeyCode.F8;
    [SerializeField] private KeyCode spawnConfiguredUnitsKey = KeyCode.F9;

    private void Awake()
    {
        if (battleSessionController == null)
        {
            battleSessionController = FindFirstObjectByType<BattleSessionController>(FindObjectsInactive.Include);
        }
    }

    private void Update()
    {
        if (!enableKeyboardShortcut)
        {
            return;
        }

        if (Input.GetKeyDown(enterBattleKey))
        {
            EnterBattleMode();
        }

        if (Input.GetKeyDown(exitBattleKey))
        {
            ExitBattleMode();
        }

        if (Input.GetKeyDown(spawnConfiguredUnitsKey))
        {
            SpawnConfiguredUnits();
        }
    }

    public void EnterBattleMode()
    {
        if (!TryResolveController())
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] BattleSessionController가 없어 전투 모드로 전환할 수 없습니다.");
            return;
        }

        Debug.Log("[TemporaryBattleModeSwitcher] 전투 모드 진입 요청");
        battleSessionController.EnterBattle();
    }

    public void ExitBattleMode()
    {
        if (!TryResolveController())
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] BattleSessionController가 없어 문명 모드로 복귀할 수 없습니다.");
            return;
        }

        Debug.Log("[TemporaryBattleModeSwitcher] 문명 모드 복귀 요청");
        battleSessionController.ExitBattle();
    }

    public void SpawnConfiguredUnits()
    {
        if (unitsToSpawn == null || unitsToSpawn.Length == 0)
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] 배치할 유닛 설정이 비어 있습니다.");
            return;
        }

        int spawnedCount = 0;
        foreach (UnitSpawnRequest request in unitsToSpawn)
        {
            if (request == null || request.unitPrefab == null)
            {
                continue;
            }

            if (SpawnUnit(request.unitPrefab, request.gridPosition, request.startHealth) != null)
            {
                spawnedCount++;
            }
        }

        Debug.Log($"[TemporaryBattleModeSwitcher] 설정된 유닛 배치 완료: spawned={spawnedCount}");
    }

    public BattleUnit SpawnPlayerUnit(BattleUnit unitPrefab, Vector2Int gridPosition, int startHealth = 10)
    {
        return SpawnUnit(unitPrefab, gridPosition, startHealth);
    }

    public BattleUnit SpawnEnemyUnit(BattleUnit unitPrefab, Vector2Int gridPosition, int startHealth = 10)
    {
        return SpawnUnit(unitPrefab, gridPosition, startHealth);
    }

    public BattleUnit SpawnUnit(BattleUnit unitPrefab, Vector2Int gridPosition, int startHealth = 10)
    {
        if (unitPrefab == null)
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] 유닛 프리팹이 없어 배치할 수 없습니다.");
            return null;
        }

        if (!TryResolveBoard())
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] BattleBoardSystem이 없어 유닛을 배치할 수 없습니다.");
            return null;
        }

        if (battleBoardSystem.GetUnitAt(gridPosition) != null)
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] {gridPosition} 위치에는 이미 유닛이 있습니다.");
            return null;
        }

        Vector3 spawnPosition = new(gridPosition.x, gridPosition.y, unitPrefab.transform.position.z);
        BattleUnit spawnedUnit = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
        int resolvedHealth = Mathf.Clamp(startHealth, 0, spawnedUnit.MaxHealth);
        spawnedUnit.Initialize(gridPosition, resolvedHealth);

        if (!battleBoardSystem.RegisterUnit(spawnedUnit, gridPosition))
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] {gridPosition} 위치에 유닛 등록에 실패했습니다.");
            Destroy(spawnedUnit.gameObject);
            return null;
        }

        Debug.Log($"[TemporaryBattleModeSwitcher] 유닛 배치 완료: name={spawnedUnit.name}, team={spawnedUnit.Team}, pos={gridPosition}, health={resolvedHealth}");
        return spawnedUnit;
    }

    private bool TryResolveController()
    {
        if (battleSessionController != null)
        {
            return true;
        }

        battleSessionController = FindFirstObjectByType<BattleSessionController>(FindObjectsInactive.Include);
        return battleSessionController != null;
    }

    private bool TryResolveBoard()
    {
        if (battleBoardSystem != null)
        {
            return true;
        }

        battleBoardSystem = BattleBoardSystem.Instance;
        return battleBoardSystem != null;
    }
}
