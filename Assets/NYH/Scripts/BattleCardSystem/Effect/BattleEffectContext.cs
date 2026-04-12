namespace NYH.BattleCardSystem
{
    using UnityEngine;

    public class BattleEffectContext
    {
        public BattleCard SourceCard { get; }
        public BattleUnit SourceUnit { get; }
        public BattleUnit TargetUnit { get; }
        public Vector2Int TargetPosition { get; }
        public BattleBoardSystem BoardSystem { get; }
        public BattleCardSystem CardSystem { get; }

        public BattleEffectContext(
            BattleCard sourceCard,
            BattleUnit sourceUnit,
            BattleUnit targetUnit,
            Vector2Int targetPosition,
            BattleBoardSystem boardSystem,
            BattleCardSystem cardSystem)
        {
            SourceCard = sourceCard;
            SourceUnit = sourceUnit;
            TargetUnit = targetUnit;
            TargetPosition = targetPosition;
            BoardSystem = boardSystem;
            CardSystem = cardSystem;
        }
    }
}
