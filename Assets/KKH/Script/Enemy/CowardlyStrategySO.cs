using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NYH.BattleCardSystem;
using UnityEngine;

/// <summary>
/// 겁이 많은(비겁한) AI 전략입니다.
/// 플레이어와 일정 거리(idealDistance)를 유지하려고 하며,
/// 포지셔닝 후 사거리 안에 플레이어가 있으면 공격함
/// </summary>
[CreateAssetMenu(fileName = "CowardlyStrategy", menuName = "AI/Strategy/Cowardly Strategy")]
public class CowardlyStrategySO : AIBehaviorStrategySO
{
    [Header("거리 설정")]
    [Tooltip("플레이어와 유지하고 싶은 이상적인 거리 (칸)")]
    [SerializeField] private float idealDistance = 3f;

    /// <summary>
    /// 레거시 환경 (EnemyAIManager) — 현재 미사용. 베이스 클래스의 abstract 구현 의무만 충족함
    /// </summary>
    public override async UniTask ExecuteBehaviorAsync(EnemyAIManager context, Transform unit)
    {
        // 레거시 환경은 더 이상 사용하지 않음
        await UniTask.CompletedTask;
    }

    /// <summary>
    /// BattleCardSystem 환경: 플레이어와 idealDistance를 유지하도록 포지셔닝한 뒤,
    /// 사거리 내에 있으면 공격함
    /// </summary>
    public override async UniTask ExecuteBehaviorAsync(IBattleAIContext context, BattleUnit unit)
    {
        if (context == null || unit == null) return;

        BattleUnit player = context.GetNearestPlayerUnit(unit);
        if (player == null) return;

        Vector2Int unitCell = context.GetGridPosition(unit);
        Vector2Int playerCell = context.GetGridPosition(player);
        int distance = Mathf.Abs(unitCell.x - playerCell.x) + Mathf.Abs(unitCell.y - playerCell.y);

        // 플레이어로부터 멀어지는 방향 벡터
        Vector2 awayDir = ((Vector2)(unitCell - playerCell)).normalized;

        // idealDistance만큼 떨어진 "이상적인 위치"를 계산
        Vector2Int idealCell = playerCell + new Vector2Int(
            Mathf.RoundToInt(awayDir.x * idealDistance),
            Mathf.RoundToInt(awayDir.y * idealDistance));

        int distToIdeal = Mathf.Abs(unitCell.x - idealCell.x) + Mathf.Abs(unitCell.y - idealCell.y);

        // 이상적 위치에서 1칸 이상 벗어나 있으면 이동
        if (distToIdeal > 1)
        {
            List<Vector2Int> path = context.FindPathTowards(unit, idealCell, context.GetMoveBudget(unit));
            if (path != null && path.Count > 0)
            {
                await context.MoveUnitAlongPathAsync(unit, path);
            }
        }

        // 포지셔닝 완료 후 사거리 내에 플레이어가 있으면 공격
        context.TryAttackPlayerInRange(unit, context.GetAttackRange(unit));

        await UniTask.Delay(200);
    }
}
