namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    public enum BattleUnitStatType
    {
        AttackPower,
        Speed,
    }

    [System.Serializable]
    public class BattleStatModifierEffect : BattleEffect
    {
        [SerializeField] private BattleUnitStatType statType = BattleUnitStatType.AttackPower;
        [SerializeField] private int amount = 1;

        public override void Apply(BattleEffectContext context, IReadOnlyList<BattleUnit> resolvedTargets)
        {
            foreach (var unit in ResolveTargetUnits(context, resolvedTargets))
            {
                if (unit == null)
                {
                    continue;
                }

                switch (statType)
                {
                    case BattleUnitStatType.Speed:
                        unit.ModifySpeed(amount);
                        break;

                    case BattleUnitStatType.AttackPower:
                    default:
                        unit.ModifyAttackPower(amount);
                        break;
                }
            }
        }

        public override Dictionary<string, string> GetDescriptionTokens(NYH.CoreCardSystem.Card sourceCard)
        {
            return new Dictionary<string, string>
            {
                { "statAmount", amount.ToString() },
                { "statType", statType == BattleUnitStatType.Speed ? "속도" : "공격력" },
            };
        }
    }
}
