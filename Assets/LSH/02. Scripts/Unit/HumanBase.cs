using Cysharp.Threading.Tasks;
using PoolBase;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public enum AgeGroupBase //나이별 상태
{
    Baby,
    Adult,
    Old
}
public enum CanMoveJobBase
{
    CanMoveJob, //병사임
    CannotMoveJob //농부, 상인 등등 이동 안하는 직업들
}
public enum LevelBase //시대별 상태임
{
    Level1,//구석기
    Level2, //신석기
    Level3 //철기
}
public enum UnitTypeBase
{
    //기본 생산 직업
    Farmer,
    Shoper,
    //1단계 돌도끼병
    RockWarrior,
    //2단계 힐러, 아처
    Healer,
    Archer,
    //3단계 마법사, 기마병, 자이언트같은거
    Wizzard,
    HorseWarrior,
    SuperUnit
}
namespace Base
{
    public class HumanBase : MonoBehaviour // 일반 유닛이 공통으로 가지는 기본 베이스가 됨.
    {
        [Header("이동 속도")] 
        [SerializeField] protected float moveSpeed = 3f;

        [Header("현재 상태")]
        [SerializeField] protected int age;
        [SerializeField] protected AgeGroupBase ageGroup;
        [SerializeField] protected float naturalDeathChance;

        [Header("직업에 따른 움직임 가능 여부")]
        [SerializeField] protected CanMoveJobBase canMoveJobBase;

        [Header("현 직업의 시대")]
        [SerializeField] protected LevelBase levelBase;

        [Header("소속 문명 ID")]
        public int ownerCivID;

        [Header("소속 노드 ID (-1 = 미소속)")]
        public int homeNodeID = -1;

        [Header("유닛 직업")]
        [SerializeField] protected UnitTypeBase unitTypeBase;
        protected Animator anime;
        protected CancellationTokenSource moveCts;
        protected List<Vector3Int> currentPath = new List<Vector3Int>();
        protected int pathIndex = 0;
        protected bool isMoving = false;

        //================================업데이트================================================

        protected virtual void Update()
        {
            MoveAlongPath();
            if (Input.GetKeyDown(KeyCode.Space))
            {
                UnitNextTurn();
            }
        }

        //=====================================초기화=============================================

        protected virtual void OnEnable()//초기화
        {
            UnitAppear();

        }
        protected virtual void OnDisable() //유닛이 비활성화 될 때 이동 루프 정지
        {
            StopMoveLoop();
        }
        void UnitAppear() //유닛이 소환되면 초기화 시킴.
        {
            age = 0; //나이는 무조건 0으로 시작
            ageGroup = AgeGroupBase.Baby; //나이 그룹도 아기부터 시작
            naturalDeathChance = 0f; //자연사망 확률도 0으로 시작
            levelBase = LevelBase.Level1; //시대도 구석기부터 시작
            ownerCivID = 0; //소속 문명은 0번.
            anime = GetComponent<Animator>();
            switch (canMoveJobBase)
            {
                case CanMoveJobBase.CanMoveJob:
                    //병사움직임
                    StartMoveLoop();
                    break;
                case CanMoveJobBase.CannotMoveJob:
                    //농부, 상인 등등 이동 안하는 직업
                    break;
                default:
                    Debug.LogError("개버그임.");
                    break;
            }
        }

        protected virtual void UnitNextTurn() //턴이 지날때마다 나오는거니까 여따가 다 때려박자
        {
            age++;
            ChangeAgeGroup();
            ChangeDeathChance();
            anime.SetInteger("age", age);
            if (Random.value < naturalDeathChance)
            {
                Debug.Log($"{gameObject.name}이(가) 사망했습니다.");
                Dead();
            }
        }
        //======================= 유닛 나이와 사망 확률 조정 =======================
        protected virtual void ChangeAgeGroup() //나이 그룹 변경
        {
            switch (age)
            {
                case < 3:
                    ageGroup = AgeGroupBase.Baby;
                    break;
                case < 15:
                    ageGroup = AgeGroupBase.Adult;
                    break;
                case >= 15:
                    ageGroup = AgeGroupBase.Old;
                    break;
            }
        }

