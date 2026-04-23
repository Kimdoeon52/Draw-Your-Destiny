namespace NYH.BattleCardSystem
{
    using NYH.CoreCardSystem;
    using UnityEngine;

    public class BattleAttackGA : GameAction
    {
        public BattleCard SourceCard { get; }
        public BattleUnit Attacker { get; }
        public BattleUnit PrimaryTarget { get; }
        public Vector2Int TargetPosition { get; }
        public int Damage { get; }
        public int Range { get; }
        public int TargetCount { get; }
        public bool HitsAllTargetsInRange { get; }
        public bool BlocksBehindTargets { get; }
        public BattleAttackPattern AttackPattern { get; }
        public AttackPatternData CustomAttackPattern { get; }
        public BattleAttackPatternOriginMode PatternOriginMode { get; }
        public BattleUnitTargetFilter TargetFilter { get; }

        public BattleAttackGA(
            BattleCard sourceCard,
            BattleUnit attacker,
            BattleUnit primaryTarget,
            Vector2Int targetPosition,
            int damage,
            int range,
            int targetCount,
            bool hitsAllTargetsInRange,
            bool blocksBehindTargets,
            BattleAttackPattern attackPattern,
            AttackPatternData customAttackPattern = null,
            BattleAttackPatternOriginMode patternOriginMode = BattleAttackPatternOriginMode.RangedPattern,
            BattleUnitTargetFilter targetFilter = BattleUnitTargetFilter.EnemiesOnly)
        {
            SourceCard = sourceCard;
            Attacker = attacker;
            PrimaryTarget = primaryTarget;
            TargetPosition = targetPosition;
            Damage = damage;
            Range = range;
            TargetCount = targetCount;
            HitsAllTargetsInRange = hitsAllTargetsInRange;
            BlocksBehindTargets = blocksBehindTargets;
            AttackPattern = attackPattern;
            CustomAttackPattern = customAttackPattern;
            PatternOriginMode = patternOriginMode;
            TargetFilter = targetFilter;
        }
    }
}
