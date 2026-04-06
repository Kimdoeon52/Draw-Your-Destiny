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
    private Vector3Int farmingTargetCell; //농사 타일 감지

    private PlayerUnitInfoByJob playerInfo;
    public int ownerCivID;
    private CancellationTokenSource moveCts;

    private BuildingInstance assignedFarm;   // 현재 배정된 농장
    [Header("농장 배정 상태")]
    [SerializeField]private bool isAssignedToFarm = false;   // 이미 배정됐는지
    private bool isProcessingFarmAssignment = false; //농장배정처리 확인용
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) //실험용
        {
            UnitNextTurn();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            UseAdultUnitCard();
        }
        //if(Input.GetKeyDown(KeyCode.V))
        //{
        //    StartFarming();
        //}
        HandleClickMove();
        MoveAlongPath();
    }
    public void UnitAppear()
    {
        humanPool = FindAnyObjectByType<HumanPool>();
        naturalDeathChance = unitInfo.startNaturalDeathChance;
        gender = Random.value < 0.5f ? Gender.Male : Gender.Female;
        ageGroup = AgeGroup.Baby;
        age = unitInfo.babyStartAge;
        anime = GetComponent<Animator>();

        switch (job)
        {
            case Job.Farmer:
                koreanArmy = false;
                break;

            case Job.Soldier:
                koreanArmy = true;
                gender = Gender.Male;
                break;

            case Job.Miner:
                koreanArmy = false;
                break;

            default:
                Debug.LogError("뭔 직업이냐 이건.");
                break;
        }

        if (job == Job.Farmer)
        {
            StartFarming();

            if (!isAssignedToFarm)
                StartMoveLoop();
        }
        else
        {
            StartMoveLoop();
        }
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

    public bool MoveToBuilding(BuildingInstance building)
    {
        if (building == null || building.data == null) //건물이 없거나 건물 데이터가 없으면
            return false;

        if (!TryFindReachableCellNearBuilding(building, out Vector3Int targetCell)) 
            //현재 유닛의 위치에서 부터 건물 주변에서 가장 가까운 거리의 타일을 찾는 targetCell
            return false;

        Vector3Int startCell = TileMapManager.Instance.groundTilemap.WorldToCell(transform.position); //현재 위치
        List<Vector3Int> path = PathFindingManager.Instance.FindPath(startCell, targetCell); //현재 위치와 건물 주변중 가장 가까운 경로

        if (path == null || path.Count == 0) //경로가 없으면 이동 실패
            return false;

        StopMoveLoop(); // 랜덤 이동 정지
        assignedFarm = building; //배정된 농장 설정
        isAssignedToFarm = true; //농장 배정 상태 설정
        farmingTargetCell = targetCell; //농사 타일 설정 (건물 주변에서 가장 가까운 타일)

        currentPath = path; //이동할 경로 설정
        pathIndex = 0; //경로 인덱스 초기화
        isMoving = true; //이동 시작

        return true;
    }

    void CheckCardUsing()//카드 사용했을때 행동들 체크.
    {

    }
    //void DoingBehavier(behavier)
    void Dead()
    {
        StopMoveLoop();

        BuildingInstance oldFarm = assignedFarm;

        if (BuildingRegistry.Instance != null)
            BuildingRegistry.Instance.UnassignFarmer(this);//농장 배정 해제

        assignedFarm = null;
        isAssignedToFarm = false;
        isProcessingFarmAssignment = false;

        humanPool.ReturnHuman(this.gameObject);

        if (BuildingRegistry.Instance != null && oldFarm != null)
            BuildingRegistry.Instance.NotifyFarmVacancy(oldFarm);//농장 빈자리 알림
        Debug.LogError("빈자리 생김김김김김!");
    }
    public void ChangeAgeGroup()//나이 그룹 바뀌는거는 이거
    {
        switch (age)
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
                naturalDeathChance = unitInfo.naturalDeathIncreasePerTurn * 2;
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

    //일단 마우스 위치까지 최소 거리 구하는 함수임
    void HandleClickMove()
    {
        if (!Input.GetMouseButtonDown(1))
            return;
        isAssignedToFarm = false;   //임시 버그 픽스1
        isProcessingFarmAssignment = false; //임시 버그 픽스2
                                                     // 마우스 위치를 월드 좌표로 변환
    Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // 2D 게임에서는 z값을 0으로 맞춰준다
        mouseWorld.z = 0f;

        // 클릭한 위치가 어떤 타일인지 계산
        Vector3Int targetCell = TileMapManager.Instance.groundTilemap.WorldToCell(mouseWorld);//WorldToCell은 월드 좌표를 타일 좌표로 변환하는 함수
        // 현재 유닛이 있는 타일 좌표 계산
        Vector3Int startCell = TileMapManager.Instance.groundTilemap.WorldToCell(transform.position);

        // 예: 영지 타일인지 / 강인지 / 건물 있는지 등
        if (!TileMapManager.Instance.IsWalkableTerritory(targetCell))
            return;

        // 시작 타일 → 목표 타일까지
        // 이동 가능한 경로를 계산
        List<Vector3Int> newPath = PathFindingManager.Instance.FindPath(startCell, targetCell); //현재 위치와 클릭한 좌표타일을 가져감
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
    void StartFarming()
    {
        if (job != Job.Farmer) //농부가 아니라면 바로 리턴
        {
            Debug.Log($"{name} 은 농부가 아님");
            return;
        }

        if (isAssignedToFarm || isProcessingFarmAssignment) //이미 농장에 배정되어 있거나, 배정 처리 중이라면
            return;

        if (BuildingRegistry.Instance == null) //빌딩 레지스트리가 없으면
            return;

        isProcessingFarmAssignment = true; //농장 배정 처리 시작

        Vector3Int currentCell = TileMapManager.Instance.groundTilemap.WorldToCell(transform.position); //현재 위치
        BuildingInstance farm = BuildingRegistry.Instance.FindNearestAvailableFarm(currentCell, ownerCivID); //현재 위치에서 가장 가까운 빈 농장 찾기
        //farm에는 가장 가까운 빈 농장이 할당됨. 만약 배정 가능한 농장이 없다면 null이 할당됨
        if (farm == null)
        {
            isProcessingFarmAssignment = false; //없으면 배치 x
            Debug.Log("배정 가능한 Farm이 없음");
            return;
        }

        bool assigned = BuildingRegistry.Instance.TryAssignFarmer(farm, this); //해당 농장에 이 유닛을 배정 시도. 성공하면 true, 실패하면 false 반환
        if (!assigned)//배정 실패
        {
            isProcessingFarmAssignment = false;
            Debug.Log("Farm 배정 실패");
            return;
        }

        bool moved = MoveToBuilding(farm); //배정된 농장으로 이동 시도. 성공하면 true, 실패하면 false 반환
        if (!moved)
        {
            BuildingRegistry.Instance.UnassignFarmer(this); //이 유닛을 농장에 배정 해제. 배정은 했지만 이동 실패했으니까 다시 배정 해제해서 다른 유닛이 그 농장에 배정될 수 있게 함
            assignedFarm = null; //배정된 농장 초기화
            isAssignedToFarm = false; //농장 배정 상태 초기화
            isProcessingFarmAssignment = false; //농장 배정 처리 초기화
            Debug.Log("Farm으로 이동 실패");
            return;
        }

        assignedFarm = farm; //배정된 농장 설정
        isAssignedToFarm = true; //농장 배정 상태 설정
        isProcessingFarmAssignment = false; //농장 배정 처리 완료
        StopMoveLoop(); //랜덤 이동 정지
        Debug.Log($"{name} 이 Farm으로 배정되어 이동 시작");
    }

    //private bool TryFindNearestFarmBuilding(out BuildingInstance nearestFarm)
    //{
    //    nearestFarm = null;

    //    if (BuildingRegistry.Instance == null)
    //        return false;

    //    List<BuildingInstance> farmBuildings = BuildingRegistry.Instance.GetBuildings(BuildingType.Farm);
    //    if (farmBuildings == null || farmBuildings.Count == 0)
    //        return false;

    //    Vector3Int currentCell = TileMapManager.Instance.groundTilemap.WorldToCell(transform.position);

    //    float minDist = float.MaxValue;

    //    foreach (var building in farmBuildings)
    //    {
    //        if (building == null || building.data == null)
    //            continue;

    //        if (building.ownerCivID != ownerCivID)
    //            continue;

    //        float dist = Vector3Int.Distance(currentCell, building.origin);
    //        if (dist < minDist)
    //        {
    //            minDist = dist;
    //            nearestFarm = building;
    //        }
    //    }

    //    return nearestFarm != null;
    //}
    private bool TryFindReachableCellNearBuilding(BuildingInstance building, out Vector3Int targetCell)
    {//건물 주변의 도달 가능한 타일을 찾아서 targetCell로 반환하는 함수임
        targetCell = Vector3Int.zero;

        if (building == null || building.footprint == null || building.footprint.Count == 0) //footprint는 건물이 차지하는 타일의 리스트임
            return false; //대충 건물이없으면 false를 리턴함

        HashSet<Vector3Int> candidateCells = new HashSet<Vector3Int>(); //순서 없고, 중복 안되는 셀 HastSet임 값으로 접근해야댐

        Vector3Int[] directions = //그냥 팔방향임
        {
            Vector3Int.right,//(1,0,0)
            Vector3Int.left, //(-1,0,0)
            Vector3Int.up, //(0,1,0)
            Vector3Int.down,//(0,-1,0)
            new Vector3Int(1, 1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(-1, -1, 0)
        };

        foreach (var cell in building.footprint)
        {
            foreach (var dir in directions)
            {
                Vector3Int near = cell + dir; //건물타일에서 팔방향으로 한칸씩 더한 타일이 near임

                if (building.footprint.Contains(near)) //건물 타일 자체는 후보에서 제외
                    continue;

                candidateCells.Add(near);//건물 타일 주변의 타일들을 후보로 추가
            }
        }

        Vector3Int currentCell = TileMapManager.Instance.groundTilemap.WorldToCell(transform.position); //현재 위치를 타일 좌표로 바꾼다.
        float minDist = float.MaxValue; //일단 최소 거리를 최대값으로 초기화
        bool found = false;

        foreach (var cell in candidateCells) //주변 타일들 중에서
        {
            if (!CanMoveToCell(cell)) //맵 안에 있는지 내 영지인지 등등 cell검사하는거임
                continue;

            List<Vector3Int> testPath = PathFindingManager.Instance.FindPath(currentCell, cell);
            //해당 타일까지 실제로 도달 가능한지 검사하는거임. 그냥 직선거리로 가까운 타일이 아니라, 길이 뚫려있는 타일 중에서 가장 가까운 타일을 찾아야하니까

            //testPath가 null이거나 경로가 없으면 그 타일은 도달 불가능하다고 간주하고 넘어감
            if (testPath == null || testPath.Count == 0)
                continue;

            float dist = Vector3Int.Distance(currentCell, cell); //현재 위치에서 후보 타일까지의 직선 거리를 계산
            if (dist < minDist) //이 후보 타일이 지금까지 찾은 타일들 중에서 가장 가까우면
            {
                minDist = dist; //최소 거리 갱신
                targetCell = cell; //목적지 타일 갱신
                found = true;
            }
        }

        return found;
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
            {
                isMoving = false;

                Vector3Int currentCell = TileMapManager.Instance.groundTilemap.WorldToCell(transform.position); //현재 위치를 타일 좌표로 바꾼다.
                if (currentCell == farmingTargetCell)
                {
                    Debug.Log($"{name} 농사 시작");
                }
            }
        }
    }

    private void OnEnable()
    {
        if (BuildingRegistry.Instance != null)
        {
            BuildingRegistry.Instance.OnBuildingRegistered += HandleBuildingRegistered;
            BuildingRegistry.Instance.OnBuildingRemoved += HandleBuildingRemoved;
            BuildingRegistry.Instance.OnFarmVacancyAvailable += HandleFarmVacancyAvailable;
        }
        //UnitAppear();
        SetPlayerUnitInfo();
    }
    private void OnDisable()
    {
        StopMoveLoop();

        if (BuildingRegistry.Instance != null)
        {
            BuildingRegistry.Instance.UnassignFarmer(this);
            BuildingRegistry.Instance.OnBuildingRegistered -= HandleBuildingRegistered;
            BuildingRegistry.Instance.OnBuildingRemoved -= HandleBuildingRemoved;
            BuildingRegistry.Instance.OnFarmVacancyAvailable -= HandleFarmVacancyAvailable;
        }

        assignedFarm = null;
        isAssignedToFarm = false;
        isProcessingFarmAssignment = false;
    }
    private void HandleBuildingRegistered(BuildingInstance building) //건물 지으면 실행될 이벤트
    {
        if (job != Job.Farmer)
            return;

        if (building == null || building.data == null)
            return;

        if (building.data.buildingType != BuildingType.Farm)
            return;

        if (building.ownerCivID != ownerCivID)
            return;

        if (isAssignedToFarm || isProcessingFarmAssignment)
            return;

        HandleFarmVacancyAvailable(building);
    }

    private void HandleBuildingRemoved(BuildingInstance building)
    {
        if (building == null)
            return;

        if (assignedFarm == building)
        {
            BuildingRegistry.Instance.UnassignFarmer(this);

            assignedFarm = null;
            isAssignedToFarm = false;
            isProcessingFarmAssignment = false;

            StartMoveLoop();
            StartFarming();
        }
    }
    private void HandleFarmVacancyAvailable(BuildingInstance farm)
    {
        if (job != Job.Farmer)
            return;

        if (farm == null || farm.data == null)
            return;

        if (farm.data.buildingType != BuildingType.Farm)
            return;

        if (farm.ownerCivID != ownerCivID)
            return;

        if (isAssignedToFarm || isProcessingFarmAssignment)
            return;

        if (BuildingRegistry.Instance == null)
            return;

        if (!BuildingRegistry.Instance.HasVacancy(farm))
            return;

        isProcessingFarmAssignment = true; // 농장 배정 처리중

        bool assigned = BuildingRegistry.Instance.TryAssignFarmer(farm, this); //농장 배정 시도
        if (!assigned)
        {
            isProcessingFarmAssignment = false; //배정 실패
            return;
        }

        bool moved = MoveToBuilding(farm); //배정된 농장으로 이동 시도
        if (!moved)
        {
            BuildingRegistry.Instance.UnassignFarmer(this); //이 유닛을 농장에 배정 해제. 배정은 했지만 이동 실패했으니까 다시 배정 해제해서 다른 유닛이 그 농장에 배정될 수 있게 함
            assignedFarm = null; //배정된 농장 초기화
            isAssignedToFarm = false; //농장 배정 상태 초기화
            isProcessingFarmAssignment = false; //농장 배정 처리중 상태 초기화
            return;
        }

        assignedFarm = farm; //배정된 농장 설정
        isAssignedToFarm = true; //농장 배정 상태 설정
        isProcessingFarmAssignment = false; //농장 배정 처리 완료

        Debug.Log($"{name} 이 기존 Farm 빈자리에 자동 투입됨");
        Debug.LogError($"{name} 이 기존 Farm 빈자리에 자동 투입됨");
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
