using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NYH.BattleCardSystem;
using UnityEngine;

/// <summary>
/// 겁이 많은(비겁한) AI 전략 (BattleCardSystem 기반)
/// 전 턴에 플레이어가 공격하거나 다가왔으면 전력 도망,
/// 그렇지 않으면 idealDistance를 유지하며 카이팅
/// </summary>
[CreateAssetMenu(fileName = "CowardlyStrategy", menuName = "AI/Strategy/Cowardly_Advanced")]
public class CowardlyStrategySO : AIBehaviorStrategySO
{
    [Header("거리 설정")]
    [Tooltip("유닛이 유지하고 싶어 하는 적정 거리")]
    [SerializeField] private float idealDistance = 5f;

    #region ── 턴 기억 시스템 (SO 내부 Dictionary) ──

    /// <summary>
    /// 유닛별 "전 턴의 기억"을 저장
    /// </summary>
    private struct TurnMemory
    {
        public int lastHealth;          // 전 턴 종료 시 체력
        public int lastPlayerDistance;  // 전 턴 종료 시 플레이어와의 맨해튼 거리
    }

    /// <summary>
    /// 유닛 인스턴스를 키로 사용하여 개별 기억을 관리
    /// SO는 에셋이므로 공유되지만, Dictionary의 Key가 유닛 인스턴스이므로
    /// 각 유닛마다 독립적인 기억을 유지할 수 있음.
    /// </summary>
    private readonly Dictionary<BattleUnit, TurnMemory> memories = new();

    #endregion

    public override async UniTask ExecuteBehaviorAsync(IBattleAIContext context, BattleUnit unit)
    {
        if (context == null || unit == null) return;

        BattleUnit player = context.GetNearestPlayerUnit(unit);
        if (player == null) return;

        Vector2Int unitCell = context.GetGridPosition(unit);
        Vector2Int playerCell = context.GetGridPosition(player);
        int distToPlayer = Mathf.Abs(unitCell.x - playerCell.x) + Mathf.Abs(unitCell.y - playerCell.y);
        int moveBudget = context.GetMoveBudget(unit);

        // ──────────────────────────────────────────────
        // 1단계: 전 턴 기억을 읽어 위협 수준 판단
        // ──────────────────────────────────────────────
        bool wasAttacked = false;       // 전 턴 대비 체력이 줄었는가?
        bool playerApproached = false;  // 전 턴 대비 플레이어가 더 가까워졌는가?

        if (memories.TryGetValue(unit, out TurnMemory memory))
        {
            wasAttacked = unit.CurrentHealth < memory.lastHealth;
            playerApproached = distToPlayer < memory.lastPlayerDistance;
        }

        bool isThreatened = wasAttacked || playerApproached;

        // ──────────────────────────────────────────────
        // 2단계: 위협 수준에 따른 목표 지점 결정
        // ──────────────────────────────────────────────
        Vector2Int targetCell = unitCell;

        if (isThreatened)
        {
            // 위협 감지! → idealDistance 무시, 전력 도망
            string reason = wasAttacked ? "공격받음" : "플레이어 접근";
            Debug.Log($"[Cowardly] {unit.name}: 위협 감지({reason})! 전력으로 도망침");

            Vector2 fleeDir = ((Vector2)(unitCell - playerCell)).normalized;
            targetCell = unitCell + new Vector2Int(
                Mathf.RoundToInt(fleeDir.x * moveBudget),
                Mathf.RoundToInt(fleeDir.y * moveBudget)
            );
        }
        else if (distToPlayer > idealDistance + 1f)
        {
            // 안전 → 플레이어가 너무 멀면 조금 다가감
            Debug.Log($"[Cowardly] {unit.name}: 위협 없음. 조금 다가감");
            targetCell = playerCell;
        }
        else if (distToPlayer < idealDistance - 1f)
        {
            // 약간 가까움 → 뒤로 물러남 (일반 카이팅)
            Debug.Log($"[Cowardly] {unit.name}: 좀 가깝네... 슬쩍 뒤로 물러남");
            Vector2 fleeDir = ((Vector2)(unitCell - playerCell)).normalized;
            targetCell = unitCell + new Vector2Int(
                Mathf.RoundToInt(fleeDir.x * moveBudget),
                Mathf.RoundToInt(fleeDir.y * moveBudget)
            );
        }
        else
        {
            // 적정 거리 유지 중 → 대기
            Debug.Log($"[Cowardly] {unit.name}: 적정 거리 유지 중. 대기");
        }

        // ──────────────────────────────────────────────
        // 3단계: 이동 실행
        // ──────────────────────────────────────────────
        if (targetCell != unitCell)
        {
            // 목표 타일이 갈 수 없는 곳이면 주변 탐색으로 보정
            if (!context.IsCellWalkable(targetCell))
            {
                targetCell = GetBestAdjacentCell(context, unitCell, playerCell, distToPlayer < idealDistance || isThreatened);
            }

            var path = context.FindPathTowards(unit, targetCell, moveBudget);
            if (path != null && path.Count > 0)
            {
                await context.MoveUnitAlongPathAsync(unit, path);
            }
        }

        // ──────────────────────────────────────────────
        // 4단계: 공격 시도 (도망갔어도 사거리 안이면 발악)
        // ──────────────────────────────────────────────
        context.TryAttackPlayerInRange(unit, context.GetAttackRange(unit));

        // ──────────────────────────────────────────────
        // 5단계: 현재 상태를 기억에 저장 (다음 턴의 판단 근거)
        // ──────────────────────────────────────────────
        Vector2Int finalCell = context.GetGridPosition(unit);
        Vector2Int finalPlayerCell = context.GetGridPosition(player);
        int finalDist = Mathf.Abs(finalCell.x - finalPlayerCell.x) + Mathf.Abs(finalCell.y - finalPlayerCell.y);

        memories[unit] = new TurnMemory
        {
            lastHealth = unit.CurrentHealth,
            lastPlayerDistance = finalDist
        };

        // 파괴된 유닛의 기억은 정리 (메모리 누수 방지)
        CleanupDeadUnits();

        await UniTask.Delay(200);
    }

