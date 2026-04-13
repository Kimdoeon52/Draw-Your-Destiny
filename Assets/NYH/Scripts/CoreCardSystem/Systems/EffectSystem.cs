using System.Collections;
using NYH.CoreCardSystem;
using UnityEngine;

public class EffectSystem : MonoBehaviour
{
    private void OnEnable()
    {
        ActionSystem.AttachPerformer<PerformEffectGA>(PerformEffectPerformer);
        ActionSystem.AttachPerformer<ContinueBehaviourGA>(PerformContinuePerformer);
        ActionSystem.AttachPerformer<ContinueBehaviourByBuildingGA>(PerformContinueByBuildingPerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<PerformEffectGA>();
        ActionSystem.DetachPerformer<ContinueBehaviourGA>();
        ActionSystem.DetachPerformer<ContinueBehaviourByBuildingGA>();
    }

    private IEnumerator PerformEffectPerformer(PerformEffectGA performEffectGA)
    {
        if (performEffectGA == null || performEffectGA.Effect == null) yield break;

        // 인덱스와 소스 카드를 함께 전달하여 호출
        GameAction effectAction = performEffectGA.Effect.GetGameAction(
            performEffectGA.EffectIndex,
            performEffectGA.SourceCard
        );

        if (effectAction != null)
        {
            ActionSystem.Instance.AddReaction(effectAction);
        }
        yield return null;
    }

    private IEnumerator PerformContinuePerformer(ContinueBehaviourGA continueGA)
    {
        OngoingEffectSystem.Instance.Register(
            continueGA.SourceCard,
            continueGA.StartEffectIndex,
            continueGA.TurnAmount
        );
        yield return null;
    }

    private IEnumerator PerformContinueByBuildingPerformer(ContinueBehaviourByBuildingGA continueBGA)
    {
        OngoingEffectSystem.Instance.Register(
            continueBGA.SourceCard,
            continueBGA.StartEffectIndex,
            continueBGA.TargetBuildingType
        );
        yield return null;
    }
}