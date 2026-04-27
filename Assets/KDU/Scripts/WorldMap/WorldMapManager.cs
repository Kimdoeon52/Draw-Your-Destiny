using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using NYH.BattleCardSystem;

// ============================================================
// WorldMapManager — 월드맵 전체 관리 싱글톤
//
// 담당:
//   - NodeData 목록 관리 (노드 소유권, 인접 정보)
//   - 노드 클릭 처리 (영지 진입 / 빈 노드 점령 / 적 노드 공격 선언)
//   - 월드맵 뷰 ↔ 영지 뷰 페이드 전환
//   - NodeButton 시각 갱신
//
// 씬 구성:
//   - worldMapView: UI Canvas 기반 월드맵 (버튼, 노드 연결선 등)
//   - territoryView: Tilemap 기반 영지 뷰 (평상시 비활성화)
//   - fadePanel: 화면 전체를 덮는 CanvasGroup (페이드 인/아웃)
// ============================================================
public class WorldMapManager : Singleton<WorldMapManager>
{
    [Header("뷰 오브젝트")]
    public GameObject worldMapView;     // 월드맵 Canvas (비활성화 ↔ 활성화)
    public GameObject territoryView;    // 영지 Tilemap 뷰 (비활성화 ↔ 활성화)

    [Header("페이드 패널")]
    public CanvasGroup fadePanel;       // 화면 전체 검정 오버레이 CanvasGroup
    public float fadeDuration = 0.4f;   // 페이드 소요 시간(초)

    [Header("선택 모드 UI")]
    public GameObject cancelSelectionButton;        // 우상단 취소 버튼 (선택 모드 시 활성)
    public TroopDispatchModal troopDispatchModal;   // 병력 수 선택 모달

    [Header("노드 시각 색상")]
    [Range(0f, 1f)] public float civTintAlpha = 0.75f;                   // 진영 색 투명도 (통합 배경 비치도록)
    public Color highlightColor = new Color(1f, 0.95f, 0.3f, 0.4f);     // 선택 모드 후보 노드
    public Color dimColor = new Color(0f, 0f, 0f, 0.45f);         // 선택 모드 비후보 노드
    [Header("전투 연동")]
    [SerializeField] private NYH.BattleCardSystem.BattleSessionController battleSessionController;

    [Header("디버그")]
    public bool debugFreeEntry = false; // true면 적/빈 노드도 영지 진입 허용

    // ── 선택 모드 ────────────────────────────────────────────────
    public enum SelectionMode { None, Attack, Move }
    private SelectionMode selectionMode = SelectionMode.None;
    private int selectionSourceNodeID = -1;
    private readonly HashSet<int> selectableNodeIDs = new HashSet<int>();

    // 문명 ID별 노드 배경 색상 (0=플레이어 파랑, 1=AI 빨강, 2=AI 초록, 3=AI 노랑, -1=중립 회색)
    public static readonly Color[] CivColors =
    {
        new Color(0.2f, 0.5f, 1f,  1f),   // 0 플레이어
        new Color(1f,   0.3f, 0.3f, 1f),  // 1 AI1
        new Color(0.3f, 0.85f,0.3f, 1f),  // 2 AI2
        new Color(1f,   0.8f, 0.1f, 1f),  // 3 AI3
    };
    public static readonly Color NeutralColor = new Color(1f, 1f, 1f, 0f);

    // ── 런타임 상태 ──────────────────────────────────────────────
    [SerializeField]
    private List<NodeData> allNodes = new List<NodeData>();

    private int currentNodeID = -1;     // 현재 영지 뷰로 진입한 노드 (-1이면 월드맵 뷰)
    private bool isTransitioning = false;

    private NodeDataManager nodeDataManager;

    // 씬의 모든 NodeButton — Refresh 호출용
    private NodeButton[] nodeButtons;

