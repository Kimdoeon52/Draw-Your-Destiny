using UnityEngine;
using Cysharp.Threading.Tasks;
using NYH.BattleCardSystem;

public abstract class AIBehaviorStrategySO : ScriptableObject
{
    /// <summary>
    /// BattleCardSystem(IBattleAIContext) 환경에서 유닛의 행동을 실행
    /// </summary>
    public abstract UniTask ExecuteBehaviorAsync(IBattleAIContext context, BattleUnit unit);
}
