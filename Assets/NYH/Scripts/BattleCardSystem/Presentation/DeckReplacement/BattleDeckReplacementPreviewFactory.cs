namespace NYH.BattleCardSystem
{
    using NYH.CoreCardSystem;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 교체 UI 안에서 보여지는 모든 카드 시각 요소를 만드는 helper입니다.
    ///
    /// 담당 범위:
    /// - 좌측 새 보상 카드 미리보기
    /// - 우측 선택된 교체 대상 카드 미리보기
    /// - 가운데 후보 카드 목록용 CardView 생성
    /// - CardView 생성 실패 시 fallback 박스 생성
    ///
    /// 중요한 규칙:
    /// 교체 UI에서 BattleCardData를 바로 화면에 그리지 않고,
    /// 반드시 BattleCard -> Preview Card 경로를 거쳐 기존 CardView 표시 체계를 재사용합니다.
    /// 이렇게 해야 덱 보기 Show와 같은 텍스트/이미지/코스트 표기를 공유할 수 있습니다.
    /// </summary>
    internal sealed class BattleDeckReplacementPreviewFactory
    {
        private readonly BattleDeckReplacementViewContext context;

        public BattleDeckReplacementPreviewFactory(BattleDeckReplacementViewContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// 좌측/우측 미리보기 영역에 헤더와 카드 뷰를 그립니다.
        /// 이 미리보기는 클릭 대상이 아니므로 레이캐스트를 막고,
        /// hover 미리보기 역시 비활성화합니다.
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
                RenderPlaceholder(root, "카드 정보를 표시할 수 없습니다.");
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
        /// 후보 목록에 들어갈 카드 한 장을 생성합니다.
        /// 후보 카드는 클릭 대상이므로 레이캐스트는 켜 두고,
        /// 현재 요구사항에 맞춰 hover 미리보기는 끕니다.
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
        /// CardView를 만들 수 없을 때 대신 보여줄 단순 fallback 카드 박스입니다.
        /// 카드 제목과 설명 정도만 표시해 최소한 디버깅이 가능하게 합니다.
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
        /// 카드가 아직 선택되지 않았을 때 안내 문구만 보여주는 placeholder입니다.
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
        /// 특정 루트 아래에 있는 기존 미리보기 요소를 모두 지웁니다.
        /// Show를 다시 열 때 이전 카드가 남지 않게 하기 위한 초기화 단계입니다.
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

        /// <summary>
        /// BattleCardData를 바로 쓰지 않고, 먼저 런타임 BattleCard를 만든 뒤
        /// 그 객체를 Preview Card로 바꿉니다.
        /// 덱 보기 Show와 같은 경로를 강제하려는 메서드입니다.
        /// </summary>
        private Card BuildPreviewCard(BattleCardData cardData)
        {
            if (cardData == null)
            {
                return null;
            }

            BattleCard runtimeCard = new(cardData);
            return BattleCardViewAdapter.CreatePreviewCard(runtimeCard);
        }

        /// <summary>
        /// 후보 카드용 CardView 인스턴스를 만듭니다.
        /// 우선순위는 다음과 같습니다.
        /// 1. 인스펙터에 연결된 BattleCardViewMini 프리팹
        /// 2. 없으면 CardViewCreator fallback
        /// </summary>
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
        /// 일반 크기 CardView를 만들어 미리보기 영역에 배치합니다.
        /// 좌측/우측 큰 카드 미리보기용으로만 사용됩니다.
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

        /// <summary>
        /// CardView 생성 실패 시에만 쓰는 텍스트 박스 fallback입니다.
        /// 카드 시각 프리팹이 깨져 있어도 원인 파악이 가능하도록 카드명/설명은 유지합니다.
        /// </summary>
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

        /// <summary>
        /// 미리보기 전용 영역은 클릭을 먹지 않게 막습니다.
        /// </summary>
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

        /// <summary>
        /// 후보 카드 영역은 클릭이 가능해야 하므로 레이캐스트를 허용합니다.
        /// </summary>
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
