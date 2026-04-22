namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    // 전투 시작 시 사용할 덱 구성을 담당합니다.
    internal sealed class BattleDeckSetupService
    {
        private readonly BattleCardPileState pileState;

        // 덱을 실제로 저장할 pile state를 보관합니다.
        public BattleDeckSetupService(BattleCardPileState pileState)
        {
            this.pileState = pileState;
        }

        // 기본 덱과 획득 카드 목록을 합쳐 현재 전투 덱으로 설정합니다.
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

        // BattleDeckCollection 우선, 없으면 인스펙터 fallback 덱으로 현재 전투 덱을 설정합니다.
        public void SetupFromInspector(IReadOnlyList<BattleCardData> baseBattleDeck, IReadOnlyList<BattleCardData> earnedBattleCards)
        {
            if (BattleDeckCollection.Instance != null)
            {
                if (BattleDeckCollection.Instance.BaseBattleDeck.Count == 0 && baseBattleDeck != null && baseBattleDeck.Count > 0)
                {
                    Debug.LogWarning($"[BattleCardSystem] BattleDeckCollection의 기본 덱이 비어 있어 fallback 기본 덱 {baseBattleDeck.Count}장을 복사합니다.");
                    BattleDeckCollection.Instance.ConfigureBaseDeck(baseBattleDeck);
                }

                SetupBattleDeck(
                    BattleDeckCollection.Instance.BaseBattleDeck,
                    BattleDeckCollection.Instance.EarnedBattleCards);
                return;
            }

            int baseCount = baseBattleDeck != null ? baseBattleDeck.Count : 0;
            int earnedCount = earnedBattleCards != null ? earnedBattleCards.Count : 0;
            Debug.LogWarning($"[BattleCardSystem] SetupFromInspector: BattleDeckCollection이 없어 fallback 덱을 사용합니다. baseDeck={baseCount}, earned={earnedCount}");
            SetupBattleDeck(baseBattleDeck, earnedBattleCards);
        }
    }
}
