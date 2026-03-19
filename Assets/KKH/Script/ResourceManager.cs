using UnityEngine;
using System;

/*
 ResourceManager.Instance.AddGold(-50)
 이런식으로 자원을 호출하여 골드, 연구, 인구를 관리하는 싱글톤 클래스임 
 */
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; } //싱글톤 인스턴스
    // UI가 새로 켜질 때 현재 값을 바로 가져갈 수 있도록 getter 추가
    public int Gold => gold;
    public int Research => research;
    public int Population => population;
    public int MaxPopulation => maxPopulation;
    public event Action<int, int, int> OnResourceChanged; //자원 변경 이벤트
    [Header("자원 초기값")]
    [SerializeField] private int gold = 1000; //골드 초기값
    [SerializeField] private int research = 0; //연구 초기값
    [SerializeField] private int population = 10; //인구 초기값
    [SerializeField] private int maxPopulation = 20; //최대 인구 초기값

    private void Awake()
    {
        //싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); //씬 전환 시에도 파괴되지 않도록 설정
        }
        else
        {
            Destroy(gameObject); //이미 인스턴스가 존재하면 현재 오브젝트를 파괴
        }
    }
    void Start()
    {
        NotifyUI();
    }

    public void AddGold(int amount)
    {
        gold += amount;
        NotifyUI(); //자원 변경 시 UI 업데이트 알림
    }
    public void AddResearch(int amount)
    {
        research += amount;
        NotifyUI(); //자원 변경 시 UI 업데이트 알림
    }
    public void AddPopulation(int amount)
    {
        population = Mathf.Clamp(population + amount, 0, maxPopulation); //인구는 0과 최대 인구 사이로 제한
        NotifyUI(); //자원 변경 시 UI 업데이트 알림
    }
    private void NotifyUI()
    {
        OnResourceChanged?.Invoke(gold, research, population); //자원 변경 이벤트 호출
    }
    
}
