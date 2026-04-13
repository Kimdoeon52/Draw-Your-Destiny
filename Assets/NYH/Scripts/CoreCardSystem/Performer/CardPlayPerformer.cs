namespace NYH.CoreCardSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using DG.Tweening;
    using UnityEngine;

    /*
     * CardPlayPerformer
     *
     * 카드 사용, 드로우, 버리기, 소멸, 손 복귀 등 카드 흐름 처리
     *
     * 역할:
     * 
     * - "카드를 실제로 쓴다"는 규칙과 그에 따른 더미 이동/연출을 담당합니다.
     * - 드로우, 플레이, 랜덤 버리기, 전체 버리기, 소멸, 손으로 복귀 같은
     *   카드 흐름 중심 로직을 모아 둔 performer입니다.
     *
     * 여기에 넣는 것:
     * - DrawCardsGA, PlayCardGA, DiscardRandomGA, DiscardAllCardsGA, ExtinctionCardGA, ReturnToHandGA 처리
     * - 손패에서 제거, 무덤/소멸 더미 이동
     * - 카드 뷰 생성 및 DOTween 애니메이션
     * - 카드 사용 후 PerformEffectGA를 연쇄 반응으로 추가하는 로직
     *
     * 여기에 넣지 않는 것:
     * - GameManager 자원 변경
     * - 카드 선택 UI
     * - 건물 설치 검증
     *
     * 사용하는 법:
     * - CardSystem.Perform()에서 카드 흐름 계열 action인지 확인한 뒤 이 performer로 위임합니다.
     * - 새 카드 사용 규칙(예: 사용 후 복사, 사용 후 추방 등)을 넣고 싶으면
     *   우선 이 파일에 들어갈 책임인지부터 판단하면 됩니다.
     */
    public class CardPlayPerformer
    {
        private readonly MonoBehaviour coroutineHost;
        private readonly CardPileState pileState;
        private readonly HandView handView;
        private readonly Transform drawPilePoint;
        private readonly Transform discardPilePoint;
        private readonly System.Action refreshPileCounts;

        public CardPlayPerformer(
            MonoBehaviour coroutineHost,
            CardPileState pileState,
            HandView handView,
            Transform drawPilePoint,
            Transform discardPilePoint,
            System.Action refreshPileCounts)
        {
            this.coroutineHost = coroutineHost;
            this.pileState = pileState;
            this.handView = handView;
            this.drawPilePoint = drawPilePoint;
            this.discardPilePoint = discardPilePoint;
            this.refreshPileCounts = refreshPileCounts;
        }

        public bool CanHandle(GameAction action)
        {
            return action is DrawCardsGA
                || action is PlayCardGA
                || action is DiscardAllCardsGA
                || action is DiscardRandomGA
                || action is ExtinctionCardGA
                || action is ReturnToHandGA;
        }

        public IEnumerator Perform(GameAction action)
        {
            if (action is DrawCardsGA drawCardsGA)
            {
                for (int i = 0; i < drawCardsGA.Amount; i++)
                {
                    if (pileState.DrawPileCount == 0)
                    {
                        RefillDeck();
                    }

                    if (pileState.HandCount >= 10)
                    {
                        break;
                    }

                    if (pileState.DrawPileCount > 0)
                    {
                        yield return DrawOneCard();
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }
            else if (action is PlayCardGA playCardGA)
            {
                yield return PlayCard(playCardGA);
            }
            else if (action is DiscardAllCardsGA discardAllCardsGA)
            {
                yield return DiscardAllCards(discardAllCardsGA);
            }
            else if (action is DiscardRandomGA discardRandomGA)
            {
                for (int i = 0; i < discardRandomGA.Amount; i++)
                {
                    Card targetCard = pileState.GetRandomHandCard();
                    if (targetCard == null)
                    {
                        break;
                    }

                    yield return DiscardCardByInstance(targetCard);
                }
            }
            else if (action is ExtinctionCardGA extinctionCardGA)
            {
                if (extinctionCardGA.IsExtinction && extinctionCardGA.TargetCard != null)
                {
                    yield return ExtinctionCardByInstance(extinctionCardGA.TargetCard);
                }
            }
            else if (action is ReturnToHandGA returnToHandGA)
            {
                yield return ReturnCardFromDiscardToHand(returnToHandGA.TargetCard);
            }
        }

        private IEnumerator DrawOneCard()
        {
            Card card = pileState.DrawRandomFromDeck();
            pileState.AddToHand(card);
            refreshPileCounts?.Invoke();

            CardView cardView = CardViewCreator.Instance.CreateCardView(card, drawPilePoint.position, drawPilePoint.rotation);
            if (cardView != null)
            {
                yield return handView.AddCard(cardView);
            }
        }

        private IEnumerator PlayCard(PlayCardGA playCardGA)
        {
            pileState.RemoveFromHand(playCardGA.Card);
            CardView cardView = handView.RemoveCard(playCardGA.Card);

            if (cardView == null)
            {
                CardView[] allViews = Object.FindObjectsByType<CardView>(FindObjectsSortMode.None);
                foreach (var cv in allViews)
                {
                    if (cv.Card == playCardGA.Card && !cv.IsHoverPreview)
                    {
                        cardView = cv;
                        break;
                    }
                }
            }

            if (handView != null)
            {
                yield return handView.UpdateCardPositions(0.15f);
            }

            bool isExtinction = false;
            bool isReturnToHand = false;
            bool hasBuildingPopulationBonus = false;

            if (playCardGA.Card?.Effects != null)
            {
                foreach (var effect in playCardGA.Card.Effects)
                {
                    if (effect is ExtinctionCardEffect)
                    {
                        isExtinction = true;
                        break;
                    }

                    if (effect is ReturnToHandEffect)
                    {
                        isReturnToHand = true;
                    }

                    if (effect is InstallBuildingEffect installBuildingEffect
                        && installBuildingEffect.buildingData != null
                        && installBuildingEffect.buildingData.populationCapBonus > 0)
                    {
                        hasBuildingPopulationBonus = true;
                    }
                }
            }

            if (isExtinction)
            {
                yield return ExtinctionCardAnimation(cardView);
            }
            else if (isReturnToHand)
            {
                pileState.AddToHand(playCardGA.Card);
                refreshPileCounts?.Invoke();
                yield return handView.AddCard(cardView);
            }
            else
            {
                yield return DiscardCardAnimation(cardView);
            }

            if (playCardGA.Card?.Effects != null)
            {
                for (int i = 0; i < playCardGA.Card.Effects.Count; i++)
                {
                    var effect = playCardGA.Card.Effects[i];
                    if (effect is InstallBuildingEffect)
                    {
                        continue;
                    }

                    if (hasBuildingPopulationBonus && effect is IncreasePopulationffect)
                    {
                        continue;
                    }

                    ActionSystem.Instance.AddReaction(new PerformEffectGA(playCardGA.Card, effect, i));
                }
            }
        }

        private void RefillDeck()
        {
            if (pileState.DiscardPileCount > 0)
            {
                Debug.Log($"<color=yellow>[CardSystem] 덱이 비어있어 무덤의 {pileState.DiscardPileCount}장을 다시 섞어 넣습니다!</color>");
                pileState.RefillDeckFromDiscard();
                refreshPileCounts?.Invoke();
            }
            else
            {
                Debug.LogWarning("[CardSystem] 덱과 무덤이 모두 비어있어 더 이상 뽑을 카드가 없습니다!");
            }
        }

        private IEnumerator DiscardCardAnimation(CardView cardView)
        {
            if (cardView == null)
            {
                yield break;
            }

            pileState.AddToDiscard(cardView.Card);
            refreshPileCounts?.Invoke();
            yield return CardViewAnimationUtility.AnimateDiscard(cardView, discardPilePoint);
        }

        private IEnumerator DiscardCardByInstance(Card targetCard)
        {
            if (targetCard == null)
            {
                yield break;
            }

            pileState.RemoveFromHand(targetCard);
            CardView cardView = handView.RemoveCard(targetCard);
            yield return DiscardCardAnimation(cardView);
        }

        private IEnumerator DiscardAllCards(DiscardAllCardsGA discardAllCardsGA)
        {
            List<Card> cardsToDiscard = pileState.ExtractAllHandCards();
            foreach (var card in cardsToDiscard)
            {
                CardView cardView = handView.RemoveCard(card);
                coroutineHost.StartCoroutine(DiscardCardAnimation(cardView));
                yield return new WaitForSeconds(0.05f);
            }
        }

        private IEnumerator ExtinctionCardAnimation(CardView cardView)
        {
            if (cardView == null)
            {
                yield break;
            }

            pileState.AddToExtinction(cardView.Card);
            refreshPileCounts?.Invoke();

            cardView.transform.DOKill();
            yield return cardView.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).WaitForCompletion();
            Object.Destroy(cardView.gameObject);
        }

        private IEnumerator ExtinctionCardByInstance(Card targetCard)
        {
            if (targetCard == null)
            {
                yield break;
            }

            pileState.RemoveFromHand(targetCard);
            CardView cardView = handView.RemoveCard(targetCard);
            yield return ExtinctionCardAnimation(cardView);
        }

        private IEnumerator ReturnCardFromDiscardToHand(Card targetCard)
        {
            if (targetCard == null || !pileState.ContainsInDiscard(targetCard))
            {
                yield break;
            }

            pileState.RemoveFromDiscard(targetCard);
            pileState.AddToHand(targetCard);
            refreshPileCounts?.Invoke();

            CardView cardView = CardViewCreator.Instance.CreateCardView(
                targetCard,
                discardPilePoint.position,
                discardPilePoint.rotation);

            if (cardView != null)
            {
                yield return handView.AddCard(cardView);
            }
        }
    }
}
