namespace NYH.CoreCardSystem
{
    using System;
    using System.Collections.Generic;
    using DG.Tweening;
    using NYH.BattleCardSystem;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    public class CardSelectionUI : MonoBehaviour
    {
        private const float RewardCardWidth = 244f;
        private const float RewardCardHeight = 380f;
        private const float RewardBundleSpacing = 20f;
        private const float RewardBundlePadding = 20f;
        private const float RewardBundlePeekOffset = 56f;
        private const float RewardBundleSwapDuration = 0.2f;
        private static readonly Vector3 FrontCardScale = Vector3.one;
        private static readonly Vector3 BackCardScale = new(0.94f, 0.94f, 1f);
        private static readonly Color RewardBundleBackgroundColor = new(0.12f, 0.12f, 0.12f, 0.55f);

        public static CardSelectionUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform container;
        [SerializeField] private Button closeButton;

        private Action<Card> onCardSelectedCallback;
        private Action<RewardCardBundleChoice> onRewardBundleSelectedCallback;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (panel != null)
            {
                panel.SetActive(false);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        public void Show(List<Card> cards, Action<Card> onSelected = null)
        {
            if (panel == null || container == null)
            {
                return;
            }

            onCardSelectedCallback = onSelected;
            onRewardBundleSelectedCallback = null;
            panel.SetActive(true);

            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(onSelected == null);
            }

            ClearContainer();

            foreach (var card in cards)
            {
                CardView cardView = CardViewCreator.Instance.CreateCardView(card, container.position, Quaternion.identity);
                cardView.transform.SetParent(container, false);
                cardView.IsHoverPreview = true;
                cardView.transform.localScale = Vector3.one;

                Button button = cardView.GetComponent<Button>();
                if (button == null)
                {
                    button = cardView.gameObject.AddComponent<Button>();
                }

                button.onClick.RemoveAllListeners();
                if (onSelected != null)
                {
                    Card capturedCard = card;
                    button.onClick.AddListener(() => OnCardClicked(capturedCard));
                    button.interactable = true;
                }
                else
                {
                    button.interactable = false;
                }
            }
        }

        public void ShowRewardBundles(List<RewardCardBundleChoice> bundles, Action<RewardCardBundleChoice> onSelected = null)
        {
            if (panel == null || container == null)
            {
                return;
            }

            onCardSelectedCallback = null;
            onRewardBundleSelectedCallback = onSelected;
            panel.SetActive(true);

            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(onSelected == null);
            }

            ClearContainer();
            ConfigureBundleContainerLayout();

            foreach (var bundle in bundles)
            {
                if (bundle != null)
                {
                    CreateRewardBundleView(bundle);
                }
            }
        }

        private void OnCardClicked(Card card)
        {
            Debug.Log($"[CardSelectionUI] 카드 선택 완료: {card.Title}");
            onCardSelectedCallback?.Invoke(card);

            CardViewHoverSystem.Instance?.Hide();
            Close();
        }

        private void OnRewardBundleClicked(RewardCardBundleChoice bundle)
        {
            string civilizationName = bundle?.CivilizationCardData != null ? bundle.CivilizationCardData.cardName : "None";
            string battleName = bundle?.BattleCardData != null ? bundle.BattleCardData.CardName : "None";
            Debug.Log($"[CardSelectionUI] 보상 세트 선택 완료: 문명={civilizationName}, 전투={battleName}");

            onRewardBundleSelectedCallback?.Invoke(bundle);

            CardViewHoverSystem.Instance?.Hide();
            Close();
        }

