namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    // 전투 이펙트가 실제로 적용될 대상 범위를 지정합니다.
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
    /*
     * BattleEffect
     *
     * 역할:
     * - 모든 전투 전용 이펙트의 공통 기반 클래스입니다.
     * - CoreCardSystem Effect와 달리 GameAction을 직접 만들기보다 전투 실행 중 Apply()로 처리됩니다.
     * - targetType을 통해 자기 자신/직접 대상/범위 판정 대상 중 어디에 적용할지 통일합니다.
     */
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

        // 전투 실행 중 계산된 context와 범위 대상 목록을 받아 실제 효과를 적용합니다.
        public abstract void Apply(BattleEffectContext context, IReadOnlyList<BattleUnit> resolvedTargets);

        // targetType 설정에 따라 이 이펙트가 실제로 순회해야 할 유닛 목록을 만듭니다.
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
