namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class CardRuntimeStateEntry
    {
        public int CardId;
        public int CurrentCost;
        public CardData SourceData;
    }

    [System.Serializable]
    public class CardPileRuntimeState
    {
        public List<CardRuntimeStateEntry> DrawPile = new();        // 덱 내
        public List<CardRuntimeStateEntry> Hand = new();            // 내 패손
        public List<CardRuntimeStateEntry> DiscardPile = new();     // 덤무
        public List<CardRuntimeStateEntry> ExtinctionPile = new();  // 소멸 미더
    }

    public class CardPileState : CardPileStateBase<Card>
    {
        private readonly List<Card> extinctionPile = new();

        public int ExtinctionPileCount => extinctionPile.Count;
        //난 남윤호다
        public void Setup(List<CardData> initialDeck)
        {
            ClearMainPiles(); //이건 뭐죠
            extinctionPile.Clear(); //이건 뭐죠

            foreach (var data in initialDeck)
            {
                if (data != null)
                {
                    drawPile.Add(new Card(data));
                }
            }

            drawPile.Shuffle();
        }

        public Card GetRandomHandCard()
        {
            if (hand.Count == 0)
            {
                return null;
            }

            return hand[Random.Range(0, hand.Count)];
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
            ClearMainPiles();
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
                    SourceData = card.data,
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

                CardData data = entry.SourceData != null
                    ? entry.SourceData
                    : resolver(entry.CardId);
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
