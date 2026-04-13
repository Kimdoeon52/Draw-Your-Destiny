using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;
using System.Collections.Generic;

/// <summary>
/// 게임 설정(오디오 볼륨, 해상도, 전체화면)을 관리하는 싱글톤 매니저.
/// PlayerPrefs를 통해 설정을 저장/로드하며, UI 슬라이더·토글·드롭다운과 연결된다.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    /// <summary>전역 접근용 싱글톤 인스턴스.</summary>
    public static SettingsManager Instance;

    #region ── Inspector: UI 컴포넌트 ──

    [Header("UI 패널")]
    [Tooltip("설정 패널 루트 오브젝트")]
    public GameObject settingsPanel;

    [Header("오디오 컴포넌트")]
    [Tooltip("메인 오디오 믹서 (Master/BGM/SFX 그룹 포함)")]
    public AudioMixer mainMixer;

    [Tooltip("마스터 볼륨 슬라이더")] public Slider masterSlider;
    [Tooltip("BGM 볼륨 슬라이더")]    public Slider bgmSlider;
    [Tooltip("SFX 볼륨 슬라이더")]    public Slider sfxSlider;

    [Header("그래픽 컴포넌트")]
    [Tooltip("전체화면 토글")]              public Toggle fullscreenToggle;
    [Tooltip("해상도 선택 드롭다운")]        public TMP_Dropdown resolutionDropdown;

    #endregion

    /// <summary>시스템에서 지원하는 해상도 목록.</summary>
    private Resolution[] resolutions;

    // =====================================================================
    #region ── Unity 생명주기 ──
    // =====================================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupResolutions();
    }

    private void Start()
    {
        LoadAndApplySettings();
    }

    private void Update()
    {
        // ESC 키로 설정 패널 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingsPanel();
        }
    }

    #endregion

    // =====================================================================
    #region ── 설정 패널 제어 ──
    // =====================================================================

    /// <summary>설정 패널의 활성/비활성 상태를 토글한다.</summary>
    public void ToggleSettingsPanel()
    {
        bool isActive = !settingsPanel.activeSelf;
        settingsPanel.SetActive(isActive);
    }

    /// <summary>설정 패널을 닫는다. (닫기 버튼 OnClick에 연결)</summary>
    public void SetSettingPanelExit()
    {
        settingsPanel.SetActive(false);
    }

    #endregion

    // =====================================================================
    #region ── 오디오 제어 (슬라이더 OnValueChanged에 연결) ──
    // =====================================================================

    /// <summary>마스터 볼륨을 설정하고 PlayerPrefs에 저장한다.</summary>
    public void SetMasterVolume(float value)
    {
        mainMixer.SetFloat("Master", LinearToDecibel(value));
        PlayerPrefs.SetFloat("MasterVol", value);
    }

    /// <summary>BGM 볼륨을 설정하고 PlayerPrefs에 저장한다.</summary>
    public void SetBGMVolume(float value)
    {
        mainMixer.SetFloat("BGM", LinearToDecibel(value));
        PlayerPrefs.SetFloat("BGMVol", value);
    }

    /// <summary>SFX 볼륨을 설정하고 PlayerPrefs에 저장한다.</summary>
    public void SetSFXVolume(float value)
    {
        mainMixer.SetFloat("SFX", LinearToDecibel(value));
        PlayerPrefs.SetFloat("SFXVol", value);
    }

    /// <summary>선형 값(0~1)을 데시벨(-80~0)로 변환한다.</summary>
    private float LinearToDecibel(float linear)
    {
        return Mathf.Log10(Mathf.Max(0.0001f, linear)) * 20f;
    }

    #endregion

    // =====================================================================
    #region ── 그래픽 제어 (토글·드롭다운 OnValueChanged에 연결) ──
    // =====================================================================

    /// <summary>전체화면 모드를 설정하고 PlayerPrefs에 저장한다.</summary>
    public void SetFullscreen(bool isFull)
    {
        Screen.fullScreen = isFull;
        PlayerPrefs.SetInt("IsFull", isFull ? 1 : 0);
    }

    /// <summary>해상도를 변경하고 PlayerPrefs에 저장한다.</summary>
    /// <param name="index">resolutionDropdown의 선택 인덱스.</param>
    public void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResIndex", index);
    }

    /// <summary>
    /// Benchmark에서 결정된 품질 레벨을 적용한다.
    /// </summary>
    /// <param name="level">품질 레벨 (0 = 최저 ~ 5 = 최고).</param>
    public void ApplyAutoGraphics(int level)
    {
        QualitySettings.SetQualityLevel(level, true);
        Application.targetFrameRate = 144;

        PlayerPrefs.SetInt("GraphicQuality", level);
        PlayerPrefs.Save();

        Debug.Log($"[SettingsManager] 자동 그래픽 품질 단계 적용: {level}");
    }

    #endregion

    // =====================================================================
    #region ── 설정 저장/로드 ──
    // =====================================================================

    /// <summary>모든 설정을 PlayerPrefs에 저장한다.</summary>
    public void SaveAllSettings() => PlayerPrefs.Save();

    /// <summary>PlayerPrefs에서 저장된 설정을 불러와 UI 및 시스템에 적용한다.</summary>
    private void LoadAndApplySettings()
    {
        // UI 슬라이더·토글에 저장값 반영
        masterSlider.value     = PlayerPrefs.GetFloat("MasterVol", 0.8f);
        bgmSlider.value        = PlayerPrefs.GetFloat("BGMVol", 0.8f);
        sfxSlider.value        = PlayerPrefs.GetFloat("SFXVol", 0.8f);
        fullscreenToggle.isOn  = PlayerPrefs.GetInt("IsFull", 1) == 1;

        // 실제 값 적용
        SetMasterVolume(masterSlider.value);
        SetBGMVolume(bgmSlider.value);
        SetSFXVolume(sfxSlider.value);
        SetFullscreen(fullscreenToggle.isOn);
    }

    /// <summary>시스템 지원 해상도 목록을 드롭다운에 세팅한다.</summary>
    private void SetupResolutions()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add($"{resolutions[i].width} x {resolutions[i].height}");

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                currentResIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = PlayerPrefs.GetInt("ResIndex", currentResIndex);
        resolutionDropdown.RefreshShownValue();
    }

    #endregion
}