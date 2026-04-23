namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    // 전투 유닛에게 걸 수 있는 간단한 상태이상 종류입니다.
    public enum BattleUnitStatusType
    {
        [InspectorName("기절")]
        Stun,

        [InspectorName("무장해제")]
        Disarm,
    }

    [System.Serializable]
    /*
     * BattleStatusEffect
     *
     * 역할:
     * - 대상 유닛에게 기절/무장해제 같은 전투 상태를 부여합니다.
     */
    public class BattleStatusEffect : BattleEffect
    {
        [Header("상태이상")]
        [Tooltip("대상에게 부여할 상태이상 종류입니다.")]
        [SerializeField] private BattleUnitStatusType statusType = BattleUnitStatusType.Stun;

        public override void Apply(BattleEffectContext context, IReadOnlyList<BattleUnit> resolvedTargets)
        {
            foreach (var unit in ResolveTargetUnits(context, resolvedTargets))
            {
                if (unit == null)
                {
                    continue;
                }

                switch (statusType)
                {
                    case BattleUnitStatusType.Disarm:
                        unit.SetDisarmed(true);
                        break;

                    case BattleUnitStatusType.Stun:
                    default:
                        unit.SetStunned(true);
                        break;
                }
            }
        }

        public override Dictionary<string, string> GetDescriptionTokens(NYH.CoreCardSystem.Card sourceCard)
        {
            return new Dictionary<string, string>
            {
                { "status", statusType == BattleUnitStatusType.Disarm ? "무장해제" : "기절" },
            };
        }
    }
}
