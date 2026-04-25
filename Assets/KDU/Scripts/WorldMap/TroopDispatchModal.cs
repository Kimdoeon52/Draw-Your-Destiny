using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
// TroopDispatchModal — 병력 수 선택 모달
//
// WorldMapManager가 후보 노드 클릭 시 Open()으로 호출.
// 출발 노드의 NodeUnit 리스트 중 전투 유닛만 행으로 표시.
// 사용자가 수량 조정 후 [확정] → 콜백으로 Dictionary<UnitType,int> 반환.
// [취소] → 콜백 호출 없이 닫기. 호출자(WorldMapManager)는 선택 모드를 유지함.
//
// 씬 구성:
//   root        — 모달 전체 GameObject (활성/비활성 토글 대상)
//   rowContainer — 행이 들어가는 부모 (VerticalLayoutGroup 권장)
//   rowPrefab   — TroopDispatchRow 프리팹
//   confirmButton / cancelButton
// ============================================================
public class TroopDispatchModal : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private TroopDispatchRow rowPrefab;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private System.Action<Dictionary<UnitType, int>> onConfirm;
    private readonly List<TroopDispatchRow> rows = new List<TroopDispatchRow>();

    private void Awake()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        if (cancelButton  != null) cancelButton.onClick.AddListener(OnCancelClicked);
        if (root != null) root.SetActive(false);
    }

    public void Open(List<NodeUnit> sourceUnits, System.Action<Dictionary<UnitType, int>> onConfirm)
    {
        this.onConfirm = onConfirm;
        ClearRows();

        if (sourceUnits != null)
        {
            foreach (NodeUnit u in sourceUnits)
            {
                if (u == null) continue;
                if (!IsCombatUnit(u.unitType)) continue;
                if (u.count <= 0) continue;

                TroopDispatchRow row = Instantiate(rowPrefab, rowContainer);
                row.Bind(u.unitType, u.count);
                row.OnValueChanged = RefreshConfirmInteractable;
                rows.Add(row);
            }
        }

        if (root != null) root.SetActive(true);
        RefreshConfirmInteractable();
    }

    private void OnConfirmClicked()
    {
        var result = new Dictionary<UnitType, int>();
        int total = 0;
        foreach (TroopDispatchRow r in rows)
        {
            if (r.Value > 0)
            {
                result[r.UnitType] = r.Value;
                total += r.Value;
            }
        }
        if (total <= 0) return; // 0명 파견은 무효

        var cb = onConfirm;
        Close();
        cb?.Invoke(result);
    }

    private void OnCancelClicked()
    {
        // 콜백 호출 없이 닫기. WorldMapManager는 선택 모드 유지.
        onConfirm = null;
        Close();
    }

    private void Close()
    {
        if (root != null) root.SetActive(false);
        ClearRows();
    }

    private void ClearRows()
    {
        foreach (TroopDispatchRow r in rows)
            if (r != null) Destroy(r.gameObject);
        rows.Clear();
    }

    private void RefreshConfirmInteractable()
    {
        if (confirmButton == null) return;
        int total = 0;
        foreach (TroopDispatchRow r in rows) total += r.Value;
        confirmButton.interactable = total > 0;
    }

    // 경제 유닛(Farmer/Shoper) 제외
    private static bool IsCombatUnit(UnitType t)
    {
        return t != UnitType.Farmer && t != UnitType.Shoper;
    }
}
