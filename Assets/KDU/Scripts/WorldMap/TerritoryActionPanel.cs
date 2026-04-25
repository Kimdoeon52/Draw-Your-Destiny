using UnityEngine;
using UnityEngine.UI;

// ============================================================
// TerritoryActionPanel — 영지 뷰 HUD의 [공격]/[이동] 버튼 컨트롤러
//
// TerritoryView 하위(ExitButton 옆)에 배치.
// 영지 진입 시 OnEnable에서 인접 노드 상태에 따라 버튼 활성/비활성을 결정.
//
// 활성 조건:
//   본인 노드 (ownerCivID == 0) && 영주성 재건 완료 (isMansionBuilt)
//   - 인접에 적 노드 존재 → 공격 버튼 활성
//   - 인접에 빈 노드 존재 → 이동 버튼 활성
// 둘 다 활성 / 한쪽만 활성 / 둘 다 비활성 가능.
// hasPlayerUnits 주둔만 된 노드(영주성 재건 전)는 파견 불가 → 둘 다 비활성.
// ============================================================
public class TerritoryActionPanel : MonoBehaviour
{
    [SerializeField] private Button attackButton;
    [SerializeField] private Button moveButton;

    private void OnEnable()
    {
        if (attackButton != null) attackButton.onClick.AddListener(OnAttackClicked);
        if (moveButton   != null) moveButton.onClick.AddListener(OnMoveClicked);
        Refresh();
    }

    private void OnDisable()
    {
        if (attackButton != null) attackButton.onClick.RemoveListener(OnAttackClicked);
        if (moveButton   != null) moveButton.onClick.RemoveListener(OnMoveClicked);
    }

    // 영지 진입 / 영주성 재건 / 노드 상태 변화 등 외부 트리거에서 호출.
    public void Refresh()
    {
        var mgr = WorldMapManager.Instance;
        if (mgr == null) return;

        NodeData node = mgr.GetNode(mgr.CurrentNodeID);
        bool canDispatch = node != null && node.ownerCivID == 0 && node.isMansionBuilt;

        bool hasEnemyAdj = false;
        bool hasEmptyAdj = false;

        if (canDispatch)
        {
            foreach (int adj in node.adjacentNodeIDs)
            {
                NodeData a = mgr.GetNode(adj);
                if (a == null) continue;
                if (a.ownerCivID > 0)        hasEnemyAdj = true;
                else if (a.ownerCivID == -1) hasEmptyAdj = true;
            }
        }

        if (attackButton != null) attackButton.gameObject.SetActive(hasEnemyAdj);
        if (moveButton   != null) moveButton.gameObject.SetActive(hasEmptyAdj);
    }

    private void OnAttackClicked() => WorldMapManager.Instance?.BeginAttackSelection();
    private void OnMoveClicked()   => WorldMapManager.Instance?.BeginMoveSelection();
}
