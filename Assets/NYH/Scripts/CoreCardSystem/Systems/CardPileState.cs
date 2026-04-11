namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class CardRuntimeStateEntry
    {
        public int CardId;
        public int CurrentCost;
    }

    [System.Serializable]
    public class CardPileRuntimeState
    {
        public List<CardRuntimeStateEntry> DrawPile = new();
        public List<CardRuntimeStateEntry> Hand = new();
        public List<CardRuntimeStateEntry> DiscardPile = new();
        public List<CardRuntimeStateEntry> ExtinctionPile = new();
    }

    /*
     * CardPileState
     *
     * 역할:
     * - draw/hand/discard/extinction pile 상태 관리
     * - 카드 더미 데이터만 전담합니다.
     * - 덱(drawPile), 손패(hand), 무덤(discardPile), 소멸(extinctionPile)을 보관합니다.
     *
     * 여기에 넣는 것:
     * - 카드가 어느 더미에 들어가는지/빠지는지 같은 "상태 관리" 로직
     * - 드로우, 셔플, 전체 손패 꺼내기, 특정 타입 손패 수 세기 같은 순수 데이터 처리
     *
     * 여기에 넣지 않는 것:
     * - DOTween 애니메이션
     * - GameManager 자원 변경
     * - 타일맵/건물 설치
     * - 카드 UI 생성
     *
     * 사용하는 법:
     * - CardSystem이 1개 생성해서 들고 있습니다.
     * - 다른 클래스(CardPlayPerformer, CardSelectionPerformer, CardResourcePerformer)가
     *   이 객체를 통해 카드 더미 상태를 읽고 수정합니다.
     */


    /// <summary>
    /// 카드 더미 상태만 담당합니다.
    /// 덱, 손패, 무덤, 소멸 더미를 중앙에서 관리합니다.
     /// </summary>
    public class CardPileState
    {
        private readonly List<Card> drawPile = new();
        private readonly List<Card> hand = new();
        private readonly List<Card> discardPile = new();
        private readonly List<Card> extinctionPile = new();

        public int DrawPileCount => drawPile.Count;
        public int HandCount => hand.Count;
        public int DiscardPileCount => discardPile.Count;
        public int ExtinctionPileCount => extinctionPile.Count;

        public IReadOnlyList<Card> Hand => hand;

        public void Setup(List<CardData> initialDeck)
        {
            drawPile.Clear();
            hand.Clear();
            discardPile.Clear();
            extinctionPile.Clear();

            foreach (var data in initialDeck)
            {
                if (data != null)
                {
                    drawPile.Add(new Card(data));
                }
            }

            drawPile.Shuffle();
        }

        public void AddToDrawPile(Card card)
        {
            if (card != null)
            {
                drawPile.Add(card);
            }
        }

        public void ShuffleDrawPile()
        {
            drawPile.Shuffle();
        }

        public bool RemoveFromDrawPile(Card card)
        {
            return card != null && drawPile.Remove(card);
        }

        public Card DrawRandomFromDeck()
        {
            return drawPile.Draw();
        }

        public void RefillDeckFromDiscard()
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            drawPile.Shuffle();
        }

        public void AddToHand(Card card)
        {
            if (card != null)
            {
                hand.Add(card);
            }
        }

        public bool RemoveFromHand(Card card)
        {
            return card != null && hand.Remove(card);
        }

        public Card GetRandomHandCard()
        {
            if (hand.Count == 0)
            {
                return null;
            }

            return hand[Random.Range(0, hand.Count)];
        }

        public List<Card> ExtractAllHandCards()
        {
            List<Card> cards = new(hand);
            hand.Clear();
            return cards;
        }

        public void AddToDiscard(Card card)
        {
            if (card != null)
            {
                discardPile.Add(card);
            }
        }

        public bool RemoveFromDiscard(Card card)
        {
            return card != null && discardPile.Remove(card);
        }

        public bool ContainsInDiscard(Card card)
        {
            return card != null && discardPile.Contains(card);
        }

        public void AddToExtinction(Card card)
        {
            if (card != null)
            {
                extinctionPile.Add(card);
            }
        }

        public List<Card> PeekDrawPile(int amount)
        {
            List<Card> result = new();
            int actualAmount = Mathf.Min(amount, drawPile.Count);
            for (int i = 0; i < actualAmount; i++)
            {
                result.Add(drawPile[i]);
            }

            return result;
        }

        public int CountHandByType(CardType type)
        {
            int count = 0;
            foreach (var card in hand)
            {
                if (card != null && card._CardType == type)
                {
                    count++;
                }
            }

            return count;
        }

        public List<Card> GetShuffledDrawPileCopy()
        {
            List<Card> copy = new(drawPile);
            copy.Shuffle();
            return copy;
        }

        public List<Card> GetShuffledDiscardPileCopy()
        {
            List<Card> copy = new(discardPile);
            copy.Shuffle();
            return copy;
        }

        public CardPileRuntimeState ExportRuntimeState()
        {
            return new CardPileRuntimeState
            {
                DrawPile = ExportPile(drawPile),
                Hand = ExportPile(hand),
                DiscardPile = ExportPile(discardPile),
                ExtinctionPile = ExportPile(extinctionPile),
            };
        }

        public void ImportRuntimeState(CardPileRuntimeState state, System.Func<int, CardData> resolver)
        {
            drawPile.Clear();
            hand.Clear();
            discardPile.Clear();
            extinctionPile.Clear();

            if (state == null || resolver == null)
            {
                return;
            }

            ImportPile(state.DrawPile, drawPile, resolver);
            ImportPile(state.Hand, hand, resolver);
            ImportPile(state.DiscardPile, discardPile, resolver);
            ImportPile(state.ExtinctionPile, extinctionPile, resolver);
        }

        private static List<CardRuntimeStateEntry> ExportPile(List<Card> source)
        {
            List<CardRuntimeStateEntry> result = new();
            foreach (var card in source)
            {
                if (card?.data == null)
                {
                    continue;
                }

                result.Add(new CardRuntimeStateEntry
                {
                    CardId = card.CardID,
                    CurrentCost = card.Cost,
                });
            }

            return result;
        }

        private static void ImportPile(
            List<CardRuntimeStateEntry> source,
            List<Card> destination,
            System.Func<int, CardData> resolver)
        {
            if (source == null)
            {
                return;
            }

            foreach (var entry in source)
            {
                if (entry == null)
                {
                    continue;
                }

                CardData data = resolver(entry.CardId);
                if (data == null)
                {
                    continue;
                }

                Card card = new(data)
                {
                    Cost = entry.CurrentCost,
                };
                destination.Add(card);
            }
        }
    }
}