    /// <summary>
    /// 파괴되었거나 비활성화된 유닛의 기억 데이터를 정리
    /// </summary>
    private void CleanupDeadUnits()
    {
        List<BattleUnit> deadUnits = null;
        foreach (var kvp in memories)
        {
            if (kvp.Key == null || !kvp.Key.IsAlive)
            {
                deadUnits ??= new List<BattleUnit>();
                deadUnits.Add(kvp.Key);
            }
        }

        if (deadUnits != null)
        {
            foreach (var dead in deadUnits)
            {
                memories.Remove(dead);
            }
        }
    }

    /// <summary>
    /// 통행 불가능 시 주변에서 가장 적절한 타일을 찾는 헬퍼 함수
    /// </summary>
    private Vector2Int GetBestAdjacentCell(IBattleAIContext context, Vector2Int unitCell, Vector2Int playerCell, bool shouldFlee)
    {
        Vector2Int bestCell = unitCell;
        int bestDist = shouldFlee ? -1 : int.MaxValue;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in dirs)
        {
            Vector2Int neighbor = unitCell + dir;
            if (context.IsCellWalkable(neighbor))
            {
                int d = Mathf.Abs(neighbor.x - playerCell.x) + Mathf.Abs(neighbor.y - playerCell.y);
                // 도망쳐야 하면 먼 곳(max), 다가가야 하면 가까운 곳(min) 선택
                if (shouldFlee ? (d > bestDist) : (d < bestDist))
                {
                    bestDist = d;
                    bestCell = neighbor;
                }
            }
        }
        return bestCell;
    }
}
