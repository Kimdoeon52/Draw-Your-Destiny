namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    public enum BattleUnitStatusType
    {
        Stun,
        Disarm,
    }

    [System.Serializable]
    public class BattleStatusEffect : BattleEffect
    {
        [SerializeField] private BattleUnitStatusType statusType = BattleUnitStatusType.Stun;

        public override void Apply(BattleEffectContext context, IReadOnlyList<BattleUnit> resolvedTargets)
        {
            foreach (var unit in ResolveTargetUnits(context, resolvedTargets))
            {
                if (unit == null)
                {
                    continue;
                }

                switch (statusType)
                {
                    case BattleUnitStatusType.Disarm:
                        unit.SetDisarmed(true);
                        break;

                    case BattleUnitStatusType.Stun:
                    default:
                        unit.SetStunned(true);
                        break;
                }
            }
        }

        public override Dictionary<string, string> GetDescriptionTokens(NYH.CoreCardSystem.Card sourceCard)
        {
            return new Dictionary<string, string>
            {
                { "status", statusType == BattleUnitStatusType.Disarm ? "무장해제" : "기절" },
            };
        }
    }
}
