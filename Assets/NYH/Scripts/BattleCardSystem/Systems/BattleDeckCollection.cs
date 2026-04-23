namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /*
     * BattleDeckCollection
     *
     * Owns the persistent battle deck list used by battle setup.
     * Reward cards are added here, and full-deck replacement is handled here.
     */
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

        public IReadOnlyList<BattleCardData> CurrentBattleDeck
        {
            get
            {
                EnsureCurrentDeckInitialized();
                return currentBattleDeck;
            }
        }

        public int LimitedDeckCount
        {
            get
            {
                EnsureCurrentDeckInitialized();
                return CountLimitedCards(currentBattleDeck);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                Debug.LogWarning($"[BattleDeckCollection] Duplicate instance will be destroyed: scene={gameObject.scene.name}, object={name}");
                return;
            }

            DontDestroyOnLoad(gameObject);
            EnsureCurrentDeckInitialized();
            Debug.Log($"[BattleDeckCollection] Awake complete: scene={gameObject.scene.name}, object={name}, baseDeck={baseBattleDeck.Count}, earned={earnedBattleCards.Count}, current={currentBattleDeck.Count}");
        }

        public void ConfigureBaseDeck(IEnumerable<BattleCardData> source)
        {
            baseBattleDeck.Clear();
            if (source != null)
            {
                baseBattleDeck.AddRange(source);
            }

            if (!BattleDeckPersistenceService.HasSavedDeck())
            {
                RebuildCurrentDeckFromParts();
                SaveCurrentDeck();
            }

            Debug.Log($"[BattleDeckCollection] Base deck configured: baseDeck={baseBattleDeck.Count}, current={currentBattleDeck.Count}");
        }

        public void ResetRun()
        {
            earnedBattleCards.Clear();
            Debug.Log("[BattleDeckCollection] Run reset: earnedBattleCards cleared");
        }

        public List<BattleCardData> BuildBattleDeckSources()
        {
            EnsureCurrentDeckInitialized();
            return new List<BattleCardData>(currentBattleDeck);
        }

        public BattleDeckAddResult AddBattleRewardCard(BattleCardData data, BattleCardData replaceTarget = null)
        {
            if (replaceTarget != null)
            {
                return ReplaceCard(replaceTarget, data);
            }

            return AddRewardCard(data);
        }

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

        public bool CanAddWithoutReplacement(BattleCardData data)
        {
            if (data == null)
            {
                return false;
            }

            EnsureCurrentDeckInitialized();
            return ShouldIgnoreDeckLimit(data) || LimitedDeckCount < BattleCardPileState.MaxDeckSize;
        }

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
                    return;
                }

                if (BattleCardCatalog.Instance == null)
                {
                    return;
                }
            }

            RebuildCurrentDeckFromParts();
        }

        private void RebuildCurrentDeckFromParts()
        {
            currentBattleDeck.Clear();
            currentBattleDeck.AddRange(baseBattleDeck);
            currentBattleDeck.AddRange(earnedBattleCards);
        }

        private void AddCardToCurrentDeck(BattleCardData data, bool trackAsEarned)
        {
            currentBattleDeck.Add(data);
            if (trackAsEarned)
            {
                earnedBattleCards.Add(data);
            }

            SaveCurrentDeck();
        }

        private void SaveCurrentDeck()
        {
            BattleDeckPersistenceService.SaveDeck(currentBattleDeck);
        }

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

        private static bool ShouldIgnoreDeckLimit(BattleCardData data)
        {
            return data != null && (data.IgnoresDeckLimit || data.CardType == BattleCardType.Potion);
        }
    }
}
