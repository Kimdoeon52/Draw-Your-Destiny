using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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

    [Header("빈 노드 점령 비용")]
    public int claimCost = 30;          // 빈 노드 즉시 점령에 필요한 금

    // 문명 ID별 노드 배경 색상 (0=플레이어 파랑, 1=AI 빨강, 2=AI 초록, 3=AI 노랑, -1=중립 회색)
    public static readonly Color[] CivColors =
    {
        new Color(0.2f, 0.5f, 1f,  1f),   // 0 플레이어
        new Color(1f,   0.3f, 0.3f, 1f),  // 1 AI1
        new Color(0.3f, 0.85f,0.3f, 1f),  // 2 AI2
        new Color(1f,   0.8f, 0.1f, 1f),  // 3 AI3
    };
    public static readonly Color NeutralColor = new Color(0.6f, 0.6f, 0.6f, 1f);

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
        if (worldMapView  != null) worldMapView.SetActive(true);
        if (territoryView != null) territoryView.SetActive(false);
        if (fadePanel     != null) fadePanel.alpha = 0f;
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

    // ── NodeButton → 클릭 처리 진입점 ────────────────────────────
    public void OnNodeClicked(int nodeID)
    {
        if (isTransitioning) return;

        NodeData node = GetNode(nodeID);
        if (node == null)
        {
            Debug.LogWarning($"[WorldMapManager] nodeID {nodeID} 를 찾을 수 없습니다.");
            return;
        }

        if (node.ownerCivID == 0)
        {
            // 아군 노드 → 영지 뷰 진입
            StartCoroutine(EnterTerritoryCoroutine(node));
        }
        else if (node.ownerCivID == -1)
        {
            // 빈 노드 → 인접 아군 노드가 있을 때만 점령 가능
            if (IsAdjacentToPlayer(nodeID))
                TryClaimNode(node);
            else
                Debug.Log("[WorldMapManager] 인접한 아군 노드가 없어 점령할 수 없습니다.");
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

    // ── 빈 노드 점령 ─────────────────────────────────────────────
    private void TryClaimNode(NodeData node)
    {
        if (ResourceManager.Instance == null) return;
        if (ResourceManager.Instance.Gold < claimCost)
        {
            Debug.Log($"[WorldMapManager] 골드 부족. 점령 비용: {claimCost}");
            return;
        }

        ResourceManager.Instance.AddGold(-claimCost);
        node.ownerCivID = 0;
        node.isMansionBuilt = false;

        Debug.Log($"[WorldMapManager] 노드 {node.nodeID} 점령 완료 (비용 {claimCost}G)");
        RefreshAllNodeButtons();
    }

    // ── 영지 뷰 진입 ─────────────────────────────────────────────
    private IEnumerator EnterTerritoryCoroutine(NodeData node)
    {
        isTransitioning = true;
        currentNodeID = node.nodeID;

        // 페이드 아웃
        yield return FadeTo(1f);

        // 월드맵 뷰 비활성 → 영지 뷰 활성
        if (worldMapView  != null) worldMapView.SetActive(false);
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
        if (worldMapView  != null) worldMapView.SetActive(true);

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
        yield return fadePanel.DOFade(targetAlpha, fadeDuration).WaitForCompletion();
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

    // ── 영주성 재건 카드 효과 ─────────────────────────────────────
    // 카드 시스템에서 영주성 재건 카드 사용 시 호출.
    // isMansionBuilt = true로 설정하고 배치 UI 잠금 해제.
    public void OnMansionRebuilt()
    {
        NodeData node = GetNode(currentNodeID);
        if (node == null)
        {
            Debug.LogWarning("[WorldMapManager] 현재 진입한 노드가 없습니다.");
            return;
        }
        node.isMansionBuilt = true;
        BuildingPlacementController.UnlockPlacement();
        Debug.Log($"[WorldMapManager] 노드 {currentNodeID} 영주성 재건 완료 — 배치 잠금 해제.");
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
        RefreshAllNodeButtons();
    }

    // ── 현재 진입 중인 노드 ───────────────────────────────────────
    public int CurrentNodeID => currentNodeID;
    public bool IsInTerritoryView => currentNodeID != -1;
}
