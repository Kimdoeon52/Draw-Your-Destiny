using NYH.BattleCardSystem;
using System.Collections.Generic;
using UnityEngine;

// Temporary battle-mode helper for scene testing.
// - F4: reset the persistent battle deck back to the configured base deck
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

    [System.Serializable]
    private class UtilityCardTestSlot
    {
        public BattleCardData cardData;
        public int addCount = 1;
    }

    [Header("Real Connection Test")]
    [SerializeField] private int testPlayerNodeID = 0;
    [SerializeField] private int testEnemyNodeID = 1;

    [Header("Optional Reference")]
    [SerializeField] private BattleSessionController battleSessionController;

    [Header("Optional Spawn")]
    [SerializeField] private BattleBoardSystem battleBoardSystem;
    [SerializeField] private UnitSpawnRequest[] unitsToSpawn;

    [Header("Optional Utility Card Test")]
    [Tooltip("F10을 눌렀을 때 배틀 덱에 추가할 테스트용 포션 카드 목록입니다.")]
    [SerializeField] private UtilityCardTestSlot[] potionCardsToAdd;
    [Tooltip("F10을 눌렀을 때 배틀 덱에 추가할 테스트용 덫 카드 목록입니다.")]
    [SerializeField] private UtilityCardTestSlot[] trapCardsToAdd;

    [Header("Keyboard Test")]
    [SerializeField] private bool enableKeyboardShortcut = true;
    [SerializeField] private KeyCode resetBattleDeckKey = KeyCode.F4;
    [SerializeField] private KeyCode enterBattleKey = KeyCode.F7;
    [SerializeField] private KeyCode exitBattleKey = KeyCode.F8;
    [SerializeField] private KeyCode spawnConfiguredUnitsKey = KeyCode.F9;
    [SerializeField] private KeyCode addUtilityCardsKey = KeyCode.F10;

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

        if (Input.GetKeyDown(resetBattleDeckKey))
        {
            ResetBattleDeckToBase();
        }

        if (Input.GetKeyDown(exitBattleKey))
        {
            ExitBattleMode();
        }

        if (Input.GetKeyDown(spawnConfiguredUnitsKey))
        {
            SpawnConfiguredUnits();
        }

        if (Input.GetKeyDown(addUtilityCardsKey))
        {
            AddConfiguredUtilityCards();
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

    public void AddConfiguredUtilityCards()
    {
        BattleCardSystem battleCardSystem = BattleCardSystem.Instance;
        BattleDeckCollection deckCollection = BattleDeckCollection.GetOrCreate();
        if (deckCollection == null)
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] BattleDeckCollection is missing.");
            return;
        }

        int addedCount = 0;
        addedCount += AddConfiguredUtilityCardsFromSlots(potionCardsToAdd, BattleCardType.Potion, deckCollection, battleCardSystem);
        addedCount += AddConfiguredUtilityCardsFromSlots(trapCardsToAdd, BattleCardType.Trap, deckCollection, battleCardSystem);

        if (addedCount <= 0)
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] No utility cards were added. Check the configured potion/trap slots.");
            return;
        }

        Debug.Log($"[TemporaryBattleModeSwitcher] Utility test cards added: total={addedCount}");
    }

    public void ResetBattleDeckToBase()
    {
        BattleDeckCollection deckCollection = BattleDeckCollection.GetOrCreate();
        if (deckCollection == null)
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] BattleDeckCollection is missing.");
            return;
        }

        List<BattleCardData> baseDeckSnapshot = new(deckCollection.BaseBattleDeck);
        deckCollection.ResetRun();
        deckCollection.ClearSavedCurrentDeck();
        deckCollection.ConfigureBaseDeck(baseDeckSnapshot);

        BattleCardSystem battleCardSystem = BattleCardSystem.Instance;
        if (battleCardSystem?.PileState != null)
        {
            battleCardSystem.PileState.Setup(deckCollection.BuildBattleDeckSources());
        }

        Debug.Log($"[TemporaryBattleModeSwitcher] Battle deck reset to base deck. baseCount={baseDeckSnapshot.Count}");
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

    private static int AddConfiguredUtilityCardsFromSlots(
        UtilityCardTestSlot[] slots,
        BattleCardType expectedType,
        BattleDeckCollection deckCollection,
        BattleCardSystem battleCardSystem)
    {
        if (slots == null || slots.Length == 0)
        {
            return 0;
        }

        int addedCount = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            UtilityCardTestSlot slot = slots[i];
            if (slot == null || slot.cardData == null)
            {
                continue;
            }

            if (slot.cardData.CardType != expectedType)
            {
                Debug.LogWarning(
                    $"[TemporaryBattleModeSwitcher] Skipping utility card with mismatched type: card={slot.cardData.CardName}, expected={expectedType}, actual={slot.cardData.CardType}");
                continue;
            }

            int repeatCount = Mathf.Max(0, slot.addCount);
            for (int countIndex = 0; countIndex < repeatCount; countIndex++)
            {
                // BattleDeckCollection.AddPotionCard already supports deck-limit-ignoring utility cards
                // such as potions and traps, so we reuse the same path for both card types here.
                deckCollection.AddPotionCard(slot.cardData);
                battleCardSystem?.PileState?.AddPotionCard(slot.cardData);
                addedCount++;
            }
        }

        return addedCount;
    }
}