    protected override void Awake()
    {
        base.Awake();
        // 기본 상태: 월드맵 뷰 활성, 영지 뷰 비활성
        if (worldMapView != null) worldMapView.SetActive(true);
        if (territoryView != null) territoryView.SetActive(false);
        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.blocksRaycasts = false; // 투명할 때 클릭 차단 방지
        }
        if (cancelSelectionButton != null) cancelSelectionButton.SetActive(false);
    }

    private void Start()
    {
        nodeDataManager = NodeDataManager.Instance;
        nodeButtons = FindObjectsByType<NodeButton>(FindObjectsSortMode.None);
        RefreshAllNodeButtons();
    }

    // ── 노드 데이터 조회 ──────────────────────────────────────────
    public NodeData GetNode(int nodeID)
    {
        return allNodes.Find(n => n.nodeID == nodeID);
    }

    // civID를 소유한 첫 번째 노드 반환
    public NodeData GetNodeByCivID(int civID)
    {
        return allNodes.Find(n => n.ownerCivID == civID);
    }

    // ── NodeButton → 클릭 처리 진입점 ────────────────────────────
    public void OnNodeClicked(int nodeID)
    {
        if (isTransitioning) return;

        // 선택 모드: 후보 노드만 받음. 비후보는 무시 (취소는 별도 버튼)
        if (selectionMode != SelectionMode.None)
        {
            if (!selectableNodeIDs.Contains(nodeID)) return;
            OpenTroopDispatchModal(nodeID);
            return;
        }

        NodeData node = GetNode(nodeID);
        if (node == null)
        {
            Debug.LogWarning($"[WorldMapManager] nodeID {nodeID} 를 찾을 수 없습니다.");
            return;
        }

        // [디버그] debugFreeEntry가 켜져 있으면 모든 노드 진입 허용
#if UNITY_EDITOR
        if (debugFreeEntry)
        {
            Debug.Log($"[WorldMapManager] 디버깅용 노드 {nodeID} 진입");
            StartCoroutine(EnterTerritoryCoroutine(node));
            return;
        }
#endif

        // 아군 노드이거나 유닛이 주둔 중이면 영지 진입
        if (node.ownerCivID == 0 || node.hasPlayerUnits)
        {
            StartCoroutine(EnterTerritoryCoroutine(node));
        }
        else if (node.ownerCivID == -1)
        {
            // 빈 노드 → 영지 뷰의 [이동] 버튼으로만 진입 가능 (직접 클릭은 안내만)
            Debug.Log("[WorldMapManager] 빈 노드는 인접 아군 노드의 [이동] 버튼으로 유닛을 파견해 점령하세요.");
        }
        else
        {
            // 적 노드 → 인접 아군 노드가 있을 때만 공격 선언
            if (IsAdjacentToPlayer(nodeID))
                Debug.Log($"[WorldMapManager] 노드 {nodeID} 공격 선언 (전투 시스템 연동 예정)");
            else
                Debug.Log("[WorldMapManager] 인접한 아군 노드가 없어 공격할 수 없습니다.");
        }
    }

    // ── 영지 뷰 진입 ─────────────────────────────────────────────
    private IEnumerator EnterTerritoryCoroutine(NodeData node)
    {
        isTransitioning = true;
        currentNodeID = node.nodeID;

        // 페이드 아웃
        yield return FadeTo(1f);

        // 월드맵 뷰 비활성 → 영지 뷰 활성
        if (worldMapView != null) worldMapView.SetActive(false);
        if (territoryView != null) territoryView.SetActive(true);

        // 타일맵 복원
        if (nodeDataManager != null)
            nodeDataManager.EnterNode(node);

        // 배치 UI 잠금 여부 처리
        if (!node.isMansionBuilt)
            BuildingPlacementController.LockPlacement();
        else
            BuildingPlacementController.UnlockPlacement();

        // 페이드 인
        yield return FadeTo(0f);

        isTransitioning = false;
    }

    // ── 영지 뷰 이탈 (나가기 버튼에서 호출) ──────────────────────
    public void ExitTerritoryView()
    {
        if (isTransitioning || currentNodeID == -1) return;
        StartCoroutine(ExitTerritoryCoroutine());
    }

    private IEnumerator ExitTerritoryCoroutine()
    {
        isTransitioning = true;

        // 페이드 아웃
        yield return FadeTo(1f);

        // 건물 저장
        NodeData node = GetNode(currentNodeID);
        if (nodeDataManager != null && node != null)
            nodeDataManager.ExitNode(node);

        // 영지 뷰 비활성 → 월드맵 뷰 활성
        if (territoryView != null) territoryView.SetActive(false);
        if (worldMapView != null) worldMapView.SetActive(true);

        currentNodeID = -1;

        RefreshAllNodeButtons();

        // 페이드 인
        yield return FadeTo(0f);

        isTransitioning = false;
    }

    // ── 페이드 헬퍼 ──────────────────────────────────────────────
    private IEnumerator FadeTo(float targetAlpha)
    {
        if (fadePanel == null) yield break;
        // 페이드 시작 전: 불투명해질 때만 raycast 차단 활성화
        if (targetAlpha > 0f) fadePanel.blocksRaycasts = true;
        yield return fadePanel.DOFade(targetAlpha, fadeDuration).WaitForCompletion();
        // 페이드 완료 후: 완전히 투명해지면 raycast 차단 해제
        if (targetAlpha <= 0f) fadePanel.blocksRaycasts = false;
    }

    // ── NodeButton 전체 갱신 ──────────────────────────────────────
    public void RefreshAllNodeButtons()
    {
        if (nodeButtons == null) return;
        foreach (NodeButton btn in nodeButtons)
        {
            if (btn == null) continue;
            NodeData data = GetNode(btn.NodeID);
            if (data != null)
                btn.Refresh(data);
        }
    }

    // ── 인접 여부 확인 ────────────────────────────────────────────
    // targetNodeID의 인접 노드 중 플레이어 소유가 있으면 true
    public bool IsAdjacentToPlayer(int targetNodeID)
    {
        NodeData target = GetNode(targetNodeID);
        if (target == null) return false;

        foreach (int adjID in target.adjacentNodeIDs)
        {
            NodeData adj = GetNode(adjID);
            if (adj != null && adj.ownerCivID == 0)
                return true;
        }
        return false;
    }

    // ── 전체 재건 카드 효과 ────────────────────────────────────────
    // 전투 승리로 점거한 노드에서 카드 시스템이 호출.
    // 잔해(isRuin=true) 건물을 모두 복구하고 영주성 재건 효과(점령 확정)까지 적용.
    // 영주성 재건 카드(OnMansionRebuilt)와의 차이: 잔해 건물 복구가 추가됨.
    public void RestoreCapturedNode()
    {
        int populationRestored = 0;
        var gameManager = GameManager.Instance;

        NodeData node = GetNode(currentNodeID);
        if (node == null)
        {
            Debug.LogWarning("[WorldMapManager] 현재 진입한 노드가 없습니다.");
            return;
        }

        int restored = 0;

        if (node.buildings != null)
        {
            foreach (BuildingInstance b in node.buildings)
            {
                if (b == null || !b.isRuin) continue;

                b.isRuin = false;
                b.isActive = true;

                // 영지 뷰 안에서 호출됐다면 visual이 살아있으니 기본 스프라이트로 즉시 복구
                if (b.visual != null && b.data != null && b.data.sprite != null)
                {
                    var sr = b.visual.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sprite = b.data.sprite;
                }

                // 인구 한도 보너스 (Mansion / House 등) — 이중 적용 방지
                if (gameManager != null && b.data != null
                    && b.data.populationCapBonus > 0 && !b.populationCapApplied)
                {
                    int beforeCap = node.playerPopulationCapacity;
                    gameManager.ApplyBuildingPopulationBonus(node, b.data.populationCapBonus);
                    populationRestored += node.playerPopulationCapacity - beforeCap;
                    b.populationCapApplied = true;
                }

                restored++;
            }
        }

        node.isMansionBuilt = true;
        node.ownerCivID = 0;
        BuildingPlacementController.UnlockPlacement();
        RebalanceUniqueGlobalBuildings();
        RefreshAllNodeButtons();

        var actionPanel = FindFirstObjectByType<TerritoryActionPanel>();
        if (actionPanel != null) actionPanel.Refresh();

        Debug.Log($"[WorldMapManager] 노드 {currentNodeID} 전체 재건 완료 — 잔해 {restored}개 복구 + 점령 확정");
    }

    // ── 영주성 재건 카드 효과 ─────────────────────────────────────
    // MansionBehaviour.OnPlaced() 또는 카드 시스템에서 영주성 재건 시 호출.
    // 빈 노드(또는 유닛만 주둔한 노드)에 영주성을 짓는 순간 점령 완료 처리.
    public void OnMansionRebuilt()
    {
        NodeData node = GetNode(currentNodeID);
        if (node == null)
        {
            Debug.LogWarning("[WorldMapManager] 현재 진입한 노드가 없습니다.");
            return;
        }
        node.isMansionBuilt = true;
        node.ownerCivID = 0;                  // 점령 완료
        BuildingPlacementController.UnlockPlacement();
        RebalanceUniqueGlobalBuildings();     // isUniqueGlobal 건물 active 상태 재조정
        RefreshAllNodeButtons();
        Debug.Log($"[WorldMapManager] 노드 {currentNodeID} 영주성 재건 완료 — 점령 + 배치 잠금 해제.");

        // 공격/이동 버튼 활성 조건이 바뀌었으므로 패널 갱신
        var actionPanel = FindFirstObjectByType<TerritoryActionPanel>();
        if (actionPanel != null) actionPanel.Refresh();
    }

    // 현재 건물 배치 가능 여부 (카드 UI 등에서 버튼 활성화 판단용)
    public bool IsBuildingAllowed => BuildingPlacementController.IsPlacementLocked == false;

    // ── 노드 소유권 변경 (외부 시스템 — 전투 결과 등 — 에서 호출) ─
    public void SetNodeOwner(int nodeID, int civID)
    {
        NodeData node = GetNode(nodeID);
        if (node == null) return;
        node.ownerCivID = civID;
        if (civID != 0) node.isMansionBuilt = false; // 점령당하면 영주성 초기화
        RebalanceUniqueGlobalBuildings();             // isUniqueGlobal 건물 active 상태 재조정
        RefreshAllNodeButtons();
    }

    // 플레이어 소유 노드 전체에 해당 buildingType 건물이 존재하는지 확인 (TileMapManager.CanPlace에서 사용)
    public bool HasUniqueGlobalBuilding(BuildingType type)
    {
        foreach (NodeData node in allNodes)
        {
            if (node.ownerCivID != 0) continue;
            foreach (BuildingInstance b in node.buildings)
            {
                if (b.data != null && b.data.buildingType == type)
                    return true;
            }
        }
        return false;
    }

    // isUniqueGlobal 건물(Lab 등) 활성화 상태 재조정
    // 플레이어 소유 노드에서 같은 buildingType은 1개만 active=true, 나머지는 false
    // 소유권 변경(점령/피탈)마다 호출 → active였던 건물을 잃으면 남은 것이 자동 활성화
    private void RebalanceUniqueGlobalBuildings()
    {
        // buildingType별로 플레이어 소유 노드의 isUniqueGlobal 건물 수집
        var grouped = new System.Collections.Generic.Dictionary<BuildingType, System.Collections.Generic.List<BuildingInstance>>();

        foreach (NodeData node in allNodes)
        {
            if (node.ownerCivID != 0) continue; // 플레이어 노드만
            foreach (BuildingInstance b in node.buildings)
            {
                if (b.data == null || !b.data.isUniqueGlobal) continue;
                BuildingType t = b.data.buildingType;
                if (!grouped.ContainsKey(t))
                    grouped[t] = new System.Collections.Generic.List<BuildingInstance>();
                grouped[t].Add(b);
            }
        }

        foreach (var pair in grouped)
        {
            var list = pair.Value;

            // 이미 active인 건물이 있으면 그것을 유지, 없으면 첫 번째를 활성화
            BuildingInstance activeOne = null;
            foreach (BuildingInstance b in list)
            {
                if (b.isActive) { activeOne = b; break; }
            }
            if (activeOne == null)
                activeOne = list[0];

            // active 하나만 남기고 나머지 비활성
            foreach (BuildingInstance b in list)
                b.isActive = (b == activeOne);
        }
    }

    // ── 유닛 주둔 상태 변경 (전투 시스템에서 호출) ───────────────
    // 전투 승리 후 유닛이 해당 노드에 진입할 때 true
    // 유닛이 모두 철수하거나 전멸하면 false
    public void SetPlayerUnitsPresent(int nodeID, bool present)
    {
        NodeData node = GetNode(nodeID);
        if (node == null) return;
        node.hasPlayerUnits = present;
        RefreshAllNodeButtons();
    }

    // ── 오프스크린 건물 턴 처리 ──────────────────────────────────
    // GameManager.EndTurn()에서 호출.
    // 플레이어가 진입 중이지 않은 노드의 건물 savedState.tick을 직접 가산.
    // 시각 없이 숫자만 처리 (프리팹 비활성 상태이므로 SpawnUnit 호출 안 함).
    public void TickOffscreenBuildings()
    {
        foreach (NodeData node in allNodes)
        {
            if (node.nodeID == currentNodeID) continue; // 진입 중인 노드는 GameManager에서 이미 처리
            if (node.ownerCivID != 0) continue;          // 플레이어 소유 노드만 (적 노드는 별도 시스템)
            foreach (BuildingInstance b in node.buildings)
                TickOffscreen(node, b);
        }
    }

    // 오프스크린 1턴 처리 — 온라인 UnitProducerBehaviour.OnTurnEnd → TrySpawnFromQueue 와 동일한 모델로 동작.
    //   tick++ → interval 도달 시 waiting++ (1 사이클 = unitsPerCycle 명)
    //   waiting > 0 && activeCount < capacity 이면 pendingSlot 순서로 1명씩 채움 + NodeData.units 누적
    private static void TickOffscreen(NodeData node, BuildingInstance b)
    {
        if (b == null || b.data == null) return;
        if (!b.isActive || b.isRuin) return;            // 비활성/잔해 건물은 생산 정지
        int capacity = b.data.unitCapacity;
        if (capacity <= 0) return;                       // 생산 건물 아님

        var state = b.savedState;
        state.tick++;

        int interval = b.data.productionInterval > 0 ? b.data.productionInterval : 3;
        if (state.tick >= interval)
        {
            state.tick = 0;
            state.waiting++;
        }

        int per = Mathf.Max(1, b.data.unitsPerCycle);
        while (state.waiting > 0 && state.activeCount < capacity)
        {
            UnitType produced = GetProducedUnitType(b.data.buildingType, state.pendingSlot);
            AddUnitToNode(node, produced, 1);

            state.activeCount++;
            state.pendingSlot++;
            if (state.pendingSlot >= per)
            {
                state.pendingSlot = 0;
                state.waiting--;
            }
        }
    }

    // BuildingType + 사이클 슬롯 → 생산되는 UnitType. 오프스크린 tick 등 Behaviour 없이 결정해야 할 때 사용.
    public static UnitType GetProducedUnitType(BuildingType buildingType, int slotInCycle)
    {
        switch (buildingType)
        {
            case BuildingType.Barracks_SoldierStone:
            case BuildingType.Barracks_SoldierBronze:
            case BuildingType.Barracks_SoldierIron:
                return UnitType.RockWarrior;

            case BuildingType.Barracks_ArcheryRange_Medic:
            case BuildingType.Barracks_ArcheryRange_Medic_Elite:
                return slotInCycle == 0 ? UnitType.Archer : UnitType.Healer;

            case BuildingType.Barracks_Stable_Knight:
                return slotInCycle == 0 ? UnitType.Knight : UnitType.HorseWarrior;

            case BuildingType.Barracks_Wizard:
                return UnitType.Wizard;

            case BuildingType.Farm:
                return UnitType.Farmer;

            case BuildingType.Market:
                return UnitType.Shoper;

            default:
                return UnitType.Farmer; // fallback (생산 건물이 아닌 경우 호출되면 안 됨)
        }
    }

    // 유닛 사망 처리 — 플레이어가 노드에 들어와 있든 아니든 동작.
    // 호출자: 노화 시스템 / 전투 패배 / 이벤트 등.
    //   온라인(Behaviour 존재): NotifyUnitDied 위임 → 큐 즉시 보충
    //   오프라인:                  savedState.activeCount 직접 감소 → 다음 tick에서 자동 보충
    public static void OnUnitDied(NodeData node, BuildingInstance building, UnitType type)
    {
        if (node == null || building == null) return;

        if (building.behaviour is UnitProducerBehaviour producer)
        {
            producer.NotifyUnitDied(type);   // node.units 차감 + 보충까지 처리됨
            return;
        }

        // 오프라인 경로
        RemoveUnitFromNode(node, type, 1);
        var state = building.savedState;
        if (state.activeCount > 0) state.activeCount--;
    }

    // ── 현재 진입 중인 노드 ───────────────────────────────────────
    public int CurrentNodeID => currentNodeID;
    public bool IsInTerritoryView => currentNodeID != -1;

    // SaveManager에서 노드 전체 순회용 (읽기 전용)
    public IReadOnlyList<NodeData> AllNodes => allNodes;

    // ── 선택 모드 (공격/이동 타깃 선택) ──────────────────────────
    // TerritoryActionPanel의 [공격]/[이동] 버튼에서 호출.
    // 영지 뷰를 빠져나와 월드맵으로 돌아간 뒤, 후보 노드만 클릭 받음.

    public bool IsSelectable(int nodeID) => selectableNodeIDs.Contains(nodeID);
    public SelectionMode CurrentSelectionMode => selectionMode;
    public int SelectionSourceNodeID => selectionSourceNodeID;

    public void BeginAttackSelection()
    {
        if (isTransitioning || currentNodeID == -1) return;
        StartCoroutine(BeginSelectionCoroutine(currentNodeID, SelectionMode.Attack));
    }

    public void BeginMoveSelection()
    {
        if (isTransitioning || currentNodeID == -1) return;
        StartCoroutine(BeginSelectionCoroutine(currentNodeID, SelectionMode.Move));
    }

    private IEnumerator BeginSelectionCoroutine(int from, SelectionMode mode)
    {
        // 영지 뷰를 빠져나옴 (currentNodeID = -1로 클리어됨)
        yield return ExitTerritoryCoroutine();

        selectionMode = mode;
        selectionSourceNodeID = from;
        BuildSelectableSet();

        if (cancelSelectionButton != null) cancelSelectionButton.SetActive(true);
        RefreshAllNodeButtons();
    }

    private void BuildSelectableSet()
    {
        selectableNodeIDs.Clear();
        NodeData src = GetNode(selectionSourceNodeID);
        if (src == null) return;

        foreach (int adj in src.adjacentNodeIDs)
        {
            NodeData a = GetNode(adj);
            if (a == null) continue;
            if (selectionMode == SelectionMode.Attack && a.ownerCivID > 0)
                selectableNodeIDs.Add(adj);
            else if (selectionMode == SelectionMode.Move && a.ownerCivID == -1)
                selectableNodeIDs.Add(adj);
        }
    }

    // 우상단 [취소] 버튼에서 호출. 선택 모드 종료 후 월드맵에 그대로 머무름.
    public void CancelSelection()
    {
        selectionMode = SelectionMode.None;
        selectionSourceNodeID = -1;
        selectableNodeIDs.Clear();
        if (cancelSelectionButton != null) cancelSelectionButton.SetActive(false);
        RefreshAllNodeButtons();
    }

    // 후보 노드 클릭 시 모달 오픈
    private void OpenTroopDispatchModal(int targetNodeID)
    {
        if (troopDispatchModal == null)
        {
            Debug.LogError("[WorldMapManager] troopDispatchModal 미연결");
            return;
        }
        NodeData src = GetNode(selectionSourceNodeID);
        if (src == null) return;

        // 콜백 시점에 selectionMode가 None일 수 있어 미리 캡처
        SelectionMode mode = selectionMode;
        int from = selectionSourceNodeID;
        int to = targetNodeID;

        troopDispatchModal.Open(src.units, troops =>
        {
            if (mode == SelectionMode.Attack)
                DeclareAttack(from, to, troops);
            else
                DeclareMove(from, to, troops);

            CancelSelection();
        });
    }

    // ── 공격/이동 선언 ──────────────────────────────────────────
    // 모달 확정 시 호출됨. 출발 노드에서 병력 차감 후 후속 처리.
    private void DeclareAttack(int fromNodeID, int targetNodeID, Dictionary<UnitType, int> troops)
    {
        NodeData src = GetNode(fromNodeID);
        NodeData target = GetNode(targetNodeID);
        if (src == null || target == null) return;

        DeductTroops(src, troops);

        Debug.Log($"[WorldMapManager] 공격 선언: 노드 {fromNodeID} → {targetNodeID}, 파견 {TroopsToString(troops)}");
        RefreshAllNodeButtons();

        // ── 공격 상황 정보(Context) 저장 ──
        lastAttackContext = new BattleAttackContext
        {
            AttackerCivID = 0, // 플레이어 공격
            SourceNodeID = fromNodeID,
            TargetNodeID = targetNodeID,
            DispatchedTroops = troops
        };

        // 가상 노드 생성 및 전투 진입
        NodeData attackerNode = new NodeData { nodeID = -999, ownerCivID = 0 };
        foreach (var kv in troops)
            attackerNode.units.Add(new NodeUnit { unitType = kv.Key, count = kv.Value });

        if (battleSessionController != null)
            battleSessionController.EnterBattle(attackerNode, target);
        else
            Debug.Log("[WorldMapManager] battleSessionController 미연결");

        RefreshAllNodeButtons();
    }

    // 기존의 pending... 필드들을 하나로 통합
    private BattleAttackContext lastAttackContext;
    // AI가 정보를 기록할 수 있게 열어주는 함수
    public void SetLastAttackContext(BattleAttackContext context)
    {
        this.lastAttackContext = context;
    }
    // 위에서 만든 유틸리티 (EnemyBrainBase에서 호출하기 편하게)
    public static Dictionary<UnitType, int> ConvertNodeUnitsToDict(List<NodeUnit> units)
    {
        var dict = new Dictionary<UnitType, int>();
        if (units == null) return dict;
        foreach (var u in units) dict[u.unitType] = u.count;
        return dict;
    }



    // 전투 승리 후 적 노드 건물 정리.
    //   완전 파괴: 군사 건물 / Lab / PotionBuilding / TrapWorkshop 그 외 비-잔해 타입 → 리스트에서 제거
    //   잔해 유지: Mansion / House / Market / Farm → isRuin=true, isActive=false (재건 카드로 복구)
    private static void DemolishCapturedNodeBuildings(NodeData target)
    {
        if (target == null || target.buildings == null) return;
        target.buildings.RemoveAll(b =>
        {
            if (b == null || b.data == null) return true;
            if (IsRuinableType(b.data.buildingType))
            {
                b.isRuin = true;
                b.isActive = false;
                return false;
            }
            return true;
        });
    }

    private static bool IsRuinableType(BuildingType t)
    {
        return t == BuildingType.Mansion
            || t == BuildingType.House
            || t == BuildingType.Market
            || t == BuildingType.Farm;
    }

    private void DeclareMove(int fromNodeID, int targetNodeID, Dictionary<UnitType, int> troops)
    {
        NodeData src = GetNode(fromNodeID);
        NodeData target = GetNode(targetNodeID);
        if (src == null || target == null) return;

        DeductTroops(src, troops); // 출발지 차감
        AddTroops(target, troops); // 목적지 추가

        target.hasPlayerUnits = true; // 유닛 주둔 표시

        Debug.Log($"[WorldMapManager] 이동 완료: {fromNodeID} -> {targetNodeID}");
        RefreshAllNodeButtons();
    }

    private static void DeductTroops(NodeData node, Dictionary<UnitType, int> troops)
    {
        if (troops == null) return;
        foreach (var kv in troops)
        {
            if (kv.Value <= 0) continue;
            NodeUnit u = node.units.Find(x => x.unitType == kv.Key);
            if (u == null) continue;
            u.count = Mathf.Max(0, u.count - kv.Value);
        }
        node.units.RemoveAll(x => x.count <= 0);
    }

    private static void AddTroops(NodeData node, Dictionary<UnitType, int> troops)
    {
        if (troops == null) return;
        foreach (var kv in troops)
            AddUnitToNode(node, kv.Key, kv.Value);
    }

    // 건물 생산 시 호출. 노드의 NodeUnit 리스트에 해당 병종 카운트를 누적.
    public static void AddUnitToNode(NodeData node, UnitType type, int count)
    {
        if (node == null || count <= 0) return;
        NodeUnit u = node.units.Find(x => x.unitType == type);
        if (u == null) node.units.Add(new NodeUnit { unitType = type, count = count });
        else u.count += count;
    }

    // 유닛 사망 / 파견 차감 시 호출. count <= 0 이 되면 엔트리 자체를 제거.
    public static void RemoveUnitFromNode(NodeData node, UnitType type, int count)
    {
        if (node == null || count <= 0) return;
        NodeUnit u = node.units.Find(x => x.unitType == type);
        if (u == null) return;
        u.count -= count;
        if (u.count <= 0) node.units.Remove(u);
    }

    private static string TroopsToString(Dictionary<UnitType, int> troops)
    {
        if (troops == null || troops.Count == 0) return "(none)";
        var parts = new List<string>();
        foreach (var kv in troops) if (kv.Value > 0) parts.Add($"{kv.Key}:{kv.Value}");
        return parts.Count == 0 ? "(none)" : string.Join(", ", parts);
    }

    private void OnEnable()
    {
        // BattleManager의 OnBattleFinished 이벤트 구독
        if (battleSessionController != null)
        {
            var bm = battleSessionController.GetComponentInChildren<BattleManager>(true);
            if (bm != null)
                bm.OnBattleFinished += OnBattleFinished;
        }
    }
    private void OnDisable()
    {
        if (battleSessionController != null)
        {
            var bm = battleSessionController.GetComponentInChildren<BattleManager>(true);
            if (bm != null)
                bm.OnBattleFinished -= OnBattleFinished;
        }
    }
    private void OnBattleFinished(BattleResult result)
    {
        if (lastAttackContext == null) return;

        int attackerID = lastAttackContext.AttackerCivID;
        int targetID = lastAttackContext.TargetNodeID;
        NodeData target = GetNode(targetID);

        // 승리 팀이 공격자 팀과 일치하는지 확인 (AttackerCivID가 0이면 Player팀, 아니면 Enemy팀)
        BattleTeam attackerTeam = (attackerID == 0) ? BattleTeam.Player : BattleTeam.Enemy;

        if (result.Winner == attackerTeam)
        {
            // [승리 시] 땅 주인 변경
            SetNodeOwner(targetID, attackerID);

            // 생존 병력 투입 (공격자가 플레이어면 PlayerTroops, 아니면 EnemyTroops)
            var survivors = (attackerID == 0) ? result.SurvivingPlayerTroops : result.SurvivingEnemyTroops;
            AddTroops(target, survivors);

            if (attackerID == 0)
            {
                DemolishCapturedNodeBuildings(target); // 플레이어 점령 시에만 건물 잔해 처리
                target.hasPlayerUnits = true;
            }
            Debug.Log($"<color=cyan>[전투 결과]</color> 진영 {attackerID}가 노드 {targetID} 점령 완료!");
        }
        else
        {
            // [패배 시] 공격 측 병력 소모 (플레이어는 이미 Declare 시점에 차감됨)
            Debug.Log($"<color=orange>[전투 결과]</color> 노드 {targetID} 방어 성공 (공격자 {attackerID} 패배)");
        }

        lastAttackContext = null;
        RefreshAllNodeButtons();
    }



    //============================Ai전용 탐색 함수 건들지마셈^^=====================================
    public List<NodeData> GetAdjacentNodes(int nodeID)
    {
        NodeData node = GetNode(nodeID); //받아온 id조회 해서 node에 저장
        if (node == null) return new List<NodeData>(); //노드가 없으면 빈 리스트 반환
        List<NodeData> adjacentNodes = new List<NodeData>();// adjacentNodes 라는 새로운 노드 저장 공간 생성
        foreach (int adjID in node.adjacentNodeIDs) //인접 노드 id마다 반복
        {
            NodeData adjNode = GetNode(adjID); //인접 노드 id로 노드 조회
            if (adjNode != null) //있으면
                adjacentNodes.Add(adjNode); //추가
        }
        return adjacentNodes; //인접 노드 리스트 반환
    }

    public List<NodeData> GetNodesByCivID(int civID)
    {
        List<NodeData> result = new List<NodeData>();
        foreach (NodeData node in allNodes)
            if (node.ownerCivID == civID) result.Add(node);
        return result;
    }
}
public class BattleAttackContext
{
    public int AttackerCivID;               // 공격자 진영 ID (0=플레이어)
    public int SourceNodeID;                // 공격이 출발한 노드 ID
    public int TargetNodeID;                // 공격 대상 노드 ID
    public Dictionary<UnitType, int> DispatchedTroops; // 이번 공격에 투입된 병력
}