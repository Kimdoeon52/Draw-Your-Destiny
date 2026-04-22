using UnityEngine;
using Cysharp.Threading.Tasks;
using NYH.BattleCardSystem;

/// <summary>
/// AI 행동 전략을 정의하는 ScriptableObject 베이스 클래스입니다.
/// 다양한 적 유닛의 행동 패턴(공격적, 도망, 전술적 등)을 이 클래스를 상속받아 구현합니다.
/// </summary>
public abstract class AIBehaviorStrategySO : ScriptableObject
{
    /// <summary>
    /// 레거시 EnemyAIManager 환경에서 유닛의 행동을 실행합니다.
    /// </summary>
    /// <param name="context">AI 관리자 컨텍스트</param>
    /// <param name="unit">행동을 수행할 유닛의 Transform</param>
    public abstract UniTask ExecuteBehaviorAsync(EnemyAIManager context, Transform unit);

    /// <summary>
    /// 새로운 BattleCardSystem(IBattleAIContext) 환경에서 유닛의 행동을 실행합니다.
    /// </summary>
    /// <param name="context">배틀 AI 컨텍스트 인터페이스</param>
    /// <param name="unit">행동을 수행할 배틀 유닛</param>
    public virtual UniTask ExecuteBehaviorAsync(IBattleAIContext context, BattleUnit unit)
    {
        // 기본값으로 빈 작업 반환
        return UniTask.CompletedTask;
    }
}
