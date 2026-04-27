namespace NYH.BattleCardSystem
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Persists the current battle deck to PlayerPrefs.
    /// Only CardIDs are saved, so loading depends on <see cref="BattleCardCatalog"/>.
    /// </summary>
    public static class BattleDeckPersistenceService
    {
        private const string PlayerPrefsKey = "NYH.Battle.CurrentBattleDeck";

        /// <summary>
        /// Returns true when a saved current battle deck exists.
        /// </summary>
        public static bool HasSavedDeck()
        {
            return PlayerPrefs.HasKey(PlayerPrefsKey);
        }

        /// <summary>
        /// Saves the deck as an ordered list of CardIDs.
        /// Duplicate IDs are preserved because duplicate cards are valid deck contents.
        /// </summary>
        public static void SaveDeck(IEnumerable<BattleCardData> cards)
        {
            BattleDeckSaveData saveData = new();
            if (cards != null)
            {
                foreach (BattleCardData card in cards)
                {
                    if (card != null)
                    {
                        saveData.CardIds.Add(card.CardID);
                    }
                }
            }

            PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(saveData));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Deletes the saved current battle deck.
        /// This is only used from explicit reset or test flows.
        /// </summary>
        public static void ClearSavedDeck()
        {
            if (!HasSavedDeck())
            {
                return;
            }

            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Attempts to rebuild a saved deck from CardIDs.
        /// Returns false if the save is missing, invalid, or the catalog is not ready yet.
        /// </summary>
        public static bool TryLoadDeck(out List<BattleCardData> cards)
        {
            cards = new List<BattleCardData>();
            if (!HasSavedDeck() || BattleCardCatalog.Instance == null)
            {
                return false;
            }

            string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            BattleDeckSaveData saveData;
            try
            {
                saveData = JsonUtility.FromJson<BattleDeckSaveData>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[BattleDeckPersistenceService] Failed to load saved battle deck: {exception.Message}");
                return false;
            }

            if (saveData?.CardIds == null || saveData.CardIds.Count == 0)
            {
                return false;
            }

            foreach (int cardId in saveData.CardIds)
            {
                BattleCardData card = BattleCardCatalog.Instance.GetById(cardId);
                if (card != null)
                {
                    cards.Add(card);
                }
                else
                {
                    Debug.LogWarning($"[BattleDeckPersistenceService] Saved battle card ID was not found: {cardId}");
                }
            }

            return cards.Count > 0;
        }

        [Serializable]
        private sealed class BattleDeckSaveData
        {
            public List<int> CardIds = new();
        }
    }
}
