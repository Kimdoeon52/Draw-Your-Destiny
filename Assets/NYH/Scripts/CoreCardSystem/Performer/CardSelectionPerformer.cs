namespace NYH.CoreCardSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using NYH.BattleCardSystem;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Handles card-pick flows that open a UI, wait for user choice, and then apply the result.
    /// This includes civilization card rewards and battle reward replacement routing.
    /// </summary>
    public class CardSelectionPerformer
    {
        private readonly CardPileState pileState;
        private readonly HandView handView;
        private readonly Text deckCountText;
        private readonly Text discardCountText;
        private readonly System.Action refreshPileCounts;
        private readonly BattleDeckReplacementSelector battleDeckReplacementSelector = new();

        public CardSelectionPerformer(
            CardPileState pileState,
            HandView handView,
            Text deckCountText,
            Text discardCountText,
            System.Action refreshPileCounts)
        {
            this.pileState = pileState;
            this.handView = handView;
            this.deckCountText = deckCountText;
            this.discardCountText = discardCountText;
            this.refreshPileCounts = refreshPileCounts;
        }

        /// <summary>
        /// Shows random civilization-card candidates and adds the chosen card into the draw pile.
        /// </summary>
        public IEnumerator OfferRandomCatalogCardToDeck(int amount)
        {
            if (CardCatalog.Instance == null)
            {
                Debug.LogWarning("[CardSystem] CardCatalog is missing. Skipping card selection.");
                yield break;
            }

            if (CardSelectionUI.Instance == null)
            {
                Debug.LogWarning("[CardSystem] CardSelectionUI is missing. Skipping card selection.");
                yield break;
            }

            List<CardData> candidateData = CardCatalog.Instance.GetRandom(amount);
            if (candidateData == null || candidateData.Count == 0)
            {
                Debug.LogWarning("[CardSystem] No card candidates are available.");
                yield break;
            }

            List<Card> previewCards = new();
            foreach (CardData data in candidateData)
            {
                if (data != null)
                {
                    previewCards.Add(new Card(data));
                }
            }

            if (previewCards.Count == 0)
            {
                yield break;
            }

            Card selectedCard = null;
            bool isChosen = false;

            CardSelectionUI.Instance.Show(previewCards, card =>
            {
                selectedCard = card;
                isChosen = true;
            });

            yield return new WaitUntil(() => isChosen);

            if (selectedCard?.data == null)
            {
                yield break;
            }

            pileState.AddToDrawPile(new Card(selectedCard.data));
            pileState.ShuffleDrawPile();
            refreshPileCounts?.Invoke();
            UpdateTexts();
        }

        /// <summary>
        /// Shows reward bundles containing one civilization card and one battle card.
        /// The chosen battle reward is routed through the persistent battle deck flow first.
        /// If the player cancels battle-card replacement, neither reward is granted.
        /// </summary>
        public IEnumerator OfferRewardBundlesToDecks(int bundleCount)
        {
            if (CardSelectionUI.Instance == null)
            {
                Debug.LogWarning("[CardSystem] CardSelectionUI is missing. Skipping reward bundle selection.");
                yield break;
            }

            if (CardCatalog.Instance == null || BattleCardCatalog.Instance == null)
            {
                Debug.LogWarning("[CardSystem] CardCatalog or BattleCardCatalog is missing. Cannot build reward bundles.");
                yield break;
            }

            List<CardData> civilizationCandidates = CardCatalog.Instance.GetRandom(bundleCount);
            List<BattleCardData> battleCandidates = BattleCardCatalog.Instance.GetRandom(bundleCount);
            int actualBundleCount = Mathf.Min(civilizationCandidates.Count, battleCandidates.Count);

            if (actualBundleCount == 0)
            {
                Debug.LogWarning("[CardSystem] No reward bundle candidates are available.");
                yield break;
            }

            List<RewardCardBundleChoice> rewardBundles = new();
            for (int i = 0; i < actualBundleCount; i++)
            {
                CardData civilizationCard = civilizationCandidates[i];
                BattleCardData battleCard = battleCandidates[i];
                if (civilizationCard == null || battleCard == null)
                {
                    continue;
                }

                rewardBundles.Add(new RewardCardBundleChoice(civilizationCard, battleCard));
            }

            if (rewardBundles.Count == 0)
            {
                Debug.LogWarning("[CardSystem] Failed to build any valid reward bundles.");
                yield break;
            }

            RewardCardBundleChoice selectedBundle = null;
            bool isChosen = false;

            CardSelectionUI.Instance.ShowRewardBundles(rewardBundles, bundle =>
            {
                selectedBundle = bundle;
                isChosen = true;
            });

            yield return new WaitUntil(() => isChosen);

            if (selectedBundle == null)
            {
                yield break;
            }

            bool battleRewardApplied = selectedBundle.BattleCardData == null;
            if (selectedBundle.BattleCardData != null)
            {
                BattleDeckCollection.GetOrCreate();
                yield return AddBattleRewardWithReplacement(
                    selectedBundle.BattleCardData,
                    wasApplied => battleRewardApplied = wasApplied);
            }

            if (!battleRewardApplied)
            {
                yield break;
            }

            if (selectedBundle.CivilizationCardData != null)
            {
                pileState.AddToDrawPile(new Card(selectedBundle.CivilizationCardData));
                pileState.ShuffleDrawPile();
                refreshPileCounts?.Invoke();
                UpdateTexts();
            }
        }

        /// <summary>
        /// Convenience wrapper that keeps civilization and battle reward counts in sync.
        /// </summary>
        public IEnumerator OfferMixedRewardToDecks(int civilizationAmount, int battleAmount)
        {
            int bundleCount = Mathf.Min(civilizationAmount, battleAmount);
            yield return OfferRewardBundlesToDecks(bundleCount);
        }

        /// <summary>
        /// Lets the user choose one card from the top of the draw pile and move it into the hand.
        /// </summary>
        public IEnumerator ChooseCardPerformer(int amount)
        {
            List<Card> choices = pileState.PeekDrawPile(amount);
            if (choices.Count == 0)
            {
                yield break;
            }

            Card selectedCard = null;
            bool isChosen = false;

            if (CardSelectionUI.Instance == null)
            {
                Debug.LogError("[CardSystem] CardSelectionUI is missing.");
                yield break;
            }

            CardSelectionUI.Instance.Show(choices, card =>
            {
                selectedCard = card;
                isChosen = true;
            });

            yield return new WaitUntil(() => isChosen);

            if (selectedCard == null)
            {
                yield break;
            }

            pileState.RemoveFromDrawPile(selectedCard);
            pileState.AddToHand(selectedCard);
            refreshPileCounts?.Invoke();
            UpdateTexts();

            CardView cardView = CardViewCreator.Instance.CreateCardView(selectedCard, Vector3.zero, Quaternion.identity);
            yield return handView.AddCard(cardView);
        }

        /// <summary>
        /// Refreshes the visible deck and discard counters after a selection flow mutates piles.
        /// </summary>
        private void UpdateTexts()
        {
            if (deckCountText != null)
            {
                deckCountText.text = $"{pileState.DrawPileCount} cards";
            }

            if (discardCountText != null)
            {
                discardCountText.text = $"{pileState.DiscardPileCount} cards";
            }
        }

        /// <summary>
        /// Adds a battle reward card and opens the replacement UI only when the deck limit is full.
        /// The callback reports whether the battle reward was actually granted.
        /// </summary>
        private IEnumerator AddBattleRewardWithReplacement(BattleCardData rewardCard, System.Action<bool> onFinished)
        {
            BattleDeckCollection deckCollection = BattleDeckCollection.GetOrCreate();

            BattleDeckAddResult result = deckCollection.AddRewardCard(rewardCard);
            if (result != BattleDeckAddResult.NeedsReplacement)
            {
                onFinished?.Invoke(result == BattleDeckAddResult.Added);
                yield break;
            }

            BattleCardData replaceTarget = null;
            yield return battleDeckReplacementSelector.SelectReplacement(
                rewardCard,
                deckCollection.GetReplaceableCards(),
                selected => replaceTarget = selected);

            if (replaceTarget == null)
            {
                Debug.LogWarning("[CardSystem] Battle reward was not added because no replacement target was selected.");
                onFinished?.Invoke(false);
                yield break;
            }

            BattleDeckAddResult replaceResult = deckCollection.ReplaceCard(replaceTarget, rewardCard);
            onFinished?.Invoke(replaceResult == BattleDeckAddResult.Replaced);
        }

        /// <summary>
        /// Debug/helper path that directly adds copies of a civilization card into the draw pile.
        /// </summary>
        public IEnumerator AddCardInMyDeck(CardData targetCard, int addAmount)
        {
            for (int i = 0; i < addAmount; i++)
            {
                pileState.AddToDrawPile(new Card(targetCard));
            }

            yield break;
        }
    }
}
