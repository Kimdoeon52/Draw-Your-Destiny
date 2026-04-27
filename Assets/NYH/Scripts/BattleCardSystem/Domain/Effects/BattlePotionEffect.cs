namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 포션 카드가 어떤 방식으로 발동되는지 정의합니다.
    /// </summary>
    public enum BattlePotionTargetingType
    {
        [InspectorName("범위형")]
        Range,

        [InspectorName("전체형")]
        All,
    }

    /// <summary>
    /// 포션 카드 전용 루트 이펙트입니다.
    /// 실제 회복, 피해, 버프 같은 능력은 다른 payload 이펙트가 담당하고,
    /// 이 클래스는 "포션이 어디를 기준으로 누구에게 영향을 주는지"를 정의합니다.
    /// </summary>
    [System.Serializable]
    public sealed class BattlePotionEffect : BattleEffect
    {
        [Header("포션 발동 방식")]
        [Tooltip(
            "포션 카드의 사용 방식을 설정합니다.\n" +
            "범위형: 플레이어가 전투 그리드의 특정 칸을 선택하고, 그 칸을 기준으로 범위를 계산합니다.\n" +
            "전체형: 칸 선택 없이 즉시 발동하며, 전역 대상 필터에 맞는 유닛들에게 바로 효과를 적용합니다.")]
        [SerializeField] private BattlePotionTargetingType targetingType = BattlePotionTargetingType.Range;

        [Tooltip(
            "이 포션의 기준 팀입니다.\n" +
            "전체형 포션이나 아군/적군 판별이 필요한 대상 필터 계산 시 사용됩니다.\n" +
            "예: 아군 전체 회복 포션은 Player 기준 + AlliesOnly 조합으로 설정합니다.")]
        [SerializeField] private BattleTeam ownerTeam = BattleTeam.Player;

        [Header("범위형 포션 설정")]
        [Tooltip(
            "범위형 포션에서 선택한 칸으로부터 얼마나 멀리까지 영향을 줄지 설정합니다.\n" +
            "실제 모양은 아래 Impact Pattern 또는 Custom Impact Pattern 설정과 함께 결정됩니다.")]
        [SerializeField] private int range = 0;

        [Tooltip(
            "범위형 포션의 영향 범위 모양입니다.\n" +
            "None이면 선택한 칸만 사용하고, 다른 패턴이면 해당 규칙에 따라 주변 칸을 계산합니다.")]
        [SerializeField] private BattleAttackPattern impactPattern = BattleAttackPattern.None;

        [Tooltip(
            "직접 만든 커스텀 범위 패턴입니다.\n" +
            "값이 있으면 Impact Pattern보다 이 설정을 우선 사용합니다.")]
        [SerializeField] private AttackPatternData customImpactPattern;

        [Tooltip(
            "범위형 포션의 영향 칸 안에 있는 유닛 중 실제로 효과를 받을 대상을 고릅니다.\n" +
            "예: 적에게만 피해를 주는 포션, 아군에게만 회복을 주는 포션.")]
        [SerializeField] private BattleUnitTargetFilter impactTargetFilter = BattleUnitTargetFilter.AlliesOnly;

        [Header("전체형 포션 설정")]
        [Tooltip(
            "전체형 포션이 즉시 적용될 유닛 그룹입니다.\n" +
            "그리드 선택 없이 전장 전체에서 이 조건에 맞는 유닛을 찾아 효과를 적용합니다.")]
        [SerializeField] private BattleUnitTargetFilter globalTargetFilter = BattleUnitTargetFilter.AlliesOnly;

        public BattlePotionTargetingType TargetingType => targetingType;
        public BattleTeam OwnerTeam => ownerTeam;
        public int Range => Mathf.Max(0, range);
        public BattleAttackPattern ImpactPattern => impactPattern;
        public AttackPatternData CustomImpactPattern => customImpactPattern;
        public BattleUnitTargetFilter ImpactTargetFilter => impactTargetFilter;
        public BattleUnitTargetFilter GlobalTargetFilter => globalTargetFilter;

        public override void Apply(BattleEffectContext context, IReadOnlyList<BattleUnit> resolvedTargets)
        {
            // BattlePotionEffect는 포션 카드의 타게팅 규칙만 제공하는 루트 이펙트입니다.
            // 실제 능력 적용은 같은 카드에 함께 들어 있는 payload 이펙트들이 담당합니다.
        }

        public override Dictionary<string, string> GetDescriptionTokens(NYH.CoreCardSystem.Card sourceCard)
        {
            return new Dictionary<string, string>
            {
                { "potionRange", Range.ToString() },
            };
        }
    }
}
