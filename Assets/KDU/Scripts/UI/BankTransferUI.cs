using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
// BankTransferUI — 은행 자원 운송 패널
//
// 구조:
//   panel
//   ├── nodeScrollView (ScrollRect)
//   │   └── Content (nodeListParent) — 인접 노드 버튼 동적 생성
//   ├── resourceInputArea — 노드 선택 후 활성화
//   │   ├── goldSlider
//   │   ├── foodSlider
//   │   └── researchSlider
//   ├── transferButton
//   └── closeButton
//
// 노드 수가 늘어나도 ScrollView Content에 버튼이 쌓이므로 확장성 있음.
// ============================================================
public class BankTransferUI : MonoBehaviour
{
    public static BankTransferUI Instance;

    [Header("루트 패널")]
    [SerializeField] private GameObject panel;

    [Header("노드 목록 (ScrollView Content에 연결)")]
    [SerializeField] private Transform nodeListParent;   // ScrollRect > Viewport > Content
    [SerializeField] private GameObject nodeButtonPrefab;

    [Header("자원 입력 영역 (노드 선택 후 활성화)")]
    [SerializeField] private GameObject resourceInputArea;
    [SerializeField] private Slider goldSlider;
    [SerializeField] private Slider foodSlider;
    [SerializeField] private Slider researchSlider;

    [Header("버튼")]
    [SerializeField] private Button transferButton;
    [SerializeField] private Button closeButton;

    private BankBehaviour currentBank;
    private int selectedTargetNodeID = -1;

    private void Awake()
    {
        Instance = this;

        if (closeButton   != null) closeButton.onClick.AddListener(Close);
        if (transferButton != null) transferButton.onClick.AddListener(OnTransferClicked);

        if (panel != null) panel.SetActive(false);
    }

    // 은행 클릭 시 호출 — 운송 패널 열기
    public void Open(BankBehaviour bank)
    {
        currentBank = bank;
        selectedTargetNodeID = -1;

        // 자원 입력 영역 숨기기 (노드 선택 전)
        if (resourceInputArea != null) resourceInputArea.SetActive(false);
        if (transferButton    != null) transferButton.interactable = false;

        // 기존 노드 버튼 제거
        if (nodeListParent != null)
        {
            foreach (Transform child in nodeListParent)
                Destroy(child.gameObject);
        }

        // 인접 플레이어 노드 버튼 동적 생성
        List<NodeData> nodes = bank.GetTransferableNodes();
        foreach (NodeData node in nodes)
        {
            if (nodeButtonPrefab == null || nodeListParent == null) break;

            GameObject btnGO = Instantiate(nodeButtonPrefab, nodeListParent);
            int capturedID = node.nodeID;

            // 버튼 텍스트: 노드 이름 + 현재 보유 자원
            Text label = btnGO.GetComponentInChildren<Text>();
            if (label != null)
                label.text = $"노드 {node.nodeID}\n금 {node.gold}  식량 {node.food}  연구 {node.research}";

            Button btn = btnGO.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => OnNodeSelected(capturedID));
        }

        panel.SetActive(true);
    }

    // 노드 선택 — 자원 입력 영역 활성화, 슬라이더 최대값을 현재 노드 보유량으로 설정
    public void OnNodeSelected(int nodeID)
    {
        selectedTargetNodeID = nodeID;

        WorldMapManager worldMap = WorldMapManager.Instance;
        if (worldMap == null) return;

        NodeData currentNode = worldMap.GetNode(worldMap.CurrentNodeID);
        if (currentNode == null) return;

        // 슬라이더 범위: 0 ~ 현재 노드 보유량
        if (goldSlider != null)
        {
            goldSlider.minValue = 0;
            goldSlider.maxValue = currentNode.gold;
            goldSlider.value    = 0;
        }
        if (foodSlider != null)
        {
            foodSlider.minValue = 0;
            foodSlider.maxValue = currentNode.food;
            foodSlider.value    = 0;
        }
        if (researchSlider != null)
        {
            researchSlider.minValue = 0;
            researchSlider.maxValue = currentNode.research;
            researchSlider.value    = 0;
        }

        // 자원 입력 영역 표시
        if (resourceInputArea != null) resourceInputArea.SetActive(true);
        if (transferButton    != null) transferButton.interactable = true;
    }

    // "전송" 버튼 클릭
    public void OnTransferClicked()
    {
        if (currentBank == null || selectedTargetNodeID < 0) return;

        int gold     = goldSlider     != null ? (int)goldSlider.value     : 0;
        int food     = foodSlider     != null ? (int)foodSlider.value     : 0;
        int research = researchSlider != null ? (int)researchSlider.value : 0;

        bool success = currentBank.TransferResources(selectedTargetNodeID, gold, food, research);
        if (success)
        {
            Close();
            // TODO: 자원 UI 갱신 (다른 팀원 담당)
        }
    }

    // 패널 닫기
    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        currentBank = null;
        selectedTargetNodeID = -1;
    }
}
