using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIUpperBar : MonoBehaviour
{
    [Header("자원 텍스트")]
    [SerializeField] private TextMeshProUGUI goldText; //골드 텍스트
    [SerializeField] private TextMeshProUGUI researchText; //골드 텍스트
    [SerializeField] private TextMeshProUGUI populationText; //골드 텍스트

    private void Start()
    {
        // 씬이 시작될 때 현재 매니저의 이벤트에 내 함수를 등록
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourceChanged += UpdateResourceUI;
            // 등록하자마자 현재 값으로 UI 초기화
            UpdateResourceUI(ResourceManager.Instance.Gold, ResourceManager.Instance.Research, ResourceManager.Instance.Population);
        }
    }
    private void OnDestroy()
    {
        // 씬이 바뀌거나 UI가 파괴될 때 구독 해제 (중요: 메모리 누수 및 에러 방지)
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourceChanged -= UpdateResourceUI;
        }
    }

    private void UpdateResourceUI(int gold, int research, int pop)
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
            populationText.text = $"{pop} / {20}"; // 인구 텍스트 업데이트 (현재 인구 / 최대 인구) : 최대 인구는 예시로 20으로 설정, 필요에 따라 변경 가능
        }
    }
}
