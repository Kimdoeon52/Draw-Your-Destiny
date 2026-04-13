using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 씬 전환을 총괄하는 싱글톤 매니저.
/// 동기/비동기 씬 로드, 전투 씬 Additive 로드/언로드를 지원한다.
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    /// <summary>전역 접근용 싱글톤 인스턴스.</summary>
    public static GameSceneManager Instance { get; private set; }

    /// <summary>월드맵 UI 루트. 전투 진입 시 비활성화, 복귀 시 재활성화.</summary>
    private GameObject _worldUIContainer;

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
        }
    }

    #endregion

    // =====================================================================
    #region ── 전투 씬 전환 (Additive 방식) ──
    // =====================================================================

    /// <summary>
    /// 전투 씬을 Additive로 로드하고, 기존 월드맵 UI를 비활성화한다.
    /// </summary>
    /// <param name="battleSceneName">로드할 전투 씬 이름.</param>
    /// <param name="currentWorldUI">비활성화시킬 월드맵 UI 루트 오브젝트.</param>
    public void EnterBattleScene(string battleSceneName, GameObject currentWorldUI)
    {
        _worldUIContainer = currentWorldUI;

        if (_worldUIContainer != null)
            _worldUIContainer.SetActive(false);

        SceneManager.LoadScene(battleSceneName, LoadSceneMode.Additive);
    }

    /// <summary>
    /// 전투 씬을 언로드하고, 월드맵 UI를 재활성화한다.
    /// </summary>
    /// <param name="battleSceneName">언로드할 전투 씬 이름.</param>
    public void ExitBattleScene(string battleSceneName)
    {
        if (_worldUIContainer != null)
            _worldUIContainer.SetActive(true);

        SceneManager.UnloadSceneAsync(battleSceneName);
    }

    #endregion

    // =====================================================================
    #region ── 일반 씬 전환 ──
    // =====================================================================

    /// <summary>
    /// 동기 방식으로 씬을 즉시 로드한다. 로딩 중 화면이 멈출 수 있다.
    /// </summary>
    /// <param name="sceneName">로드할 씬 이름.</param>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 비동기 방식으로 씬을 로드한다.
    /// 로딩 화면(진행률 바)을 구현할 때 사용한다.
    /// </summary>
    /// <param name="sceneName">로드할 씬 이름.</param>
    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// 현재 활성화된 씬을 다시 로드한다 (재시작).
    /// </summary>
    public void ReloadCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    #endregion

    // =====================================================================
    #region ── 내부 코루틴 ──
    // =====================================================================

    /// <summary>비동기 씬 로드 코루틴. operation.progress로 진행률 표시 가능 (0.0~0.9).</summary>
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            // 로딩바 UI에 연결할 경우: float progress = Mathf.Clamp01(operation.progress / 0.9f);
            yield return null;
        }
    }

    #endregion
}
