namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /*
     * BattleDeckSetupService
     *
     * 역할:
     * - 전투가 시작될 때 BattleCardPileState에 사용할 전투덱을 세팅합니다.
     * - BattleDeckCollection이 있으면 저장된 현재 전투덱을 우선 사용합니다.
     * - BattleDeckCollection이 없으면 인스펙터 fallback 덱을 사용합니다.
     */
    internal sealed class BattleDeckSetupService
    {
        private readonly BattleCardPileState pileState;

        // 실제 카드 더미를 소유한 pile state를 받아 덱 구성만 담당합니다.
        public BattleDeckSetupService(BattleCardPileState pileState)
        {
            this.pileState = pileState;
        }

        // 기본덱과 획득 카드 목록을 합쳐 현재 전투용 draw pile을 구성합니다.
        // BattleDeckCollection이 없을 때 사용하는 fallback 경로입니다.
        public void SetupBattleDeck(IEnumerable<BattleCardData> baseDeck, IEnumerable<BattleCardData> earnedCards)
        {
            List<BattleCardData> mergedDeck = new();
            if (baseDeck != null)
            {
                mergedDeck.AddRange(baseDeck);
            }

            if (earnedCards != null)
            {
                mergedDeck.AddRange(earnedCards);
            }

            pileState.Setup(mergedDeck);
        }

        // 전투 시작 시 호출되는 기본 세팅 진입점입니다.
        // 저장소가 있으면 currentBattleDeck을 사용하고, 없으면 인스펙터 덱을 합쳐 사용합니다.
        public void SetupFromInspector(IReadOnlyList<BattleCardData> baseBattleDeck, IReadOnlyList<BattleCardData> earnedBattleCards)
        {
            if (BattleDeckCollection.Instance != null)
            {
                if (BattleDeckCollection.Instance.BaseBattleDeck.Count == 0 && baseBattleDeck != null && baseBattleDeck.Count > 0)
                {
                    Debug.LogWarning($"[BattleCardSystem] BattleDeckCollection base deck is empty. Copying fallback base deck: {baseBattleDeck.Count}");
                    BattleDeckCollection.Instance.ConfigureBaseDeck(baseBattleDeck);
                }

                pileState.Setup(BattleDeckCollection.Instance.BuildBattleDeckSources());
                return;
            }

            int baseCount = baseBattleDeck != null ? baseBattleDeck.Count : 0;
            int earnedCount = earnedBattleCards != null ? earnedBattleCards.Count : 0;
            Debug.LogWarning($"[BattleCardSystem] SetupFromInspector: BattleDeckCollection is missing. Using fallback deck. baseDeck={baseCount}, earned={earnedCount}");
            SetupBattleDeck(baseBattleDeck, earnedBattleCards);
        }
    }
}
