namespace NYH.CoreCardSystem
{
    using System;
    using System.Collections.Generic;
    using NYH.BattleCardSystem;
    using UnityEngine;
    using UnityEngine.UI;

    public class CardSelectionUI : MonoBehaviour
    {
        private const float RewardCardWidth = 244f;
        private const float RewardCardHeight = 380f;
        private const float RewardBundleSpacing = 16f;
        private const float RewardBundlePadding = 16f;

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

            if (CardViewHoverSystem.Instance != null)
            {
                CardViewHoverSystem.Instance.Hide();
            }

            Close();
        }

        private void OnRewardBundleClicked(RewardCardBundleChoice bundle)
        {
            string civilizationName = bundle?.CivilizationCardData != null ? bundle.CivilizationCardData.cardName : "None";
            string battleName = bundle?.BattleCardData != null ? bundle.BattleCardData.CardName : "None";
            Debug.Log($"[CardSelectionUI] 보상 세트 선택 완료: 문명={civilizationName}, 전투={battleName}");

            onRewardBundleSelectedCallback?.Invoke(bundle);

            if (CardViewHoverSystem.Instance != null)
            {
                CardViewHoverSystem.Instance.Hide();
            }

            Close();
        }

        private void CreateRewardBundleView(RewardCardBundleChoice bundle)
        {
            GameObject root = new GameObject("RewardBundle", typeof(RectTransform), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup));
            root.transform.SetParent(container, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            float rootWidth = (RewardCardWidth * 2f) + RewardBundleSpacing + (RewardBundlePadding * 2f);
            float rootHeight = RewardCardHeight + (RewardBundlePadding * 2f);
            rootRect.sizeDelta = new Vector2(rootWidth, rootHeight);

            Image background = root.GetComponent<Image>();
            background.color = new Color(0.15f, 0.15f, 0.15f, 0.45f);

            HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = RewardBundleSpacing;
            layout.padding = new RectOffset((int)RewardBundlePadding, (int)RewardBundlePadding, (int)RewardBundlePadding, (int)RewardBundlePadding);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Button button = root.GetComponent<Button>();
            button.onClick.RemoveAllListeners();

            if (onRewardBundleSelectedCallback != null)
            {
                RewardCardBundleChoice capturedBundle = bundle;
                button.onClick.AddListener(() => OnRewardBundleClicked(capturedBundle));
                button.interactable = true;
            }
            else
            {
                button.interactable = false;
            }

            CreateCivilizationRewardView(bundle.CivilizationCardData, root.transform);
            CreateBattleRewardView(bundle.BattleCardData, root.transform);
        }

        private void CreateCivilizationRewardView(CardData civilizationCardData, Transform parent)
        {
            if (civilizationCardData == null)
            {
                return;
            }

            Card previewCard = new Card(civilizationCardData);
            CardView cardView = CardViewCreator.Instance.CreateCardView(previewCard, parent.position, Quaternion.identity);
            cardView.transform.SetParent(parent, false);
            cardView.IsHoverPreview = true;
            cardView.transform.localScale = Vector3.one;

            Button button = cardView.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
            }

            LayoutElement layoutElement = cardView.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = cardView.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredWidth = RewardCardWidth;
            layoutElement.preferredHeight = RewardCardHeight;
        }

        private void CreateBattleRewardView(BattleCardData battleCardData, Transform parent)
        {
            if (battleCardData == null)
            {
                return;
            }

            Card previewCard = BattleCardViewAdapter.CreatePreviewCard(battleCardData);
            CardView cardView = CardViewCreator.Instance.CreateCardView(previewCard, parent.position, Quaternion.identity);
            cardView.transform.SetParent(parent, false);
            cardView.IsHoverPreview = true;
            cardView.transform.localScale = Vector3.one;

            Button button = cardView.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
            }

            LayoutElement layoutElement = cardView.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = cardView.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredWidth = RewardCardWidth;
            layoutElement.preferredHeight = RewardCardHeight;
        }

        private void ClearContainer()
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        public void Close()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }
}
