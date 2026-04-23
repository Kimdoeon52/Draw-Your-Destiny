namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using System.Linq;
    using NYH.CoreCardSystem;
    using UnityEngine;

    // 전투 보상 카드를 덱에 반영하려고 시도했을 때의 결과입니다.
    public enum BattleDeckAddResult
    {
        Added,
        Replaced,
        NeedsReplacement,
        Invalid,
    }

    /*
     * BattleCardPileState
     *
     * 역할:
     * - 전투 중 draw/hand/discard/exhausted 더미의 실제 런타임 상태를 관리합니다.
     * - 드로우, 멀리건, 턴 종료 버림, 보상 카드 fallback 추가를 처리합니다.
     *
     * 담당하지 않는 것:
     * - 영구 전투덱 저장은 BattleDeckCollection이 담당합니다.
     * - 카드 사용 가능 여부와 비용 지불은 BattleCardSystem/CostService가 담당합니다.
     */
    public class BattleCardPileState : CardPileStateBase<BattleCard>
    {
        // 덱에 포함된 카드 중에서 덱 제한(MaxDeckSize)에 포함되는 카드의 최대 개수입니다.
        public const int MaxDeckSize = 30; 

        private readonly List<BattleCard> exhaustedPile = new();

        public int ExhaustedPileCount => exhaustedPile.Count;
        public int LimitedDeckCount => CountLimitedCards(drawPile) + CountLimitedCards(hand) + CountLimitedCards(discardPile);

        // BattleCardData 목록을 런타임 BattleCard로 바꿔 draw pile을 새로 구성합니다.
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

        // 영구 덱 저장소가 없을 때 현재 전투 더미에 보상 카드를 직접 추가하거나 교체합니다.
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

        // 포션처럼 덱 제한을 무시하는 카드를 draw pile에 즉시 추가합니다.
        public void AddPotionCard(BattleCardData data)
        {
            if (data == null)
            {
                return;
            }

            drawPile.Add(new BattleCard(data));
            drawPile.Shuffle();
        }

        // 지정 수만큼 draw pile에서 카드를 뽑고, 비었으면 discard pile을 섞어 보충합니다.
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

        // 멀리건 전체 교체용으로 손패 전체를 draw pile에 되돌리고 섞습니다.
        public void ReturnHandToDrawPileAndShuffle()
        {
            drawPile.AddRange(hand);
            hand.Clear();
            drawPile.Shuffle();
        }

        // 선택된 카드만 덱으로 되돌리고 같은 수만큼 다시 뽑아 멀리건 결과를 만듭니다.
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

        // 특정 전투 카드가 현재 손패에 있는지 확인합니다.
        public bool ContainsInHand(BattleCard card)
        {
            return card != null && hand.Contains(card);
        }

        // 카드를 discard pile로 보내고 필요하면 draw pile을 보충합니다.
        public void SendToDiscard(BattleCard card)
        {
            AddToDiscard(card);
            RefillDeckFromDiscardIfNeeded();
        }

        // 소모/소멸 카드처럼 이번 전투에서 더 이상 쓰지 않을 카드를 exhausted pile로 보냅니다.
        public void Exhaust(BattleCard card)
        {
            if (card != null)
            {
                exhaustedPile.Add(card);
            }
        }

        // 턴 종료 시 손패 전체를 discard pile로 이동합니다.
        public void DiscardHand()
        {
            discardPile.AddRange(hand);
            hand.Clear();
            RefillDeckFromDiscardIfNeeded();
        }

        // 덱 제한에 포함되는 현재 playable pile의 카드들을 모아 반환합니다.
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
