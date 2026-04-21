namespace NYH.BattleCardSystem
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using DG.Tweening;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /// <summary>
    /// 멀리건 선택 상태와 확정 애니메이션 흐름을 담당합니다.
    /// 손패 카드뷰 생성은 BattleHandPresenter에 맡기고, 턴 전환은 콜백으로만 요청합니다.
    /// </summary>
    internal sealed class BattleMulliganController
    {
        private readonly HashSet<BattleCard> selectedCards = new();

        public bool IsResolving { get; private set; }
        public bool HasSelection => selectedCards.Count > 0;

        public void ClearSelection()
        {
            selectedCards.Clear();
        }

        public void ToggleSelection(BattleCard battleCard, bool isMulliganPhase, BattleHandPresenter handPresenter)
        {
            if (battleCard == null || !isMulliganPhase || IsResolving)
            {
                return;
            }

            if (selectedCards.Contains(battleCard))
            {
                selectedCards.Remove(battleCard);
            }
            else
            {
                selectedCards.Add(battleCard);
            }

            if (handPresenter != null
                && handPresenter.TryGetMulliganCardView(battleCard, out CardView cardView)
                && cardView != null)
            {
                cardView.SetMulliganMarked(selectedCards.Contains(battleCard));
            }
        }

        public IEnumerator ResolveRoutine(
            BattleManager battleManager,
            HandView handView,
            BattleHandPresenter handPresenter,
            float mulliganCenterY,
            Action refreshHud,
            Action onResolved)
        {
            if (battleManager == null || handView == null || handPresenter == null)
            {
                yield break;
            }

            IsResolving = true;
            refreshHud?.Invoke();

            List<BattleCard> selectedSnapshot = new(selectedCards);
            BattleMulliganResult mulliganResult = battleManager.ConfirmMulligan(selectedSnapshot);
            if (mulliganResult == null)
            {
                IsResolving = false;
                refreshHud?.Invoke();
                yield break;
            }

            List<CardView> returningViews = CollectReturningViews(mulliganResult, handPresenter);
            AnimateReturnedCards(returningViews, handView);

            if (returningViews.Count > 0)
            {
                yield return new WaitForSeconds(0.22f);
            }

            DestroyReturnedCards(returningViews);
            handPresenter.RemoveMulliganCards(selectedSnapshot);
            AddRedrawnCards(mulliganResult, handPresenter);

            yield return handView.LayoutCardsInCenter(0.15f, mulliganCenterY);

            ConfigureResolvedCardsForBattlePlay(mulliganResult, handView, handPresenter);

            yield return new WaitForSeconds(0.1f);
            yield return handView.UpdateCardPositions(0.25f);

            selectedCards.Clear();
            handPresenter.ClearMulliganViews();
            IsResolving = false;
            onResolved?.Invoke();
            refreshHud?.Invoke();
        }

        private static List<CardView> CollectReturningViews(
            BattleMulliganResult mulliganResult,
            BattleHandPresenter handPresenter)
        {
            List<CardView> returningViews = new();
            foreach (BattleCard card in mulliganResult.ReturnedCards)
            {
                if (card != null
                    && handPresenter.TryGetMulliganCardView(card, out CardView view)
                    && view != null)
                {
                    returningViews.Add(view);
                }
            }

            return returningViews;
        }

        private static void AnimateReturnedCards(List<CardView> returningViews, HandView handView)
        {
            foreach (CardView returningView in returningViews)
            {
                handView.RemoveCard(returningView.Card);
                returningView.SetMulliganMarked(false);
                returningView.AllowHoverPreview = false;
                returningView.transform.DOKill();
                returningView.transform
                    .DOLocalMove(returningView.transform.localPosition + new Vector3(0f, 140f, 0f), 0.2f)
                    .SetEase(Ease.InBack);
                returningView.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
            }
        }

        private static void DestroyReturnedCards(List<CardView> returningViews)
        {
            foreach (CardView returningView in returningViews)
            {
                if (returningView != null)
                {
                    UnityEngine.Object.Destroy(returningView.gameObject);
                }
            }
        }

        private static void AddRedrawnCards(
            BattleMulliganResult mulliganResult,
            BattleHandPresenter handPresenter)
        {
            foreach (BattleCard redrawnCard in mulliganResult.RedrawnCards)
            {
                handPresenter.AddRedrawnMulliganCardImmediate(redrawnCard);
            }
        }

        private static void ConfigureResolvedCardsForBattlePlay(
            BattleMulliganResult mulliganResult,
            HandView handView,
            BattleHandPresenter handPresenter)
        {
            foreach (BattleCard keptCard in mulliganResult.KeptCards)
            {
                if (keptCard != null
                    && handPresenter.TryGetMulliganCardView(keptCard, out CardView keptView)
                    && keptView != null)
                {
                    handPresenter.ConfigureCardViewForBattlePlay(keptCard, keptView);
                }
            }

            for (int i = 0; i < mulliganResult.RedrawnCards.Count && i < handView.Cards.Count; i++)
            {
                BattleCard redrawnCard = mulliganResult.RedrawnCards[i];
                if (redrawnCard == null)
                {
                    continue;
                }

                CardView cardView = handView.Cards[handView.Cards.Count - mulliganResult.RedrawnCards.Count + i];
                if (cardView != null)
                {
                    handPresenter.ConfigureCardViewForBattlePlay(redrawnCard, cardView);
                }
            }
        }
    }
}
