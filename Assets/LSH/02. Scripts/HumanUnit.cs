using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Unity.Properties;
using Unity.VisualScripting;
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

    [Header("직업")]
    [SerializeField] public Job job;
    [Header("애니메이션 모음")]
    [SerializeField] public Animator anime;
    

    private BuildingType curbuildingType = BuildingType.None;
    //private BuildingType 
    //private bool isDead = false;

    private PlayerUnitInfoByJob playerInfo;
    public int ownerCivID;
    private CancellationTokenSource moveCts;

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
        humanPool = FindAnyObjectByType<HumanPool>();
        naturalDeathChance = unitInfo.startNaturalDeathChance;
        gender = Random.value < 0.5f ? Gender.Male : Gender.Female;
        ageGroup = AgeGroup.Baby; //처음 나올 때는 응애
        age = unitInfo.babyStartAge;
        anime = GetComponent<Animator>();
        
        switch (job) //직업에 따른 초기화를 여기서 하도록
        {
            case Job.Farmer:
                koreanArmy = false;
                break;
            case Job.Soldier:
                koreanArmy = true;
                gender = Gender.Male; //군인은 남자만 나옴
                break;
            case Job.Miner:
                koreanArmy = false;
                break;

            default:
                Debug.LogError("뭔 직업이냐 이건.");
                break;
        }
        StartMoveLoop(); //유닛이 나올 때 움직이는거 시작
    }
    public void UnitNextTurn() //턴이 지날때마다 나오는거니까 여따가 다 때려박을까
    {
        age++;
        ChangeAgeGroup();
        ChangeDeathPer();
        CheckCardUsing();
        anime.SetInteger("age", age);
        if (Random.value < naturalDeathChance)
        {
            Dead();
        }
    }

    //------------------유닛 움직이는거임-----------------------------
    private bool CanMoveToCell(Vector3Int cell) //셀 받아서
    {
        if (!TileMapManager.Instance.IsValidPosition(cell)) //유효한 위치인지 검사
            return false;

        if (TileMapManager.Instance.GetOwner(cell) != ownerCivID) //내 영지만 허용
            return false;

        TileType tileType = TileMapManager.Instance.GetTileType(cell); //타일 타입 받아와서

        return tileType == TileType.Plain //평지, 도시, 농지 허용
            || tileType == TileType.City
            || tileType == TileType.Farmland;
    }

    private void TryRandomStepMove()
    {
        Vector3Int currentCell = TileMapManager.Instance.groundTilemap.WorldToCell(transform.position);

        Vector3Int[] directions =
        {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(-1, -1, 0),
        new Vector3Int(1, -1, 0),
        new Vector3Int(-1, 1, 0)
    };

        List<Vector3Int> possibleMoves = new List<Vector3Int>();

        foreach (var dir in directions)
        {
            Vector3Int nextCell = currentCell + dir;

            // 타일 검사 
            if (!CanMoveToCell(nextCell))
                continue;

            possibleMoves.Add(nextCell);
        }

        if (possibleMoves.Count == 0)
            return;

        Vector3Int randomTarget = possibleMoves[Random.Range(0, possibleMoves.Count)];

        currentPath = new List<Vector3Int> { randomTarget };
        pathIndex = 0;
        isMoving = true;
    }
    private async UniTask RamdomMoveLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested) //토큰이 취소되지 않은 동안 계속 반복
            {
                await UniTask.Delay(3000, cancellationToken: token); // 3초 //토큰 전달하여 대기 중에도 취소 가능하게 함
                if (token.IsCancellationRequested)
                    break;
                if (!gameObject.activeInHierarchy)
                    continue;
                if (!isMoving)
                {
                    TryRandomStepMove();
                }

            }
        }
        catch (System.OperationCanceledException) //작업이 취소되었을 때 발생하는 예외 처리
        {
            // 작업이 취소되었을 때 처리할 내용
        }
    }

    private void StartMoveLoop()
    {
        StopMoveLoop();

        moveCts = new CancellationTokenSource(); //토큰생성
        RamdomMoveLoop(moveCts.Token).Forget();
    }
    private void StopMoveLoop()
    {
        if (moveCts != null)
        {
            moveCts.Cancel(); //토큰 취소
            moveCts.Dispose(); //자원 해제
            moveCts = null; //참조 제거
        }
    }
    void CheckCardUsing()//카드 사용했을때 행동들 체크.
    {

    }
    //void DoingBehavier(behavier)
    void Dead() //죽는거는 이거
    {
        StopMoveLoop();
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

    private void OnEnable()
    {
        UnitAppear();
        SetPlayerUnitInfo();
    }

    private void SetPlayerUnitInfo()
    {
        if (playerInfo == null)
        {
            Debug.LogWarning("playerInfo가 아직 없음");
            return;
        }

        job = playerInfo.job;
        age = playerInfo.age;
        unitInfo.maxHealth = playerInfo.maxHealth;
        unitInfo.attackPower = playerInfo.attackPower;
    }

    public void SetUnitInfo(PlayerUnitInfoByJob info)
    {
        playerInfo = info;
    }

}
