namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using System.Linq;
    using NYH.CoreCardSystem;
    using UnityEngine;

    public enum BattleDeckAddResult
    {
        Added,
        Replaced,
        NeedsReplacement,
        Invalid,
    }

    public class BattleCardPileState : CardPileStateBase<BattleCard>
    {
        // 덱에 포함된 카드 중에서 덱 제한(MaxDeckSize)에 포함되는 카드의 최대 개수입니다.
        public const int MaxDeckSize = 30; 

        private readonly List<BattleCard> exhaustedPile = new();

        public int ExhaustedPileCount => exhaustedPile.Count;
        public int LimitedDeckCount => CountLimitedCards(drawPile) + CountLimitedCards(hand) + CountLimitedCards(discardPile);

        public void Setup(IEnumerable<BattleCardData> deckSources)
        {
            ClearMainPiles();
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

        public BattleMulliganResult MulliganSelectedCards(IReadOnlyList<BattleCard> selectedCards)
        {
            BattleMulliganResult result = new();
            if (selectedCards == null || selectedCards.Count == 0)
            {
                result.KeptCards.AddRange(hand);
                return result;
            }

            HashSet<BattleCard> selectedSet = new(selectedCards.Where(card => card != null));
            if (selectedSet.Count == 0)
            {
                result.KeptCards.AddRange(hand);
                return result;
            }

            for (int i = hand.Count - 1; i >= 0; i--)
            {
                BattleCard card = hand[i];
                if (card == null)
                {
                    continue;
                }

                if (selectedSet.Contains(card))
                {
                    hand.RemoveAt(i);
                    result.ReturnedCards.Insert(0, card);
                }
            }

            result.KeptCards.AddRange(hand);

            if (result.ReturnedCards.Count == 0)
            {
                return result;
            }

            drawPile.AddRange(result.ReturnedCards);
            drawPile.Shuffle();
            result.RedrawnCards.AddRange(DrawCards(result.ReturnedCards.Count));
            return result;
        }

        public bool ContainsInHand(BattleCard card)
        {
            return card != null && hand.Contains(card);
        }

        public void SendToDiscard(BattleCard card)
        {
            AddToDiscard(card);
            RefillDeckFromDiscardIfNeeded();
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
            RefillDeckFromDiscardIfNeeded();
        }

        public List<BattleCard> GetLimitedDeckCards()
        {
            List<BattleCard> result = new();
            CollectLimitedCards(drawPile, result);
            CollectLimitedCards(hand, result);
            CollectLimitedCards(discardPile, result);
            return result;
        }

        public override void RefillDeckFromDiscard()
        {
            if (discardPile.Count == 0)
            {
                return;
            }

            base.RefillDeckFromDiscard();
        }

        private void RefillDeckFromDiscardIfNeeded()
        {
            if (drawPile.Count > 0 || discardPile.Count == 0)
            {
                return;
            }

            RefillDeckFromDiscard();
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
