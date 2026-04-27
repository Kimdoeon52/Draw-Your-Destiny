namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /// <summary>
    /// Persistent authority for the player's battle deck.
    /// baseBattleDeck tracks the latest scene-provided base deck,
    /// earnedBattleCards tracks cards added beyond base,
    /// and currentBattleDeck is the actual saved deck used for future battles.
    /// </summary>
    public class BattleDeckCollection : Singleton<BattleDeckCollection>
    {
        [Header("Base Battle Deck")]
        [SerializeField] private List<BattleCardData> baseBattleDeck = new();

        [Header("Earned Battle Cards")]
        [SerializeField] private List<BattleCardData> earnedBattleCards = new();

        [Header("Current Battle Deck")]
        [SerializeField] private List<BattleCardData> currentBattleDeck = new();

        public IReadOnlyList<BattleCardData> BaseBattleDeck => baseBattleDeck;
        public IReadOnlyList<BattleCardData> EarnedBattleCards => earnedBattleCards;

        /// <summary>
        /// Returns the current persisted battle deck, loading or rebuilding it on demand.
        /// </summary>
        public IReadOnlyList<BattleCardData> CurrentBattleDeck
        {
            get
            {
                EnsureCurrentDeckInitialized();
                return currentBattleDeck;
            }
        }

        /// <summary>
        /// Counts only cards that consume the normal deck limit.
        /// Potions, traps, and cards with IgnoresDeckLimit are excluded.
        /// </summary>
        public int LimitedDeckCount
        {
            get
            {
                EnsureCurrentDeckInitialized();
                return CountLimitedCards(currentBattleDeck);
            }
        }

        /// <summary>
        /// Ensures a collection instance exists even when the active scene does not contain one.
        /// </summary>
        public static BattleDeckCollection GetOrCreate()
        {
            BattleDeckCollection[] collections = FindObjectsByType<BattleDeckCollection>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            BattleDeckCollection preferredCollection = null;
            foreach (BattleDeckCollection collection in collections)
            {
                if (collection == null)
                {
                    continue;
                }

                if (preferredCollection == null || (preferredCollection.baseBattleDeck.Count == 0 && collection.baseBattleDeck.Count > 0))
                {
                    preferredCollection = collection;
                }
            }

            if (preferredCollection != null)
            {
                if (Instance != null && Instance != preferredCollection)
                {
                    Instance.TryAdoptSceneBaseDeck(preferredCollection.baseBattleDeck);
                    return Instance;
                }

                return preferredCollection;
            }

            if (Instance != null)
            {
                return Instance;
            }

            GameObject collectionObject = new(nameof(BattleDeckCollection));
            return collectionObject.AddComponent<BattleDeckCollection>();
        }

        /// <summary>
        /// Makes the collection persistent across scene changes and eagerly initializes current state.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                Instance?.TryAdoptSceneBaseDeck(baseBattleDeck);
                Debug.LogWarning($"[BattleDeckCollection] Duplicate instance will be destroyed: scene={gameObject.scene.name}, object={name}");
                return;
            }

            DontDestroyOnLoad(gameObject);
            EnsureCurrentDeckInitialized();
            Debug.Log($"[BattleDeckCollection] Awake complete: scene={gameObject.scene.name}, object={name}, baseDeck={baseBattleDeck.Count}, earned={earnedBattleCards.Count}, current={currentBattleDeck.Count}");
        }

        /// <summary>
        /// Synchronizes the latest scene base deck into persistent state.
        /// If no saved current deck exists, currentBattleDeck is rebuilt from base + earned.
        /// If a saved current deck exists, it is preserved and only the derived earned list is refreshed.
        /// </summary>
        public void ConfigureBaseDeck(IEnumerable<BattleCardData> source)
        {
            if (source != null)
            {
                List<BattleCardData> copiedBaseDeck = new();
                foreach (BattleCardData card in source)
                {
                    if (card != null)
                    {
                        copiedBaseDeck.Add(card);
                    }
                }

                baseBattleDeck.Clear();
                baseBattleDeck.AddRange(copiedBaseDeck);
            }

            if (!BattleDeckPersistenceService.HasSavedDeck())
            {
                RebuildAndSaveCurrentDeckFromParts();
            }
            else
            {
                EnsureCurrentDeckInitialized();
                RecalculateEarnedCardsFromCurrentDeck();
                if (ShouldRecoverCurrentDeckFromBase())
                {
                    Debug.LogWarning($"[BattleDeckCollection] Recovering saved deck from base deck: baseDeck={baseBattleDeck.Count}, current={currentBattleDeck.Count}, earned={earnedBattleCards.Count}");
                    RebuildAndSaveCurrentDeckFromParts();
                    RecalculateEarnedCardsFromCurrentDeck();
                }
            }

            Debug.Log($"[BattleDeckCollection] Base deck configured: baseDeck={baseBattleDeck.Count}, current={currentBattleDeck.Count}");
        }

        /// <summary>
        /// Resets only the earned-card tracking for a fresh run.
        /// Saved current deck contents are preserved unless there is no save yet.
        /// </summary>
        public void ResetRun()
        {
            earnedBattleCards.Clear();
            if (!BattleDeckPersistenceService.HasSavedDeck())
            {
                RebuildAndSaveCurrentDeckFromParts();
            }

            Debug.Log("[BattleDeckCollection] Run reset: earnedBattleCards cleared");
        }

        /// <summary>
        /// Clears the saved current deck and the in-memory current deck cache.
        /// The next ConfigureBaseDeck call will rebuild from the latest base + earned state.
        /// </summary>
        public void ClearSavedCurrentDeck()
        {
            BattleDeckPersistenceService.ClearSavedDeck();
            currentBattleDeck.Clear();
        }

        /// <summary>
        /// Returns a detached deck copy suitable for BattleCardPileState.Setup().
        /// Callers can freely mutate the returned list.
        /// </summary>
        public List<BattleCardData> BuildBattleDeckSources()
        {
            EnsureCurrentDeckInitialized();
            return new List<BattleCardData>(currentBattleDeck);
        }

        /// <summary>
        /// Compatibility entry point that either adds a reward card or replaces one immediately.
        /// </summary>
        public BattleDeckAddResult AddBattleRewardCard(BattleCardData data, BattleCardData replaceTarget = null)
        {
            if (replaceTarget != null)
            {
                return ReplaceCard(replaceTarget, data);
            }

            return AddRewardCard(data);
        }

        /// <summary>
        /// Adds a reward card when the limited deck has room.
        /// Returns NeedsReplacement instead of auto-removing a card when the limit is full.
        /// </summary>
        public BattleDeckAddResult AddRewardCard(BattleCardData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[BattleDeckCollection] Add reward failed: data is null");
                return BattleDeckAddResult.Invalid;
            }

            EnsureCurrentDeckInitialized();
            if (CanAddWithoutReplacement(data))
            {
                AddCardToCurrentDeck(data, trackAsEarned: true);
                Debug.Log($"[BattleDeckCollection] Reward added: {data.CardName}, current={currentBattleDeck.Count}");
                return BattleDeckAddResult.Added;
            }

            Debug.LogWarning($"[BattleDeckCollection] Replacement required: {data.CardName}");
            return BattleDeckAddResult.NeedsReplacement;
        }

        /// <summary>
        /// Replaces one limited-deck card with a newly earned card and persists the result.
        /// </summary>
        public BattleDeckAddResult ReplaceCard(BattleCardData removeTarget, BattleCardData addTarget)
        {
            if (removeTarget == null || addTarget == null)
            {
                Debug.LogWarning("[BattleDeckCollection] Replace failed: removeTarget or addTarget is null");
                return BattleDeckAddResult.Invalid;
            }

            EnsureCurrentDeckInitialized();
            if (ShouldIgnoreDeckLimit(removeTarget))
            {
                Debug.LogWarning($"[BattleDeckCollection] Replace failed: target ignores deck limit, target={removeTarget.CardName}");
                return BattleDeckAddResult.Invalid;
            }

            int removeIndex = currentBattleDeck.IndexOf(removeTarget);
            if (removeIndex < 0)
            {
                Debug.LogWarning($"[BattleDeckCollection] Replace failed: target not found, new={addTarget.CardName}, target={removeTarget.CardName}");
                return BattleDeckAddResult.Invalid;
            }

            currentBattleDeck.RemoveAt(removeIndex);
            earnedBattleCards.Remove(removeTarget);
            currentBattleDeck.Add(addTarget);
            earnedBattleCards.Add(addTarget);
            SaveCurrentDeck();

            Debug.Log($"[BattleDeckCollection] Reward replaced: removed={removeTarget.CardName}, added={addTarget.CardName}, current={currentBattleDeck.Count}");
            return BattleDeckAddResult.Replaced;
        }

        /// <summary>
        /// Returns true if the card can be added without opening replacement flow.
        /// </summary>
        public bool CanAddWithoutReplacement(BattleCardData data)
        {
            if (data == null)
            {
                return false;
            }

            EnsureCurrentDeckInitialized();
            return ShouldIgnoreDeckLimit(data) || LimitedDeckCount < BattleCardPileState.MaxDeckSize;
        }

        /// <summary>
        /// Returns cards that may legally be replaced in the reward replacement UI.
        /// </summary>
        public List<BattleCardData> GetReplaceableCards()
        {
            EnsureCurrentDeckInitialized();
            List<BattleCardData> result = new();
            foreach (BattleCardData card in currentBattleDeck)
            {
                if (card != null && !ShouldIgnoreDeckLimit(card))
                {
                    result.Add(card);
                }
            }

            return result;
        }

        /// <summary>
        /// Adds a deck-limit-ignoring card, such as a potion or trap, directly into the current deck.
        /// </summary>
        public void AddPotionCard(BattleCardData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[BattleDeckCollection] Add potion failed: data is null");
                return;
            }

            EnsureCurrentDeckInitialized();
            AddCardToCurrentDeck(data, trackAsEarned: true);
            Debug.Log($"[BattleDeckCollection] Potion added: {data.CardName}, current={currentBattleDeck.Count}");
        }

        /// <summary>
        /// Permanently removes one matching card from the persisted current deck and saves immediately.
        /// </summary>
        public bool RemoveSinglePersistedCard(BattleCardData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[BattleDeckCollection] Remove persisted card failed: data is null");
                return false;
            }

            EnsureCurrentDeckInitialized();

            int removeIndex = currentBattleDeck.IndexOf(data);
            if (removeIndex < 0)
            {
                Debug.LogWarning($"[BattleDeckCollection] Remove persisted card failed: target not found, card={data.CardName}");
                return false;
            }

            currentBattleDeck.RemoveAt(removeIndex);
            earnedBattleCards.Remove(data);
            SaveCurrentDeck();

            Debug.Log($"[BattleDeckCollection] Persisted card removed: {data.CardName}, current={currentBattleDeck.Count}");
            return true;
        }

        /// <summary>
        /// Initializes currentBattleDeck from save when possible.
        /// Falls back to base + earned reconstruction if no usable save exists.
        /// </summary>
        private void EnsureCurrentDeckInitialized()
        {
            if (currentBattleDeck.Count > 0)
            {
                return;
            }

            if (BattleDeckPersistenceService.HasSavedDeck())
            {
                if (BattleDeckPersistenceService.TryLoadDeck(out List<BattleCardData> savedDeck))
                {
                    currentBattleDeck.AddRange(savedDeck);
                    RecalculateEarnedCardsFromCurrentDeck();
                    return;
                }

                if (BattleCardCatalog.Instance == null)
                {
                    return;
                }
            }

            RebuildCurrentDeckFromParts();
        }

        /// <summary>
        /// Rebuilds the current deck from the latest base deck plus tracked earned cards.
        /// </summary>
        private void RebuildCurrentDeckFromParts()
        {
            currentBattleDeck.Clear();
            currentBattleDeck.AddRange(baseBattleDeck);
            currentBattleDeck.AddRange(earnedBattleCards);
        }

        /// <summary>
        /// Rebuilds the current deck from base + earned and immediately persists the result.
        /// </summary>
        private void RebuildAndSaveCurrentDeckFromParts()
        {
            RebuildCurrentDeckFromParts();
            SaveCurrentDeck();
        }

        /// <summary>
        /// Appends a card to the current deck and optionally tracks it as earned before saving.
        /// </summary>
        private void AddCardToCurrentDeck(BattleCardData data, bool trackAsEarned)
        {
            currentBattleDeck.Add(data);
            if (trackAsEarned)
            {
                earnedBattleCards.Add(data);
            }

            SaveCurrentDeck();
        }

        /// <summary>
        /// Writes the current deck order into PlayerPrefs.
        /// </summary>
        private void SaveCurrentDeck()
        {
            BattleDeckPersistenceService.SaveDeck(currentBattleDeck);
        }

        /// <summary>
        /// Re-derives earnedBattleCards by subtracting the base deck multiset from the current deck.
        /// This keeps inspector/debug state aligned after loading a saved current deck.
        /// </summary>
        private void RecalculateEarnedCardsFromCurrentDeck()
        {
            if (currentBattleDeck.Count == 0)
            {
                return;
            }

            earnedBattleCards.Clear();

            Dictionary<int, int> baseCounts = new();
            foreach (BattleCardData card in baseBattleDeck)
            {
                if (card == null)
                {
                    continue;
                }

                baseCounts.TryGetValue(card.CardID, out int count);
                baseCounts[card.CardID] = count + 1;
            }

            foreach (BattleCardData card in currentBattleDeck)
            {
                if (card == null)
                {
                    continue;
                }

                if (baseCounts.TryGetValue(card.CardID, out int count) && count > 0)
                {
                    baseCounts[card.CardID] = count - 1;
                    continue;
                }

                earnedBattleCards.Add(card);
            }
        }

        /// <summary>
        /// Counts only cards that consume the normal deck cap.
        /// </summary>
        private static int CountLimitedCards(IEnumerable<BattleCardData> source)
        {
            int count = 0;
            if (source == null)
            {
                return count;
            }

            foreach (BattleCardData card in source)
            {
                if (card != null && !ShouldIgnoreDeckLimit(card))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Returns true when the card should be excluded from the normal 30-card deck limit.
        /// </summary>
        private static bool ShouldIgnoreDeckLimit(BattleCardData data)
        {
            return data != null
                && (data.IgnoresDeckLimit
                || data.CardType == BattleCardType.Potion
                || data.CardType == BattleCardType.Trap);
        }

        /// <summary>
        /// Adopts the scene-authored base deck when a runtime-created collection instance already exists.
        /// This prevents an empty DontDestroyOnLoad collection from discarding the real scene default deck.
        /// </summary>
        private void TryAdoptSceneBaseDeck(IReadOnlyList<BattleCardData> sceneBaseDeck)
        {
            if (sceneBaseDeck == null || sceneBaseDeck.Count == 0 || baseBattleDeck.Count > 0)
            {
                return;
            }

            baseBattleDeck.AddRange(sceneBaseDeck);
            Debug.Log($"[BattleDeckCollection] Adopted scene base deck from duplicate instance: baseDeck={baseBattleDeck.Count}");
        }

        /// <summary>
        /// Returns true when the loaded current deck is obviously invalid for the configured base deck.
        /// This repairs saves that were produced while the base deck was accidentally empty.
        /// </summary>
        private bool ShouldRecoverCurrentDeckFromBase()
        {
            return baseBattleDeck.Count > 0
                && currentBattleDeck.Count < baseBattleDeck.Count
                && BattleCardCatalog.Instance != null;
        }
    }
}
