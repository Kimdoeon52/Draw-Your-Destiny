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

    private void OnEnable()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourceChanged += UpdateResourceUI;// 자원 변경 이벤트 구독
        }
    }
    private void OnDisable()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourceChanged -= UpdateResourceUI;// 자원 변경 이벤트 구독 해제
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
