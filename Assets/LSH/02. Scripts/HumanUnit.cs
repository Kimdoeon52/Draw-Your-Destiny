using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class HumanUnit : MonoBehaviour
{
    [Header("이동  속도")]
    [SerializeField] private float moveSpeed = 3f;

    private List<Vector3Int> currentPath = new List<Vector3Int>();
    private int pathIndex = 0;
    private bool isMoving = false;

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
        HandleClickMove();
        MoveAlongPath();
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

    void HandleClickMove()
    {
        if (!Input.GetMouseButtonDown(1))
            return;
        
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector3Int targetCell = TileMapManager.Instance.groundTilemap.WorldToCell(mouseWorld);
        Vector3Int startCell = TileMapManager.Instance.groundTilemap.WorldToCell(transform.position);

        // 클릭한 곳이 영지가 아니면 이동 불가
        if (!TileMapManager.Instance.IsWalkableTerritory(targetCell))
            return;

        List<Vector3Int> newPath = PathFindingManager.Instance.FindPath(startCell, targetCell);
        if (newPath != null && newPath.Count > 0)
        {
            currentPath = newPath;
            pathIndex = 0;
            isMoving = true;
        }
    }

    void MoveAlongPath()
    {
        if (!isMoving || currentPath == null || pathIndex >= currentPath.Count)
            return;

        Vector3 targetWorld = TileMapManager.Instance.GetCellCenterWorld(currentPath[pathIndex]);
        targetWorld.z = transform.position.z;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorld,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetWorld) < 0.02f)
        {
            transform.position = targetWorld;
            pathIndex++;

            if (pathIndex >= currentPath.Count)
                isMoving = false;
        }
    }
}
