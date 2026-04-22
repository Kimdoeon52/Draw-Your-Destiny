namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;

    // 전투 카드 더미를 기존 카드 목록 UI에서 볼 수 있게 변환하고 표시합니다.
    internal sealed class BattleCardPileViewService
    {
        private readonly BattleCardPileState pileState;

        // 표시할 더미 정보를 가진 pile state를 보관합니다.
        public BattleCardPileViewService(BattleCardPileState pileState)
        {
            this.pileState = pileState;
        }

        // 현재 draw pile을 일반 Card 미리보기 목록으로 변환해 보여줍니다.
        public void ShowDeck()
        {
            if (pileState.DrawPileCount == 0 || CardListUI.Instance == null)
            {
                return;
            }

            CardListUI.Instance.Show(
                ConvertToPreviewCards(pileState.GetShuffledDrawPileCopy()),
                "전투 덱 확인");
        }

        // 현재 discard pile을 일반 Card 미리보기 목록으로 변환해 보여줍니다.
        public void ShowDiscardPile()
        {
            if (pileState.DiscardPileCount == 0 || CardListUI.Instance == null)
            {
                return;
            }

            CardListUI.Instance.Show(
                ConvertToPreviewCards(pileState.GetShuffledDiscardPileCopy()),
                "전투 버림 더미 확인");
        }

        // BattleCard를 CardListUI가 이해하는 일반 Card 프리뷰 객체로 변환합니다.
        private static List<Card> ConvertToPreviewCards(IEnumerable<BattleCard> battleCards)
        {
            List<Card> previewCards = new();
            if (battleCards == null)
            {
                return previewCards;
            }

            foreach (BattleCard battleCard in battleCards)
            {
                Card previewCard = BattleCardViewAdapter.CreatePreviewCard(battleCard);
                if (previewCard != null)
                {
                    previewCards.Add(previewCard);
                }
            }

            return previewCards;
        }
    }
}
