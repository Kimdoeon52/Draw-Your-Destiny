using NYH.BattleCardSystem;
using UnityEngine;

// ?„ì‹œ ?ŒìŠ¤?¸ìš© ?„íˆ¬ ëª¨ë“œ ?„í™˜ ?¤í¬ë¦½íŠ¸.
// ë²„íŠ¼ OnClick?ì„œ EnterBattleMode / ExitBattleModeë¥??°ê²°?˜ê±°??
// enableKeyboardShortcut??ì¼œì„œ ???…ë ¥?¼ë¡œ??ë¹ ë¥´ê²??ŒìŠ¤?¸í•  ???ˆìŠµ?ˆë‹¤.
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
            Debug.LogWarning("[TemporaryBattleModeSwitcher] BattleSessionControllerê°€ ?†ì–´ ?„íˆ¬ ëª¨ë“œë¡??„í™˜?????†ìŠµ?ˆë‹¤.");
            return;
        }

        Debug.Log("[TemporaryBattleModeSwitcher] ?„íˆ¬ ëª¨ë“œ ì§„ì… ?”ì²­");
        battleSessionController.EnterBattle();
    }

    public void ExitBattleMode()
    {
        if (!TryResolveController())
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] BattleSessionControllerê°€ ?†ì–´ ë¬¸ëª… ëª¨ë“œë¡?ë³µê??????†ìŠµ?ˆë‹¤.");
            return;
        }

        Debug.Log("[TemporaryBattleModeSwitcher] ë¬¸ëª… ëª¨ë“œ ë³µê? ?”ì²­");
        battleSessionController.ExitBattle();
    }

    public void SpawnConfiguredUnits()
    {
        if (unitsToSpawn == null || unitsToSpawn.Length == 0)
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] ë°°ì¹˜??? ë‹› ?¤ì •??ë¹„ì–´ ?ˆìŠµ?ˆë‹¤.");
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

        Debug.Log($"[TemporaryBattleModeSwitcher] ?¤ì •??? ë‹› ë°°ì¹˜ ?„ë£Œ: spawned={spawnedCount}");
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
            Debug.LogWarning("[TemporaryBattleModeSwitcher] ? ë‹› ?„ë¦¬?¹ì´ ?†ì–´ ë°°ì¹˜?????†ìŠµ?ˆë‹¤.");
            return null;
        }

        if (!TryResolveBoard())
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] BattleBoardSystem???†ì–´ ? ë‹›??ë°°ì¹˜?????†ìŠµ?ˆë‹¤.");
            return null;
        }

        BattleGridCoordinateService coordinateService = BattleGridCoordinateService.Instance;
        if (!coordinateService.RefreshFromTilemaps() && !coordinateService.IsCombatCell(gridPosition))
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] ÀüÅõ ÁÂÇ¥ ¼­ºñ½º¸¦ ÃÊ±âÈ­ÇÒ ¼ö ¾ø½À´Ï´Ù. pos={gridPosition}");
            return null;
        }

        if (!coordinateService.IsCombatCell(gridPosition))
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] ÀüÅõ ¼¿ÀÌ ¾Æ´Ñ À§Ä¡¿¡´Â À¯´ÖÀ» ¹èÄ¡ÇÒ ¼ö ¾ø½À´Ï´Ù. pos={gridPosition}");
            return null;
        }

        if (battleBoardSystem.GetUnitAt(gridPosition) != null)
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] {gridPosition} ?„ì¹˜?ëŠ” ?´ë? ? ë‹›???ˆìŠµ?ˆë‹¤.");
            return null;
        }

        Vector3 spawnPosition = BattleUnit.GetWorldPositionForGrid(gridPosition, unitPrefab.transform.position.z);
        BattleUnit spawnedUnit = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
        int resolvedHealth = Mathf.Clamp(startHealth, 0, spawnedUnit.MaxHealth);
        spawnedUnit.Initialize(gridPosition, resolvedHealth);
        spawnedUnit.SnapToGridCenter();

        if (!battleBoardSystem.RegisterUnit(spawnedUnit, gridPosition))
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] {gridPosition} ?„ì¹˜??? ë‹› ?±ë¡???¤íŒ¨?ˆìŠµ?ˆë‹¤.");
            Destroy(spawnedUnit.gameObject);
            return null;
        }

        Debug.Log($"[TemporaryBattleModeSwitcher] ? ë‹› ë°°ì¹˜ ?„ë£Œ: name={spawnedUnit.name}, team={spawnedUnit.Team}, pos={gridPosition}, health={resolvedHealth}");
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
}

