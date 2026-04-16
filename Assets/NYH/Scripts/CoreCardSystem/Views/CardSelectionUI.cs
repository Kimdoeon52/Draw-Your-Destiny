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
        private const float RewardBundleHeaderHeight = 34f;
        private const float RewardBundleSectionSpacing = 12f;
        private const float RewardBundleGapWidth = 44f;
        private static readonly Color RewardBundleBackgroundColor = new(0.12f, 0.12f, 0.12f, 0.88f);
        private static readonly Color RewardBundleSectionColor = new(0.2f, 0.2f, 0.2f, 0.92f);
        private static readonly Color RewardBundleHeaderColor = new(0.92f, 0.76f, 0.37f, 0.95f);
        private static readonly Color RewardBundleGapColor = new(1f, 1f, 1f, 0.18f);

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
            GameObject root = new(
                "RewardBundle",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(VerticalLayoutGroup),
                typeof(LayoutElement));
            root.transform.SetParent(container, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            float sectionWidth = RewardCardWidth + (RewardBundlePadding * 2f);
            float rootWidth = (sectionWidth * 2f) + RewardBundleGapWidth + (RewardBundlePadding * 2f);
            float rootHeight = RewardCardHeight + RewardBundleHeaderHeight + (RewardBundlePadding * 3f);
            rootRect.sizeDelta = new Vector2(rootWidth, rootHeight);

            Image background = root.GetComponent<Image>();
            background.color = RewardBundleBackgroundColor;

            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            layout.spacing = RewardBundlePadding;
            layout.padding = new RectOffset((int)RewardBundlePadding, (int)RewardBundlePadding, (int)RewardBundlePadding, (int)RewardBundlePadding);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            LayoutElement rootLayoutElement = root.GetComponent<LayoutElement>();
            rootLayoutElement.preferredWidth = rootWidth;
            rootLayoutElement.preferredHeight = rootHeight;

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

            CreateBundleHeader(root.transform);

            GameObject row = new("BundleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(root.transform, false);

            HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = RewardBundleSectionSpacing;
            rowLayout.padding = new RectOffset(0, 0, 0, 0);
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            LayoutElement rowLayoutElement = row.GetComponent<LayoutElement>();
            rowLayoutElement.preferredWidth = rootWidth - (RewardBundlePadding * 2f);
            rowLayoutElement.preferredHeight = RewardCardHeight + RewardBundlePadding;

            Transform civilizationSection = CreateRewardSection("문명 카드", row.transform);
            CreateCivilizationRewardView(bundle.CivilizationCardData, civilizationSection);

            CreateBundleGap(row.transform);

            Transform battleSection = CreateRewardSection("전투 카드", row.transform);
            CreateBattleRewardView(bundle.BattleCardData, battleSection);
        }

        private void CreateBundleHeader(Transform parent)
        {
            GameObject header = new("BundleHeader", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            header.transform.SetParent(parent, false);

            Image headerImage = header.GetComponent<Image>();
            headerImage.color = RewardBundleHeaderColor;

            LayoutElement headerLayout = header.GetComponent<LayoutElement>();
            headerLayout.preferredWidth = (RewardCardWidth * 2f) + RewardBundleGapWidth + (RewardBundlePadding * 2f);
            headerLayout.preferredHeight = RewardBundleHeaderHeight;

            CreateText(
                "보상 세트",
                header.transform,
                20,
                TextAnchor.MiddleCenter,
                Color.black,
                FontStyle.Bold,
                (RewardCardWidth * 2f) + RewardBundleGapWidth,
                RewardBundleHeaderHeight);
        }

        private Transform CreateRewardSection(string title, Transform parent)
        {
            GameObject section = new(
                title.Replace(" ", string.Empty),
                typeof(RectTransform),
                typeof(Image),
                typeof(VerticalLayoutGroup),
                typeof(LayoutElement));
            section.transform.SetParent(parent, false);

            Image sectionImage = section.GetComponent<Image>();
            sectionImage.color = RewardBundleSectionColor;

            VerticalLayoutGroup sectionLayout = section.GetComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 8f;
            sectionLayout.padding = new RectOffset(10, 10, 10, 10);
            sectionLayout.childAlignment = TextAnchor.UpperCenter;
            sectionLayout.childForceExpandWidth = false;
            sectionLayout.childForceExpandHeight = false;

            LayoutElement sectionLayoutElement = section.GetComponent<LayoutElement>();
            sectionLayoutElement.preferredWidth = RewardCardWidth + (RewardBundlePadding * 2f);
            sectionLayoutElement.preferredHeight = RewardCardHeight + RewardBundlePadding;

            CreateText(
                title,
                section.transform,
                18,
                TextAnchor.MiddleCenter,
                Color.white,
                FontStyle.Bold,
                RewardCardWidth,
                28f);

            return section.transform;
        }

        private void CreateBundleGap(Transform parent)
        {
            GameObject gap = new("BundleGap", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            gap.transform.SetParent(parent, false);

            Image gapImage = gap.GetComponent<Image>();
            gapImage.color = RewardBundleGapColor;

            LayoutElement gapLayout = gap.GetComponent<LayoutElement>();
            gapLayout.preferredWidth = RewardBundleGapWidth;
            gapLayout.preferredHeight = RewardCardHeight * 0.7f;

            CreateText(
                "+",
                gap.transform,
                28,
                TextAnchor.MiddleCenter,
                Color.white,
                FontStyle.Bold,
                RewardBundleGapWidth,
                RewardCardHeight * 0.7f);
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

            SetPreviewOnly(cardView.gameObject);

            LayoutElement layoutElement = cardView.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = cardView.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredWidth = RewardCardWidth;
            layoutElement.preferredHeight = RewardCardHeight;
        }

        private static Text CreateText(
            string content,
            Transform parent,
            int fontSize,
            TextAnchor alignment,
            Color color,
            FontStyle fontStyle,
            float preferredWidth,
            float preferredHeight)
        {
            GameObject textObject = new("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = fontStyle;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            LayoutElement layoutElement = textObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.preferredHeight = preferredHeight;

            return text;
        }

        private static void SetPreviewOnly(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
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

            SetPreviewOnly(cardView.gameObject);

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
