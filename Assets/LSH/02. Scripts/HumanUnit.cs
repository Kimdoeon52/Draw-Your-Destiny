using UnityEditor;
using UnityEngine;

public class HumanUnit : MonoBehaviour
{
    [Header("원본 데이터")]
    public UnitInfo unitInfo;
    public BuildingData buildingData;
    public HumanPool humanPool;

    [Header("현재 상태")]
    [SerializeField] public int age;
    [SerializeField] private Gender gender;
    [SerializeField] private AgeGroup ageGroup;
    [SerializeField] private bool koreanArmy;
    [SerializeField] private float naturalDeathChance;

    private BuildingType curbuildingType = BuildingType.None;
    //private BuildingType 
    //private bool isDead = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) //실험용
        {
            UnitNextTurn();
        }
        if(Input.GetKeyDown(KeyCode.R))
        {
            UseAdultUnitCard();
        }
    }
    public void UnitAppear() //유닛이 나올 때 초기화하는거
    {
        Debug.Log("유닛이 나와서 초기화!");
        humanPool = FindAnyObjectByType<HumanPool>();
        naturalDeathChance = unitInfo.startNaturalDeathChance;
        gender = Random.value < 0.5f ? Gender.Male : Gender.Female;
        ageGroup = AgeGroup.Baby; //처음 나올 때는 응애
        age = unitInfo.babyStartAge;
        koreanArmy = false;
    }
    public void UnitNextTurn() //턴이 지날때마다 나오는거니까 여따가 다 때려박을까
    {
        age++;
        ChangeAgeGroup();
        ChangeDeathPer();
        CheckCardUsing();
        if (Random.value < naturalDeathChance)
        {
            Dead();
        }
    }

    void CheckCardUsing()//카드 사용했을때 행동들 체크.
    {

    }
    //void DoingBehavier(behavier)
    void Dead() //죽는거는 이거
    {
        //UnitAppear(); //죽으면 초기화
        humanPool.ReturnHuman(this.gameObject);
    }
    public void ChangeAgeGroup()//나이 그룹 바뀌는거는 이거
    {
        switch(age)
        {
            case < 3:
                ageGroup = AgeGroup.Baby;
                break;
            case < 15:
                ageGroup = AgeGroup.Adult;
                break;
            case >= 15:
                ageGroup = AgeGroup.Old;
                break;
        }
    }

    public void ChangeDeathPer()
    {
        switch (ageGroup)
        {
            case AgeGroup.Baby:
                naturalDeathChance = unitInfo.startNaturalDeathChance;
                break;
            case AgeGroup.Adult:
                naturalDeathChance = unitInfo.naturalDeathIncreasePerTurn*2;
                break;
            case AgeGroup.Old:
                naturalDeathChance = 0.7f + unitInfo.naturalDeathIncreasePerTurn * (age - 14f); 
                break;
        }
    }

    public void UseAdultUnitCard() //만약 어른을 소환하는 카드를 사용시
    {
        ageGroup = AgeGroup.Adult;
        age = unitInfo.adultStartAge;
        Debug.Log("어른 소환 카드 사용!");
    }

}
