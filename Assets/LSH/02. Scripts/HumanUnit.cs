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

        // 마우스 위치를 월드 좌표로 변환
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // 2D 게임에서는 z값을 0으로 맞춰준다
        mouseWorld.z = 0f;

        // 클릭한 위치가 어떤 타일인지 계산
        Vector3Int targetCell = TileMapManager.Instance.groundTilemap.WorldToCell(mouseWorld);
        // 현재 유닛이 있는 타일 좌표 계산
        Vector3Int startCell = TileMapManager.Instance.groundTilemap.WorldToCell(transform.position);

        // 예: 영지 타일인지 / 강인지 / 건물 있는지 등
        if (!TileMapManager.Instance.IsWalkableTerritory(targetCell))
            return;

        // 시작 타일 → 목표 타일까지
        // 이동 가능한 경로를 계산
        List<Vector3Int> newPath = PathFindingManager.Instance.FindPath(startCell, targetCell);
        //경로가 존재하면 이동 시작
        if (newPath != null && newPath.Count > 0)
        {
            // 계산된 경로 저장
            currentPath = newPath;
            // 현재 이동할 경로 인덱스 초기화
            pathIndex = 0;
            // 이동 시작
            isMoving = true;
        }
    }

    void MoveAlongPath()
    {
        // isMoving == false → 이동 안함
        // 경로 없음 → 이동 안함
        // 경로 끝 도달 → 이동 안함
        if (!isMoving || currentPath == null || pathIndex >= currentPath.Count)
            return;

        // 타일의 정확한 "중앙 위치"를 가져온다
        Vector3 targetWorld = TileMapManager.Instance.GetCellCenterWorld(currentPath[pathIndex]);

        // Z값은 현재 유닛 값 유지
        targetWorld.z = transform.position.z;

        // MoveTowards
        // 현재 위치에서 targetWorld 방향으로
        // moveSpeed 속도로 이동
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorld,
            moveSpeed * Time.deltaTime
        );
        // 목표 타일에 도착했는지 검사
        if (Vector3.Distance(transform.position, targetWorld) < 0.02f)
        {
            // 정확히 타일 중앙에 위치시킴
            transform.position = targetWorld;
            // 다음 타일로 이동
            pathIndex++;
            // 경로 끝에 도달했으면 이동 종료
            if (pathIndex >= currentPath.Count)
                isMoving = false;
        }
    }
}
