namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    public enum BattleTrapTriggerTargetRule
    {
        [InspectorName("상대만")]
        OpponentsOnly,

        [InspectorName("아군만")]
        AlliesOnly,

        [InspectorName("모든 유닛")]
        AllUnits,
    }

    public enum BattleTrapVisibilityRule
    {
        [InspectorName("상대에게 숨김")]
        HiddenFromOpposingTeam,

        [InspectorName("모두에게 공개")]
        VisibleToAll,
    }

    [System.Serializable]
    public sealed class BattleTrapEffect : BattleEffect
    {
        [Header("소유 / 발동")]
        [SerializeField] private BattleTeam ownerTeam = BattleTeam.Player;
        [SerializeField] private BattleTrapTriggerTargetRule triggerTargetRule = BattleTrapTriggerTargetRule.OpponentsOnly;
        [SerializeField] private BattleTrapVisibilityRule visibilityRule = BattleTrapVisibilityRule.HiddenFromOpposingTeam;
        [SerializeField] private int triggerCount = 1;

        [Header("발동 범위")]
        [SerializeField] private int impactRange = 0;
        [SerializeField] private BattleAttackPattern impactPattern = BattleAttackPattern.None;
        [SerializeField] private AttackPatternData customImpactPattern;
        [SerializeField] private BattleUnitTargetFilter impactTargetFilter = BattleUnitTargetFilter.EnemiesOnly;

        public BattleTeam OwnerTeam => ownerTeam;
        public BattleTrapTriggerTargetRule TriggerTargetRule => triggerTargetRule;
        public BattleTrapVisibilityRule VisibilityRule => visibilityRule;
        public int TriggerCount => Mathf.Max(1, triggerCount);
        public int ImpactRange => Mathf.Max(0, impactRange);
        public BattleAttackPattern ImpactPattern => impactPattern;
        public AttackPatternData CustomImpactPattern => customImpactPattern;
        public BattleUnitTargetFilter ImpactTargetFilter => impactTargetFilter;

        public override void Apply(BattleEffectContext context, IReadOnlyList<BattleUnit> resolvedTargets)
        {
            // BattleTrapEffect는 설치/발동 정의만 담당합니다.
        }

        public override Dictionary<string, string> GetDescriptionTokens(NYH.CoreCardSystem.Card sourceCard)
        {
            return new Dictionary<string, string>
            {
                { "trapRange", ImpactRange.ToString() },
                { "trapTriggerCount", TriggerCount.ToString() },
            };
        }
    }
}
