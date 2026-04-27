using NYH.BattleCardSystem;
using UnityEngine;

// Temporary battle-mode helper for scene testing.
// - F7: enter battle using real node data
// - F8: exit battle
// - F9: if inspector spawn data exists, enter an empty battle and spawn those units
//       otherwise fall back to spawning from the current territory node data
public class TemporaryBattleModeSwitcher : MonoBehaviour
{
    [System.Serializable]
    private class UnitSpawnRequest
    {
        public BattleUnit unitPrefab;
        public Vector2Int gridPosition;
        public int startHealth = 10;
    }

    [Header("Real Connection Test")]
    [SerializeField] private int testPlayerNodeID = 0;
    [SerializeField] private int testEnemyNodeID = 1;

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
            Debug.LogWarning("[TemporaryBattleModeSwitcher] BattleSessionController is missing.");
            return;
        }

        if (WorldMapManager.Instance == null)
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] WorldMapManager is missing.");
            return;
        }

        NodeData playerNodeData = WorldMapManager.Instance.GetNode(testPlayerNodeID);
        NodeData enemyNodeData = WorldMapManager.Instance.GetNode(testEnemyNodeID);

        if (playerNodeData == null)
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] Player node not found: id={testPlayerNodeID}");
        }

        if (enemyNodeData == null)
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] Enemy node not found: id={testEnemyNodeID}");
        }

        Debug.Log($"[TemporaryBattleModeSwitcher] Entering battle with node data: player={testPlayerNodeID}, enemy={testEnemyNodeID}");
        battleSessionController.EnterBattle(playerNodeData, enemyNodeData);
    }

    public void ExitBattleMode()
    {
        if (!TryResolveController())
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] BattleSessionController is missing.");
            return;
        }

        Debug.Log("[TemporaryBattleModeSwitcher] Exiting battle mode.");
        battleSessionController.ExitBattle();
    }

    public void SpawnConfiguredUnits()
    {
        if (unitsToSpawn != null && unitsToSpawn.Length > 0)
        {
            SpawnConfiguredUnitsFromInspector();
            return;
        }

        SpawnCurrentTerritoryUnits();
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
            Debug.LogWarning("[TemporaryBattleModeSwitcher] Unit prefab is missing.");
            return null;
        }

        if (!TryResolveBoard())
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] BattleBoardSystem is missing.");
            return null;
        }

        BattleGridCoordinateService coordinateService = BattleGridCoordinateService.Instance;
        if (!coordinateService.RefreshFromTilemaps() && !coordinateService.IsCombatCell(gridPosition))
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] Could not refresh combat coordinates. pos={gridPosition}");
            return null;
        }

        if (!coordinateService.IsCombatCell(gridPosition))
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] Target cell is not a combat cell. pos={gridPosition}");
            return null;
        }

        if (battleBoardSystem.GetUnitAt(gridPosition) != null)
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] Cell is already occupied. pos={gridPosition}");
            return null;
        }

        Vector3 spawnPosition = BattleUnit.GetWorldPositionForGrid(gridPosition, unitPrefab.transform.position.z);
        BattleUnit spawnedUnit = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
        int resolvedHealth = Mathf.Clamp(startHealth, 0, spawnedUnit.MaxHealth);
        spawnedUnit.Initialize(gridPosition, resolvedHealth);
        spawnedUnit.SnapToGridCenter();

        if (!battleBoardSystem.RegisterUnit(spawnedUnit, gridPosition))
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] Failed to register unit at {gridPosition}.");
            Destroy(spawnedUnit.gameObject);
            return null;
        }

        Debug.Log($"[TemporaryBattleModeSwitcher] Unit spawned: name={spawnedUnit.name}, team={spawnedUnit.Team}, pos={gridPosition}, health={resolvedHealth}");
        spawnedUnit.LogGridAlignment("TemporaryBattleModeSwitcher.SpawnUnit");
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

    private void SpawnConfiguredUnitsFromInspector()
    {
        if (!TryResolveController())
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] BattleSessionController is missing.");
            return;
        }

        if (!battleSessionController.IsBattleActive)
        {
            Debug.Log("[TemporaryBattleModeSwitcher] Entering empty battle mode for inspector-configured unit spawn.");
            battleSessionController.EnterBattle();
        }

        int spawnedCount = 0;
        for (int i = 0; i < unitsToSpawn.Length; i++)
        {
            UnitSpawnRequest request = unitsToSpawn[i];
            if (request == null || request.unitPrefab == null)
            {
                continue;
            }

            if (SpawnUnit(request.unitPrefab, request.gridPosition, request.startHealth) != null)
            {
                spawnedCount++;
            }
        }

        if (spawnedCount == 0)
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] No configured units were spawned.");
            return;
        }

        Debug.Log($"[TemporaryBattleModeSwitcher] Configured unit spawn complete: spawned={spawnedCount}");
    }

    private void SpawnCurrentTerritoryUnits()
    {
        if (WorldMapManager.Instance == null || !WorldMapManager.Instance.IsInTerritoryView)
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] Current territory view is unavailable and no configured units were provided.");
            return;
        }

        int currentNodeID = WorldMapManager.Instance.CurrentNodeID;
        NodeData currentNodeData = WorldMapManager.Instance.GetNode(currentNodeID);

        if (currentNodeData == null)
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] Current node data not found: id={currentNodeID}");
            return;
        }

        if (!TryResolveController())
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] BattleSessionController is missing.");
            return;
        }

        Debug.Log($"[TemporaryBattleModeSwitcher] Entering battle with current territory node data: node={currentNodeID}");
        battleSessionController.EnterBattle(currentNodeData, currentNodeData);
    }
}
