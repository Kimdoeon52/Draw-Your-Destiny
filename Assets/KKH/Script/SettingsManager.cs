using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    [Header("오디오 컴포넌트")]
    public AudioMixer mainMixer;
    public Slider masterSlider, bgmSlider, sfxSlider;

    [Header("그래픽 컴포넌트")]
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;

    private Resolution[] resolutions;
    public static SettingsManager Instance;
    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
        SetupResolutions();
        // 해상도 목록 초기화
        SetupResolutions();
    }

    void Start()
    {
        // 저장된 설정 불러오기 및 적용
        LoadAndApplySettings();
    }

    // --- 오디오 제어 (슬라이더 연결용) ---
    public void SetMasterVolume(float value)
    {
        mainMixer.SetFloat("Master", Mathf.Log10(Mathf.Max(0.0001f, value)) * 20);
        PlayerPrefs.SetFloat("MasterVol", value);
    }

    public void SetBGMVolume(float value)
    {
        mainMixer.SetFloat("BGM", Mathf.Log10(Mathf.Max(0.0001f, value)) * 20);
        PlayerPrefs.SetFloat("BGMVol", value);
    }

    public void SetSFXVolume(float value)
    {
        mainMixer.SetFloat("SFX", Mathf.Log10(Mathf.Max(0.0001f, value)) * 20);
        PlayerPrefs.SetFloat("SFXVol", value);
    }

    // --- 그래픽 제어 (토글/드롭다운 연결용) ---
    public void SetFullscreen(bool isFull)
    {
        Screen.fullScreen = isFull;
        PlayerPrefs.SetInt("IsFull", isFull ? 1 : 0);
    }

    public void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResIndex", index);
    }

    private void SetupResolutions()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        int currentResIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add(resolutions[i].width + " x " + resolutions[i].height);
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                currentResIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = PlayerPrefs.GetInt("ResIndex", currentResIndex);
        resolutionDropdown.RefreshShownValue();
    }

    // --- 자동 그래픽 제어 (버튼 연결용) ---

    public void ApplyAutoGraphics(int level)
    {
        // 유니티 Quality Settings 적용 (Project Settings > Quality 인덱스 기준)
        QualitySettings.SetQualityLevel(level, true);

        // PC 환경이므로 60프레임 이상 확보 시도
        Application.targetFrameRate = 144;

        // 설정값 저장
        PlayerPrefs.SetInt("GraphicQuality", level);
        PlayerPrefs.Save();

        Debug.Log($"[Auto-Graphics] 현재 설정된 품질 단계: {level}");
    }
    private void LoadAndApplySettings()
    {
        masterSlider.value = PlayerPrefs.GetFloat("MasterVol", 0.8f);
        bgmSlider.value = PlayerPrefs.GetFloat("BGMVol", 0.8f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVol", 0.8f);
        fullscreenToggle.isOn = PlayerPrefs.GetInt("IsFull", 1) == 1;

        // 실제 값 적용 호출
        SetMasterVolume(masterSlider.value);
        SetBGMVolume(bgmSlider.value);
        SetSFXVolume(sfxSlider.value);
        SetFullscreen(fullscreenToggle.isOn);
    }
    

    public void SaveAllSettings() => PlayerPrefs.Save();
}