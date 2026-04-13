using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 상단 HUD 바의 자원·시대·턴 정보를 표시하는 UI 컴포넌트.
/// <see cref="ResourceManager.OnResourceChanged"/>와 <see cref="GameManager.OnEraChanged"/>
/// 이벤트를 구독하여 자동으로 갱신된다.
/// </summary>
public class UIUpperBar : MonoBehaviour
{
    #region ── Inspector: 자원 텍스트 ──

    [Header("자원 텍스트")]
    [Tooltip("골드 표시용 TMP")]       [SerializeField] private TextMeshProUGUI goldText;
    [Tooltip("연구 표시용 TMP")]       [SerializeField] private TextMeshProUGUI researchText;
    [Tooltip("인구 표시용 TMP")]       [SerializeField] private TextMeshProUGUI populationText;
    [Tooltip("식량 표시용 TMP")]       [SerializeField] private TextMeshProUGUI foodText;

    #endregion

    #region ── Inspector: 상태 텍스트 ──

    [Header("시대 텍스트")]
    [Tooltip("현재 시대 표시용 TMP")]   [SerializeField] private TextMeshProUGUI eraText;

    [Header("턴 텍스트")]
    [Tooltip("현재 턴 표시용 TMP")]     [SerializeField] private TextMeshProUGUI turnText;

    #endregion

    // =====================================================================
    #region ── Unity 생명주기 ──
    // =====================================================================

    private void Awake()
    {
        Debug.Log("[UIUpperBar] Awake called.");
    }

    private void Start()
    {
        // ResourceManager 이벤트 구독 + 즉시 UI 초기화
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourceChanged += UpdateResourceUI;
            UpdateResourceUI(
                ResourceManager.Instance.Gold,
                ResourceManager.Instance.Research,
                ResourceManager.Instance.Population,
                ResourceManager.Instance.Food
            );
        }

        // GameManager 이벤트 구독 + 즉시 시대 UI 초기화
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEraChanged += UpdateEraUI;
            UpdateEraUI(GameManager.Instance.playerEra);
        }
        else
        {
            Debug.LogError("[UIUpperBar] GameManager 인스턴스를 찾을 수 없습니다. 시대 UI가 갱신되지 않습니다.");
        }
    }

    /// <summary>
    /// 파괴 시 이벤트 구독을 해제한다 (메모리 누수 및 MissingReference 방지).
    /// </summary>
    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged -= UpdateResourceUI;

        if (GameManager.Instance != null)
            GameManager.Instance.OnEraChanged -= UpdateEraUI;
    }

    #endregion

    // =====================================================================
    #region ── UI 갱신 ──
    // =====================================================================

    /// <summary>
    /// 자원 변경 이벤트 콜백. 골드·연구·인구·식량 텍스트를 갱신한다.
    /// </summary>
    private void UpdateResourceUI(int gold, int research, int pop, int food)
    {
        if (goldText)
            goldText.text = gold.ToString("N0");

        if (researchText)
            researchText.text = research.ToString();

        if (populationText)
        {
            int maxPop = ResourceManager.Instance.MaxPopulation;
            populationText.text = $"{pop}/{maxPop}";
        }

        if (foodText)
            foodText.text = food.ToString("N0");

        // 턴 정보 갱신
        int currentTurn = GameManager.Instance != null ? GameManager.Instance.currentTurn : 0;
        turnText.text = $"현재턴: {currentTurn}턴";
    }

    /// <summary>
    /// 시대 변경 이벤트 콜백. 시대 텍스트를 갱신한다.
    /// </summary>
    private void UpdateEraUI(Era era)
    {
        if (eraText)
            eraText.text = $"시대: {era}";
    }

    #endregion
}
