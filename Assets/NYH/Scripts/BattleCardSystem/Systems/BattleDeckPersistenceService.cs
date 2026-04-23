namespace NYH.BattleCardSystem
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    internal static class BattleDeckPersistenceService
    {
        private const string PlayerPrefsKey = "NYH.Battle.CurrentBattleDeck";

        public static bool HasSavedDeck()
        {
            return PlayerPrefs.HasKey(PlayerPrefsKey);
        }

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
