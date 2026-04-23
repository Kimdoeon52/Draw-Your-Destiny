namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;

    /*
     * BattleCardRewardService
     *
     * 역할:
     * - 전투 보상 카드와 포션 카드를 실제 전투덱 저장소에 반영합니다.
     * - BattleDeckCollection이 있으면 영구 전투덱에 저장합니다.
     * - BattleDeckCollection이 없으면 현재 전투의 pileState에만 fallback으로 추가합니다.
     */
    internal sealed class BattleCardRewardService
    {
        private readonly BattleCardPileState pileState;
        private readonly List<BattleCardData> earnedBattleCards;

        // fallback 경로에서 사용할 현재 전투 pile state와 획득 카드 목록을 보관합니다.
        public BattleCardRewardService(BattleCardPileState pileState, List<BattleCardData> earnedBattleCards)
        {
            this.pileState = pileState;
            this.earnedBattleCards = earnedBattleCards;
        }

        // 전투 보상 카드를 추가하거나, replaceTarget이 있으면 기존 카드와 교체합니다.
        // BattleDeckCollection이 있는 일반 흐름에서는 저장된 현재 전투덱을 수정합니다.
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

        // 포션 카드는 덱 제한과 별개로 즉시 전투덱에 추가합니다.
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
