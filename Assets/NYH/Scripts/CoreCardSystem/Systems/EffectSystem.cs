using System.Collections;
using NYH.CoreCardSystem;
using UnityEngine;

public class EffectSystem : MonoBehaviour
{
    private void OnEnable()
    {
        ActionSystem.AttachPerformer<PerformEffectGA>(PerformEffectPerformer);
        // ContinueBehaviourGA도 여기서 처리하도록 등록!
        ActionSystem.AttachPerformer<ContinueBehaviourGA>(PerformContinuePerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<PerformEffectGA>();
        ActionSystem.DetachPerformer<ContinueBehaviourGA>();
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

    // CardSystem에 있던 로직을 이쪽으로 이사 왔습니다.
    private IEnumerator PerformContinuePerformer(ContinueBehaviourGA continueGA)
    {
        OngoingEffectSystem.Instance.Register(
            continueGA.SourceCard,
            continueGA.StartEffectIndex,
            continueGA.TurnAmount
        );
        yield return null;
    }
}