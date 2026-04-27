namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    // 피해량을 고정값으로 쓸지, 유닛 스탯 기반으로 계산할지 정합니다.
    public enum BattleDamageScalingMode
    {
        [InspectorName("고정 수치만 사용")]
        Fixed,

        [InspectorName("참조 스탯 x 배율")]
        SourceUnitValue,

        [InspectorName("고정 수치 + 참조 스탯 x 배율")]
        FixedPlusSourceUnitValue,
    }

    // 스탯 기반 피해 계산에서 어느 유닛의 값을 읽을지 정합니다.
    public enum BattleDamageValueSourceUnit
    {
        [InspectorName("카드 사용 유닛")]
        SourceUnit,

        [InspectorName("직접 선택 대상")]
        PrimaryTarget,

        [InspectorName("범위 판정 첫 대상")]
        FirstResolvedTarget,
    }

    // 피해 계산에 사용할 유닛 스탯 종류입니다.
    public enum BattleUnitValueType
    {
        [InspectorName("현재 공격력")]
        CurrentAttackPower,

        [InspectorName("기본 공격력")]
        BaseAttackPower,

        [InspectorName("현재 속도")]
        CurrentSpeed,

        [InspectorName("기본 속도")]
        BaseSpeed,

        [InspectorName("현재 체력")]
        CurrentHealth,

        [InspectorName("잃은 체력")]
        MissingHealth,

        [InspectorName("최대 체력")]
        MaxHealth,
    }

    [System.Serializable]
    /*
     * BattleDamageEffect
     *
     * 역할:
     * - 대상 유닛에게 피해를 적용합니다.
     * - 고정 피해, 유닛 스탯 기반 피해, 고정+스탯 혼합 피해를 모두 지원합니다.
     */
    public class BattleDamageEffect : BattleEffect
    {
        [Header("피해 계산 방식")]
        [Tooltip("고정 피해인지, 특정 유닛의 스탯을 참조해서 계산할지 정합니다.")]
        [SerializeField] private BattleDamageScalingMode scalingMode = BattleDamageScalingMode.FixedPlusSourceUnitValue;

        [Header("참조할 유닛 / 스탯")]
        [Tooltip("스탯을 읽어올 기준 유닛입니다.\n카드 사용 유닛: 카드를 쓴 아군/적 유닛\n직접 선택 대상: 플레이어가 직접 찍은 대상\n범위 판정 첫 대상: 범위 계산 후 가장 먼저 잡힌 대상")]
        [SerializeField] private BattleDamageValueSourceUnit valueSourceUnit = BattleDamageValueSourceUnit.SourceUnit;

        [Tooltip("피해 계산에 사용할 스탯 종류입니다. 공격력, 속도, 현재 체력 등을 선택할 수 있습니다.")]
        [SerializeField] private BattleUnitValueType valueType = BattleUnitValueType.CurrentAttackPower;

        [Header("수치")]
        [Tooltip("고정 피해값입니다.\n'고정 수치만 사용' 또는 '고정 수치 + 참조 스탯 x 배율' 방식에서 사용됩니다.")]
        [SerializeField] private int amount = 0;

        [Tooltip("참조한 스탯에 곱할 배율입니다.\n'참조 스탯 x 배율' 또는 '고정 수치 + 참조 스탯 x 배율' 방식에서 사용됩니다.")]
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
