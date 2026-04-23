namespace NYH.BattleCardSystem
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /*
     * BattleDeckPersistenceService
     *
     * 역할:
     * - 현재 전투덱을 PlayerPrefs에 저장하고 다시 불러옵니다.
     * - BattleCardData 자체가 아니라 CardID 목록만 저장합니다.
     * - ID를 BattleCardData로 되돌리는 작업은 BattleCardCatalog를 사용합니다.
     */
    internal static class BattleDeckPersistenceService
    {
        private const string PlayerPrefsKey = "NYH.Battle.CurrentBattleDeck";

        // 저장된 전투덱 데이터가 있는지 확인합니다.
        public static bool HasSavedDeck()
        {
            return PlayerPrefs.HasKey(PlayerPrefsKey);
        }

        // 전투덱을 카드 ID 목록으로 변환해 PlayerPrefs에 저장합니다.
        // 같은 카드가 여러 장 있을 수 있으므로 ID를 중복 포함한 순서 그대로 저장합니다.
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

        // 저장된 카드 ID 목록을 BattleCardData 목록으로 복원합니다.
        // 카탈로그가 아직 없거나 저장값이 비어 있으면 false를 반환합니다.
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
        // JsonUtility 직렬화를 위한 단순 저장 데이터입니다.
        private sealed class BattleDeckSaveData
        {
            public List<int> CardIds = new();
        }
    }
}
