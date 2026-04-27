namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 덫이 어떤 유닛에 의해 발동될 수 있는지 정의합니다.
    /// </summary>
    public enum BattleTrapTriggerTargetRule
    {
        [InspectorName("상대만")]
        OpponentsOnly,

        [InspectorName("아군만")]
        AlliesOnly,

        [InspectorName("모든 유닛")]
        AllUnits,
    }

    /// <summary>
    /// 덫이 상대 팀에게 보이는지 여부를 정의합니다.
    /// </summary>
    public enum BattleTrapVisibilityRule
    {
        [InspectorName("상대 팀에게 숨김")]
        HiddenFromOpposingTeam,

        [InspectorName("모든 팀에게 공개")]
        VisibleToAll,
    }

    /// <summary>
    /// 덫 카드 전용 루트 이펙트입니다.
    /// 실제 피해, 상태이상, 버프 같은 능력은 payload 이펙트가 담당하고,
    /// 이 클래스는 덫의 설치/발동/가시성 규칙을 정의합니다.
    /// </summary>
    [System.Serializable]
    public sealed class BattleTrapEffect : BattleEffect
    {
        [Header("덫 소유 및 발동 조건")]
        [Tooltip(
            "이 덫의 소유 팀입니다.\n" +
            "적군/아군 판별, 발동 조건, 가시성 계산의 기준으로 사용됩니다.\n" +
            "현재는 플레이어 설치가 기본이지만, 이후 적 설치 덫 확장을 위해 유지합니다.")]
        [SerializeField] private BattleTeam ownerTeam = BattleTeam.Player;

        [Tooltip(
            "어떤 유닛이 이 덫을 밟았을 때 발동할지 설정합니다.\n" +
            "상대만: 소유 팀의 반대편 유닛만 발동\n" +
            "아군만: 같은 팀 유닛만 발동\n" +
            "모든 유닛: 팀과 관계없이 누구든 밟으면 발동")]
        [SerializeField] private BattleTrapTriggerTargetRule triggerTargetRule = BattleTrapTriggerTargetRule.OpponentsOnly;

        [Tooltip(
            "이 덫이 상대 팀에게 보이는지 설정합니다.\n" +
            "기본값인 '상대 팀에게 숨김'은 소유 팀만 덫의 위치를 알 수 있게 합니다.")]
        [SerializeField] private BattleTrapVisibilityRule visibilityRule = BattleTrapVisibilityRule.HiddenFromOpposingTeam;

        [Tooltip(
            "이 덫이 최대 몇 번 발동 가능한지 설정합니다.\n" +
            "1이면 한 번 발동 후 사라지고, 2 이상이면 남은 횟수만큼 유지됩니다.")]
        [SerializeField] private int triggerCount = 1;

        [Header("덫 설치 외형")]
        [Tooltip(
            "덫을 전투 그리드에 설치했을 때 보드 위에 생성할 외형 프리팹입니다.\n" +
            "건물처럼 바닥에 깔리는 오브젝트를 넣어두면 설치 시점에 해당 칸에 생성되고,\n" +
            "덫이 소모되거나 전투가 끝나면 자동으로 제거됩니다.")]
        [SerializeField] private GameObject installedTrapVisualPrefab;

        [Header("덫 발동 범위")]
        [Tooltip(
            "유닛이 덫을 밟은 칸을 기준으로 얼마나 멀리까지 영향을 줄지 설정합니다.")]
        [SerializeField] private int impactRange = 0;

        [Tooltip(
            "덫이 발동했을 때 영향을 주는 범위 모양입니다.\n" +
            "None이면 밟은 칸만, 다른 패턴이면 해당 규칙에 따라 주변 칸을 함께 계산합니다.")]
        [SerializeField] private BattleAttackPattern impactPattern = BattleAttackPattern.None;

        [Tooltip(
            "직접 만든 커스텀 발동 패턴입니다.\n" +
            "값이 있으면 기본 Impact Pattern 대신 이 패턴을 우선 사용합니다.")]
        [SerializeField] private AttackPatternData customImpactPattern;

        [Tooltip(
            "발동 범위 안에 있는 유닛 중 실제로 효과를 받을 대상을 고릅니다.\n" +
            "예: 적만 피해, 아군만 회복, 전체 상태이상 등.")]
        [SerializeField] private BattleUnitTargetFilter impactTargetFilter = BattleUnitTargetFilter.EnemiesOnly;

        public BattleTeam OwnerTeam => ownerTeam;
        public BattleTrapTriggerTargetRule TriggerTargetRule => triggerTargetRule;
        public BattleTrapVisibilityRule VisibilityRule => visibilityRule;
        public int TriggerCount => Mathf.Max(1, triggerCount);
        public GameObject InstalledTrapVisualPrefab => installedTrapVisualPrefab;
        public int ImpactRange => Mathf.Max(0, impactRange);
        public BattleAttackPattern ImpactPattern => impactPattern;
        public AttackPatternData CustomImpactPattern => customImpactPattern;
        public BattleUnitTargetFilter ImpactTargetFilter => impactTargetFilter;

        public override void Apply(BattleEffectContext context, IReadOnlyList<BattleUnit> resolvedTargets)
        {
            // BattleTrapEffect는 설치 규칙과 발동 규칙만 정의합니다.
            // 실제 효과 적용은 TrapSystem이 발동 시 대상을 계산한 뒤,
            // 카드에 포함된 다른 payload 이펙트들이 처리합니다.
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
