using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Cysharp.Threading.Tasks;

/// <summary>
/// 적 AI 턴이 도달했을 때 동작하는 전술 전투용 AI 매니저.
/// '턴 시작 → 경로 탐색(A*) → 유닛 이동 → 공격 범위 탐색 → 공격 실행 → 턴 종료'
/// 순서로 비동기(UniTask) 흐름을 제어한다.
/// </summary>
///
/// <remarks>
/// ──────────────────────────────────────────────────────────────
/// ■ 씬 배치 가이드
/// ──────────────────────────────────────────────────────────────
///   1. 빈 GameObject 생성 → 이름: "EnemyAIManager"
///   2. 이 스크립트를 부착
///   3. Inspector에서 아래 필드를 할당:
///      ┌──────────────────────────┬──────────────────────────────────┐
///      │ 필드                      │ 할당 대상                         │
///      ├──────────────────────────┼──────────────────────────────────┤
///      │ Grid                     │ 씬의 Grid 오브젝트 (Tilemap 부모) │
///      │ Tilemap                  │ Ground 타일맵                     │
///      │ Enemy Units              │ AI가 제어할 적 유닛 Transform 배열 │
///      │ Player Unit (Target)     │ 공격 대상인 플레이어 유닛 Transform │
///      │ Enemy Layer Mask         │ 적 유닛 레이어                    │
///      │ Player Layer Mask        │ 플레이어 유닛 레이어               │
///      └──────────────────────────┴──────────────────────────────────┘
///
/// ■ 흐름 요약
///   1) 턴 매니저가 적 턴 도래 시 ExecuteAITurnAsync() 호출
///   2) 보유 적 유닛을 순회하며:
///      a. A* 경로 탐색 → FindPathToPlayer()
///      b. 비동기 이동 → MoveUnitAlongPathAsync()
///      c. 십자 범위 탐색·공격 → TryAttackNearbyPlayer()
///   3) 모든 유닛 행동 완료 후 EndAITurn() 으로 턴 종료
///
/// ■ TODO 연동 포인트
///   - FindPathToPlayer : A* 알고리즘 내부 로직 직접 구현
///   - MoveUnitAlongPathAsync : 프로젝트 내 'UnitMoveWithUniTask' 이동 로직 호출
///   - TryAttackNearbyPlayer : 공격 판정·데미지 처리 로직 연동
///   - EndAITurn : 턴 매니저 이벤트/콜백 연결
/// ──────────────────────────────────────────────────────────────
/// </remarks>
public class EnemyAIManager : MonoBehaviour
{
    #region ── Inspector: 씬 레퍼런스 ──

    [Header("씬 레퍼런스")]
    [Tooltip("씬의 Grid 컴포넌트 (Tilemap의 부모 오브젝트)")]
    public Grid grid;

    [Tooltip("경로 탐색 및 유효성 검사에 사용할 Tilemap")]
    [SerializeField] private Tilemap tilemap;

    [Tooltip("AI가 제어할 적 유닛들의 Transform 배열")]
    [SerializeField] private Transform[] enemyUnits;

    [Tooltip("공격 대상인 플레이어 유닛의 Transform")]
    public Transform playerUnit;

    #endregion

    #region ── Inspector: AI 설정 ──

    [Header("AI 설정")]
    [Tooltip("이 유닛(들)이 사용할 AI 행동 전략 (ScriptableObject)")]
    [SerializeField] private AIBehaviorStrategySO currentStrategy;

    [Tooltip("적 유닛의 턴당 최대 이동 칸 수")]
    [SerializeField] private int maxMovePerTurn = 3;

    [Tooltip("타일 간 이동 속도 (units/sec)")]
    [SerializeField] private float moveSpeed = 4f;

    [Tooltip("공격 사거리 (십자 방향 칸 수)")]
    [SerializeField] private int attackRange = 2;

    #endregion

    #region ── Inspector: 레이어 마스크 ──

    [Header("레이어 마스크")]
    [Tooltip("적 유닛이 속한 레이어 (아군 충돌 방지용)")]
    [SerializeField] private LayerMask enemyLayerMask;

