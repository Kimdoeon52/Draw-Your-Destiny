namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    public enum BattleUnitStatType
    {
        [InspectorName("공격력")]
        AttackPower,

        [InspectorName("속도")]
        Speed,
    }

    [System.Serializable]
    public class BattleStatModifierEffect : BattleEffect
    {
        [Header("능력치 변경")]
        [Tooltip("변경할 스탯 종류입니다.")]
        [SerializeField] private BattleUnitStatType statType = BattleUnitStatType.AttackPower;

        [Tooltip("증가/감소 수치입니다. 음수를 넣으면 감소 디버프로도 사용할 수 있습니다.")]
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
