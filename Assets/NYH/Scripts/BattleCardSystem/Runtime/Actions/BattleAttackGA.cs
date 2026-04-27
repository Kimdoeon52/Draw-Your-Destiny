namespace NYH.BattleCardSystem
{
    using NYH.CoreCardSystem;
    using UnityEngine;

    /*
     * BattleAttackGA
     *
     * 역할:
     * - 전투 공격 1회를 실행하기 위해 필요한 모든 계산 결과와 카드 설정을 담는 GameAction입니다.
     * - 실제 피해 적용과 피격 대상 계산은 BattleTacticalPerformer/BattleAttackQueryService가 담당합니다.
     */
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
