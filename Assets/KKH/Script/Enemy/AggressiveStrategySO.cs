using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NYH.BattleCardSystem;
using UnityEngine;

/// <summary>
/// 공격적인 AI 전략
/// 플레이어에게 최단 거리로 접근하여 인접할 경우 공격
/// </summary>
[CreateAssetMenu(fileName = "AggressiveStrategy", menuName = "AI/Strategy/Aggressive Strategy")]
public class AggressiveStrategySO : AIBehaviorStrategySO
{
    /// <summary>
    /// BattleCardSystem 환경: 가장 가까운 플레이어 유닛을 찾아 이동 예산만큼 접근하고 공격
    /// </summary>
    public override async UniTask ExecuteBehaviorAsync(IBattleAIContext context, BattleUnit unit)
    {
        if (context == null || unit == null) return;

        // 가장 가까운 플레이어 유닛 탐색
        BattleUnit target = context.GetNearestPlayerUnit(unit);
        if (target == null) return;

        // 이동 예산 내에서 타겟 방향으로의 경로 탐색
        List<Vector2Int> path = context.FindPathTowards(unit, context.GetGridPosition(target), context.GetMoveBudget(unit));
        if (path != null && path.Count > 0)
        {
            await context.MoveUnitAlongPathAsync(unit, path);
        }

        // 사거리 내 플레이어 유닛 공격 시도
        context.TryAttackPlayerInRange(unit, context.GetAttackRange(unit));
        
        // 시각적 피드백을 위한 지연
        await UniTask.Delay(200);
    }
}
