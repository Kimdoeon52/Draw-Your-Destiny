using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    [Header("UI Popups")]
    public GameObject optionsPopup;
    public GameObject creditsPopup;

    private SettingsManager settings;

    void Start()
    {
        settings = GetComponent<SettingsManager>();
        // 시작 시 모든 팝업은 비활성화
        if (optionsPopup) optionsPopup.SetActive(false);
        if (creditsPopup) creditsPopup.SetActive(false);
    }

    // --- 버튼 이벤트 함수 ---
    public void OnClickStartGame()
    {
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.LoadScene("TestKKH");
    }

    public void OnClickOpenOptions() => optionsPopup.SetActive(true);

    public void OnClickCloseOptions()
    {
        if (settings != null) settings.SaveAllSettings();
        optionsPopup.SetActive(false);
    }

    public void OnClickOpenCredits() => creditsPopup.SetActive(true);
    public void OnClickCloseCredits() => creditsPopup.SetActive(false);

    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}