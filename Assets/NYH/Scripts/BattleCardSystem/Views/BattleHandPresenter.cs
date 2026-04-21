namespace NYH.BattleCardSystem
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /// <summary>
    /// 전투 손패 CardView 생성과 카드뷰 바인딩을 담당합니다.
    /// 멀리건 선택 상태, 턴 흐름, 카드 실행 규칙은 담당하지 않습니다.
    /// </summary>
    internal sealed class BattleHandPresenter
    {
        private readonly HandView handView;
        private readonly CardViewCreator cardViewCreator;
        private readonly BattleUIController playOwner;
        private readonly Action<BattleCard> onMulliganCardClicked;
        private readonly Dictionary<BattleCard, CardView> mulliganCardViews = new();

        public BattleHandPresenter(
            HandView handView,
            CardViewCreator cardViewCreator,
            BattleUIController playOwner,
            Action<BattleCard> onMulliganCardClicked)
        {
            this.handView = handView;
            this.cardViewCreator = cardViewCreator;
            this.playOwner = playOwner;
            this.onMulliganCardClicked = onMulliganCardClicked;
        }

        public bool HasRequiredReferences => handView != null && cardViewCreator != null;

        public IEnumerator RebuildHandRoutine(
            IEnumerable<BattleCard> handCards,
            bool isMulliganPhase,
            Action refreshHud)
        {
            handView.ClearAllCardsImmediate();
            ClearMulliganViews();

            if (handCards == null)
            {
                refreshHud?.Invoke();
                yield break;
            }

            foreach (BattleCard battleCard in handCards)
            {
                if (battleCard == null)
                {
                    continue;
                }

                Card previewCard = BattleCardViewAdapter.CreatePreviewCard(battleCard);
                if (previewCard == null)
                {
                    continue;
                }

                CardView cardView = cardViewCreator.CreateCardView(previewCard, Vector3.zero, Quaternion.identity);
                if (cardView == null)
                {
                    continue;
                }

                if (isMulliganPhase)
                {
                    ConfigureCardViewForMulligan(battleCard, cardView);
                }
                else
                {
                    ConfigureCardViewForBattlePlay(battleCard, cardView);
                }

                yield return handView.AddCard(cardView);
            }

            refreshHud?.Invoke();
        }

        public void ConfigureCardViewForBattlePlay(BattleCard battleCard, CardView cardView)
        {
            if (battleCard == null || cardView == null)
            {
                return;
            }

            BattleMulliganCardHandler mulliganHandler = cardView.GetComponent<BattleMulliganCardHandler>();
            if (mulliganHandler != null)
            {
                UnityEngine.Object.Destroy(mulliganHandler);
            }

            cardView.UseBuiltInInteractions = true;
            cardView.AllowHoverPreview = true;
            cardView.SetMulliganMarked(false);

            BattleCardPlayHandler playHandler = cardView.GetComponent<BattleCardPlayHandler>();
            if (playHandler == null)
            {
                playHandler = cardView.gameObject.AddComponent<BattleCardPlayHandler>();
            }

            playHandler.Bind(battleCard, playOwner);
            cardView.RefreshPlayHandlerBinding();
        }

        public void ConfigureCardViewForMulligan(BattleCard battleCard, CardView cardView)
        {
            if (battleCard == null || cardView == null)
            {
                return;
            }

            BattleCardPlayHandler playHandler = cardView.GetComponent<BattleCardPlayHandler>();
            if (playHandler != null)
            {
                UnityEngine.Object.Destroy(playHandler);
            }

            cardView.ClearPlayHandlerBinding();
            cardView.UseBuiltInInteractions = false;
            cardView.AllowHoverPreview = true;
            cardView.SetMulliganMarked(false);

            BattleMulliganCardHandler handler = cardView.GetComponent<BattleMulliganCardHandler>();
            if (handler == null)
            {
                handler = cardView.gameObject.AddComponent<BattleMulliganCardHandler>();
            }

            handler.Bind(battleCard, onMulliganCardClicked);
            cardView.RefreshPlayHandlerBinding();
            mulliganCardViews[battleCard] = cardView;
        }

        public CardView AddRedrawnMulliganCardImmediate(BattleCard battleCard)
        {
            if (battleCard == null)
            {
                return null;
            }

            Card previewCard = BattleCardViewAdapter.CreatePreviewCard(battleCard);
            if (previewCard == null)
            {
                return null;
            }

            CardView cardView = cardViewCreator.CreateCardView(previewCard, Vector3.zero, Quaternion.identity);
            if (cardView == null)
            {
                return null;
            }

            cardView.UseBuiltInInteractions = false;
            cardView.AllowHoverPreview = false;
            cardView.SetMulliganMarked(false);
            handView.AddCardImmediate(cardView);
            return cardView;
        }

        public bool TryGetMulliganCardView(BattleCard battleCard, out CardView cardView)
        {
            return mulliganCardViews.TryGetValue(battleCard, out cardView);
        }

        public void RemoveMulliganCards(IEnumerable<BattleCard> cards)
        {
            if (cards == null)
            {
                return;
            }

            foreach (BattleCard card in cards)
            {
                if (card != null)
                {
                    mulliganCardViews.Remove(card);
                }
            }
        }

        public void ClearMulliganViews()
        {
            mulliganCardViews.Clear();
        }
    }
}
