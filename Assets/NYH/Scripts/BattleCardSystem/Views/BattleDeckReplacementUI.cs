namespace NYH.BattleCardSystem
{
    using System;
    using System.Collections.Generic;
    using TMPro;
    using NYH.CoreCardSystem;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// 전투 덱 교체 화면의 메인 컨트롤러입니다.
    /// 후보 카드 생성, 선택 상태 관리, 확인/취소 흐름만 담당합니다.
    /// </summary>
    public class BattleDeckReplacementUI : MonoBehaviour
    {
        public static BattleDeckReplacementUI Instance { get; private set; }

        private readonly List<CandidateView> candidateViews = new();

        [Header("화면 루트")]
        [Tooltip("\uAD50\uCCB4 \uD654\uBA74 \uC804\uCCB4\uB97C \uCF1C\uACE0 \uB044\uB294 \uCD5C\uC0C1\uC704 \uD328\uB110\uC785\uB2C8\uB2E4.")]
        [SerializeField] private GameObject overlayPanel;
        [Tooltip("\uC0C8\uB85C \uC5BB\uB294 \uC804\uD22C \uCE74\uB4DC \uBBF8\uB9AC\uBCF4\uAE30\uB97C \uADF8\uB9B4 \uCEE8\uD14C\uC774\uB108\uC785\uB2C8\uB2E4.")]
        [SerializeField] private RectTransform rewardPreviewRoot;
        [Tooltip("\uD604\uC7AC \uC120\uD0DD\uD55C \uAD50\uCCB4 \uB300\uC0C1 \uCE74\uB4DC \uBBF8\uB9AC\uBCF4\uAE30\uB97C \uADF8\uB9B4 \uCEE8\uD14C\uC774\uB108\uC785\uB2C8\uB2E4.")]
        [SerializeField] private RectTransform selectedPreviewRoot;
        [Tooltip("\uAD50\uCCB4 \uD6C4\uBCF4 \uCE74\uB4DC\uB4E4\uC774 \uB3D9\uC801\uC73C\uB85C \uC0DD\uC131\uB420 Content \uB8E8\uD2B8\uC785\uB2C8\uB2E4. Scroll View > Viewport > Content\uB97C \uC5F0\uACB0\uD558\uBA74 \uB429\uB2C8\uB2E4.")]
        [SerializeField] private RectTransform candidateContentRoot;

        [Header("주요 텍스트(TMP 권장)")]
        [Tooltip("\uD654\uBA74 \uC0C1\uB2E8 \uBA54\uC778 \uC81C\uBAA9 \uD14D\uC2A4\uD2B8\uC785\uB2C8\uB2E4.")]
        [SerializeField] private TMP_Text titleText;
        [Tooltip("\uD654\uBA74 \uC0C1\uB2E8 \uBCF4\uC870 \uC124\uBA85 \uD14D\uC2A4\uD2B8\uC785\uB2C8\uB2E4.")]
        [SerializeField] private TMP_Text subtitleText;
        [Tooltip("\uC6B0\uCE21 \uC120\uD0DD \uCE74\uB4DC \uC601\uC5ED \uC81C\uBAA9 \uD14D\uC2A4\uD2B8\uC785\uB2C8\uB2E4.")]
        [SerializeField] private TMP_Text selectedHeaderText;

        [Header("레거시 텍스트 대체용")]
        [Tooltip("TMP \uB300\uC2E0 \uAE30\uBCF8 UI Text\uB97C \uC4F0\uB294 \uACBD\uC6B0 \uC81C\uBAA9 \uD14D\uC2A4\uD2B8\uB97C \uC5F0\uACB0\uD569\uB2C8\uB2E4.")]
        [SerializeField] private Text legacyTitleText;
        [Tooltip("TMP \uB300\uC2E0 \uAE30\uBCF8 UI Text\uB97C \uC4F0\uB294 \uACBD\uC6B0 \uBCF4\uC870 \uC124\uBA85 \uD14D\uC2A4\uD2B8\uB97C \uC5F0\uACB0\uD569\uB2C8\uB2E4.")]
        [SerializeField] private Text legacySubtitleText;
        [Tooltip("TMP \uB300\uC2E0 \uAE30\uBCF8 UI Text\uB97C \uC4F0\uB294 \uACBD\uC6B0 \uC120\uD0DD \uCE74\uB4DC \uC81C\uBAA9 \uD14D\uC2A4\uD2B8\uB97C \uC5F0\uACB0\uD569\uB2C8\uB2E4.")]
        [SerializeField] private Text legacySelectedHeaderText;

        [Header("버튼")]
        [Tooltip("\uC120\uD0DD\uD55C \uCE74\uB4DC\uB97C \uAE30\uC900\uC73C\uB85C \uAD50\uCCB4\uB97C \uD655\uC815\uD558\uB294 \uBC84\uD2BC\uC785\uB2C8\uB2E4.")]
        [SerializeField] private Button confirmButton;
        [Tooltip("\uAD50\uCCB4\uB97C \uCDE8\uC18C\uD558\uACE0 \uBCF4\uC0C1\uC744 \uBAA8\uB450 \uBC1B\uC9C0 \uC54A\uACE0 \uB118\uC5B4\uAC00\uB294 \uBC84\uD2BC\uC785\uB2C8\uB2E4.")]
        [SerializeField] private Button cancelButton;

        [Header("후보 카드 프리팹")]
        [Tooltip("교체 후보 카드에 사용할 CardView 프리팹입니다. BattleCardViewMini.prefab을 연결합니다.")]
        [SerializeField] private CardView candidateCardViewPrefab;

        private BattleCardData selectedCandidate;
        private Action<BattleCardData> onConfirmed;
        private Action onCanceled;
        private CardView runtimeCandidateCardTemplate;
        private CardView runtimeCandidateCardTemplateSource;

        public bool IsOpen => overlayPanel != null && overlayPanel.activeSelf;

        public static BattleDeckReplacementUI GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            BattleDeckReplacementUI[] existingUis = FindObjectsByType<BattleDeckReplacementUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            if (existingUis.Length > 0 && existingUis[0] != null)
            {
                return existingUis[0];
            }

            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform candidate in transforms)
            {
                if (candidate == null || !string.Equals(candidate.name, "BattleDeckReplacementUI", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                BattleDeckReplacementUI existingComponent = candidate.GetComponent<BattleDeckReplacementUI>();
                return existingComponent != null
                    ? existingComponent
                    : candidate.gameObject.AddComponent<BattleDeckReplacementUI>();
            }

            Canvas parentCanvas = FindPreferredParentCanvas();
            if (parentCanvas == null)
            {
                GameObject canvasObject = new(
                    "BattleDeckReplacementCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                parentCanvas = canvasObject.GetComponent<Canvas>();
                parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                DontDestroyOnLoad(canvasObject);
            }

            EnsureEventSystemExists();

            GameObject root = new("BattleDeckReplacementUI", typeof(RectTransform));
            root.transform.SetParent(parentCanvas.transform, false);
            return root.AddComponent<BattleDeckReplacementUI>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureViewReady(out BattleDeckReplacementViewContext context);
            if (context.OverlayPanel != null)
            {
                context.OverlayPanel.SetActive(false);
            }

            ApplyViewContext(context);
        }

        private void OnDestroy()
        {
            ClearRuntimeCandidateTemplateCache();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Show(
            BattleCardData rewardCard,
            IReadOnlyList<BattleCardData> candidates,
            Action<BattleCardData> onConfirmed,
            Action onCanceled)
        {
            BattleDeckReplacementPreviewFactory previewFactory = EnsureViewReady(out BattleDeckReplacementViewContext context);
            if (!BattleDeckReplacementViewBindingUtility.HasMinimumBindings(context))
            {
                Debug.LogWarning("[BattleDeckReplacementUI] 필수 UI 참조가 빠져 있어 교체 화면을 열 수 없습니다.");
                onCanceled?.Invoke();
                return;
            }

            context.CandidateCardViewPrefab = ResolveCandidateCardViewPrefab(context);
            if (context.CandidateCardViewPrefab == null)
            {
                Debug.LogWarning("[BattleDeckReplacementUI] 후보 카드 프리팹이 연결되지 않았습니다. BattleCardViewMini.prefab을 직접 연결해 주세요.");
                onCanceled?.Invoke();
                return;
            }

            this.onConfirmed = onConfirmed;
            this.onCanceled = onCanceled;
            selectedCandidate = null;

            context.ConfirmButton.interactable = false;
            BattleDeckReplacementViewBindingUtility.SetButtonLabel(context.ConfirmButton, "선택 후 확인");
            BattleDeckReplacementViewBindingUtility.SetButtonLabel(context.CancelButton, "취소");
            BattleDeckReplacementViewBindingUtility.SetText(
                context.TitleText,
                context.LegacyTitleText,
                "교체할 카드");
            BattleDeckReplacementViewBindingUtility.SetText(
                context.SubtitleText,
                context.LegacySubtitleText,
                rewardCard != null ? $"새 보상: {rewardCard.CardName}" : string.Empty);
            BattleDeckReplacementViewBindingUtility.SetText(
                context.SelectedHeaderText,
                context.LegacySelectedHeaderText,
                "교체 미리보기");

            previewFactory.Clear(context.RewardPreviewRoot);
            previewFactory.Clear(context.SelectedPreviewRoot);
            ClearCandidates(context);

            if (context.RewardPreviewRoot != null)
            {
                previewFactory.RenderPrimaryPreview(context.RewardPreviewRoot, rewardCard, "새로 얻는 카드");
            }

            if (context.SelectedPreviewRoot != null)
            {
                previewFactory.RenderPlaceholder(context.SelectedPreviewRoot, "교체할 카드를 선택하세요.");
            }

            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    BattleCardData candidate = candidates[i];
                    if (candidate != null)
                    {
                        CreateCandidateView(context, previewFactory, candidate, i);
                    }
                }
            }

            if (context.CandidateContentRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(context.CandidateContentRoot);
            }

            if (context.RewardPreviewRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(context.RewardPreviewRoot);
            }

            if (context.SelectedPreviewRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(context.SelectedPreviewRoot);
            }

            Canvas.ForceUpdateCanvases();
            context.OverlayPanel.SetActive(true);
            ApplyViewContext(context);
        }

        public void Close()
        {
            Dismiss(invokeCancel: true);
        }

        internal void HandleCandidateCardClicked(BattleCardData candidate, int candidateIndex)
        {
            BattleDeckReplacementPreviewFactory previewFactory = EnsureViewReady(out BattleDeckReplacementViewContext context);

            selectedCandidate = candidate;
            context.ConfirmButton.interactable = selectedCandidate != null;

            foreach (CandidateView candidateView in candidateViews)
            {
                bool isSelected = candidateView.Index == candidateIndex;
                candidateView.SelectionFrame?.SetSelected(isSelected);
            }

            BattleDeckReplacementViewBindingUtility.SetText(
                context.SelectedHeaderText,
                context.LegacySelectedHeaderText,
                candidate != null ? $"\uAD50\uCCB4 \uB300\uC0C1: {candidate.CardName}" : "\uAD50\uCCB4 \uBBF8\uB9AC\uBCF4\uAE30");

            if (context.SelectedPreviewRoot != null)
            {
                previewFactory.Clear(context.SelectedPreviewRoot);
                previewFactory.RenderPrimaryPreview(context.SelectedPreviewRoot, candidate, "\uC120\uD0DD\uD55C \uCE74\uB4DC");
                LayoutRebuilder.ForceRebuildLayoutImmediate(context.SelectedPreviewRoot);
            }

            if (context.CandidateContentRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(context.CandidateContentRoot);
            }

            Canvas.ForceUpdateCanvases();
            ApplyViewContext(context);
        }

        /// <summary>
        /// 후보 카드를 Content 바로 아래에 생성합니다.
        /// 중간 래퍼 패널 없이 카드 자체를 선택 대상으로 사용합니다.
        /// </summary>
        private void CreateCandidateView(
            BattleDeckReplacementViewContext context,
            BattleDeckReplacementPreviewFactory previewFactory,
            BattleCardData candidate,
            int index)
        {
            GameObject candidateObject = null;
            CardView cardView = previewFactory.CreateCandidateCardView(context.CandidateContentRoot, candidate);
            if (cardView != null)
            {
                candidateObject = cardView.gameObject;
            }
            else
            {
                candidateObject = previewFactory.RenderFallbackCandidate(context.CandidateContentRoot, candidate);
            }

            if (candidateObject == null)
            {
                return;
            }

            candidateObject.name = $"Candidate_{index}_{candidate.CardName}";

            RectTransform rectTransform = candidateObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }

            LayoutElement layout = candidateObject.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = candidateObject.AddComponent<LayoutElement>();
            }

            Vector2 visualSize = ResolveVisualSize(rectTransform);
            layout.preferredWidth = visualSize.x;
            layout.preferredHeight = visualSize.y;

            BattleDeckReplacementCandidateClickHandler clickHandler =
                candidateObject.GetComponent<BattleDeckReplacementCandidateClickHandler>();
            if (clickHandler == null)
            {
                clickHandler = candidateObject.AddComponent<BattleDeckReplacementCandidateClickHandler>();
            }

            clickHandler.Setup(this, candidate, index);

            SelectionFrame selectionFrame = EnsureSelectionFrame(candidateObject.transform);
            candidateViews.Add(new CandidateView(index, candidate, selectionFrame));
        }

        private void HandleConfirmClicked()
        {
            if (selectedCandidate == null)
            {
                return;
            }

            Action<BattleCardData> confirmCallback = onConfirmed;
            BattleCardData confirmedCandidate = selectedCandidate;
            Dismiss(invokeCancel: false);
            confirmCallback?.Invoke(confirmedCandidate);
        }

        private void HandleCancelClicked()
        {
            Dismiss(invokeCancel: true);
        }

        private void Dismiss(bool invokeCancel)
        {
            BattleDeckReplacementPreviewFactory previewFactory = EnsureViewReady(out BattleDeckReplacementViewContext context);
            if (context.OverlayPanel == null)
            {
                return;
            }

            context.OverlayPanel.SetActive(false);
            previewFactory.Clear(context.RewardPreviewRoot);
            previewFactory.Clear(context.SelectedPreviewRoot);
            ClearCandidates(context);
            selectedCandidate = null;

            if (context.ConfirmButton != null)
            {
                context.ConfirmButton.interactable = false;
            }

            Action cancelCallback = onCanceled;
            onConfirmed = null;
            onCanceled = null;

            ApplyViewContext(context);
            CardViewHoverSystem.Instance?.Hide();

            if (invokeCancel)
            {
                cancelCallback?.Invoke();
            }
        }

        private void ClearCandidates(BattleDeckReplacementViewContext context)
        {
            candidateViews.Clear();

            if (context.CandidateContentRoot == null)
            {
                return;
            }

            for (int i = context.CandidateContentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(context.CandidateContentRoot.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 후보 카드 프리팹 참조를 실제 런타임 템플릿으로 정리합니다.
        /// 실수로 Content 안의 샘플 카드 오브젝트를 연결한 경우에도,
        /// 목록을 비우기 전에 숨겨진 템플릿을 따로 복제해 참조가 끊기지 않도록 보정합니다.
        /// </summary>
        private CardView ResolveCandidateCardViewPrefab(BattleDeckReplacementViewContext context)
        {
            CardView assignedPrefab = context.CandidateCardViewPrefab;
            if (assignedPrefab == null)
            {
                return runtimeCandidateCardTemplate;
            }

            if (!IsSceneTemplateInsideCandidateContent(context, assignedPrefab))
            {
                ClearRuntimeCandidateTemplateCache();
                return assignedPrefab;
            }

            if (runtimeCandidateCardTemplate != null && runtimeCandidateCardTemplateSource == assignedPrefab)
            {
                return runtimeCandidateCardTemplate;
            }

            ClearRuntimeCandidateTemplateCache();

            runtimeCandidateCardTemplate = Instantiate(assignedPrefab, transform);
            runtimeCandidateCardTemplateSource = assignedPrefab;
            runtimeCandidateCardTemplate.gameObject.name = $"{assignedPrefab.gameObject.name}_RuntimeTemplate";
            runtimeCandidateCardTemplate.gameObject.SetActive(false);
            runtimeCandidateCardTemplate.transform.SetParent(transform, false);
            return runtimeCandidateCardTemplate;
        }

        private void ClearRuntimeCandidateTemplateCache()
        {
            if (runtimeCandidateCardTemplate != null)
            {
                Destroy(runtimeCandidateCardTemplate.gameObject);
            }

            runtimeCandidateCardTemplate = null;
            runtimeCandidateCardTemplateSource = null;
        }

        private static bool IsSceneTemplateInsideCandidateContent(
            BattleDeckReplacementViewContext context,
            CardView candidatePrefab)
        {
            if (context.CandidateContentRoot == null || candidatePrefab == null)
            {
                return false;
            }

            if (!candidatePrefab.gameObject.scene.IsValid())
            {
                return false;
            }

            return candidatePrefab.transform.IsChildOf(context.CandidateContentRoot);
        }

        private BattleDeckReplacementPreviewFactory EnsureViewReady(out BattleDeckReplacementViewContext context)
        {
            context = CreateViewContext();
            if (!BattleDeckReplacementViewBindingUtility.HasAnyAssignedReference(context))
            {
                BattleDeckReplacementRuntimeLayoutBuilder.BuildIfNeeded(context, HandleConfirmClicked, HandleCancelClicked);
            }

            BattleDeckReplacementViewBindingUtility.FinalizeBindings(context, HandleConfirmClicked, HandleCancelClicked);
            ApplyViewContext(context);
            return new BattleDeckReplacementPreviewFactory(context);
        }

        private BattleDeckReplacementViewContext CreateViewContext()
        {
            return new BattleDeckReplacementViewContext(transform, GetComponent<RectTransform>())
            {
                OverlayPanel = overlayPanel,
                RewardPreviewRoot = rewardPreviewRoot,
                SelectedPreviewRoot = selectedPreviewRoot,
                CandidateContentRoot = candidateContentRoot,
                TitleText = titleText,
                SubtitleText = subtitleText,
                SelectedHeaderText = selectedHeaderText,
                LegacyTitleText = legacyTitleText,
                LegacySubtitleText = legacySubtitleText,
                LegacySelectedHeaderText = legacySelectedHeaderText,
                ConfirmButton = confirmButton,
                CancelButton = cancelButton,
                CandidateCardViewPrefab = candidateCardViewPrefab,
            };
        }

        private void ApplyViewContext(BattleDeckReplacementViewContext context)
        {
            overlayPanel = context.OverlayPanel;
            rewardPreviewRoot = context.RewardPreviewRoot;
            selectedPreviewRoot = context.SelectedPreviewRoot;
            candidateContentRoot = context.CandidateContentRoot;
            titleText = context.TitleText;
            subtitleText = context.SubtitleText;
            selectedHeaderText = context.SelectedHeaderText;
            legacyTitleText = context.LegacyTitleText;
            legacySubtitleText = context.LegacySubtitleText;
            legacySelectedHeaderText = context.LegacySelectedHeaderText;
            confirmButton = context.ConfirmButton;
            cancelButton = context.CancelButton;
        }

        private static void EnsureEventSystemExists()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystemObject);
        }

        private static Canvas FindPreferredParentCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null || !canvas.isActiveAndEnabled)
                {
                    continue;
                }

                if (!canvas.isRootCanvas || !canvas.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return canvas;
                }
            }

            return null;
        }

        private static Vector2 ResolveVisualSize(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return new Vector2(
                    BattleDeckReplacementUiMetrics.PreviewCardWidth,
                    BattleDeckReplacementUiMetrics.PreviewCardHeight);
            }

            Vector2 rectSize = rectTransform.rect.size;
            if (rectSize.x > 0f && rectSize.y > 0f)
            {
                return rectSize;
            }

            Vector2 sizeDelta = rectTransform.sizeDelta;
            if (sizeDelta.x > 0f && sizeDelta.y > 0f)
            {
                return sizeDelta;
            }

            BoxCollider boxCollider = rectTransform.GetComponent<BoxCollider>();
            if (boxCollider != null && boxCollider.size.x > 0f && boxCollider.size.y > 0f)
            {
                return new Vector2(boxCollider.size.x, boxCollider.size.y);
            }

            BoxCollider2D boxCollider2D = rectTransform.GetComponent<BoxCollider2D>();
            if (boxCollider2D != null && boxCollider2D.size.x > 0f && boxCollider2D.size.y > 0f)
            {
                return boxCollider2D.size;
            }

            return new Vector2(
                BattleDeckReplacementUiMetrics.PreviewCardWidth,
                BattleDeckReplacementUiMetrics.PreviewCardHeight);
        }

        private static SelectionFrame EnsureSelectionFrame(Transform cardRoot)
        {
            SelectionFrame existingFrame = cardRoot.GetComponentInChildren<SelectionFrame>(true);
            if (existingFrame != null)
            {
                existingFrame.SetSelected(false);
                return existingFrame;
            }

            GameObject frameObject = new(
                "SelectionFrame",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline),
                typeof(SelectionFrame));
            frameObject.transform.SetParent(cardRoot, false);

            RectTransform frameRect = frameObject.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = new Vector2(-8f, -8f);
            frameRect.offsetMax = new Vector2(8f, 8f);

            Image frameImage = frameObject.GetComponent<Image>();
            frameImage.color = BattleDeckReplacementUiMetrics.CandidateSelectedColor;
            frameImage.raycastTarget = false;

            Outline outline = frameObject.GetComponent<Outline>();
            outline.effectDistance = new Vector2(6f, 6f);
            outline.effectColor = BattleDeckReplacementUiMetrics.CandidateOutlineColor;

            SelectionFrame selectionFrame = frameObject.GetComponent<SelectionFrame>();
            selectionFrame.Initialize(frameImage, outline);
            selectionFrame.SetSelected(false);
            return selectionFrame;
        }

        private sealed class CandidateView
        {
            public CandidateView(int index, BattleCardData data, SelectionFrame selectionFrame)
            {
                Index = index;
                Data = data;
                SelectionFrame = selectionFrame;
            }

            public int Index { get; }
            public BattleCardData Data { get; }
            public SelectionFrame SelectionFrame { get; }
        }

        private sealed class SelectionFrame : MonoBehaviour
        {
            private Image frameImage;
            private Outline frameOutline;

            public void Initialize(Image frameImage, Outline frameOutline)
            {
                this.frameImage = frameImage;
                this.frameOutline = frameOutline;
            }

            public void SetSelected(bool isSelected)
            {
                if (frameImage != null)
                {
                    frameImage.enabled = isSelected;
                }

                if (frameOutline != null)
                {
                    frameOutline.enabled = isSelected;
                }
            }
        }
    }
}
