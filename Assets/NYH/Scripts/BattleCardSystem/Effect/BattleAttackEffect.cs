namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class BattleAttackEffect : BattleEffect
    {
        [Header("공격 범위")]
        [Tooltip("공격 가능한 타일 거리입니다. 라인/다이아/커스텀 패턴의 기준 거리가 됩니다.")]
        [SerializeField] private int range = 1;

        [Tooltip("동시에 맞출 최대 적 수입니다. '범위 내 모두 공격'이 켜져 있으면 이 값은 무시됩니다.")]
        [SerializeField] private int targetCount = 1;

        [Tooltip("체크하면 범위 안에 들어온 적을 수 제한 없이 모두 공격합니다.")]
        [SerializeField] private bool hitsAllTargetsInRange;

        [Header("공격 모양")]
        [Tooltip("기본 공격 범위 모양입니다. 커스텀 패턴을 지정하지 않을 때 사용됩니다.")]
        [SerializeField] private BattleAttackPattern attackPattern = BattleAttackPattern.None;

        [Tooltip("직접 만든 패턴 데이터입니다. 지정하면 기본 패턴 대신 이 패턴 셀을 사용합니다.")]
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
