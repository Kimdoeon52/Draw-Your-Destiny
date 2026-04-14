namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;

    public abstract class CardPileStateBase<TCard>
    {
        protected readonly List<TCard> drawPile = new();
        protected readonly List<TCard> hand = new();
        protected readonly List<TCard> discardPile = new();

        public int DrawPileCount => drawPile.Count;
        public int HandCount => hand.Count;
        public int DiscardPileCount => discardPile.Count;

        public IReadOnlyList<TCard> Hand => hand;

        public void AddToDrawPile(TCard card)
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

        public bool RemoveFromDrawPile(TCard card)
        {
            return card != null && drawPile.Remove(card);
        }

        public TCard DrawRandomFromDeck()
        {
            return drawPile.Draw();
        }

        public virtual void RefillDeckFromDiscard()
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            drawPile.Shuffle();
        }

        public void AddToHand(TCard card)
        {
            if (card != null)
            {
                hand.Add(card);
            }
        }

        public bool RemoveFromHand(TCard card)
        {
            return card != null && hand.Remove(card);
        }

        public List<TCard> ExtractAllHandCards()
        {
            List<TCard> cards = new(hand);
            hand.Clear();
            return cards;
        }

        public void AddToDiscard(TCard card)
        {
            if (card != null)
            {
                discardPile.Add(card);
            }
        }

        public bool RemoveFromDiscard(TCard card)
        {
            return card != null && discardPile.Remove(card);
        }

        public bool ContainsInDiscard(TCard card)
        {
            return card != null && discardPile.Contains(card);
        }

        public List<TCard> GetShuffledDrawPileCopy()
        {
            List<TCard> copy = new(drawPile);
            copy.Shuffle();
            return copy;
        }

        public List<TCard> GetShuffledDiscardPileCopy()
        {
            List<TCard> copy = new(discardPile);
            copy.Shuffle();
            return copy;
        }

        protected void ClearMainPiles()
        {
            drawPile.Clear();
            hand.Clear();
            discardPile.Clear();
        }
    }
}