    [Tooltip("플레이어 유닛이 속한 레이어 (공격 대상 탐색용)")]
    [SerializeField] private LayerMask playerLayerMask;

    #endregion

    #region ── Inspector: 위치 보정 ──

    [Header("위치 보정")]
    [Tooltip("유닛 피벗이 하단일 경우 타일 중앙으로 보정하는 오프셋")]
    [SerializeField] private Vector3 unitPositionOffset = new Vector3(0f, 0.5f, 0f);

    #endregion

    #region ── 이벤트 ──

    /// <summary>
    /// AI 턴이 종료되었을 때 발행되는 이벤트.
    /// 턴 매니저가 구독하여 다음 턴으로 전환한다.
    /// </summary>
    public event Action OnAITurnFinished;

    #endregion

    #region ── 내부 상태 (런타임) ──

    /// <summary>AI 턴이 실행 중이면 true. 중복 실행을 방지한다.</summary>
    private bool isExecutingTurn = false;

    #endregion

    // =====================================================================
    #region ── 테스트용 입력 (턴 시스템 미구현 시 사용) ──
    // =====================================================================

    /// <summary>
    /// V키로 적 AI 턴을 수동 실행한다.
    /// 턴 시스템이 완성되면 이 메서드를 제거하고
    /// 턴 매니저에서 ExecuteAITurnAsync()를 직접 호출한다.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V) && !isExecutingTurn)
        {
            Debug.Log("[EnemyAIManager] V키 입력 — 적 AI 턴 수동 실행");
            ExecuteAITurnAsync().Forget();
        }
    }

    #endregion

    // =====================================================================
    #region ── 1. 턴 관리 및 AI 실행 시작 (Turn Check) ──
    // =====================================================================

    /// <summary>
    /// 적 AI 턴의 전체 흐름을 비동기로 실행한다.
    /// 턴 매니저에서 적 턴 도래 시 이 함수를 호출한다.
    /// </summary>
    /// <remarks>
    /// 흐름: 보유 적 유닛 순회 → (경로 탐색 → 이동 → 공격 탐색) → 턴 종료
    /// </remarks>
    public async UniTask ExecuteAITurnAsync()
    {
        if (isExecutingTurn)
        {
            Debug.LogWarning("[EnemyAIManager] AI 턴이 이미 실행 중입니다.");
            return;
        }

        isExecutingTurn = true;
        Debug.Log("[EnemyAIManager] ▶ AI 턴 시작");

        // ── 모든 적 유닛을 순차적으로 행동시킨다 ──
        for (int i = 0; i < enemyUnits.Length; i++)
        {
            Transform unit = enemyUnits[i];

            // 유닛이 파괴되었거나 비활성화된 경우 건너뛴다
            if (unit == null || !unit.gameObject.activeInHierarchy)
                continue;

            // ── 유닛별 전략 실행 ──
            EnemyUnitPatton unitPatton = unit.GetComponent<EnemyUnitPatton>();
            AIBehaviorStrategySO strategyToUse = currentStrategy;

            // 1. 유닛에 개별 전략이 있으면 그것을 우선 사용
            if (unitPatton != null && unitPatton.myStrategy != null)
            {
                strategyToUse = unitPatton.myStrategy;
                Debug.Log($"[EnemyAIManager] 유닛 [{i}] ({unit.name}) → 개별 전략 사용: {strategyToUse.name}");
            }
            else
            {
                Debug.Log($"[EnemyAIManager] 유닛 [{i}] ({unit.name}) → 기본 전략 사용: {currentStrategy?.name ?? "없음"}");
            }

            // 2. 전략 실행
            if (strategyToUse != null)
            {
                await strategyToUse.ExecuteBehaviorAsync(this, unit);
            }
            else
            {
                Debug.LogWarning("[EnemyAIManager] 할당된 AI 전략(Strategy)이 없습니다.");
            }
        }

        // ── 모든 유닛 행동 완료 → 턴 종료 ──
        EndAITurn();
    }

    #endregion

    // =====================================================================
    #region ── 2. 이동 대상 탐색 (Pathfinding) ──
    // =====================================================================

    /// <summary>
    /// A* 알고리즘으로 시작 셀(startCell)에서 목표 셀(targetCell)까지의
    /// 최단 경로를 계산하여 반환한다.
    /// </summary>
    /// <param name="startCell">적 유닛의 현재 Grid 셀 좌표.</param>
    /// <param name="targetCell">플레이어 유닛의 Grid 셀 좌표 (목표).</param>
    /// <returns>
    /// 시작 셀에서 목표 셀까지의 경로 (셀 좌표 리스트).
    /// maxMovePerTurn 칸까지만 잘라서 반환한다.
    /// 경로를 찾을 수 없으면 빈 리스트를 반환한다.
    /// </returns>
    /// <remarks>
    /// ★ 반환 리스트에는 startCell 을 포함하지 않는다.
    /// ★ 반환 리스트의 최대 길이는 maxMovePerTurn 이다.
    /// </remarks>
    // 1. 노드 구조체 (내부 클래스로 사용)
    private class Node
    {
        public Vector3Int pos;
        public int G, H, F;
        public Node parent;
    }

    public List<Vector3Int> FindPathToPlayer(Vector3Int startCell, Vector3Int targetCell)
    {
        List<Vector3Int> resultPath = new List<Vector3Int>();

        List<Node> openList = new List<Node>();
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>(); // 음수 좌표도 처리 가능
        Dictionary<Vector3Int, Node> openDict = new Dictionary<Vector3Int, Node>();

        // 2. 방향 배열 (상, 하, 우, 좌)
        Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.right, Vector3Int.left };

        // 3. 시작점 초기화
        Node startNode = new Node
        {
            pos = startCell,
            G = 0,
            H = Mathf.Abs(startCell.x - targetCell.x) + Mathf.Abs(startCell.y - targetCell.y),
            parent = null
        };
        startNode.F = startNode.G + startNode.H;

        openList.Add(startNode);
        openDict[startCell] = startNode;

        Node targetNode = null;

        // 4. 탐색 시작 (안전을 위해 최대 1000번 루프 제한)
        int count = 0;
        while (openList.Count > 0 && count < 1000)
        {
            count++;

            // a) openList에서 F가 가장 작은 노드 꺼내기 (회원님의 minF 탐색과 동일)
            int minIndex = 0;
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].F < openList[minIndex].F)
                    minIndex = i;
            }

            Node current = openList[minIndex];
            openList.RemoveAt(minIndex);
            openDict.Remove(current.pos);

            // b) 목표 도달 확인
            if (current.pos == targetCell)
            {
                targetNode = current;
                break;
            }

            // c) 방문 처리
            closedSet.Add(current.pos);

            // d) 4방향 탐색
            foreach (Vector3Int dir in dirs)
            {
                Vector3Int nextPos = current.pos + dir;

                // 이미 방문했거나 통행 불가능한 셀이면 무시 (목표 셀은 예외적으로 허용)
                if (closedSet.Contains(nextPos)) continue;
                if (!IsCellWalkable(nextPos) && nextPos != targetCell) continue;

                int newG = current.G + 1;

                // 이미 openList에 있는데 기존 경로가 더 짧으면 무시
                if (openDict.TryGetValue(nextPos, out Node existingNode))
                {
                    if (existingNode.G <= newG)
                        continue;
                }

                // 새로운 노드 생성
                Node neighbor = new Node
                {
                    pos = nextPos,
                    G = newG,
                    H = Mathf.Abs(nextPos.x - targetCell.x) + Mathf.Abs(nextPos.y - targetCell.y),
                    parent = current
                };
                neighbor.F = neighbor.G + neighbor.H;

                openList.Add(neighbor);
                openDict[nextPos] = neighbor;
            }
        }

        // 5. 경로 역추적 (거꾸로 타고 올라가기)
        if (targetNode != null)
        {
            Node curr = targetNode;
            while (curr.parent != null)
            {
                resultPath.Insert(0, curr.pos); // 리스트 맨 앞에 넣어서 순서를 뒤집음 정방향으로 만듦
                curr = curr.parent;
            }
        }

        // 6. 경로 마지막 셀이 플레이어 위치와 같으면 제거
        //    → 적이 플레이어 타일 위로 올라가지 않고 바로 옆에서 멈춤
        if (resultPath.Count > 0 && resultPath[resultPath.Count - 1] == targetCell)
        {
            resultPath.RemoveAt(resultPath.Count - 1);
        }

        // 7. 턴당 최대 이동 칸수(maxMovePerTurn)만큼만 자르기
        if (resultPath.Count > maxMovePerTurn)
        {
            resultPath = resultPath.GetRange(0, maxMovePerTurn);
        }

        return resultPath;
    }


    #endregion

    // =====================================================================
    #region ── 3. 비동기 이동 실행 (Unit Movement) ──
    // =====================================================================

    /// <summary>
    /// 지정된 유닛을 경로(path)를 따라 비동기로 이동시킨다.
    /// 셀 단위로 순차 이동하며, 모든 셀 이동이 완료될 때까지 대기한다.
    /// </summary>
    /// <param name="unit">이동시킬 적 유닛의 Transform.</param>
    /// <param name="path">이동할 Grid 셀 좌표 리스트 (순서대로 이동).</param>
    public async UniTask MoveUnitAlongPathAsync(Transform unit, List<Vector3Int> path)
    {
        Debug.Log($"[EnemyAIManager] ▶ 유닛 이동 시작 — 경로 {path.Count}칸");

        foreach (Vector3Int cell in path)
        {
            Vector3 targetPos = CellToWorldPosition(cell);
            await MoveToCell(unit, targetPos);
        }

        Debug.Log("[EnemyAIManager] ■ 유닛 이동 완료");
    }

    /// <summary>
    /// 유닛을 현재 위치에서 targetPos까지 부드럽게 이동시킨다.
    /// MoveUnitAlongPathAsync 내부에서 셀 하나 이동 시 호출된다.
    /// </summary>
    /// <param name="unit">이동시킬 유닛의 Transform.</param>
    /// <param name="targetPos">목표 월드 좌표.</param>
    /// <remarks>
    /// ★ PathDrawingManager.MoveToNextCell 패턴과 동일한 구조.
    /// ★ 프로젝트 내 공용 이동 유틸리티가 완성되면 해당 메서드로 교체 가능.
    /// </remarks>
    private async UniTask MoveToCell(Transform unit, Vector3 targetPos)
    {
        while (Vector3.Distance(unit.position, targetPos) > 0.01f)
        {
            unit.position = Vector3.MoveTowards(
                unit.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );
            await UniTask.Yield();
        }

        // 최종 위치 스냅
        unit.position = targetPos;
    }

    #endregion

    // =====================================================================
    #region ── 4. 십자 범위 탐색 및 공격 (Linear Grid Search) ──
    // =====================================================================

    /// <summary>
    /// 유닛의 현재 위치를 기준으로 상·하·좌·우 4방향 attackRange 칸 이내를
    /// 선형 탐색하여 플레이어 유닛이 감지되면 공격을 실행한다.
    /// </summary>
    /// <param name="unit">공격을 시도하는 적 유닛의 Transform.</param>
    /// <param name="unitCell">적 유닛의 현재 Grid 셀 좌표.</param>
    /// <returns>공격에 성공하면 true, 범위 내에 플레이어가 없으면 false.</returns>
    public bool TryAttackNearbyPlayer(Transform unit, Vector3Int unitCell)
    {
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        Vector3Int playerCell = grid.WorldToCell(playerUnit.position);

        foreach (Vector3Int dir in directions)
        {
            for (int dist = 1; dist <= attackRange; dist++)
            {
                Vector3Int checkCell = unitCell + dir * dist;

                // 타일맵 밖이면 해당 방향 탐색 중단
                if (!tilemap.HasTile(checkCell))
                    break;

                // 장애물(적 아군 유닛)이 중간에 있으면 해당 방향 탐색 중단
                // (단, 플레이어 셀은 통과 가능하도록 플레이어 체크를 먼저 수행)
                if (checkCell == playerCell)
                {
                    // ★ 플레이어 발견! → 공격 실행
                    ExecuteAttack(unit, playerUnit);
                    return true;
                }

                // Physics2D로 해당 셀에 장애물(적 아군)이 있는지 확인
                Vector3 checkWorldPos = grid.GetCellCenterWorld(checkCell);
                Collider2D obstacle = Physics2D.OverlapPoint(checkWorldPos, enemyLayerMask);
                if (obstacle != null)
                    break; // 아군 유닛이 가로막고 있으면 이 방향 더 이상 탐색 불가
            }
        }

        return false;
    }

    /// <summary>
    /// 실제 공격 처리를 수행한다.
    /// 데미지 시스템이 완성되면 이 메서드 내부를 교체한다.
    /// </summary>
    /// <param name="attacker">공격하는 적 유닛.</param>
    /// <param name="target">공격 대상 플레이어 유닛.</param>
    /// <remarks>
    /// ★ 데미지 시스템 연동 시 교체 포인트 ★
    ///   target.GetComponent&lt;Health&gt;().TakeDamage(attackDamage);
    /// </remarks>
    private void ExecuteAttack(Transform attacker, Transform target)
    {
        // TODO: 데미지 시스템 연동 시 아래 로그를 실제 데미지 처리로 교체
        float distance = Vector3.Distance(attacker.position, target.position);
        Debug.Log($"[EnemyAIManager] ⚔ {attacker.name} → {target.name} 공격! (거리: {distance:F1})");
    }

    #endregion

    // =====================================================================
    #region ── 5. 턴 종료 (Turn End) ──
    // =====================================================================

    /// <summary>
    /// AI 턴을 종료하고, 다음 턴으로 전환하기 위한 이벤트를 발행한다.
    /// </summary>
    /// <remarks>
    /// ★ 턴 매니저 연동 시 교체 포인트 ★
    ///   - OnAITurnFinished 이벤트를 턴 매니저가 구독하도록 연결
    ///   - 또는 턴 매니저의 공개 API 를 직접 호출하는 방식으로 교체 가능
    /// </remarks>
    private void EndAITurn()
    {
        isExecutingTurn = false;
        Debug.Log("[EnemyAIManager] ■ AI 턴 종료 — 다음 턴으로 전환");

        // TODO: 턴 매니저에게 턴 종료를 알린다
        // ──────────────────────────────────────────────────────────────
        // 구현 시 참고 사항:
        //   - 이벤트 방식: OnAITurnFinished?.Invoke();
        //   - 직접 호출 방식: TurnManager.Instance.NextTurn();
        //   - 필요 시 턴 종료 연출(딜레이 등)을 추가
        // ──────────────────────────────────────────────────────────────

        OnAITurnFinished?.Invoke();
    }

    #endregion

    // =====================================================================
    #region ── 유틸리티 ──
    // =====================================================================

    /// <summary>
    /// Grid 셀 좌표를 월드 좌표(타일 중앙 + 오프셋)로 변환한다.
    /// </summary>
    /// <param name="cell">Grid 셀 좌표.</param>
    /// <returns>유닛이 위치해야 할 월드 좌표.</returns>
    private Vector3 CellToWorldPosition(Vector3Int cell)
    {
        return grid.GetCellCenterWorld(cell) + unitPositionOffset;
    }

    /// <summary>
    /// 두 Grid 셀이 상하좌우로 인접한지 판별한다 (맨해튼 거리 == 1).
    /// </summary>
    private bool IsAdjacent(Vector3Int a, Vector3Int b)
    {
        return (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y)) == 1;
    }

    /// <summary>
    /// 특정 셀에 장애물(적 유닛·벽 등)이 있는지 확인한다.
    /// A* 경로 탐색에서 통행 가능 여부 판단에 사용한다.
    /// </summary>
    /// <param name="cell">확인할 Grid 셀 좌표.</param>
    /// <returns>통행 가능하면 true, 장애물이 있으면 false.</returns>
    public bool IsCellWalkable(Vector3Int cell)
    {
        // ① 타일맵에 타일이 없으면 통행 불가
        if (!tilemap.HasTile(cell))
            return false;

        // ② 해당 셀에 적 아군 유닛(Collider)이 점유 중이면 통행 불가
        Vector3 worldPos = grid.GetCellCenterWorld(cell);
        Collider2D blocker = Physics2D.OverlapPoint(worldPos, enemyLayerMask);
        if (blocker != null)
            return false;

        return true;
    }

    #endregion
}
