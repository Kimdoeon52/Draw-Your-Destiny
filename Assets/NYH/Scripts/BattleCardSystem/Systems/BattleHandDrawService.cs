namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;

    // 전투 손패 드로우, 멀리건, 턴 종료 손패 정리를 담당합니다.
    internal sealed class BattleHandDrawService
    {
        // 살아 있는 플레이어 유닛 종류 수에 더해 기본으로 몇 장을 뽑을지 설정하는 값입니다.
        private int baseDrawCount = 2; // 기본으로 유닛 타입 수 +2장 뽑기 이수치 변경해서 더 늘리거나 줄이기 가능

        private readonly BattleCardPileState pileState;
        private readonly BattleOpeningHandService openingHandService = new();

        // 손패와 더미를 조작할 pile state를 보관합니다.
        public BattleHandDrawService(BattleCardPileState pileState)
        {
            this.pileState = pileState;
        }

        // 살아 있는 플레이어 유닛 종류 수를 기준으로 전투 시작 손패를 뽑습니다.
        public List<BattleCard> DrawOpeningHand(int unitTypeCount)
        {
            int drawCount = openingHandService.CalculateDrawCountByAliveUnitTypes(unitTypeCount, baseDrawCount);
            return pileState.DrawCards(drawCount);
        }

        // 손패 전체를 덱으로 되돌리고 시작 손패 규칙으로 다시 뽑습니다.
        public List<BattleCard> MulliganOpeningHand(int unitTypeCount)
        {
            pileState.ReturnHandToDrawPileAndShuffle();
            return DrawOpeningHand(unitTypeCount);
        }

        // 선택한 카드만 멀리건하고 결과 정보를 반환합니다.
        public BattleMulliganResult MulliganSelectedCards(IReadOnlyList<BattleCard> selectedCards)
        {
            return pileState.MulliganSelectedCards(selectedCards);
        }

        // 살아 있는 플레이어 유닛 종류 수를 기준으로 턴 시작 카드를 뽑습니다.
        public List<BattleCard> DrawTurnCards(int aliveUnitTypeCount)
        {
            int drawCount = openingHandService.CalculateDrawCountByAliveUnitTypes(aliveUnitTypeCount, baseDrawCount);
            return pileState.DrawCards(drawCount);
        }

        // 현재 손패를 모두 버림 더미로 이동합니다.
        public void EndTurnDiscardHand()
        {
            pileState.DiscardHand();
        }
    }
}
