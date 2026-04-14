namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class BattleAttackEffect : BattleEffect
    {
        [SerializeField] private int range = 1;
        [SerializeField] private int targetCount = 1;
        [SerializeField] private bool hitsAllTargetsInRange;
        [SerializeField] private BattleAttackPattern attackPattern = BattleAttackPattern.None;
        [SerializeField] private AttackPatternData customAttackPattern;

        public int Range => Mathf.Max(1, range);
        public int TargetCount => Mathf.Max(1, targetCount);
        public bool HitsAllTargetsInRange => hitsAllTargetsInRange;
        public BattleAttackPattern AttackPattern => attackPattern;
        public AttackPatternData CustomAttackPattern => customAttackPattern;

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
