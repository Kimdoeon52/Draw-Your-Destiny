using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
// NodeButton — 월드맵 노드 UI 버튼 1개
//
// 씬의 각 노드 버튼 GameObject에 부착.
// WorldMapManager.OnNodeClicked(nodeID)를 호출하여 클릭 처리를 위임.
//
// 시각 상태:
//   배경 Image 색상 — 소유 문명 색상(CivColors) 또는 중립색
//   nameLabel       — 노드 이름 표시 (선택 사항)
//   mansionIcon     — 영주성 재건 여부 아이콘 (선택 사항)
// ============================================================
[RequireComponent(typeof(Button))]
public class NodeButton : MonoBehaviour
{
    [Header("노드 식별")]
    [SerializeField] private int nodeID;

    [Header("UI 참조 (선택)")]
    [SerializeField] private Image backgroundImage;     // 노드 배경 색상
    [SerializeField] private TextMeshProUGUI nameLabel; // 노드 이름
    [SerializeField] private GameObject mansionIcon;    // 영주성 재건 완료 아이콘

    public int NodeID => nodeID;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if(backgroundImage != null)
            backgroundImage.alphaHitTestMinimumThreshold = 0.3f;
    }

    private void OnClick()
    {
        WorldMapManager.Instance?.OnNodeClicked(nodeID);
    }

    // WorldMapManager.RefreshAllNodeButtons()에서 호출
    public void Refresh(NodeData data)
    {
        if (data == null) return;

        // 배경 색상 갱신
        if (backgroundImage != null)
        {
            backgroundImage.color = data.ownerCivID == -1
                ? WorldMapManager.NeutralColor
                : WorldMapManager.CivColors[data.ownerCivID];
        }

        // 영주성 아이콘 갱신
        if (mansionIcon != null)
            mansionIcon.SetActive(data.isMansionBuilt);
    }
}
