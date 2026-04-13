using UnityEngine;

/// <summary>
/// 로비(메인 메뉴) 화면의 UI 이벤트를 처리한다.
/// 게임 시작, 옵션, 크레딧, 종료 버튼에 연결한다.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    #region ── Inspector: UI 팝업 ──

    [Header("UI 팝업")]
    [Tooltip("옵션 설정 팝업 패널")]
    public GameObject optionsPopup;

    [Tooltip("크레딧 팝업 패널")]
    public GameObject creditsPopup;

    #endregion

    private SettingsManager settings;

    private void Start()
    {
        settings = GetComponent<SettingsManager>();

        // 시작 시 모든 팝업 비활성화
        if (optionsPopup) optionsPopup.SetActive(false);
        if (creditsPopup) creditsPopup.SetActive(false);
    }

    // =====================================================================
    #region ── 버튼 이벤트 핸들러 (Inspector OnClick에 연결) ──
    // =====================================================================

    /// <summary>게임 시작 버튼 — TestKKH 씬으로 전환한다.</summary>
    public void OnClickStartGame()
    {
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.LoadScene("TestKKH");
    }

    /// <summary>옵션 팝업을 연다.</summary>
    public void OnClickOpenOptions() => optionsPopup.SetActive(true);

    /// <summary>옵션 팝업을 닫고 설정을 저장한다.</summary>
    public void OnClickCloseOptions()
    {
        if (settings != null) settings.SaveAllSettings();
        optionsPopup.SetActive(false);
    }

    /// <summary>크레딧 팝업을 연다.</summary>
    public void OnClickOpenCredits() => creditsPopup.SetActive(true);

    /// <summary>크레딧 팝업을 닫는다.</summary>
    public void OnClickCloseCredits() => creditsPopup.SetActive(false);

    /// <summary>게임을 종료한다 (에디터에서는 플레이 모드 중지).</summary>
    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}