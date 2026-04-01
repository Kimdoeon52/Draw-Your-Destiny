using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    [Header("적 타입")]
    public EnemyType enemyType;

    [Header("이동 속도")]
    [SerializeField] private float moveSpeed = 3f;

    private List<Vector3Int> currentPath = new List<Vector3Int>();
    private int pathIndex = 0;
    private bool isMoving = false;

    [Header("원본 데이터")]
    public EnemyUnitInfo enemyInfo;
    public EnemyUnitPool enemyPool;

    [Header("현재 상태")]
    [SerializeField] public int age;
    [SerializeField] private AgeGroup ageGroup;
    [SerializeField] private float naturalDeathChance;
    [SerializeField] private Gender gender;

    [Header("직업")]
    [SerializeField] public Job job;

    private BuildingType curbuildingType = BuildingType.None;

    public int ownerCivID;
    private CancellationTokenSource moveCts;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // 실험용
        {
            UnitNextTurn();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            UseAdultUnitCard();
        }

        HandleClickMove();
        MoveAlongPath();
    }

    public void UnitAppear() // 유닛이 나올 때 초기화
    {
        naturalDeathChance = enemyInfo.startNaturalDeathChance;
        gender = Random.value < 0.5f ? Gender.Male : Gender.Female;
        ageGroup = AgeGroup.Baby;
        age = enemyInfo.babyStartAge;

        // 이동 관련 초기화
        currentPath.Clear();
        pathIndex = 0;
        isMoving = false;

        switch (job)
        {
            case Job.Farmer:
                break;

            case Job.Soldier:
                break;

            case Job.Miner:
                break;

            default:
                Debug.LogError("뭔 직업이냐 이건.");
                break;
        }
        StartMoveLoop();
    }

    public void UnitNextTurn()
    {
        age++;
        ChangeAgeGroup();
        ChangeDeathPer();
        CheckCardUsing();

        if (Random.value < naturalDeathChance)
        {
            Dead();
            return;
        }
    }

    //------------------유닛 움직이는거임-----------------------------

    //타일 검사하는 함수임
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
            while(!token.IsCancellationRequested) //토큰이 취소되지 않은 동안 계속 반복
            {
                await UniTask.Delay(3000, cancellationToken: token); // 3초 //토큰 전달하여 대기 중에도 취소 가능하게 함
                if (token.IsCancellationRequested)
                    break;
                if (!gameObject.activeInHierarchy)
                    continue;
                if(!isMoving)
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
    void CheckCardUsing()
    {

    }

    void Dead()
    {
        StopMoveLoop();
        if (enemyPool == null)
        {
            Debug.LogWarning($"{name} 의 enemyPool이 연결되지 않았습니다.");
            gameObject.SetActive(false);
            return;
        }

        enemyPool.ReturnEnemy(gameObject);
    }

    public void ChangeAgeGroup()
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
                naturalDeathChance = enemyInfo.startNaturalDeathChance;
                break;

            case AgeGroup.Adult:
                naturalDeathChance = enemyInfo.naturalDeathIncreasePerTurn * 2;
                break;

            case AgeGroup.Old:
                naturalDeathChance = 0.7f + enemyInfo.naturalDeathIncreasePerTurn * (age - 14f);
                break;
        }
    }

    public void UseAdultUnitCard()
    {
        ageGroup = AgeGroup.Adult;
        age = enemyInfo.adultStartAge;
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