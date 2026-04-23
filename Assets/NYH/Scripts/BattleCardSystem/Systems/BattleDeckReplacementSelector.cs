namespace NYH.BattleCardSystem
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /*
     * BattleDeckReplacementSelector
     *
     * 역할:
     * - 교체 후보 BattleCardData를 기존 CardListUI에서 볼 수 있는 Card 미리보기로 바꿉니다.
     * - 유저가 선택한 preview Card를 다시 원래 BattleCardData로 되돌립니다.
     *
     * 하지 않는 일:
     * - 실제 덱 추가/삭제 규칙은 BattleDeckCollection이 담당합니다.
     */
    internal sealed class BattleDeckReplacementSelector
    {
        // 교체 후보를 CardListUI에 띄우고, 유저가 선택한 BattleCardData를 콜백으로 돌려줍니다.
        public IEnumerator SelectReplacement(
            IReadOnlyList<BattleCardData> candidates,
            Action<BattleCardData> onSelected)
        {
            if (CardListUI.Instance == null)
            {
                Debug.LogWarning("[BattleDeckReplacementSelector] CardListUI is missing.");
                onSelected?.Invoke(null);
                yield break;
            }

            List<Card> previewCards = new();
            Dictionary<Card, BattleCardData> previewMap = new();
            if (candidates != null)
            {
                // CardListUI는 일반 Card를 표시하므로 전투 카드를 미리보기 Card로 변환합니다.
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
            // 기존 ShowDeck에서 쓰던 리스트 UI를 선택 모드로 열어 교체할 카드를 고릅니다.
            CardListUI.Instance.Show(
                previewCards,
                "교체할 전투 카드 선택",
                selectedCard =>
                {
                    previewMap.TryGetValue(selectedCard, out selectedData);
                    isChosen = true;
                });

            yield return new WaitUntil(() => isChosen);
            onSelected?.Invoke(selectedData);
        }
    }
}
