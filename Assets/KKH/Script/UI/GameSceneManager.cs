using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameSceneManager : MonoBehaviour
{
    // 어디서든 접근 가능하도록 싱글톤 구성
    public static GameSceneManager Instance { get; private set; }

    // 기존 월드맵의 UI 캔버스나 루트 오브젝트를 저장해둘 변수
    private GameObject _worldUIContainer;

    void Awake()
    {
        // 싱글톤 중복 생성 방지 및 유지 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 전투 씬으로 전환 (기존 씬 유지, 월드맵 UI 비활성화)
    /// </summary>
    public void EnterBattleScene(string battleSceneName, GameObject currentWorldUI)
    {
        _worldUIContainer = currentWorldUI;
        if (_worldUIContainer != null)
        {
            _worldUIContainer.SetActive(false); // 기존 월드맵 UI 끄기
        }
        
        // 배틀 씬을 Additive(병합) 방식으로 추가 로드
        SceneManager.LoadScene(battleSceneName, LoadSceneMode.Additive);
    }

    /// <summary>
    /// 전투 종료 시 월드맵으로 복귀 (전투 씬 언로드, 기존 UI 활성화)
    /// </summary>
    public void ExitBattleScene(string battleSceneName)
    {
        if (_worldUIContainer != null)
        {
            _worldUIContainer.SetActive(true); // 월드맵 UI 다시 켜기
        }
        
        // 배틀 씬을 메모리에서 해제
        SceneManager.UnloadSceneAsync(battleSceneName);
    }

    /// <summary>
    /// 동기 방식으로 씬을 전환함 (즉시 로딩 - 로딩 여백 구형 필요)
    /// </summary>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName); // SceneManager를 이용해 해당 이름의 씬을 즉각 로드 (로딩 중 화면 멈춤 발생 가능)
    }

    /// <summary>
    /// 비동기 방식으로 씬을 전환함 (로딩 화면 구현 시 사용)
    /// </summary>
    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        // 로딩이 완료될 때까지 대기
        while (!operation.isDone)
        {
            // operation.progress를 통해 로딩바(Slider) 등에 진행률 표시 가능 (0.0 ~ 0.9)
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            yield return null;
        }
    }

    /// <summary>
    /// 현재 활성화된 씬을 다시 로드함
    /// </summary>
    public void ReloadCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
}
