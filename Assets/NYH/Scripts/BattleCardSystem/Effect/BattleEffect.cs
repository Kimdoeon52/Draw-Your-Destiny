namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    public enum BattleEffectTargetType
    {
        Self,
        PrimaryTarget,
        ResolvedTargets,
    }

    [System.Serializable]
    public abstract class BattleEffect : Effect
    {
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
