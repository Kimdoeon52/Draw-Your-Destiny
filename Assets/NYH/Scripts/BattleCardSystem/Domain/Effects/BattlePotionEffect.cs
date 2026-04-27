namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    public enum BattlePotionTargetingType
    {
        [InspectorName("범위형")]
        Range,

        [InspectorName("전부")]
        All,
    }

    [System.Serializable]
    public sealed class BattlePotionEffect : BattleEffect
    {
        [Header("포션 타게팅")]
        [SerializeField] private BattlePotionTargetingType targetingType = BattlePotionTargetingType.Range;

        [Tooltip("효과 판정의 기준 팀입니다. 기본값은 플레이어입니다.")]
        [SerializeField] private BattleTeam ownerTeam = BattleTeam.Player;

        [Header("범위형 포션")]
        [SerializeField] private int range = 1;
        [SerializeField] private BattleAttackPattern impactPattern = BattleAttackPattern.Area;
        [SerializeField] private AttackPatternData customImpactPattern;
        [SerializeField] private BattleUnitTargetFilter impactTargetFilter = BattleUnitTargetFilter.AllUnits;

        [Header("전부 포션")]
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
            // BattlePotionEffect는 대상 계산 정의만 담당합니다.
        }

        public override Dictionary<string, string> GetDescriptionTokens(NYH.CoreCardSystem.Card sourceCard)
        {
            return new Dictionary<string, string>
            {
                { "potionRange", Mathf.Max(0, range).ToString() },
            };
        }
    }
}
