namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /*
     * BattleEffectContext
     *
     * 역할:
     * - 전투 이펙트 Apply()가 필요로 하는 실행 당시 정보를 한 묶음으로 전달합니다.
     * - 카드, 사용자, 직접 대상, 조준 위치, 이동 경로, 보드/카드 시스템 참조를 담습니다.
     */
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
