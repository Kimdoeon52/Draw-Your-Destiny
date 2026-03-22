using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameSceneManager : MonoBehaviour
{
    // 어디서든 접근 가능하도록 싱글톤 구성
    public static GameSceneManager Instance { get; private set; }

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
    /// 동기 방식으로 씬을 전환함 (즉시 로딩)
    /// </summary>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
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