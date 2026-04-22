using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Cysharp.Threading.Tasks;

/// <summary>
/// 적 AI 턴이 도달했을 때 동작하는 전술 전투용 AI 매니저입니다.
/// '턴 시작 → 경로 탐색(A*) → 유닛 이동 → 공격 범위 탐색 → 공격 실행 → 턴 종료'의 흐름을 제어합니다.
/// </summary>
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
    [Tooltip("기본 행동 전략 (별도 전략이 없는 유닛들이 사용)")]
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
    [Tooltip("적 유닛이 속한 레이어 (아군 충돌 방지 및 탐색용)")]
    [SerializeField] private LayerMask enemyLayerMask;

    [Tooltip("플레이어 유닛이 속한 레이어 (공격 대상 탐색용)")]
    [SerializeField] private LayerMask playerLayerMask;

    #endregion

    #region ── Inspector: 위치 보정 ──

    [Header("위치 보정")]
    [Tooltip("유닛 피벗 위치에 따른 타일 중앙 보정 오프셋")]
    [SerializeField] private Vector3 unitPositionOffset = new Vector3(0f, 0.5f, 0f);

    #endregion

    #region ── 이벤트 ──

    /// <summary>
    /// AI 턴이 모두 종료되었을 때 발생하는 이벤트입니다. 턴 매니저에서 다음 턴으로 넘길 때 사용합니다.
    /// </summary>
    public event Action OnAITurnFinished;

    #endregion

    #region ── 내부 상태 (런타임) ──

    /// <summary>AI 턴이 현재 실행 중인지 여부</summary>
    private bool isExecutingTurn = false;

    #endregion

    /// <summary>
    /// 적의 데미지 텍스트 UI를 업데이트하고 활성화합니다.
    /// </summary>
    /// <param name="enemyTransform">대상 적</param>
    /// <param name="damageAmount">데미지 수치</param>
    public void UpdateEnemyDamageText(Transform enemyTransform, int damageAmount)
    {
        EnemyUnitPatton unitPatton = enemyTransform.GetComponent<EnemyUnitPatton>();

        if (unitPatton != null && unitPatton.damageText != null)
        {
            unitPatton.damageText.text = $"-{damageAmount}";
            unitPatton.damageText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[{enemyTransform.name}] 에 EnemyUnitPatton 이 없거나 TextMeshPro가 할당되지 않았습니다.");
        }
    }


    #region ── 1. 턴 관리 및 AI 실행 시작 ──

    /// <summary>
    /// 모든 적 유닛의 행동을 순차적으로 실행하는 메인 루틴입니다.
    /// </summary>
    public async UniTask ExecuteAITurnAsync()
    {
        if (isExecutingTurn)
        {
            Debug.LogWarning("[EnemyAIManager] AI 턴이 이미 실행 중입니다.");
            return;
        }

        isExecutingTurn = true;
        Debug.Log("[EnemyAIManager] ▶ AI 턴 시작");

        for (int i = 0; i < enemyUnits.Length; i++)
        {
            Transform unit = enemyUnits[i];

            if (unit == null || !unit.gameObject.activeInHierarchy)
                continue;

            EnemyUnitPatton unitPatton = unit.GetComponent<EnemyUnitPatton>();
            AIBehaviorStrategySO strategyToUse = currentStrategy;

            // 유닛별 커스텀 전략이 있으면 우선 사용
            if (unitPatton != null && unitPatton.myStrategy != null)
            {
                strategyToUse = unitPatton.myStrategy;
                Debug.Log($"[EnemyAIManager] 유닛 [{i}] ({unit.name}) → 개별 전략({strategyToUse.name}) 실행");
            }

            if (strategyToUse != null)
            {
                await strategyToUse.ExecuteBehaviorAsync(this, unit);
            }
            else
            {
                Debug.LogWarning("[EnemyAIManager] 할당된 AI 전략이 없습니다.");
            }
        }

        EndAITurn();
    }

    #endregion

    #region ── 2. 경로 탐색 (A*) ──

    private class Node
    {
        public Vector3Int pos;
        public int G, H, F;
        public Node parent;
    }

    /// <summary>
    /// 시작 위치에서 목표 위치까지의 최단 경로를 계산합니다. (A* 알고리즘)
    /// </summary>
    /// <returns>최대 이동 칸 수 내의 경로 리스트</returns>
    public List<Vector3Int> FindPathToPlayer(Vector3Int startCell, Vector3Int targetCell)
    {
        List<Vector3Int> resultPath = new List<Vector3Int>();
        List<Node> openList = new List<Node>();
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
        Dictionary<Vector3Int, Node> openDict = new Dictionary<Vector3Int, Node>();
        Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.right, Vector3Int.left };

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
        int count = 0;

        while (openList.Count > 0 && count < 1000)
        {
            count++;
            int minIndex = 0;
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].F < openList[minIndex].F) minIndex = i;
            }

            Node current = openList[minIndex];
            openList.RemoveAt(minIndex);
            openDict.Remove(current.pos);

            if (current.pos == targetCell)
            {
                targetNode = current;
                break;
            }

            closedSet.Add(current.pos);

            foreach (Vector3Int dir in dirs)
            {
                Vector3Int nextPos = current.pos + dir;

                if (closedSet.Contains(nextPos)) continue;
                if (!IsCellWalkable(nextPos) && nextPos != targetCell) continue;

                int newG = current.G + 1;

                if (openDict.TryGetValue(nextPos, out Node existingNode))
                {
                    if (existingNode.G <= newG) continue;
                }

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

        if (targetNode != null)
        {
            Node curr = targetNode;
            while (curr.parent != null)
            {
                resultPath.Insert(0, curr.pos);
                curr = curr.parent;
            }
        }

        // 목적지(플레이어 셀) 자체는 경로에서 제외 (플레이어 앞에서 멈추기 위함)
        if (resultPath.Count > 0 && resultPath[resultPath.Count - 1] == targetCell)
        {
            resultPath.RemoveAt(resultPath.Count - 1);
        }

        // 최대 이동 거리 제한
        if (resultPath.Count > maxMovePerTurn)
        {
            resultPath = resultPath.GetRange(0, maxMovePerTurn);
        }

        return resultPath;
    }

    #endregion

    #region ── 3. 유닛 이동 ──

    /// <summary>
    /// 경로를 따라 유닛을 실제로 이동시킵니다.
    /// </summary>
    public async UniTask MoveUnitAlongPathAsync(Transform unit, List<Vector3Int> path)
    {
        foreach (Vector3Int cell in path)
        {
            Vector3 targetPos = CellToWorldPosition(cell);
            await MoveToCell(unit, targetPos);
        }
    }

    private async UniTask MoveToCell(Transform unit, Vector3 targetPos)
    {
        while (Vector3.Distance(unit.position, targetPos) > 0.01f)
        {
            unit.position = Vector3.MoveTowards(unit.position, targetPos, moveSpeed * Time.deltaTime);
            await UniTask.Yield();
        }
        unit.position = targetPos;
    }

    #endregion

    #region ── 4. 공격 체크 및 실행 ──

    /// <summary>
    /// 현재 위치 주변 십자 방향으로 플레이어가 있는지 확인하고 있으면 공격합니다.
    /// </summary>
    public bool TryAttackNearbyPlayer(Transform unit, Vector3Int unitCell)
    {
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        Vector3Int playerCell = grid.WorldToCell(playerUnit.position);

        foreach (Vector3Int dir in directions)
        {
            for (int dist = 1; dist <= attackRange; dist++)
            {
                Vector3Int checkCell = unitCell + dir * dist;

                if (!tilemap.HasTile(checkCell)) break;

                if (checkCell == playerCell)
                {
                    ExecuteAttack(unit, playerUnit);
                    return true;
                }

                // 이동 가능한 셀인지 확인 (장애물 체크)
                Vector3 checkWorldPos = grid.GetCellCenterWorld(checkCell);
                Collider2D obstacle = Physics2D.OverlapPoint(checkWorldPos, enemyLayerMask);
                if (obstacle != null) break;
            }
        }
        return false;
    }

    private void ExecuteAttack(Transform attacker, Transform target)
    {
        // TODO: 실제 데미지 시스템 연동 (Health 컴포넌트 호출 등)
        Debug.Log($"[EnemyAIManager] ⚔ {attacker.name} → {target.name} 공격 실행");
    }

    #endregion

    #region ── 5. 턴 종료 ──

    private void EndAITurn()
    {
        isExecutingTurn = false;
        Debug.Log("[EnemyAIManager] ■ AI 턴 종료");
        OnAITurnFinished?.Invoke();
    }

    #endregion

    #region ── 유틸리티 ──

    private Vector3 CellToWorldPosition(Vector3Int cell)
    {
        return grid.GetCellCenterWorld(cell) + unitPositionOffset;
    }

    /// <summary>
    /// 해당 셀이 이동 가능한 타일인지 확인합니다.
    /// </summary>
    public bool IsCellWalkable(Vector3Int cell)
    {
        if (!tilemap.HasTile(cell)) return false;

        Vector3 worldPos = grid.GetCellCenterWorld(cell);
        Collider2D blocker = Physics2D.OverlapPoint(worldPos, enemyLayerMask);
        if (blocker != null) return false;

        return true;
    }

    #endregion
}
