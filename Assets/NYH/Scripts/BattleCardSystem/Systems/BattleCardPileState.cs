namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    public enum BattleDeckAddResult
    {
        Added,
        Replaced,
        NeedsReplacement,
        Invalid,
    }

    public class BattleCardPileState
    {
        public const int MaxDeckSize = 30;

        private readonly List<BattleCard> drawPile = new();
        private readonly List<BattleCard> hand = new();
        private readonly List<BattleCard> discardPile = new();
        private readonly List<BattleCard> exhaustedPile = new();

        public int DrawPileCount => drawPile.Count;
        public int HandCount => hand.Count;
        public int DiscardPileCount => discardPile.Count;
        public int ExhaustedPileCount => exhaustedPile.Count;
        public int LimitedDeckCount => CountLimitedCards(drawPile) + CountLimitedCards(hand) + CountLimitedCards(discardPile);

        public IReadOnlyList<BattleCard> Hand => hand;

        public void Setup(IEnumerable<BattleCardData> deckSources)
        {
            drawPile.Clear();
            hand.Clear();
            discardPile.Clear();
            exhaustedPile.Clear();

            if (deckSources == null)
            {
                return;
            }

            foreach (var data in deckSources)
            {
                if (data != null)
                {
                    drawPile.Add(new BattleCard(data));
                }
            }

            drawPile.Shuffle();
        }

        public BattleDeckAddResult AddRewardCard(BattleCardData data, BattleCard replaceTarget = null)
        {
            if (data == null)
            {
                return BattleDeckAddResult.Invalid;
            }

            BattleCard newCard = new(data);
            if (newCard.IgnoresDeckLimit)
            {
                drawPile.Add(newCard);
                drawPile.Shuffle();
                return BattleDeckAddResult.Added;
            }

            if (LimitedDeckCount < MaxDeckSize)
            {
                drawPile.Add(newCard);
                drawPile.Shuffle();
                return BattleDeckAddResult.Added;
            }

            if (replaceTarget == null)
            {
                return BattleDeckAddResult.NeedsReplacement;
            }

            if (!RemoveFromAnyPlayablePile(replaceTarget))
            {
                return BattleDeckAddResult.Invalid;
            }

            drawPile.Add(newCard);
            drawPile.Shuffle();
            return BattleDeckAddResult.Replaced;
        }

        public void AddPotionCard(BattleCardData data)
        {
            if (data == null)
            {
                return;
            }

            drawPile.Add(new BattleCard(data));
            drawPile.Shuffle();
        }

        public List<BattleCard> DrawCards(int amount)
        {
            List<BattleCard> drawn = new();
            for (int i = 0; i < amount; i++)
            {
                if (drawPile.Count == 0)
                {
                    RefillDeckFromDiscard();
                }

                if (drawPile.Count == 0)
                {
                    break;
                }

                BattleCard card = drawPile.Draw();
                hand.Add(card);
                drawn.Add(card);
            }

            return drawn;
        }

        public void ReturnHandToDrawPileAndShuffle()
        {
            drawPile.AddRange(hand);
            hand.Clear();
            drawPile.Shuffle();
        }

        public bool RemoveFromHand(BattleCard card)
        {
            return card != null && hand.Remove(card);
        }

        public bool ContainsInHand(BattleCard card)
        {
            return card != null && hand.Contains(card);
        }

        public void SendToDiscard(BattleCard card)
        {
            if (card != null)
            {
                discardPile.Add(card);
            }
        }

        public void Exhaust(BattleCard card)
        {
            if (card != null)
            {
                exhaustedPile.Add(card);
            }
        }

        public void DiscardHand()
        {
            discardPile.AddRange(hand);
            hand.Clear();
        }

        public List<BattleCard> GetLimitedDeckCards()
        {
            List<BattleCard> result = new();
            CollectLimitedCards(drawPile, result);
            CollectLimitedCards(hand, result);
            CollectLimitedCards(discardPile, result);
            return result;
        }

        private void RefillDeckFromDiscard()
        {
            if (discardPile.Count == 0)
            {
                return;
            }

            drawPile.AddRange(discardPile);
            discardPile.Clear();
            drawPile.Shuffle();
        }

        private bool RemoveFromAnyPlayablePile(BattleCard target)
        {
            return drawPile.Remove(target) || hand.Remove(target) || discardPile.Remove(target);
        }

        private static int CountLimitedCards(List<BattleCard> source)
        {
            int count = 0;
            foreach (var card in source)
            {
                if (card != null && !card.IgnoresDeckLimit)
                {
                    count++;
                }
            }

            return count;
        }

        private static void CollectLimitedCards(List<BattleCard> source, List<BattleCard> destination)
        {
            foreach (var card in source)
            {
                if (card != null && !card.IgnoresDeckLimit)
                {
                    destination.Add(card);
                }
            }
        }
    }
}
