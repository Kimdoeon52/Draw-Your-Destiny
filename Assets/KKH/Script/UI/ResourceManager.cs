using UnityEngine;
using System;

/// <summary>
/// 게임 내 자원(골드·연구·인구·식량)을 관리하는 싱글톤 매니저.
/// 자원 변경 시 <see cref="OnResourceChanged"/> 이벤트를 발행하여 UI를 갱신한다.
/// </summary>
/// <example>
/// 사용법: ResourceManager.Instance.AddGold(-50);
/// </example>
public class ResourceManager : MonoBehaviour
{
    /// <summary>전역 접근용 싱글톤 인스턴스.</summary>
    public static ResourceManager Instance { get; private set; }

    #region ── 자원 프로퍼티 (읽기 전용) ──

    /// <summary>현재 골드량.</summary>
    public int Gold => gold;

    /// <summary>현재 연구 포인트.</summary>
    public int Research => research;

    /// <summary>현재 인구 수.</summary>
    public int Population => population;

    /// <summary>최대 인구 수.</summary>
    public int MaxPopulation => maxPopulation;

    /// <summary>현재 식량.</summary>
    public int Food => food;

    #endregion

    /// <summary>
    /// 자원이 변경될 때 발행되는 이벤트.
    /// 파라미터 순서: gold, research, population, food.
    /// </summary>
    public event Action<int, int, int, int> OnResourceChanged;

    #region ── Inspector: 자원 초기값 ──

    [Header("자원 초기값")]
    [SerializeField] private int gold          = 1000;
    [SerializeField] private int research      = 0;
    [SerializeField] private int population    = 10;
    [SerializeField] private int maxPopulation = 20;
    [SerializeField] private int food          = 500;

    #endregion

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

    private void Start()
    {
        NotifyUI();
    }

    #endregion

    // =====================================================================
    #region ── 공개 API: 자원 증감 ──
    // =====================================================================

    /// <summary>골드를 증감시킨다. 음수로 차감 가능.</summary>
    public void AddGold(int amount)
    {
        gold += amount;
        NotifyUI();
    }

    /// <summary>연구 포인트를 증감시킨다.</summary>
    public void AddResearch(int amount)
    {
        research += amount;
        NotifyUI();
    }

    /// <summary>인구를 증감시킨다. 0 ~ maxPopulation 범위로 클램핑.</summary>
    public void AddPopulation(int amount)
    {
        population = Mathf.Clamp(population + amount, 0, maxPopulation);
        NotifyUI();
    }

    /// <summary>최대 인구 한도를 증가시킨다.</summary>
    public void AddMaxPopulation(int amount)
    {
        maxPopulation += amount;
        NotifyUI();
    }

    /// <summary>식량을 증감시킨다.</summary>
    public void AddFood(int amount)
    {
        food += amount;
        NotifyUI();
    }

    /// <summary>로드 시 모든 재화를 한 번에 설정한다.</summary>
    public void SetAll(int gold, int research, int food, int population, int maxPopulation)
    {
        this.gold          = gold;
        this.research      = research;
        this.food          = food;
        this.population    = population;
        this.maxPopulation = maxPopulation;
        NotifyUI();
    }

    #endregion

    // =====================================================================
    #region ── 내부 로직 ──
    // =====================================================================

    /// <summary>자원 변경을 UI에 알린다.</summary>
    private void NotifyUI()
    {
        OnResourceChanged?.Invoke(gold, research, population, food);
    }

    #endregion
}
