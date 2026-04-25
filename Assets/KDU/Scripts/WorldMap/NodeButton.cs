using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
// NodeButton — 월드맵 노드 UI 버튼 1개
//
// 씬의 각 노드 버튼 GameObject에 부착.
// WorldMapManager.OnNodeClicked(nodeID)를 호출하여 클릭 처리를 위임.
//
// 시각 구조 (통합 배경 위에 올라가는 레이어):
//   root Image      — region 모양 스프라이트, 진영 색 틴트 (alpha 낮춤). alphaHitTest로 모양대로 클릭.
//   stateOverlay    — 선택 모드일 때만 활성. 후보면 하이라이트 색, 비후보면 디밍 색.
//   nameLabel       — 노드 이름 (위에 그려짐)
//   mansionIcon     — 영주성 재건 여부 아이콘
// ============================================================
[RequireComponent(typeof(Button))]
public class NodeButton : MonoBehaviour
{
    [Header("노드 식별")]
    [SerializeField] private int nodeID;

    [Header("UI 참조 (선택)")]
    [SerializeField] private Image backgroundImage;     // 노드 영역 모양 + 진영 색 틴트
    [SerializeField] private Image stateOverlay;        // 선택 모드 하이라이트/디밍 (raycastTarget=false)
    [SerializeField] private TextMeshProUGUI nameLabel; // 노드 이름
    [SerializeField] private GameObject mansionIcon;    // 영주성 재건 완료 아이콘

    public int NodeID => nodeID;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (backgroundImage != null)
            backgroundImage.alphaHitTestMinimumThreshold = 0.3f;

        if (stateOverlay != null)
            stateOverlay.gameObject.SetActive(false);
    }

    private void OnClick()
    {
        WorldMapManager.Instance?.OnNodeClicked(nodeID);
    }

    // WorldMapManager.RefreshAllNodeButtons()에서 호출
    public void Refresh(NodeData data)
    {
        if (data == null) return;

        var mgr = WorldMapManager.Instance;

        // 진영 색 — 알파 낮춰서 통합 배경이 비치도록
        if (backgroundImage != null)
        {
            Color c = data.ownerCivID == -1
                ? WorldMapManager.NeutralColor
                : WorldMapManager.CivColors[data.ownerCivID];
            c.a = mgr != null ? mgr.civTintAlpha : 0.3f;
            backgroundImage.color = c;
        }

        // 영주성 아이콘
        if (mansionIcon != null)
            mansionIcon.SetActive(data.isMansionBuilt);

        // 상태 오버레이 — 선택 모드 / 후보 여부에 따라
        if (stateOverlay != null)
        {
            if (mgr == null || mgr.CurrentSelectionMode == WorldMapManager.SelectionMode.None)
            {
                stateOverlay.gameObject.SetActive(false);
            }
            else
            {
                stateOverlay.gameObject.SetActive(true);
                stateOverlay.color = mgr.IsSelectable(nodeID)
                    ? mgr.highlightColor
                    : mgr.dimColor;
            }
        }
    }
}
