namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    [System.Serializable]
    public class BattleMoveEffect : BattleEffect
    {
        [Header("이동 수치")]
        [Tooltip("기본 이동 칸 수입니다.")]
        [SerializeField] private int amount = 1;

        [Tooltip("체크하면 실제 이동량 = 기본 이동 칸 수 + 사용 유닛의 현재 속도 입니다.")]
        [SerializeField] private bool includeSourceUnitSpeed = true;

        public int Amount => amount;
        public bool IncludeSourceUnitSpeed => includeSourceUnitSpeed;

        public override void Apply(BattleEffectContext context, IReadOnlyList<BattleUnit> resolvedTargets)
        {
            if (context?.SourceUnit == null || context.BoardSystem == null)
            {
                return;
            }

            ActionSystem.Instance.AddReaction(
                new BattleMoveGA(
                    context.SourceCard,
                    context.SourceUnit,
                    context.TargetPosition,
                    context.PlannedPath,
                    amount,
                    includeSourceUnitSpeed ? context.SourceUnit.CurrentSpeed : 0));
        }

        public override Dictionary<string, string> GetDescriptionTokens(NYH.CoreCardSystem.Card sourceCard)
        {
            return new Dictionary<string, string>
            {
                { "moveAmount", BuildMoveTokenText() },
                { "moveFlat", amount.ToString() },
            };
        }

        private string BuildMoveTokenText()
        {
            if (!includeSourceUnitSpeed)
            {
                return amount.ToString();
            }

            return $"{amount} + 현재속도";
        }
    }
}
