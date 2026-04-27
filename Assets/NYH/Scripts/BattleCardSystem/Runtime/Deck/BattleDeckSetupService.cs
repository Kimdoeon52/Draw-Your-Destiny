namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Populates the runtime battle pile at battle start.
    /// The preferred path is the persistent <see cref="BattleDeckCollection"/> state.
    /// </summary>
    internal sealed class BattleDeckSetupService
    {
        private readonly BattleCardPileState pileState;

        /// <summary>
        /// Binds this setup helper to the runtime pile state used by the current battle.
        /// </summary>
        public BattleDeckSetupService(BattleCardPileState pileState)
        {
            this.pileState = pileState;
        }

        /// <summary>
        /// Legacy fallback that merges base and earned cards directly.
        /// This exists for compatibility, but normal flow should prefer BattleDeckCollection.
        /// </summary>
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

        /// <summary>
        /// Standard battle-start setup.
        /// Syncs the latest base deck into the collection, then builds the runtime pile from the
        /// saved current deck if one already exists.
        /// </summary>
        public void SetupFromInspector(IReadOnlyList<BattleCardData> baseBattleDeck)
        {
            BattleDeckCollection deckCollection = BattleDeckCollection.GetOrCreate();
            if (deckCollection == null)
            {
                int baseCount = baseBattleDeck != null ? baseBattleDeck.Count : 0;
                Debug.LogWarning($"[BattleCardSystem] SetupFromInspector: failed to create BattleDeckCollection. Using base deck only. baseDeck={baseCount}");
                SetupBattleDeck(baseBattleDeck, null);
                return;
            }

            deckCollection.ConfigureBaseDeck(baseBattleDeck);
            pileState.Setup(deckCollection.BuildBattleDeckSources());
        }
    }
}
