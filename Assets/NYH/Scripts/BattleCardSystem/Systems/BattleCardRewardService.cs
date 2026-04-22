namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;

    // 전투 보상 카드와 포션 카드 추가 규칙을 담당합니다.
    internal sealed class BattleCardRewardService
    {
        private readonly BattleCardPileState pileState;
        private readonly List<BattleCardData> earnedBattleCards;

        // 보상 카드를 반영할 현재 pile state와 fallback 보상 목록을 보관합니다.
        public BattleCardRewardService(BattleCardPileState pileState, List<BattleCardData> earnedBattleCards)
        {
            this.pileState = pileState;
            this.earnedBattleCards = earnedBattleCards;
        }

        // 전투 보상 카드를 영속 컬렉션 또는 현재 전투 덱에 추가합니다.
        public BattleDeckAddResult AddEarnedBattleCard(BattleCardData data, BattleCard replaceTarget = null)
        {
            if (BattleDeckCollection.Instance != null)
            {
                BattleCardData replaceTargetData = replaceTarget != null ? replaceTarget.Data : null;
                return BattleDeckCollection.Instance.AddBattleRewardCard(data, replaceTargetData);
            }

            BattleDeckAddResult result = pileState.AddRewardCard(data, replaceTarget);
            if (result == BattleDeckAddResult.Added || result == BattleDeckAddResult.Replaced)
            {
                earnedBattleCards?.Add(data);
            }

            return result;
        }

        // 포션 카드를 영속 컬렉션이 있으면 그쪽에, 없으면 현재 전투 덱에 바로 추가합니다.
        public void AddPotionCard(BattleCardData potionData)
        {
            if (BattleDeckCollection.Instance != null)
            {
                BattleDeckCollection.Instance.AddPotionCard(potionData);
                return;
            }

            pileState.AddPotionCard(potionData);
        }
    }
}
