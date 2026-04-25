using NYH.BattleCardSystem;
using UnityEngine;

// 임시 테스트용 전투 모드 전환 스크립트.
// 버튼 OnClick에서 EnterBattleMode / ExitBattleMode로 연결하면 됩니다.
// enableKeyboardShortcut을 켜서 단축키로도 빠르게 테스트할 수 있습니다.
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
    [Tooltip("WorldMapManager에서 가져올 플레이어 노드 ID (실제 데이터 테스트용)")]
    [SerializeField] private int testPlayerNodeID = 0;
    [Tooltip("WorldMapManager에서 가져올 적 노드 ID (실제 데이터 테스트용)")]
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
            Debug.LogWarning("[TemporaryBattleModeSwitcher] BattleSessionController가 없어 전투 모드로 전환할 수 없습니다.");
            return;
        }

        if (WorldMapManager.Instance == null)
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] WorldMapManager가 없어 실제 데이터를 가져올 수 없습니다.");
            return;
        }

        // WorldMapManager에서 실제 씬 데이터 가져오기
        NodeData playerNodeData = WorldMapManager.Instance.GetNode(testPlayerNodeID);
        NodeData enemyNodeData = WorldMapManager.Instance.GetNode(testEnemyNodeID);

        if (playerNodeData == null)
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] 플레이어 노드 {testPlayerNodeID} 데이터를 찾을 수 없습니다.");
        }
        if (enemyNodeData == null)
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] 적 노드 {testEnemyNodeID} 데이터를 찾을 수 없습니다.");
        }

        Debug.Log($"[TemporaryBattleModeSwitcher] 실제 데이터 연결 테스트 시작: Player Node {testPlayerNodeID}, Enemy Node {testEnemyNodeID}");
        battleSessionController.EnterBattle(playerNodeData, enemyNodeData);
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
        if (WorldMapManager.Instance == null || !WorldMapManager.Instance.IsInTerritoryView)
        {
            Debug.LogWarning("[TemporaryBattleModeSwitcher] 현재 영지 뷰가 아니거나 WorldMapManager가 없어 데이터를 가져올 수 없습니다.");
            return;
        }

        // 1. 현재 진입해 있는 영지의 진짜 데이터를 가져옴
        int currentNodeID = WorldMapManager.Instance.CurrentNodeID;
        NodeData currentNodeData = WorldMapManager.Instance.GetNode(currentNodeID);

        if (currentNodeData == null)
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] 현재 노드({currentNodeID}) 데이터를 찾을 수 없습니다.");
            return;
        }

        if (!TryResolveController()) return;

        Debug.Log($"[TemporaryBattleModeSwitcher] 현재 영지({currentNodeID})의 실제 유닛 데이터로 전투 진입");

        // 2. 정식 전투 진입 프로세스 호출
        // 플레이어 유닛과 적 유닛 모두 현재 노드 데이터를 사용하여 스폰 테스트
        battleSessionController.EnterBattle(currentNodeData, currentNodeData);
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

        BattleGridCoordinateService coordinateService = BattleGridCoordinateService.Instance;
        if (!coordinateService.RefreshFromTilemaps() && !coordinateService.IsCombatCell(gridPosition))
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] 좌표계 갱신 못함. pos={gridPosition}");
            return null;
        }

        if (!coordinateService.IsCombatCell(gridPosition))
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] 전투 셀이 아닌 위치에는 소환할 수 없습니다. pos={gridPosition}");
            return null;
        }

        if (battleBoardSystem.GetUnitAt(gridPosition) != null)
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] {gridPosition} 위치에는 이미 유닛이 있습니다.");
            return null;
        }

        Vector3 spawnPosition = BattleUnit.GetWorldPositionForGrid(gridPosition, unitPrefab.transform.position.z);
        BattleUnit spawnedUnit = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
        int resolvedHealth = Mathf.Clamp(startHealth, 0, spawnedUnit.MaxHealth);
        spawnedUnit.Initialize(gridPosition, resolvedHealth);
        spawnedUnit.SnapToGridCenter();

        if (!battleBoardSystem.RegisterUnit(spawnedUnit, gridPosition))
        {
            Debug.LogWarning($"[TemporaryBattleModeSwitcher] {gridPosition} 위치의 유닛 등록 실패.");
            Destroy(spawnedUnit.gameObject);
            return null;
        }

        Debug.Log($"[TemporaryBattleModeSwitcher] 유닛 배치 완료: name={spawnedUnit.name}, team={spawnedUnit.Team}, pos={gridPosition}, health={resolvedHealth}");
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