        protected virtual void ChangeDeathChance() //자연사망 확률 변경
        {
            switch (ageGroup)
            {
                case AgeGroupBase.Baby:
                    naturalDeathChance = 0f; //아기는 안죽어
                    break;
                case AgeGroupBase.Adult:
                    naturalDeathChance = 0.1f; //사람이 죽을 확률은 10%
                    break;
                case AgeGroupBase.Old:
                    naturalDeathChance = 0.7f + (0.05f * (age - 14)); //늙으면 죽을 확률이 70% 그 후로 확률업
                    break;
            }
        }
        //=========================사망 시 풀 반환 처리를 위한 곳========================================
        private IHumanPool ownerPool; //자신이 속한 풀 참조

        public void SetOwnerPool(IHumanPool pool) //GetHuman에서 자신을 불러온 풀을 참조하는거임 그래야 원래 풀로 돌아갈 수 있음
        {
            ownerPool = pool; //풀 참조
        }
        protected virtual void Dead() //사망 처리
        {
            StopMoveLoop();
            UnitRegistry.Instance?.Unregister(this);
            if(ownerPool == null)
            {
                Debug.LogError("풀 참조가 없습니다. 사망한 유닛이 풀로 반환되지 않습니다.");
                return;
            }
            ownerPool?.ReturnHuman(gameObject); //pool이 null인지 체크 후 객체 풀로 반환하여 재사용
        }
        //======================== 가만히 있는 유닛 랜덤 이동 처리 =================================

        protected virtual void StartMoveLoop() //이동 루프 시작
        {
            StopMoveLoop(); //먼저 정지부터 시킴. 중복방지임

            moveCts = new CancellationTokenSource(); //토큰 생성
            isMoving = true;
            MoveRandomLoop(moveCts.Token).Forget(); //랜덤 이동 루프 시작, 토큰 전달
        }
        protected async UniTask MoveRandomLoop(CancellationToken token)//가만히 있을때 지 혼자 움직이는 거임.
        {
            try
            {
                while (!token.IsCancellationRequested) //취소 전달될때까지 반복
                {
                    await UniTask.Delay(2000, cancellationToken: token); //2초 대기 token으로 취소 가능하게 함
                    if (token.IsCancellationRequested) //취소되었는지 다시 한번 확인
                        break;
                    if (!gameObject.activeInHierarchy) //유닛이 비활성화 되었는지 확인
                        break;
                    TryRandomStepMove(); //랜덤 이동 시도
                }
            }
            catch (System.OperationCanceledException)
            {
                //이동 루프가 취소되었을 때 예외 처리
                Debug.Log("루프 취소.");
            }
        }
        protected virtual void TryRandomStepMove() //랜덤 이동 시도
        {
            Vector3Int currentCell = TileMapManager.Instance.cityTilemap.WorldToCell(transform.position);

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

                //타일 검사
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
        protected virtual void StopMoveLoop() //이동 루프 정지
        {
            if (moveCts != null)
            {
                moveCts.Cancel(); //토큰 취소
                moveCts.Dispose(); //자원 해제
                moveCts = null; //참조 제거
            }
        }
        // ========================== 유닛 이동 처리 ===========================
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

        //========================== 유닛 타일 검사 ===========================
        private bool CanMoveToCell(Vector3Int cell) //셀 받아서
        {
            if (!TileMapManager.Instance.IsValidPosition(cell)) //유효한 위치인지 검사
            {
                Debug.Log("못움직임임");
                return false;
            }

            TileType tileType = TileMapManager.Instance.GetTileType(cell); //타일 타입 받아와서
            Debug.Log("움직이이이이임");
            return tileType == TileType.Plain //평지, 도시, 농지 허용
                || tileType == TileType.City
                || tileType == TileType.Farmland;
        }

        //이동 상인, 농부는 이동 x 스위치로 구현할것.

        //사망 처리

        //나이 증가.

        //숄져 유닛 종류도 정해놓을 것. <-a* 이동처리 필요 이거는 다른 스크립트에서
    }
    interface IFightable //가상 함수임 유닛 마다 수치가 다르니까 인터페이스로 만들어서 구현
    {
        void TakeDamage(int damage); //데미지 처리 함수
        void SetupHealth(); //체력 설정 함수
        void Attack(int targetID); //공격 함수//
    }
}