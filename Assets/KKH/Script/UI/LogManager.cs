using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using System;


public class LogManager : MonoBehaviour
{
    public static LogManager instance;
    [Header("UI References")]
    [SerializeField] private GameObject logBubblePrefab; // 말풍선 프리팹 (Background 이미지 + Text)
    [SerializeField] private Transform spawnPoint;       // 로그가 처음 생성될 위치 (화면 좌측 하단)

    [Header("Settings")]
    [SerializeField] private float logSpacing = 60f;     // 로그 사이의 간격 (위로 밀어낼 거리)
    [SerializeField] private float displayDuration = 3f; // 로그가 떠 있는 시간
    [SerializeField] private float fadeDuration = 1f;    // 사라지는 시간

    private List<RectTransform> activeLogs = new List<RectTransform>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
    public void AddLog(string message, Color textColor)
    {

        MoveExistingLogs(); // 기존 로그 위로 이동

        GameObject newLog = Instantiate(logBubblePrefab, spawnPoint);
        RectTransform rect = newLog.GetComponent<RectTransform>();
        CanvasGroup griyo = newLog.GetComponent<CanvasGroup>();
        TextMeshProUGUI tmp = newLog.GetComponent<TextMeshProUGUI>();

        if (tmp != null)
        {
            tmp.text = message;
            tmp.color = textColor;
        }
    }

    private void MoveExistingLogs()
    {
        throw new NotImplementedException();
    }
}
