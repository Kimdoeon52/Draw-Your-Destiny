namespace NYH.BattleCardSystem
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    internal sealed class BattleDeckReplacementSelector
    {
        public IEnumerator SelectReplacement(
            IReadOnlyList<BattleCardData> candidates,
            Action<BattleCardData> onSelected)
        {
            if (CardSelectionUI.Instance == null)
            {
                Debug.LogWarning("[BattleDeckReplacementSelector] CardSelectionUI is missing.");
                onSelected?.Invoke(null);
                yield break;
            }

            List<Card> previewCards = new();
            Dictionary<Card, BattleCardData> previewMap = new();
            if (candidates != null)
            {
                foreach (BattleCardData candidate in candidates)
                {
                    Card previewCard = BattleCardViewAdapter.CreatePreviewCard(candidate);
                    if (previewCard == null)
                    {
                        continue;
                    }

                    previewCards.Add(previewCard);
                    previewMap[previewCard] = candidate;
                }
            }

            if (previewCards.Count == 0)
            {
                Debug.LogWarning("[BattleDeckReplacementSelector] No replacement candidates.");
                onSelected?.Invoke(null);
                yield break;
            }

            BattleCardData selectedData = null;
            bool isChosen = false;
            CardSelectionUI.Instance.Show(previewCards, selectedCard =>
            {
                previewMap.TryGetValue(selectedCard, out selectedData);
                isChosen = true;
            });

            yield return new WaitUntil(() => isChosen);
            onSelected?.Invoke(selectedData);
        }
    }
}
