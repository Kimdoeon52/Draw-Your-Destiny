using UnityEngine;
using Cysharp.Threading.Tasks;
using NYH.BattleCardSystem;

/// <summary>
/// KKH 적 AI 전략의 공통 기반 클래스입니다.
/// 기존 EnemyAIManager 기반 실행은 유지하고,
/// NYH 전투 시스템과 통합하기 위한 Battle AI 컨텍스트 실행 경로를 추가합니다.
/// </summary>
public abstract class AIBehaviorStrategySO : ScriptableObject
{
    public abstract UniTask ExecuteBehaviorAsync(EnemyAIManager context, Transform unit);

    public virtual UniTask ExecuteBehaviorAsync(IBattleAIContext context, BattleUnit unit)
    {
        return UniTask.CompletedTask;
    }
}
