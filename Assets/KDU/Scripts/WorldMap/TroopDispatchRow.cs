using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
// TroopDispatchRow — 병력 수 선택 모달의 한 줄 (UnitType 1개)
//
// TroopDispatchModal에서 NodeUnit 하나당 1개씩 동적 생성.
// [-] [현재값/최대값] [+] 스테퍼 형태.
// ============================================================
public class TroopDispatchRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;       // 유닛 종류 이름
    [SerializeField] private TextMeshProUGUI valueLabel;  // "n / max"
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;

    public UnitType UnitType { get; private set; }
    public int Value { get; private set; }
    public int Max { get; private set; }

    public System.Action OnValueChanged;

    public void Bind(UnitType type, int max)
    {
        UnitType = type;
        Max = max;
        Value = 0;

        if (label != null) label.text = type.ToString();
        UpdateValueLabel();

        if (minusButton != null)
        {
            minusButton.onClick.RemoveAllListeners();
            minusButton.onClick.AddListener(() => SetValue(Value - 1));
        }
        if (plusButton != null)
        {
            plusButton.onClick.RemoveAllListeners();
            plusButton.onClick.AddListener(() => SetValue(Value + 1));
        }
    }

    private void SetValue(int v)
    {
        int clamped = Mathf.Clamp(v, 0, Max);
        if (clamped == Value) return;
        Value = clamped;
        UpdateValueLabel();
        OnValueChanged?.Invoke();
    }

    private void UpdateValueLabel()
    {
        if (valueLabel != null) valueLabel.text = $"{Value} / {Max}";
    }
}
