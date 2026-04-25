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
    /// Dedicated UI for the "replace a battle card reward" flow.
    /// It shows the incoming reward, the current replacement candidates, and the selected target.
    /// </summary>
    public class BattleDeckReplacementUI : MonoBehaviour
    {
        private const float PreviewCardWidth = 244f;
        private const float PreviewCardHeight = 380f;
        private const float CandidateSpacing = 16f;

        private static readonly Color OverlayColor = new(0f, 0f, 0f, 0.72f);
        private static readonly Color WindowColor = new(0.11f, 0.12f, 0.15f, 0.97f);
        private static readonly Color SectionColor = new(0.17f, 0.18f, 0.22f, 1f);
        private static readonly Color ContentColor = new(0.14f, 0.15f, 0.18f, 1f);
        private static readonly Color ButtonColor = new(0.25f, 0.28f, 0.34f, 1f);
        private static readonly Color CandidateDefaultColor = new(0.22f, 0.24f, 0.29f, 1f);
        private static readonly Color CandidateSelectedColor = new(0.84f, 0.61f, 0.19f, 1f);

        public static BattleDeckReplacementUI Instance { get; private set; }

        private readonly List<CandidateView> candidateViews = new();

        private GameObject overlayPanel;
        private RectTransform rewardPreviewRoot;
        private RectTransform selectedPreviewRoot;
        private RectTransform candidateContentRoot;
        private TMP_Text titleText;
        private TMP_Text subtitleText;
        private TMP_Text selectedHeaderText;
        private Button confirmButton;

        private BattleCardData selectedCandidate;
        private Action<BattleCardData> onConfirmed;
        private Action onCanceled;

        /// <summary>
        /// Returns true while the replacement window is visible.
        /// </summary>
        public bool IsOpen => overlayPanel != null && overlayPanel.activeSelf;

        /// <summary>
        /// Returns an existing replacement UI or creates a runtime one if none exists.
        /// A canvas and EventSystem are also created on demand.
        /// </summary>
        public static BattleDeckReplacementUI GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            Canvas parentCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
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
            }

            EnsureEventSystemExists();

            GameObject root = new("BattleDeckReplacementUI", typeof(RectTransform));
            root.transform.SetParent(parentCanvas.transform, false);
            return root.AddComponent<BattleDeckReplacementUI>();
        }

        /// <summary>
        /// Registers the singleton instance and builds the UI structure once.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildUiIfNeeded();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Opens the replacement UI for a new reward card.
        /// Confirm returns the chosen replacement target; cancel returns null via the caller flow.
        /// </summary>
        public void Show(
            BattleCardData rewardCard,
            IReadOnlyList<BattleCardData> candidates,
            Action<BattleCardData> onConfirmed,
            Action onCanceled)
        {
            BuildUiIfNeeded();

            this.onConfirmed = onConfirmed;
            this.onCanceled = onCanceled;
            selectedCandidate = null;
            confirmButton.interactable = false;

            titleText.text = "Choose a Battle Card to Replace";
            subtitleText.text = "Select one card to replace if you want to keep the new reward card.";
            selectedHeaderText.text = "Replacement Preview";

            ClearPreview(rewardPreviewRoot);
            ClearPreview(selectedPreviewRoot);
            ClearCandidates();

            CreatePreviewContent(rewardPreviewRoot, rewardCard, "New Reward Card");
            CreatePreviewPlaceholder(selectedPreviewRoot, "Select a card to replace.");

            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    BattleCardData candidate = candidates[i];
                    if (candidate != null)
                    {
                        CreateCandidateView(candidate, i);
                    }
                }
            }

            overlayPanel.SetActive(true);
        }

        /// <summary>
        /// Closes the window as a cancel action.
        /// </summary>
        public void Close()
        {
            Dismiss(invokeCancel: true);
        }

        /// <summary>
        /// Builds the runtime UI only once.
        /// The layout is split into reward preview, candidate list, and selected preview.
        /// </summary>
        private void BuildUiIfNeeded()
        {
            if (overlayPanel != null)
            {
                return;
            }

            RectTransform rootRect = GetComponent<RectTransform>();
            StretchRect(rootRect);

            overlayPanel = CreatePanel("OverlayPanel", transform, OverlayColor);
            StretchRect(overlayPanel.GetComponent<RectTransform>());

            GameObject window = CreatePanel("Window", overlayPanel.transform, WindowColor);
            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.sizeDelta = new Vector2(1600f, 860f);
            windowRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup windowLayout = window.AddComponent<VerticalLayoutGroup>();
            windowLayout.padding = new RectOffset(28, 28, 24, 24);
            windowLayout.spacing = 18f;
            windowLayout.childAlignment = TextAnchor.UpperCenter;
            windowLayout.childControlWidth = true;
            windowLayout.childControlHeight = false;
            windowLayout.childForceExpandWidth = true;
            windowLayout.childForceExpandHeight = false;

            titleText = CreateText("TitleText", window.transform, 42f, FontStyles.Bold, TextAlignmentOptions.Center);
            subtitleText = CreateText("SubtitleText", window.transform, 24f, FontStyles.Normal, TextAlignmentOptions.Center);
            subtitleText.color = new Color(0.88f, 0.9f, 0.94f, 0.94f);

            GameObject contentRow = new("ContentRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            contentRow.transform.SetParent(window.transform, false);

            LayoutElement contentRowLayout = contentRow.GetComponent<LayoutElement>();
            contentRowLayout.preferredHeight = 620f;

            HorizontalLayoutGroup contentLayout = contentRow.GetComponent<HorizontalLayoutGroup>();
            contentLayout.spacing = 22f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = true;

            rewardPreviewRoot = CreatePreviewColumn(contentRow.transform, "RewardColumn", "New Reward Card", out _);
            candidateContentRoot = CreateCandidateColumn(contentRow.transform);
            selectedPreviewRoot = CreatePreviewColumn(contentRow.transform, "SelectedColumn", "Replacement Preview", out selectedHeaderText);

            GameObject buttonRow = new("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            buttonRow.transform.SetParent(window.transform, false);

            LayoutElement buttonLayout = buttonRow.GetComponent<LayoutElement>();
            buttonLayout.preferredHeight = 72f;

            HorizontalLayoutGroup buttonRowLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            buttonRowLayout.spacing = 16f;
            buttonRowLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonRowLayout.childControlWidth = false;
            buttonRowLayout.childControlHeight = false;
            buttonRowLayout.childForceExpandWidth = false;
            buttonRowLayout.childForceExpandHeight = false;

            confirmButton = CreateButton("ConfirmButton", buttonRow.transform, "Confirm Replace", HandleConfirmClicked);
            CreateButton("CancelButton", buttonRow.transform, "Cancel", HandleCancelClicked);

            overlayPanel.SetActive(false);
        }

        /// <summary>
        /// Creates a titled column used for the reward preview and selected-candidate preview.
        /// </summary>
        private RectTransform CreatePreviewColumn(Transform parent, string name, string header, out TMP_Text headerText)
        {
            GameObject column = CreatePanel(name, parent, SectionColor);
            column.AddComponent<LayoutElement>().preferredWidth = 340f;

            VerticalLayoutGroup columnLayout = column.AddComponent<VerticalLayoutGroup>();
            columnLayout.padding = new RectOffset(16, 16, 16, 16);
            columnLayout.spacing = 12f;
            columnLayout.childAlignment = TextAnchor.UpperCenter;
            columnLayout.childControlWidth = true;
            columnLayout.childControlHeight = false;
            columnLayout.childForceExpandWidth = true;
            columnLayout.childForceExpandHeight = false;

            headerText = CreateText($"{name}Header", column.transform, 28f, FontStyles.Bold, TextAlignmentOptions.Center);
            headerText.text = header;

            GameObject previewRoot = CreatePanel($"{name}PreviewRoot", column.transform, ContentColor);
            LayoutElement previewLayout = previewRoot.AddComponent<LayoutElement>();
            previewLayout.preferredHeight = 520f;

            RectTransform previewRect = previewRoot.GetComponent<RectTransform>();
            previewRect.sizeDelta = new Vector2(PreviewCardWidth + 40f, PreviewCardHeight + 100f);
            return previewRect;
        }

        /// <summary>
        /// Creates the horizontally scrollable candidate list column.
        /// </summary>
        private RectTransform CreateCandidateColumn(Transform parent)
        {
            GameObject column = CreatePanel("CandidateColumn", parent, SectionColor);
            column.AddComponent<LayoutElement>().flexibleWidth = 1f;

            VerticalLayoutGroup columnLayout = column.AddComponent<VerticalLayoutGroup>();
            columnLayout.padding = new RectOffset(16, 16, 16, 16);
            columnLayout.spacing = 12f;
            columnLayout.childAlignment = TextAnchor.UpperCenter;
            columnLayout.childControlWidth = true;
            columnLayout.childControlHeight = false;
            columnLayout.childForceExpandWidth = true;
            columnLayout.childForceExpandHeight = false;

            TMP_Text headerText = CreateText("CandidateHeader", column.transform, 28f, FontStyles.Bold, TextAlignmentOptions.Center);
            headerText.text = "Candidates";

            GameObject scrollRoot = CreatePanel("CandidateScrollRoot", column.transform, ContentColor);
            LayoutElement scrollLayout = scrollRoot.AddComponent<LayoutElement>();
            scrollLayout.preferredHeight = 520f;

            RectTransform scrollRect = scrollRoot.GetComponent<RectTransform>();
            scrollRect.sizeDelta = new Vector2(820f, 520f);

            ScrollRect scrollRectComponent = scrollRoot.AddComponent<ScrollRect>();
            scrollRectComponent.horizontal = true;
            scrollRectComponent.vertical = false;

            GameObject viewport = new("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollRoot.transform, false);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            StretchRect(viewport.GetComponent<RectTransform>());

            GameObject content = new("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0.5f);
            contentRect.anchorMax = new Vector2(0f, 0.5f);
            contentRect.pivot = new Vector2(0f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup contentLayout = content.GetComponent<HorizontalLayoutGroup>();
            contentLayout.spacing = CandidateSpacing;
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.childAlignment = TextAnchor.MiddleLeft;
            contentLayout.childControlWidth = false;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = content.GetComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRectComponent.viewport = viewport.GetComponent<RectTransform>();
            scrollRectComponent.content = contentRect;
            return contentRect;
        }

        /// <summary>
        /// Creates one clickable replacement candidate card.
        /// Candidate selection is UI-only here; actual replacement happens elsewhere after confirm.
        /// </summary>
        private void CreateCandidateView(BattleCardData candidate, int index)
        {
            GameObject root = CreatePanel($"CandidateButton_{index}", candidateContentRoot, CandidateDefaultColor);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(PreviewCardWidth + 28f, PreviewCardHeight + 82f);

            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = PreviewCardWidth + 28f;
            layout.preferredHeight = PreviewCardHeight + 82f;

            VerticalLayoutGroup rootLayout = root.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(12, 12, 12, 12);
            rootLayout.spacing = 8f;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = false;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            Outline outline = root.AddComponent<Outline>();
            outline.effectDistance = new Vector2(4f, 4f);
            outline.effectColor = new Color(0f, 0f, 0f, 0.25f);

            Button button = root.AddComponent<Button>();
            button.onClick.AddListener(() => HandleCandidateSelected(candidate));

            GameObject previewRoot = CreatePanel("PreviewRoot", root.transform, ContentColor);
            LayoutElement previewLayout = previewRoot.AddComponent<LayoutElement>();
            previewLayout.preferredWidth = PreviewCardWidth + 8f;
            previewLayout.preferredHeight = PreviewCardHeight + 20f;

            CreateCardPreview(previewRoot.transform, candidate);

            TMP_Text label = CreateText("CandidateLabel", root.transform, 20f, FontStyles.Normal, TextAlignmentOptions.Center);
            label.text = candidate.CardName;

            candidateViews.Add(new CandidateView(candidate, root.GetComponent<Image>(), button));
        }

        /// <summary>
        /// Updates local selection state and the right-hand preview.
        /// </summary>
        private void HandleCandidateSelected(BattleCardData candidate)
        {
            selectedCandidate = candidate;
            confirmButton.interactable = selectedCandidate != null;

            foreach (CandidateView candidateView in candidateViews)
            {
                candidateView.Background.color = candidateView.Data == candidate
                    ? CandidateSelectedColor
                    : CandidateDefaultColor;
            }

            selectedHeaderText.text = candidate != null
                ? $"Replacing: {candidate.CardName}"
                : "Replacement Preview";

            ClearPreview(selectedPreviewRoot);
            CreatePreviewContent(selectedPreviewRoot, candidate, "Selected Card");
        }

        /// <summary>
        /// Confirms the selected candidate and closes the UI without invoking cancel.
        /// </summary>
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

        /// <summary>
        /// Treats the close action as a cancel.
        /// </summary>
        private void HandleCancelClicked()
        {
            Dismiss(invokeCancel: true);
        }

        /// <summary>
        /// Clears the temporary UI state and optionally notifies the cancel callback.
        /// </summary>
        private void Dismiss(bool invokeCancel)
        {
            if (overlayPanel == null)
            {
                return;
            }

            overlayPanel.SetActive(false);
            ClearPreview(rewardPreviewRoot);
            ClearPreview(selectedPreviewRoot);
            ClearCandidates();
            selectedCandidate = null;
            confirmButton.interactable = false;

            Action cancelCallback = onCanceled;
            onConfirmed = null;
            onCanceled = null;

            CardViewHoverSystem.Instance?.Hide();

            if (invokeCancel)
            {
                cancelCallback?.Invoke();
            }
        }

        /// <summary>
        /// Adds a section title and card preview into a preview column.
        /// </summary>
        private void CreatePreviewContent(RectTransform root, BattleCardData cardData, string header)
        {
            TMP_Text headerText = CreateText($"{header}Header", root, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
            headerText.text = header;

            if (cardData == null)
            {
                CreatePreviewPlaceholder(root, "No card data is available.");
                return;
            }

            CreateCardPreview(root, cardData);
        }

        /// <summary>
        /// Creates a simple placeholder panel when there is no card preview to show.
        /// </summary>
        private void CreatePreviewPlaceholder(Transform parent, string message)
        {
            GameObject placeholder = CreatePanel("PlaceholderPreview", parent, CandidateDefaultColor);
            LayoutElement layout = placeholder.AddComponent<LayoutElement>();
            layout.preferredWidth = PreviewCardWidth;
            layout.preferredHeight = PreviewCardHeight;

            RectTransform rect = placeholder.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(PreviewCardWidth, PreviewCardHeight);

            TMP_Text messageText = CreateText("PlaceholderText", placeholder.transform, 24f, FontStyles.Normal, TextAlignmentOptions.Center);
            messageText.text = message;

            RectTransform messageRect = messageText.rectTransform;
            messageRect.anchorMin = Vector2.zero;
            messageRect.anchorMax = Vector2.one;
            messageRect.offsetMin = new Vector2(18f, 18f);
            messageRect.offsetMax = new Vector2(-18f, -18f);
        }

        /// <summary>
        /// Creates a non-interactive preview card for the supplied battle card data.
        /// Falls back to a simple text box if the card view system is unavailable.
        /// </summary>
        private void CreateCardPreview(Transform parent, BattleCardData cardData)
        {
            Card previewCard = BattleCardViewAdapter.CreatePreviewCard(cardData);
            if (previewCard == null)
            {
                CreatePreviewPlaceholder(parent, "Failed to build card preview.");
                return;
            }

            if (CardViewCreator.Instance == null)
            {
                CreateFallbackCardBox(parent, previewCard);
                return;
            }

            CardView cardView = CardViewCreator.Instance.CreateCardView(previewCard, Vector3.zero, Quaternion.identity);
            if (cardView == null)
            {
                CreateFallbackCardBox(parent, previewCard);
                return;
            }

            cardView.transform.SetParent(parent, false);
            cardView.IsHoverPreview = true;
            cardView.UseBuiltInInteractions = false;
            cardView.AllowHoverPreview = false;
            cardView.transform.localScale = Vector3.one;
            cardView.transform.localRotation = Quaternion.identity;

            LayoutElement layout = cardView.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = cardView.gameObject.AddComponent<LayoutElement>();
            }

            layout.preferredWidth = PreviewCardWidth;
            layout.preferredHeight = PreviewCardHeight;

            RectTransform cardRect = cardView.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.5f, 0f);
                cardRect.anchorMax = new Vector2(0.5f, 0f);
                cardRect.pivot = new Vector2(0.5f, 0f);
                cardRect.sizeDelta = new Vector2(PreviewCardWidth, PreviewCardHeight);
                cardRect.anchoredPosition = new Vector2(0f, 8f);
            }
        }

        /// <summary>
        /// Lightweight text fallback used when the standard card view cannot be created.
        /// </summary>
        private void CreateFallbackCardBox(Transform parent, Card previewCard)
        {
            GameObject root = CreatePanel("FallbackCardBox", parent, CandidateDefaultColor);
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = PreviewCardWidth;
            layout.preferredHeight = PreviewCardHeight;

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(PreviewCardWidth, PreviewCardHeight);

            VerticalLayoutGroup rootLayout = root.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(16, 16, 18, 18);
            rootLayout.spacing = 10f;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = false;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            TMP_Text title = CreateText("FallbackTitle", root.transform, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
            title.text = previewCard.Title;

            TMP_Text description = CreateText("FallbackDescription", root.transform, 18f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            description.text = previewCard.Description;
            description.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        }

        /// <summary>
        /// Destroys all child objects under a preview root.
        /// </summary>
        private void ClearPreview(Transform root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Transform child in root)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Removes candidate button listeners and destroys the candidate UI objects.
        /// </summary>
        private void ClearCandidates()
        {
            foreach (CandidateView candidateView in candidateViews)
            {
                if (candidateView.Button != null)
                {
                    candidateView.Button.onClick.RemoveAllListeners();
                }

                if (candidateView.Background != null)
                {
                    Destroy(candidateView.Background.gameObject);
                }
            }

            candidateViews.Clear();
        }

        /// <summary>
        /// Creates a TMP text object with the shared visual style used by this window.
        /// </summary>
        private static TMP_Text CreateText(
            string name,
            Transform parent,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = new Color(0.94f, 0.95f, 0.99f, 1f);
            return text;
        }

        /// <summary>
        /// Creates a simple button with the shared replacement-window styling.
        /// </summary>
        private static Button CreateButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = CreatePanel(name, parent, ButtonColor);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(220f, 58f);

            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 220f;
            layout.preferredHeight = 58f;

            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            TMP_Text labelText = CreateText("Label", buttonObject.transform, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
            labelText.text = label;

            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return button;
        }

        /// <summary>
        /// Creates a plain colored UI panel.
        /// </summary>
        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        /// <summary>
        /// Stretches a RectTransform to fill its parent.
        /// </summary>
        private static void StretchRect(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Ensures UI input exists before opening the runtime-generated replacement UI.
        /// </summary>
        private static void EnsureEventSystemExists()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystemObject);
        }

        /// <summary>
        /// Tracks the visuals associated with one selectable candidate card.
        /// </summary>
        private sealed class CandidateView
        {
            public CandidateView(BattleCardData data, Image background, Button button)
            {
                Data = data;
                Background = background;
                Button = button;
            }

            public BattleCardData Data { get; }
            public Image Background { get; }
            public Button Button { get; }
        }
    }
}
