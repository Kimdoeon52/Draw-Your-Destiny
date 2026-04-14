namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    public enum BattleEffectTargetType
    {
        [InspectorName("자기 자신")]
        Self,

        [InspectorName("직접 선택 대상 1명")]
        PrimaryTarget,

        [InspectorName("범위 판정 대상들")]
        ResolvedTargets,
    }

    [System.Serializable]
    public abstract class BattleEffect : Effect
    {
        [Header("대상 지정")]
        [Tooltip("이 이펙트를 누구에게 적용할지 정합니다.\n자기 자신: 카드를 사용한 유닛\n직접 선택 대상 1명: 플레이어가 직접 찍은 대상\n범위 판정 대상들: 공격/범위 판정으로 실제 맞은 대상들")]
        [SerializeField] private BattleEffectTargetType targetType = BattleEffectTargetType.ResolvedTargets;

        public BattleEffectTargetType TargetType => targetType;

        public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
        {
            return null;
        }

        public abstract void Apply(BattleEffectContext context, IReadOnlyList<BattleUnit> resolvedTargets);

        protected IEnumerable<BattleUnit> ResolveTargetUnits(BattleEffectContext context, IReadOnlyList<BattleUnit> resolvedTargets)
        {
            if (context == null)
            {
                yield break;
            }

            switch (targetType)
            {
                case BattleEffectTargetType.Self:
                    if (context.SourceUnit != null)
                    {
                        yield return context.SourceUnit;
                    }
                    yield break;

                case BattleEffectTargetType.PrimaryTarget:
                    if (context.TargetUnit != null)
                    {
                        yield return context.TargetUnit;
                        yield break;
                    }

                    if (resolvedTargets != null && resolvedTargets.Count > 0 && resolvedTargets[0] != null)
                    {
                        yield return resolvedTargets[0];
                    }
                    yield break;

                case BattleEffectTargetType.ResolvedTargets:
                default:
                    if (resolvedTargets == null)
                    {
                        yield break;
                    }

                    for (int i = 0; i < resolvedTargets.Count; i++)
                    {
                        if (resolvedTargets[i] != null)
                        {
                            yield return resolvedTargets[i];
                        }
                    }
                    yield break;
            }
        }
    }
}
