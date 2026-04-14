namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    public class BattleEffectContext
    {
        public BattleCard SourceCard { get; }
        public BattleUnit SourceUnit { get; }
        public BattleUnit TargetUnit { get; }
        public Vector2Int TargetPosition { get; }
        public IReadOnlyList<Vector2Int> PlannedPath { get; }
        public BattleBoardSystem BoardSystem { get; }
        public BattleCardSystem CardSystem { get; }

        public BattleEffectContext(
            BattleCard sourceCard,
            BattleUnit sourceUnit,
            BattleUnit targetUnit,
            Vector2Int targetPosition,
            IReadOnlyList<Vector2Int> plannedPath,
            BattleBoardSystem boardSystem,
            BattleCardSystem cardSystem)
        {
            SourceCard = sourceCard;
            SourceUnit = sourceUnit;
            TargetUnit = targetUnit;
            TargetPosition = targetPosition;
            PlannedPath = plannedPath;
            BoardSystem = boardSystem;
            CardSystem = cardSystem;
        }
    }
}
