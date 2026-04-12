namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    public enum BattleDamageScalingMode
    {
        Fixed,
        SourceUnitValue,
        FixedPlusSourceUnitValue,
    }

    public enum BattleDamageValueSourceUnit
    {
        SourceUnit,
        PrimaryTarget,
        FirstResolvedTarget,
    }

    public enum BattleUnitValueType
    {
        CurrentAttackPower,
        BaseAttackPower,
        CurrentSpeed,
        BaseSpeed,
        CurrentHealth,
        MissingHealth,
        MaxHealth,
    }

    [System.Serializable]
    public class BattleDamageEffect : BattleEffect
    {
        [SerializeField] private BattleDamageScalingMode scalingMode = BattleDamageScalingMode.FixedPlusSourceUnitValue;
        [SerializeField] private BattleDamageValueSourceUnit valueSourceUnit = BattleDamageValueSourceUnit.SourceUnit;
        [SerializeField] private BattleUnitValueType valueType = BattleUnitValueType.CurrentAttackPower;
        [SerializeField] private int amount = 0;
        [SerializeField] private float multiplier = 1f;

        public override void Apply(BattleEffectContext context, IReadOnlyList<BattleUnit> resolvedTargets)
        {
            if (context == null)
            {
                return;
            }

            foreach (var unit in ResolveTargetUnits(context, resolvedTargets))
            {
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                BattleUnit valueUnit = ResolveValueUnit(context, resolvedTargets);
                int unitValue = ReadUnitValue(valueUnit);
                int damage = scalingMode switch
                {
                    BattleDamageScalingMode.SourceUnitValue => Mathf.FloorToInt(unitValue * multiplier),
                    BattleDamageScalingMode.FixedPlusSourceUnitValue => amount + Mathf.FloorToInt(unitValue * multiplier),
                    _ => amount,
                };

                unit.TakeDamage(Mathf.Max(0, damage));
            }
        }

        public override Dictionary<string, string> GetDescriptionTokens(NYH.CoreCardSystem.Card sourceCard)
        {
            return new Dictionary<string, string>
            {
                { "damage", BuildDamageTokenText() },
                { "damageFlat", amount.ToString() },
                { "damageMultiplier", multiplier.ToString("0.##") },
                { "damageValueType", GetValueTypeLabel() },
            };
        }

        private BattleUnit ResolveValueUnit(BattleEffectContext context, IReadOnlyList<BattleUnit> resolvedTargets)
        {
            if (context == null)
            {
                return null;
            }

            return valueSourceUnit switch
            {
                BattleDamageValueSourceUnit.PrimaryTarget => context.TargetUnit,
                BattleDamageValueSourceUnit.FirstResolvedTarget => resolvedTargets != null && resolvedTargets.Count > 0 ? resolvedTargets[0] : null,
                _ => context.SourceUnit,
            };
        }

        private int ReadUnitValue(BattleUnit unit)
        {
            if (unit == null)
            {
                return 0;
            }

            return valueType switch
            {
                BattleUnitValueType.BaseAttackPower => unit.AttackPower,
                BattleUnitValueType.CurrentSpeed => unit.CurrentSpeed,
                BattleUnitValueType.BaseSpeed => unit.Speed,
                BattleUnitValueType.CurrentHealth => unit.CurrentHealth,
                BattleUnitValueType.MissingHealth => Mathf.Max(0, unit.MaxHealth - unit.CurrentHealth),
                BattleUnitValueType.MaxHealth => unit.MaxHealth,
                _ => unit.CurrentAttackPower,
            };
        }

        private string BuildDamageTokenText()
        {
            return scalingMode switch
            {
                BattleDamageScalingMode.SourceUnitValue => $"{GetValueTypeLabel()} x {multiplier:0.##}",
                BattleDamageScalingMode.FixedPlusSourceUnitValue => $"{amount} + {GetValueTypeLabel()} x {multiplier:0.##}",
                _ => amount.ToString(),
            };
        }

        private string GetValueTypeLabel()
        {
            return valueType switch
            {
                BattleUnitValueType.BaseAttackPower => "기본공격력",
                BattleUnitValueType.CurrentSpeed => "현재속도",
                BattleUnitValueType.BaseSpeed => "기본속도",
                BattleUnitValueType.CurrentHealth => "현재체력",
                BattleUnitValueType.MissingHealth => "잃은체력",
                BattleUnitValueType.MaxHealth => "최대체력",
                _ => "현재공격력",
            };
        }
    }
}