        private void CreateRewardBundleView(RewardCardBundleChoice bundle)
        {
            GameObject root = new(
                "RewardBundle",
                typeof(RectTransform),
                typeof(Image),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            root.transform.SetParent(container, false);

            float rootWidth = RewardCardWidth + RewardBundlePeekOffset + (RewardBundlePadding * 2f);
            float rootHeight = RewardCardHeight + (RewardBundlePadding * 2f);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(rootWidth, rootHeight);

            Image background = root.GetComponent<Image>();
            background.color = RewardBundleBackgroundColor;
            background.raycastTarget = false;

            HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 0f;
            layout.padding = new RectOffset((int)RewardBundlePadding, (int)RewardBundlePadding, (int)RewardBundlePadding, (int)RewardBundlePadding);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            LayoutElement rootLayoutElement = root.GetComponent<LayoutElement>();
            rootLayoutElement.preferredWidth = rootWidth;
            rootLayoutElement.preferredHeight = rootHeight;

            GameObject stackRoot = new("RewardBundleStack", typeof(RectTransform));
            stackRoot.transform.SetParent(root.transform, false);

            RectTransform stackRect = stackRoot.GetComponent<RectTransform>();
            stackRect.anchorMin = new Vector2(0.5f, 0.5f);
            stackRect.anchorMax = new Vector2(0.5f, 0.5f);
            stackRect.pivot = new Vector2(0.5f, 0.5f);
            stackRect.sizeDelta = new Vector2(RewardCardWidth + RewardBundlePeekOffset, RewardCardHeight);
            stackRect.anchoredPosition = Vector2.zero;

            CardView civilizationCardView = CreateCivilizationRewardView(bundle.CivilizationCardData, stackRect);
            CardView battleCardView = CreateBattleRewardView(bundle.BattleCardData, stackRect);
            if (civilizationCardView == null || battleCardView == null)
            {
                Destroy(root);
                return;
            }

            RewardBundleViewState state = new(bundle, civilizationCardView, battleCardView);
            AttachBundleCardHandler(civilizationCardView, state, RewardCardKind.Civilization);
            AttachBundleCardHandler(battleCardView, state, RewardCardKind.Battle);
            ApplyRewardBundleVisualState(state, false);
        }

        private CardView CreateCivilizationRewardView(CardData civilizationCardData, Transform parent)
        {
            if (civilizationCardData == null)
            {
                return null;
            }

            Card previewCard = new Card(civilizationCardData);
            return CreateRewardCardView(previewCard, parent);
        }

        private CardView CreateBattleRewardView(BattleCardData battleCardData, Transform parent)
        {
            if (battleCardData == null)
            {
                return null;
            }

            Card previewCard = BattleCardViewAdapter.CreatePreviewCard(battleCardData);
            return CreateRewardCardView(previewCard, parent);
        }

        private CardView CreateRewardCardView(Card previewCard, Transform parent)
        {
            if (previewCard == null)
            {
                return null;
            }

            CardView cardView = CardViewCreator.Instance.CreateCardView(previewCard, parent.position, Quaternion.identity);
            cardView.transform.SetParent(parent, false);
            cardView.IsHoverPreview = true;
            cardView.UseBuiltInInteractions = false;
            cardView.transform.localScale = Vector3.one;
            cardView.transform.localRotation = Quaternion.identity;

            LayoutElement layoutElement = cardView.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = cardView.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredWidth = RewardCardWidth;
            layoutElement.preferredHeight = RewardCardHeight;

            RectTransform rectTransform = cardView.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(RewardCardWidth, RewardCardHeight);
            }

            return cardView;
        }

        private void ClearContainer()
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        private void ConfigureBundleContainerLayout()
        {
            HorizontalOrVerticalLayoutGroup layoutGroup = container.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                return;
            }

