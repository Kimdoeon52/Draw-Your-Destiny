namespace NYH.CoreCardSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /*
     * CardSelectionPerformer
     *
     * - 선택형 카드 및 선택 UI 처리
     *
     * 역할:
     * - 카드 선택/발견 UI 흐름을 전담합니다.
     * - "후보 카드 보여주기 -> 유저가 하나 선택 -> 덱 또는 손패에 반영" 흐름을 처리합니다.
     *
     * 여기에 넣는 것:
     * - CardCatalog에서 후보 카드 가져오기
     * - CardSelectionUI 띄우기
     * - 선택 결과를 drawPile/hand에 반영하기
     * - 선택 카드 관련 UI 텍스트 갱신
     *
     * 여기에 넣지 않는 것:
     * - 일반 카드 사용 규칙
     * - 버리기/소멸 연출
     * - GameManager 자원 변경
     *
     * 사용하는 법:
     * - OfferRandomCatalogCardToDeck(): 카탈로그에서 랜덤 후보를 띄우고 1장을 덱에 추가
     * - ChooseCardPerformer(): 현재 덱 상단 일부를 보여주고 1장을 골라 손패로 이동
     * - 선택형 카드/발견형 카드가 늘어나면 이 performer 안에서 확장하면 됩니다.
     */
    public class CardSelectionPerformer
    {
        private readonly CardPileState pileState;
        private readonly HandView handView;
        private readonly Text deckCountText;
        private readonly Text discardCountText;
        private readonly System.Action refreshPileCounts;

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

        public IEnumerator OfferRandomCatalogCardToDeck(int amount)
        {
            if (CardCatalog.Instance == null)
            {
                Debug.LogWarning("[CardSystem] CardCatalog가 없어 카드 선택을 건너뜁니다.");
                yield break;
            }

            if (CardSelectionUI.Instance == null)
            {
                Debug.LogWarning("[CardSystem] CardSelectionUI가 없어 카드 선택을 건너뜁니다.");
                yield break;
            }

            List<CardData> candidateData = CardCatalog.Instance.GetRandom(amount);
            if (candidateData == null || candidateData.Count == 0)
            {
                Debug.LogWarning("[CardSystem] 선택할 카드 후보가 없습니다.");
                yield break;
            }

            List<Card> previewCards = new();
            foreach (var data in candidateData)
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
                Debug.LogError("[CardSystem] CardSelectionUI가 없습니다!");
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

        private void UpdateTexts()
        {
            if (deckCountText != null)
            {
                deckCountText.text = $"{pileState.DrawPileCount}장";
            }

            if (discardCountText != null)
            {
                discardCountText.text = $"{pileState.DiscardPileCount}장";
            }
        }
    }
}
