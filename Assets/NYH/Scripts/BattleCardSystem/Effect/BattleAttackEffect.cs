namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class BattleAttackEffect : BattleEffect
    {
        // 공격 카드는 크게 두 단계로 동작합니다.
        // 1. 조준: 플레이어가 어디를 찍을 수 있는지
        // 2. 피격: 실제로 어떤 칸/유닛이 맞는지
        // 분리 패턴을 끄면 조준/피격이 같은 패턴을 사용합니다.

        [Header("기본 공격 판정")]
        [Tooltip("기본 공격 거리입니다.\n분리 패턴을 끄면 조준 범위와 실제 피격 범위 둘 다 이 값을 사용합니다.")]
        [SerializeField] private int range = 1;

        [Header("단일 타격 / 범위 전체 타격")]
        [Tooltip("한 번의 공격 판정으로 실제 맞을 수 있는 최대 대상 수입니다.\n예: 1이면 범위 안에서 가장 가까운 1명만 맞고, 노란 피격 표시도 실제 맞는 대상 칸 위주로 표시됩니다.")]
        [SerializeField] private int targetCount = 1;

        [Tooltip("플레이어가 타일/대상을 몇 번 선택해야 공격이 확정되는지 정합니다.\n보통 일반 공격 카드는 1을 사용합니다.")]
        [SerializeField] private int selectionCount = 1;

        [Tooltip("체크하면 범위 안에 들어온 모든 대상을 제한 없이 공격합니다.\n근거리 패턴과 함께 쓰면 유닛 앞/주변 범위 안의 적을 전부 때리는 근접 광역 공격이 됩니다.\n이 경우 Target Count는 사실상 무시됩니다.")]
        [SerializeField] private bool hitsAllTargetsInRange;

        [Header("범위 공격 대상 필터")]
        [Tooltip("범위 공격이 실제로 맞출 유닛 팀을 정합니다.\n적만: 기존 공격 카드 기본값입니다.\n모든 유닛: 아군과 적을 모두 맞춥니다.")]
        [SerializeField] private BattleUnitTargetFilter impactTargetFilter = BattleUnitTargetFilter.EnemiesOnly;

        [Tooltip("근거리 패턴에서 범위 안 대상이 공격을 막습니다.\n막는 대상의 칸은 맞지만, 같은 줄에서 그 뒤쪽 칸은 피격 범위에서 제외됩니다.")]
        [SerializeField] private bool blocksBehindTargets;

        [Header("기본 패턴")]
        [Tooltip("분리 패턴을 끄면 이 패턴이 조준/피격 둘 다에 사용됩니다.")]
        [SerializeField] private BattleAttackPattern attackPattern = BattleAttackPattern.None;

        [Tooltip("직접 만든 커스텀 패턴입니다.\n지정하면 기본 Attack Pattern 대신 이 패턴을 사용합니다.")]
        [SerializeField] private AttackPatternData customAttackPattern;

        [Header("원거리/근거리 패턴 기준")]
        [Tooltip("RangedPattern(원거리): 기존 카드용입니다. 선택한 칸을 기준으로 실제 공격 범위를 계산합니다.\nMeleePattern(근거리): I자/부채꼴 같은 근접 카드용입니다. 선택한 칸은 방향만 정하고, 실제 공격은 유닛 바로 앞에서 시작합니다.")]
        [SerializeField] private BattleAttackPatternOriginMode patternOriginMode = BattleAttackPatternOriginMode.RangedPattern;

        [Header("조준 패턴 / 피격 패턴 분리")]
        [Tooltip("체크하면 플레이어가 고르는 범위와 실제 공격이 터지는 범위를 따로 설정할 수 있습니다.")]
        [SerializeField] private bool useSeparatePatterns;

        [Tooltip("플레이어가 선택할 수 있는 조준 거리입니다.\nUse Separate Patterns가 꺼져 있으면 기본 Range를 사용합니다.")]
        [SerializeField] private int targetingRange = 1;

        [Tooltip("플레이어가 고를 수 있는 칸의 모양입니다.\n예: 다이아 범위 안에서 한 칸을 고르게 만들고 싶을 때 사용합니다.")]
        [SerializeField] private BattleAttackPattern targetingPattern = BattleAttackPattern.None;

        [Tooltip("직접 만든 조준용 커스텀 패턴입니다.\n지정하면 Targeting Pattern 대신 사용합니다.")]
        [SerializeField] private AttackPatternData customTargetingPattern;

        [Tooltip("실제 공격이 터질 때의 피격 거리입니다.\n원거리 패턴은 선택한 칸 기준 거리로, 근거리 패턴은 유닛 앞에서부터 세는 거리로 사용합니다.")]
        [SerializeField] private int impactRange = 1;

        [Tooltip("실제 공격이 퍼지는 모양입니다.\n원거리 패턴: 선택한 칸 기준으로 펼쳐집니다.\n근거리 패턴: 선택한 칸은 방향만 정하고 유닛 바로 앞부터 펼쳐집니다.")]
        [SerializeField] private BattleAttackPattern impactPattern = BattleAttackPattern.None;

        [Tooltip("직접 만든 피격용 커스텀 패턴입니다.\n원거리 패턴은 선택한 칸을 앵커로, 근거리 패턴은 유닛 위치를 앵커로 사용합니다.")]
        [SerializeField] private AttackPatternData customImpactPattern;

        public int Range => Mathf.Max(1, range);
        public int TargetCount => Mathf.Max(1, targetCount);
        public int SelectionCount => Mathf.Max(1, selectionCount);
        public bool HitsAllTargetsInRange => hitsAllTargetsInRange;
        public BattleUnitTargetFilter ImpactTargetFilter => impactTargetFilter;
        public bool BlocksBehindTargets => blocksBehindTargets;
        public BattleAttackPattern AttackPattern => attackPattern;
        public AttackPatternData CustomAttackPattern => customAttackPattern;
        public BattleAttackPatternOriginMode PatternOriginMode => patternOriginMode;
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
