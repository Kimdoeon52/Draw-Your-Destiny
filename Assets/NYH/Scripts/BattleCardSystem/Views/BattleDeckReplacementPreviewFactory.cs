namespace NYH.BattleCardSystem
{
    using NYH.CoreCardSystem;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 교체 UI에서 쓰는 카드 미리보기와 후보 카드 시각 요소를 생성합니다.
    /// 전투 카드 데이터는 모두 BattleCard -> Preview Card 경로로 통일해서 만듭니다.
    /// </summary>
    internal sealed class BattleDeckReplacementPreviewFactory
    {
        private readonly BattleDeckReplacementViewContext context;

        public BattleDeckReplacementPreviewFactory(BattleDeckReplacementViewContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// 좌측/우측 미리보기 영역에 헤더와 카드 뷰를 생성합니다.
        /// 미리보기 영역은 클릭 대상이 아니므로 레이캐스트를 막습니다.
        /// </summary>
        public void RenderPrimaryPreview(RectTransform root, BattleCardData cardData, string header)
        {
            TMP_Text headerText = BattleDeckReplacementViewElements.CreateText(
                $"{header}Header",
                root,
                24f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            headerText.text = header;

            Card previewCard = BuildPreviewCard(cardData);
            if (previewCard == null)
            {
                RenderPlaceholder(root, "카드 데이터를 표시할 수 없습니다.");
                return;
            }

            if (TryCreateStandardCardView(
                root,
                previewCard,
                new Vector2(
                    BattleDeckReplacementUiMetrics.PreviewCardWidth,
                    BattleDeckReplacementUiMetrics.PreviewCardHeight),
                allowHoverPreview: false,
                blocksRaycasts: false,
                out _))
            {
                return;
            }

            CreateFallbackCardBox(
                root,
                previewCard.Title,
                previewCard.Description,
                new Vector2(
                    BattleDeckReplacementUiMetrics.PreviewCardWidth,
                    BattleDeckReplacementUiMetrics.PreviewCardHeight),
                blocksRaycasts: false);
        }

        /// <summary>
        /// 후보 목록용 카드 뷰를 생성합니다.
        /// 후보 카드는 BattleCardViewMini 프리팹을 우선 사용하고, 없으면 기본 CardView 생성기로 fallback합니다.
        /// </summary>
        public CardView CreateCandidateCardView(Transform parent, BattleCardData cardData)
        {
            Card previewCard = BuildPreviewCard(cardData);
            if (previewCard == null)
            {
                return null;
            }

            CardView cardView = CreateCandidateCardViewInstance(previewCard);
            if (cardView == null)
            {
                return null;
            }

            cardView.transform.SetParent(parent, false);
            cardView.IsHoverPreview = true;
            cardView.UseBuiltInInteractions = false;
            cardView.AllowHoverPreview = false;
            cardView.transform.localScale = Vector3.one;
            cardView.transform.localRotation = Quaternion.identity;

            RectTransform cardRect = cardView.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = Vector2.zero;
            }

            EnableRaycastBlocking(cardView.gameObject);
            return cardView;
        }

        /// <summary>
        /// 카드 뷰 생성이 실패했을 때만 쓰는 간단한 fallback 후보 카드입니다.
        /// </summary>
        public GameObject RenderFallbackCandidate(Transform parent, BattleCardData cardData)
        {
            Card previewCard = BuildPreviewCard(cardData);
            string title = previewCard != null ? previewCard.Title : cardData?.CardName ?? "알 수 없는 카드";
            string description = previewCard != null
                ? previewCard.Description
                : "카드 미리보기를 만들지 못했습니다.";

            return CreateFallbackCardBox(
                parent,
                title,
                description,
                new Vector2(
                    BattleDeckReplacementUiMetrics.PreviewCardWidth,
                    BattleDeckReplacementUiMetrics.PreviewCardHeight),
                blocksRaycasts: true);
        }

        /// <summary>
        /// 카드 대신 안내 문구만 보여주는 플레이스홀더를 생성합니다.
        /// </summary>
        public void RenderPlaceholder(Transform parent, string message)
        {
            GameObject placeholder = BattleDeckReplacementViewElements.CreatePanel(
                "PlaceholderPreview",
                parent,
                BattleDeckReplacementUiMetrics.CandidateDefaultColor);
            DisableRaycastBlocking(placeholder);

            LayoutElement layout = placeholder.AddComponent<LayoutElement>();
            layout.preferredWidth = BattleDeckReplacementUiMetrics.PreviewCardWidth;
            layout.preferredHeight = BattleDeckReplacementUiMetrics.PreviewCardHeight;

            RectTransform rect = placeholder.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(
                BattleDeckReplacementUiMetrics.PreviewCardWidth,
                BattleDeckReplacementUiMetrics.PreviewCardHeight);

            TMP_Text messageText = BattleDeckReplacementViewElements.CreateText(
                "PlaceholderText",
                placeholder.transform,
                24f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            messageText.text = message;

            RectTransform messageRect = messageText.rectTransform;
            messageRect.anchorMin = Vector2.zero;
            messageRect.anchorMax = Vector2.one;
            messageRect.offsetMin = new Vector2(18f, 18f);
            messageRect.offsetMax = new Vector2(-18f, -18f);
        }

        /// <summary>
        /// 지정한 루트 아래에 생성된 미리보기 오브젝트를 모두 제거합니다.
        /// </summary>
        public void Clear(Transform root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Transform child in root)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private Card BuildPreviewCard(BattleCardData cardData)
        {
            if (cardData == null)
            {
                return null;
            }

            BattleCard runtimeCard = new(cardData);
            return BattleCardViewAdapter.CreatePreviewCard(runtimeCard);
        }

        private CardView CreateCandidateCardViewInstance(Card previewCard)
        {
            if (previewCard == null)
            {
                return null;
            }

            if (context.CandidateCardViewPrefab != null)
            {
                CardView prefabInstance = UnityEngine.Object.Instantiate(context.CandidateCardViewPrefab);
                prefabInstance.gameObject.SetActive(true);
                prefabInstance.Setup(previewCard);
                return prefabInstance;
            }

            return CardViewCreator.Instance != null
                ? CardViewCreator.Instance.CreateCardView(previewCard, Vector3.zero, Quaternion.identity)
                : null;
        }

        /// <summary>
        /// 일반 크기 카드 뷰를 생성하고, 미리보기 용도에 맞는 상호작용 설정을 적용합니다.
        /// </summary>
        private bool TryCreateStandardCardView(
            Transform parent,
            Card previewCard,
            Vector2 fixedSize,
            bool allowHoverPreview,
            bool blocksRaycasts,
            out CardView cardView)
        {
            cardView = null;
            if (previewCard == null || CardViewCreator.Instance == null)
            {
                return false;
            }

            cardView = CardViewCreator.Instance.CreateCardView(previewCard, Vector3.zero, Quaternion.identity);
            if (cardView == null)
            {
                return false;
            }

            cardView.transform.SetParent(parent, false);
            cardView.IsHoverPreview = true;
            cardView.UseBuiltInInteractions = false;
            cardView.AllowHoverPreview = allowHoverPreview;
            cardView.transform.localScale = Vector3.one;
            cardView.transform.localRotation = Quaternion.identity;

            RectTransform cardRect = cardView.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.sizeDelta = fixedSize;
                cardRect.anchoredPosition = Vector2.zero;
            }

            LayoutElement layout = cardView.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = cardView.gameObject.AddComponent<LayoutElement>();
            }

            layout.preferredWidth = fixedSize.x;
            layout.preferredHeight = fixedSize.y;

            if (blocksRaycasts)
            {
                EnableRaycastBlocking(cardView.gameObject);
            }
            else
            {
                DisableRaycastBlocking(cardView.gameObject);
            }

            return true;
        }

        private GameObject CreateFallbackCardBox(
            Transform parent,
            string titleTextValue,
            string descriptionTextValue,
            Vector2 previewSize,
            bool blocksRaycasts)
        {
            GameObject root = BattleDeckReplacementViewElements.CreatePanel(
                "FallbackCardBox",
                parent,
                BattleDeckReplacementUiMetrics.CandidateDefaultColor);

            if (blocksRaycasts)
            {
                EnableRaycastBlocking(root);
            }
            else
            {
                DisableRaycastBlocking(root);
            }

            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = previewSize.x;
            layout.preferredHeight = previewSize.y;

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = previewSize;
            rect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup rootLayout = root.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(16, 16, 18, 18);
            rootLayout.spacing = 10f;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = false;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            TMP_Text title = BattleDeckReplacementViewElements.CreateText(
                "FallbackTitle",
                root.transform,
                24f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            title.text = titleTextValue;

            TMP_Text description = BattleDeckReplacementViewElements.CreateText(
                "FallbackDescription",
                root.transform,
                18f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft);
            description.text = descriptionTextValue;
            description.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            return root;
        }

        private static void DisableRaycastBlocking(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = target.AddComponent<CanvasGroup>();
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private static void EnableRaycastBlocking(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = target.AddComponent<CanvasGroup>();
            }

            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
    }
}
