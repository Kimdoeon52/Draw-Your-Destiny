using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CowardlyStrategy", menuName = "AI/Strategy/Cowardly_Advanced")]
public class CowardlyStrategySO : AIBehaviorStrategySO
{
    [Header("거리 설정")]
    [Tooltip("유닛이 유지하고 싶어 하는 적정 거리입니다.")]
    [SerializeField] private float idealDistance = 5f;
    
    [Tooltip("한 번에 이동할 최대 칸 수")]
    [SerializeField] private int moveStep = 3;

    public override async UniTask ExecuteBehaviorAsync(EnemyAIManager context, Transform unit)
    {
        Vector3Int unitCell = context.grid.WorldToCell(unit.position);
        Vector3Int playerCell = context.grid.WorldToCell(context.playerUnit.position);
        float distToPlayer = Vector3.Distance(unit.position, context.playerUnit.position);

        Vector3Int targetCell = unitCell;

        // --- 상황별 목표 지점 설정 ---
        
        if (distToPlayer > idealDistance + 1f)
        {
            // 상황 1: 플레이어가 너무 멀어! (조금 관찰하러 다가감)
            Debug.Log($"[Cautious] {unit.name}: 플레이어가 너무 멉니다. 조금 다가갑니다.");
            // 플레이어 방향으로 이동하되, 목적지는 플레이어 위치로 설정 (FindPath 내에서 최대 이동거리만큼만 잘림)
            targetCell = playerCell;
        }
        else if (distToPlayer < idealDistance - 1f)
        {
            // 상황 2: 플레이어가 너무 가까워! (겁쟁이 모드 발동, 뒤로 물러남)
            Debug.Log($"[Cowardly] {unit.name}: 너무 가깝습니다! 거리를 벌립니다.");
            Vector3 fleeDir = (unit.position - context.playerUnit.position).normalized;
            targetCell = unitCell + new Vector3Int(
                Mathf.RoundToInt(fleeDir.x * moveStep),
                Mathf.RoundToInt(fleeDir.y * moveStep),
                0
            );
        }
        else
        {
            // 상황 3: 적정 거리 유지 중. (이 자리에서 대기하며 공격 시도)
            Debug.Log($"[Cautious] {unit.name}: 적정 거리를 유지하며 대기합니다.");
        }

        // --- 이동 실행 ---
        // 목표 셀이 현재 셀과 다를 때만 이동 시도
        if (targetCell != unitCell)
        {
            // 가려는 지점이 갈 수 없는 곳이라면 보정 (기존 로직 재사용)
            if (!context.IsCellWalkable(targetCell))
            {
                targetCell = GetBestAdjacentCell(context, unitCell, playerCell, distToPlayer < idealDistance);
            }

            List<Vector3Int> path = context.FindPathToPlayer(unitCell, targetCell);
            if (path != null && path.Count > 0)
            {
                await context.MoveUnitAlongPathAsync(unit, path);
            }
        }

        // --- 공격 시도 ---
        // 도망갔든 다가갔든 이동 후에 공격 범위 안에 플레이어가 있으면 공격합니다.
        Vector3Int currentCell = context.grid.WorldToCell(unit.position);
        context.TryAttackNearbyPlayer(unit, currentCell);

        await UniTask.Delay(200);
    }

    // 통행 불가능 시 주변에서 가장 적절한 타일을 찾는 헬퍼 함수
    private Vector3Int GetBestAdjacentCell(EnemyAIManager context, Vector3Int unitCell, Vector3Int playerCell, bool shouldFlee)
    {
        Vector3Int bestCell = unitCell;
        float bestDist = shouldFlee ? -1f : float.MaxValue;
        Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        foreach (var dir in dirs)
        {
            Vector3Int neighbor = unitCell + dir;
            if (context.IsCellWalkable(neighbor))
            {
                float d = Vector3.Distance(neighbor, playerCell);
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