            layoutGroup.spacing = RewardBundleSpacing;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
        }

        private void AttachBundleCardHandler(CardView cardView, RewardBundleViewState state, RewardCardKind cardKind)
        {
            BundleCardClickHandler clickHandler = cardView.gameObject.GetComponent<BundleCardClickHandler>();
            if (clickHandler == null)
            {
                clickHandler = cardView.gameObject.AddComponent<BundleCardClickHandler>();
            }

            clickHandler.Setup(this, state, cardKind);
        }

        private void HandleRewardBundleCardClicked(RewardBundleViewState state, RewardCardKind clickedKind)
        {
            if (state == null || state.IsAnimating)
            {
                return;
            }

            bool clickedFrontCard =
                (state.IsCivilizationFront && clickedKind == RewardCardKind.Civilization) ||
                (!state.IsCivilizationFront && clickedKind == RewardCardKind.Battle);

            if (clickedFrontCard)
            {
                OnRewardBundleClicked(state.Bundle);
                return;
            }

            state.IsCivilizationFront = !state.IsCivilizationFront;
            ApplyRewardBundleVisualState(state, true);
        }

        private void ApplyRewardBundleVisualState(RewardBundleViewState state, bool animated)
        {
            if (state?.CivilizationCardView == null || state.BattleCardView == null)
            {
                return;
            }

            CardView frontCard = state.IsCivilizationFront ? state.CivilizationCardView : state.BattleCardView;
            CardView backCard = state.IsCivilizationFront ? state.BattleCardView : state.CivilizationCardView;

            RectTransform frontRect = frontCard.GetComponent<RectTransform>();
            RectTransform backRect = backCard.GetComponent<RectTransform>();
            if (frontRect == null || backRect == null)
            {
                return;
            }

            Vector2 frontPos = new(-RewardBundlePeekOffset * 0.5f, 0f);
            Vector2 backPos = new(RewardBundlePeekOffset * 0.5f, 0f);

            frontCard.transform.SetAsLastSibling();
            backCard.transform.SetAsFirstSibling();

            frontCard.transform.DOKill();
            backCard.transform.DOKill();

            if (!animated)
            {
                frontRect.anchoredPosition = frontPos;
                backRect.anchoredPosition = backPos;
                frontCard.transform.localScale = FrontCardScale;
                backCard.transform.localScale = BackCardScale;
                frontCard.AllowHoverPreview = true;
                backCard.AllowHoverPreview = false;
                return;
            }

            state.IsAnimating = true;
            frontCard.AllowHoverPreview = true;
            backCard.AllowHoverPreview = false;

            Sequence sequence = DOTween.Sequence();
            sequence.Join(frontRect.DOAnchorPos(frontPos, RewardBundleSwapDuration).SetEase(Ease.OutCubic));
            sequence.Join(backRect.DOAnchorPos(backPos, RewardBundleSwapDuration).SetEase(Ease.OutCubic));
            sequence.Join(frontCard.transform.DOScale(FrontCardScale, RewardBundleSwapDuration).SetEase(Ease.OutCubic));
            sequence.Join(backCard.transform.DOScale(BackCardScale, RewardBundleSwapDuration).SetEase(Ease.OutCubic));
            sequence.OnComplete(() => state.IsAnimating = false);
            sequence.OnKill(() => state.IsAnimating = false);
        }

        public void Close()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private enum RewardCardKind
        {
            Civilization,
            Battle,
        }

        private sealed class RewardBundleViewState
        {
            public RewardBundleViewState(
                RewardCardBundleChoice bundle,
                CardView civilizationCardView,
                CardView battleCardView)
            {
                Bundle = bundle;
                CivilizationCardView = civilizationCardView;
                BattleCardView = battleCardView;
                IsCivilizationFront = true;
            }

            public RewardCardBundleChoice Bundle { get; }
            public CardView CivilizationCardView { get; }
            public CardView BattleCardView { get; }
            public bool IsCivilizationFront { get; set; }
            public bool IsAnimating { get; set; }
        }

        private sealed class BundleCardClickHandler : MonoBehaviour, IPointerClickHandler
        {
            private CardSelectionUI owner;
            private RewardBundleViewState state;
            private RewardCardKind cardKind;

            public void Setup(CardSelectionUI owner, RewardBundleViewState state, RewardCardKind cardKind)
            {
                this.owner = owner;
                this.state = state;
                this.cardKind = cardKind;
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (eventData.button != PointerEventData.InputButton.Left || owner == null)
                {
                    return;
                }

                owner.HandleRewardBundleCardClicked(state, cardKind);
            }
        }
    }
}

