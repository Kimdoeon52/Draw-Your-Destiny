using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// 적 유닛이 플레이어를 향해 적극적으로 다가가고, 인접 시 공격하는 공격적 AI 전략.
/// A* 경로 탐색 -> 경로 이동 -> 인접 시 공격의 흐름을 띈다.
/// </summary>
[CreateAssetMenu(fileName = "AggressiveStrategy", menuName = "AI/Strategy/Aggressive Strategy")]
public class AggressiveStrategySO : AIBehaviorStrategySO
{
    public override async UniTask ExecuteBehaviorAsync(EnemyAIManager context, Transform unit)
    {
        // 1) context를 통해 현재 유닛과 플레이어의 타일맵 Grid 좌표를 구함
        Vector3Int unitCell = context.grid.WorldToCell(unit.position);
        Vector3Int targetCell = context.grid.WorldToCell(context.playerUnit.position);

        // 2) 경로 계산
        List<Vector3Int> path = context.FindPathToPlayer(unitCell, targetCell);

        // 3) 경로가 있으면 이동
        if (path != null && path.Count > 0)
        {
            await context.MoveUnitAlongPathAsync(unit, path);
        }

        // 4) 이동이 완료되면 공격 실행
        Vector3Int currentCell = context.grid.WorldToCell(unit.position);
        bool attacked = context.TryAttackNearbyPlayer(unit, currentCell);

        if (attacked)
        {
            Debug.Log($"[AggressiveStrategySO] {unit.name} 공격 성공!");
        }
        else
        {
            Debug.Log($"[AggressiveStrategySO] {unit.name} 공격 범위 내 플레이어 없음.");
        }

        // 5) 유닛 간 행동 사이 짧은 딜레이 (시각적 연출용)
        await UniTask.Delay(200);
    }
}
