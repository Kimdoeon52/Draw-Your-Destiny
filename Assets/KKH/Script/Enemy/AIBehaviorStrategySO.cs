using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 적 AI의 행동 로직(이동, 공격 등)을 정의하는 기반 전략 스크립터블 오브젝트.
/// 전략 패턴(Strategy Pattern)을 활용하여 구체적인 행동은 하위 클래스에서 구현한다.
/// </summary>
public abstract class AIBehaviorStrategySO : ScriptableObject
{
    /// <summary>
    /// AI 매니저 컨텍스트와 타겟 유닛을 전달받아 비동기 행동을 실행한다.
    /// </summary>
    /// <param name="context">현재 적 AI를 관리하는 매니저 인스턴스.</param>
    /// <param name="unit">행동을 수행할 적 유닛의 Transform.</param>
    /// <returns>비동기 행동 처리를 위한 UniTask.</returns>
    public abstract UniTask ExecuteBehaviorAsync(EnemyAIManager context, Transform unit);
}
