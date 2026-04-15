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

        [Tooltip("한 번의 공격 판정으로 실제 맞을 수 있는 최대 적 수입니다.\n예: 2로 두면 범위 안의 적 중 최대 2명만 맞습니다.")]
        [SerializeField] private int targetCount = 1;

        [Tooltip("플레이어가 타일/대상을 몇 번 선택해야 공격이 확정되는지 정합니다.\n보통 일반 공격 카드는 1을 사용합니다.")]
        [SerializeField] private int selectionCount = 1;

        [Tooltip("체크하면 범위 안에 들어온 모든 적을 제한 없이 공격합니다.\n이 경우 Target Count는 사실상 무시됩니다.")]
        [SerializeField] private bool hitsAllTargetsInRange;

        [Header("기본 패턴")]
        [Tooltip("분리 패턴을 끄면 이 패턴이 조준/피격 둘 다에 사용됩니다.")]
        [SerializeField] private BattleAttackPattern attackPattern = BattleAttackPattern.None;

        [Tooltip("직접 만든 커스텀 패턴입니다.\n지정하면 기본 Attack Pattern 대신 이 패턴을 사용합니다.")]
        [SerializeField] private AttackPatternData customAttackPattern;

        [Header("조준 패턴 / 피격 패턴 분리")]
        [Tooltip("체크하면 플레이어가 고르는 범위와 실제 공격이 터지는 범위를 따로 설정할 수 있습니다.")]
        [SerializeField] private bool useSeparatePatterns;

        [Tooltip("플레이어가 선택할 수 있는 조준 거리입니다.\nUse Separate Patterns가 꺼져 있으면 기본 Range를 사용합니다.")]
        [SerializeField] private int targetingRange = 1;

        [Tooltip("플레이어가 고를 수 있는 칸의 모양입니다.\n예: 다이아 범위 안에서 한 칸을 고르게 만들고 싶을 때 사용합니다.")]
        [SerializeField] private BattleAttackPattern targetingPattern = BattleAttackPattern.None;

        [Tooltip("직접 만든 조준용 커스텀 패턴입니다.\n지정하면 Targeting Pattern 대신 사용합니다.")]
        [SerializeField] private AttackPatternData customTargetingPattern;

        [Tooltip("실제 공격이 터질 때의 피격 거리입니다.\nUse Separate Patterns가 꺼져 있으면 기본 Range를 사용합니다.")]
        [SerializeField] private int impactRange = 1;

        [Tooltip("선택한 지점 기준으로 실제 공격이 퍼지는 모양입니다.\n예: 십자 범위, 다이아 범위, 직선 범위 등.")]
        [SerializeField] private BattleAttackPattern impactPattern = BattleAttackPattern.None;

        [Tooltip("직접 만든 피격용 커스텀 패턴입니다.\n지정하면 Impact Pattern 대신 사용합니다.")]
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
