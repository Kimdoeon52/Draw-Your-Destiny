namespace NYH.BattleCardSystem
{
    using TMPro;
    using NYH.CoreCardSystem;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 교체 UI 컨트롤러와 helper들이 공유해서 쓰는 참조 묶음입니다.
    ///
    /// 사용 방식:
    /// 1. BattleDeckReplacementUI가 자기 직렬화 필드를 이 컨텍스트로 복사합니다.
    /// 2. helper들이 이 컨텍스트를 받아 UI를 보정하거나 생성합니다.
    /// 3. 보정 결과를 다시 컨트롤러 필드로 반영합니다.
    ///
    /// 이렇게 하면 helper가 BattleDeckReplacementUI 내부 필드를 직접 만지지 않아도 되어
    /// 역할 경계가 더 분명해집니다.
    /// </summary>
    internal sealed class BattleDeckReplacementViewContext
    {
        public BattleDeckReplacementViewContext(Transform rootTransform, RectTransform rootRect)
        {
            RootTransform = rootTransform;
            RootRect = rootRect;
        }

        /// <summary>
        /// 교체 UI 컴포넌트가 붙어 있는 루트 Transform입니다.
        /// 런타임 fallback UI를 만들 때 부모 기준점으로 사용합니다.
        /// </summary>
        public Transform RootTransform { get; }

        /// <summary>
        /// 루트 RectTransform입니다.
        /// fallback 레이아웃 생성 시 stretch 기준으로 사용합니다.
        /// </summary>
        public RectTransform RootRect { get; }

        public GameObject OverlayPanel;
        public RectTransform RewardPreviewRoot;
        public RectTransform SelectedPreviewRoot;
        public RectTransform CandidateContentRoot;
        public TMP_Text TitleText;
        public TMP_Text SubtitleText;
        public TMP_Text SelectedHeaderText;
        public Text LegacyTitleText;
        public Text LegacySubtitleText;
        public Text LegacySelectedHeaderText;
        public Button ConfirmButton;
        public Button CancelButton;
        public CardView CandidateCardViewPrefab;
    }

    /// <summary>
    /// 교체 UI에서 공통으로 사용하는 숫자/색상 상수 모음입니다.
    /// UI 크기와 색을 한 곳에서 관리하려고 분리했습니다.
    /// </summary>
    internal static class BattleDeckReplacementUiMetrics
    {
        public const float CandidatePadding = 18f;
        public const float PreviewCardWidth = 244f;
        public const float PreviewCardHeight = 380f;
        public const float CandidateSpacing = 16f;
        public const float WindowWidth = 1600f;
        public const float WindowHeight = 940f;
        public const float ContentRowHeight = 560f;
        public const float FooterHeight = 112f;

        public static readonly Color OverlayColor = new(0f, 0f, 0f, 0.72f);
        public static readonly Color WindowColor = new(0.11f, 0.12f, 0.15f, 0.97f);
        public static readonly Color SectionColor = new(0.17f, 0.18f, 0.22f, 1f);
        public static readonly Color ContentColor = new(0.14f, 0.15f, 0.18f, 1f);
        public static readonly Color ButtonColor = new(0.25f, 0.28f, 0.34f, 1f);
        public static readonly Color CandidateDefaultColor = new(1f, 1f, 1f, 0.02f);
        public static readonly Color CandidateSelectedColor = new(0.96f, 0.82f, 0.29f, 0.14f);
        public static readonly Color CandidateOutlineColor = new(0.98f, 0.84f, 0.31f, 0.95f);
    }

    /// <summary>
    /// 교체 UI에서 반복적으로 쓰는 기본 UI 요소 생성기입니다.
    /// 런타임 fallback 레이아웃과 placeholder/fallback 카드 생성 시 공통으로 사용합니다.
    /// </summary>
    internal static class BattleDeckReplacementViewElements
    {
        /// <summary>
        /// 기본 스타일이 적용된 TMP 텍스트를 하나 만듭니다.
        /// </summary>
        public static TMP_Text CreateText(
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
        /// 기본 버튼 색과 라벨을 적용한 버튼을 만듭니다.
        /// fallback UI를 코드로 생성할 때만 사용합니다.
        /// </summary>
        public static Button CreateButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = CreatePanel(name, parent, BattleDeckReplacementUiMetrics.ButtonColor);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(240f, 64f);

            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 240f;
            layout.preferredHeight = 64f;

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
        /// 단색 Image 패널을 하나 만듭니다.
        /// </summary>
        public static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        /// <summary>
        /// RectTransform을 부모에 꽉 차게 늘립니다.
        /// </summary>
        public static void StretchRect(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
