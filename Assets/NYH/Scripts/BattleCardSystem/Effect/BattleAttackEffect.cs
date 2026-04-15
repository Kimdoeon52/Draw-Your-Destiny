namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class BattleAttackEffect : BattleEffect
    {
        [Header("공격사거리")]
        [SerializeField] private int range = 1;

        [SerializeField] private int targetCount = 1;
        [SerializeField] private int selectionCount = 1;
        [SerializeField] private bool hitsAllTargetsInRange;

        [Header("기존 피격 패턴")]
        [SerializeField] private BattleAttackPattern attackPattern = BattleAttackPattern.None;
        [SerializeField] private AttackPatternData customAttackPattern;

        [Header("조준 패턴 / 피격 패턴 분리")]
        [SerializeField] private bool useSeparatePatterns;
        [SerializeField] private int targetingRange = 1;
        [SerializeField] private BattleAttackPattern targetingPattern = BattleAttackPattern.None;
        [SerializeField] private AttackPatternData customTargetingPattern;
        [SerializeField] private int impactRange = 1;
        [SerializeField] private BattleAttackPattern impactPattern = BattleAttackPattern.None;
        [SerializeField] private AttackPatternData customImpactPattern;

        public int Range => Mathf.Max(1, range);
        public int TargetCount => Mathf.Max(1, targetCount);
        public int SelectionCount => Mathf.Max(1, selectionCount);
        public bool HitsAllTargetsInRange => hitsAllTargetsInRange;
        public BattleAttackPattern AttackPattern => attackPattern;
        public AttackPatternData CustomAttackPattern => customAttackPattern;
        public bool UseSeparatePatterns => useSeparatePatterns;
        public int TargetingRange => useSeparatePatterns ? Mathf.Max(1, targetingRange) : Range;
        public BattleAttackPattern TargetingPattern => useSeparatePatterns ? targetingPattern : AttackPattern;
        public AttackPatternData CustomTargetingPattern => useSeparatePatterns ? customTargetingPattern : CustomAttackPattern;
        public int ImpactRange => useSeparatePatterns ? Mathf.Max(1, impactRange) : Range;
        public BattleAttackPattern ImpactPattern => useSeparatePatterns ? impactPattern : AttackPattern;
        public AttackPatternData CustomImpactPattern => useSeparatePatterns ? customImpactPattern : CustomAttackPattern;

        public override void Apply(BattleEffectContext context, IReadOnlyList<BattleUnit> resolvedTargets)
        {
        }

        public override Dictionary<string, string> GetDescriptionTokens(NYH.CoreCardSystem.Card sourceCard)
        {
            return new Dictionary<string, string>
            {
                { "attackRange", Range.ToString() },
                { "attackTargetCount", TargetCount.ToString() },
            };
        }
    }
}
