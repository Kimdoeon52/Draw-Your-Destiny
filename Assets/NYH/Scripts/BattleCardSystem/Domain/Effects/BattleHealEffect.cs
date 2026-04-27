namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    /*
     * BattleHealEffect
     *
     * 역할:
     * - 범위 판정으로 선택된 대상에게 회복을 적용합니다.
     * - 회복 범위 설정은 공격 패턴 시스템과 같은 개념을 재사용합니다.
     */
    public class BattleHealEffect : BattleEffect
    {
        [Header("회복량")]
        [Tooltip("대상 유닛에게 회복시킬 고정 수치입니다. 회복 후 체력은 최대 체력을 넘지 않습니다.")]
        [SerializeField] private int amount = 1;

        [Header("회복 범위")]
        [Tooltip("회복 범위 거리입니다. 선택한 칸을 기준으로 아래 패턴만큼 회복 대상 칸을 계산합니다.")]
        [SerializeField] private int range = 1;

        [Tooltip("회복 범위 모양입니다.\n없음/기본 단일: 선택한 칸 1개만 회복합니다.\n상하좌우 인접 4칸/범위: 선택한 칸 기준 다이아 범위로 회복합니다.\n직선: 카드 사용 유닛에서 선택한 방향으로 직선 범위를 회복합니다.")]
        [SerializeField] private BattleAttackPattern healPattern = BattleAttackPattern.Adjacent4;

        [Tooltip("직접 만든 회복 범위 패턴입니다.\n지정하면 Heal Pattern 대신 이 패턴을 사용합니다.")]
        [SerializeField] private AttackPatternData customHealPattern;

        [Tooltip("회복 범위 기준입니다.\n원거리 패턴: 선택한 칸 기준으로 회복 범위가 펼쳐집니다.\n근거리 패턴: 선택한 칸은 방향만 정하고 유닛 바로 앞부터 회복 범위가 펼쳐집니다.")]
        [SerializeField] private BattleAttackPatternOriginMode healPatternOriginMode = BattleAttackPatternOriginMode.RangedPattern;

        [Header("회복 대상 필터")]
        [Tooltip("범위 안에서 실제로 회복할 유닛 팀을 정합니다.\n아군만: 힐러 카드 기본값입니다.\n모든 유닛: 아군과 적을 모두 회복합니다.")]
        [SerializeField] private BattleUnitTargetFilter healTargetFilter = BattleUnitTargetFilter.AlliesOnly;

        public int Amount => Mathf.Max(0, amount);
        public int Range => Mathf.Max(1, range);
        public BattleAttackPattern HealPattern => healPattern;
        public AttackPatternData CustomHealPattern => customHealPattern;
        public BattleAttackPatternOriginMode HealPatternOriginMode => healPatternOriginMode;
        public BattleUnitTargetFilter HealTargetFilter => healTargetFilter;

        public override void Apply(BattleEffectContext context, IReadOnlyList<BattleUnit> resolvedTargets)
        {
            if (context == null || Amount <= 0)
            {
                return;
            }

            foreach (BattleUnit unit in ResolveTargetUnits(context, resolvedTargets))
            {
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                unit.Heal(Amount);
            }
        }

        public override Dictionary<string, string> GetDescriptionTokens(NYH.CoreCardSystem.Card sourceCard)
        {
            return new Dictionary<string, string>
            {
                { "heal", Amount.ToString() },
                { "healRange", Range.ToString() },
            };
        }
    }
}
