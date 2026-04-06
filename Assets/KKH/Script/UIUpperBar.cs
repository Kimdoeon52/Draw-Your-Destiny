using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIUpperBar : MonoBehaviour
{
    [Header("자원 텍스트")]
    [SerializeField] private TextMeshProUGUI goldText; //골드 텍스트
    [SerializeField] private TextMeshProUGUI researchText; //연구 텍스트
    [SerializeField] private TextMeshProUGUI populationText; //인구 텍스트
    [SerializeField] private TextMeshProUGUI foodText; //식량 텍스트
    [SerializeField] private TextMeshProUGUI ironText; //광석 텍스트

    [Header("시대 텍스트")]
    [SerializeField] private TextMeshProUGUI eraText; // 시대 텍스트

    void Awake()
    {
        Debug.Log("[UIUpperBar] Awake called.");
    }
    private void Start()
    {
        // 씬이 시작될 때 현재 매니저의 이벤트에 내 함수를 등록
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourceChanged += UpdateResourceUI;
            // 등록하자마자 현재 값으로 UI 초기화
            UpdateResourceUI(
                ResourceManager.Instance.Gold, 
                ResourceManager.Instance.Research, 
                ResourceManager.Instance.Population,
                ResourceManager.Instance.Food,
                ResourceManager.Instance.Iron
            );
        }

        if (GameManager.Instance != null)
        {
            Debug.Log("[UIUpperBar] GameManager Instance found. Subscribing to OnEraChanged.");
            GameManager.Instance.OnEraChanged += UpdateEraUI;
            UpdateEraUI(GameManager.Instance.playerEra);
        }
        else
          {
            Debug.LogError("[UIUpperBar] GameManager Instance is NULL. Era UI will not update."); // 

          }
    }
    private void OnDestroy()
    {
        // 씬이 바뀌거나 UI가 파괴될 때 구독 해제 (중요: 메모리 누수 및 에러 방지)
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourceChanged -= UpdateResourceUI;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEraChanged -= UpdateEraUI;
        }
    }

    private void UpdateResourceUI(int gold, int research, int pop, int food, int iron)
    {
        if (goldText)
        {
            goldText.text = gold.ToString("N0"); //골드 텍스트 업데이트
        }
        if (researchText)
        {
            researchText.text = research.ToString(); //연구 텍스트 업데이트
        }
        if (populationText)
        {
            // 인구 텍스트 업데이트 (현재 인구 / 최대 인구)
            int maxPop = ResourceManager.Instance.MaxPopulation;
            populationText.text = $"{pop}/{maxPop}";
        }
        if (foodText)
        {
            foodText.text = food.ToString("N0"); //식량 텍스트 업데이트
        }
        if (ironText)
        {
            ironText.text = iron.ToString("N0"); //광석 텍스트 업데이트
        }
    }

    private void UpdateEraUI(Era era)
    { Debug.Log($"[UIUpperBar] UpdateEraUI called. New Era: {era.ToString()}"); // 이 줄 추가   
        if (eraText)
        {
            eraText.text = $"시대: {era.ToString()}"; // 시대 텍스트 업데이트
        }
    }
}
